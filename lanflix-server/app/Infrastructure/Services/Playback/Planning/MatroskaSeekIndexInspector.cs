using System.Collections.Concurrent;

namespace Lanflix.Infrastructure.Services.Playback.Planning;

internal enum MatroskaSeekIndexStatus
{
    Compatible,
    MissingDirectCueReference,
    Unknown
}

internal sealed record MatroskaBytePatch(long Offset, byte[] Replacement);

internal sealed record MatroskaSeekIndexPatch(IReadOnlyList<MatroskaBytePatch> Bytes);

/// <summary>
/// Inspects the small amount of Matroska metadata Media3 uses to expose a
/// seekable timeline. For valid files with a chained SeekHead, it also creates
/// a same-length virtual header patch that points the initial SeekHead directly
/// at Cues. The source file is never changed or copied.
/// </summary>
internal sealed class MatroskaSeekIndexInspector
{
    private const ulong EbmlId = 0x1A45DFA3;
    private const ulong SegmentId = 0x18538067;
    private const ulong SeekHeadId = 0x114D9B74;
    private const ulong SeekId = 0x4DBB;
    private const ulong SeekTargetId = 0x53AB;
    private const ulong SeekPositionId = 0x53AC;
    private const ulong CuesId = 0x1C53BB6B;
    private const ulong ClusterId = 0x1F43B675;
    private const long MaximumHeaderScanBytes = 4 * 1024 * 1024;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public MatroskaSeekIndexStatus Inspect(string filePath) => Analyze(filePath).Status;

    public MatroskaSeekIndexPatch? GetVirtualPatch(string filePath) => Analyze(filePath).Patch;

    private Inspection Analyze(string filePath)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists) return new Inspection(MatroskaSeekIndexStatus.Unknown, null);

        if (_cache.TryGetValue(file.FullName, out var cached) &&
            cached.LastWriteUtc == file.LastWriteTimeUtc && cached.Length == file.Length)
            return cached.Inspection;

        Inspection inspection;
        try
        {
            using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
                64 * 1024, FileOptions.RandomAccess);
            inspection = Analyze(stream);
        }
        catch (IOException)
        {
            inspection = new Inspection(MatroskaSeekIndexStatus.Unknown, null);
        }
        catch (UnauthorizedAccessException)
        {
            inspection = new Inspection(MatroskaSeekIndexStatus.Unknown, null);
        }
        catch (InvalidDataException)
        {
            inspection = new Inspection(MatroskaSeekIndexStatus.Unknown, null);
        }

        _cache[file.FullName] = new CacheEntry(file.LastWriteTimeUtc, file.Length, inspection);
        return inspection;
    }

    internal static MatroskaSeekIndexStatus Inspect(Stream stream) => Analyze(stream).Status;

    internal static MatroskaSeekIndexPatch? GetVirtualPatch(Stream stream) => Analyze(stream).Patch;

    private static Inspection Analyze(Stream stream)
    {
        if (!stream.CanRead || !stream.CanSeek)
            return new Inspection(MatroskaSeekIndexStatus.Unknown, null);
        stream.Position = 0;

        if (!TryReadElementHeader(stream, out var ebml, out var ebmlSize) || ebml != EbmlId || ebmlSize < 0 ||
            !TrySkip(stream, ebmlSize))
            return new Inspection(MatroskaSeekIndexStatus.Unknown, null);

        if (!TryReadElementHeader(stream, out var segment, out var segmentSize) || segment != SegmentId)
            return new Inspection(MatroskaSeekIndexStatus.Unknown, null);

        var segmentDataStart = stream.Position;
        var segmentEnd = segmentSize < 0 ? stream.Length : Math.Min(stream.Length, segmentDataStart + segmentSize);
        var scanEnd = Math.Min(segmentEnd, segmentDataStart + MaximumHeaderScanBytes);

        while (stream.Position < scanEnd)
        {
            if (!TryReadElementHeader(stream, out var id, out var size) || size < 0)
                return new Inspection(MatroskaSeekIndexStatus.Unknown, null);

            if (id == SeekHeadId)
                return AnalyzeInitialSeekHead(stream, size, segmentDataStart);

            if (id == ClusterId)
                return new Inspection(MatroskaSeekIndexStatus.MissingDirectCueReference, null);

            if (!TrySkip(stream, size))
                return new Inspection(MatroskaSeekIndexStatus.Unknown, null);
        }

        return new Inspection(MatroskaSeekIndexStatus.Unknown, null);
    }

    private static Inspection AnalyzeInitialSeekHead(Stream stream, long size, long segmentDataStart)
    {
        var entries = ReadSeekHead(stream, size);
        if (entries is null)
            return new Inspection(MatroskaSeekIndexStatus.Unknown, null);
        if (entries.Any(entry => entry.Target == CuesId))
            return new Inspection(MatroskaSeekIndexStatus.Compatible, null);

        var chained = entries.FirstOrDefault(entry => entry.Target == SeekHeadId && entry.Position is not null);
        if (chained is null)
            return new Inspection(MatroskaSeekIndexStatus.MissingDirectCueReference, null);

        if (chained.Position!.Value > (ulong)(stream.Length - segmentDataStart))
            return new Inspection(MatroskaSeekIndexStatus.MissingDirectCueReference, null);
        stream.Position = segmentDataStart + (long)chained.Position.Value;
        if (!TryReadElementHeader(stream, out var nestedId, out var nestedSize) ||
            nestedId != SeekHeadId || nestedSize < 0)
            return new Inspection(MatroskaSeekIndexStatus.MissingDirectCueReference, null);

        var nestedEntries = ReadSeekHead(stream, nestedSize);
        var cues = nestedEntries?.FirstOrDefault(entry => entry.Target == CuesId && entry.Position is not null);
        if (cues is null || chained.TargetDataOffset is null || chained.PositionDataOffset is null ||
            !TryEncodeUnsigned(CuesId, chained.TargetDataLength, out var targetBytes) ||
            !TryEncodeUnsigned(cues.Position!.Value, chained.PositionDataLength, out var positionBytes))
            return new Inspection(MatroskaSeekIndexStatus.MissingDirectCueReference, null);

        var patch = new MatroskaSeekIndexPatch([
            new MatroskaBytePatch(chained.TargetDataOffset.Value, targetBytes),
            new MatroskaBytePatch(chained.PositionDataOffset.Value, positionBytes)
        ]);
        return new Inspection(MatroskaSeekIndexStatus.MissingDirectCueReference, patch);
    }

    private static List<SeekEntry>? ReadSeekHead(Stream stream, long size)
    {
        var end = CheckedEnd(stream, size);
        if (end is null) return null;
        var entries = new List<SeekEntry>();

        while (stream.Position < end.Value)
        {
            if (!TryReadElementHeader(stream, out var id, out var childSize) || childSize < 0)
                return null;
            if (id == SeekId)
            {
                var entry = ReadSeekEntry(stream, childSize);
                if (entry is null) return null;
                entries.Add(entry);
            }
            else if (!TrySkip(stream, childSize))
            {
                return null;
            }
        }
        return stream.Position == end.Value ? entries : null;
    }

    private static SeekEntry? ReadSeekEntry(Stream stream, long size)
    {
        var end = CheckedEnd(stream, size);
        if (end is null) return null;
        ulong? target = null;
        ulong? position = null;
        long? targetOffset = null;
        long? positionOffset = null;
        var targetLength = 0;
        var positionLength = 0;

        while (stream.Position < end.Value)
        {
            if (!TryReadElementHeader(stream, out var id, out var childSize) || childSize is < 0 or > 8)
                return null;
            var dataOffset = stream.Position;
            if (id == SeekTargetId)
            {
                if (!TryReadUnsigned(stream, childSize, out var value)) return null;
                target = value;
                targetOffset = dataOffset;
                targetLength = (int)childSize;
            }
            else if (id == SeekPositionId)
            {
                if (!TryReadUnsigned(stream, childSize, out var value)) return null;
                position = value;
                positionOffset = dataOffset;
                positionLength = (int)childSize;
            }
            else if (!TrySkip(stream, childSize))
            {
                return null;
            }
        }

        return stream.Position == end.Value
            ? new SeekEntry(target, position, targetOffset, targetLength, positionOffset, positionLength)
            : null;
    }

    private static bool TryReadUnsigned(Stream stream, long length, out ulong value)
    {
        value = 0;
        if (length is <= 0 or > 8 || length > stream.Length - stream.Position) return false;
        for (var index = 0; index < length; index++)
        {
            var next = stream.ReadByte();
            if (next < 0) return false;
            value = (value << 8) | (byte)next;
        }
        return true;
    }

    private static bool TryEncodeUnsigned(ulong value, int length, out byte[] bytes)
    {
        bytes = [];
        if (length is <= 0 or > 8 || (length < 8 && value >= 1UL << (length * 8))) return false;
        bytes = new byte[length];
        for (var index = length - 1; index >= 0; index--)
        {
            bytes[index] = (byte)value;
            value >>= 8;
        }
        return true;
    }

    private static bool TryReadElementHeader(Stream stream, out ulong id, out long size)
    {
        id = 0;
        size = 0;
        return TryReadVint(stream, true, out id, out _) &&
            TryReadVint(stream, false, out var rawSize, out var sizeLength) &&
            TryConvertSize(rawSize, sizeLength, out size);
    }

    private static bool TryReadVint(Stream stream, bool preserveMarker, out ulong value, out int length)
    {
        value = 0;
        length = 0;
        var first = stream.ReadByte();
        if (first < 0) return false;
        var marker = 0x80;
        length = 1;
        while (length <= 8 && (first & marker) == 0)
        {
            marker >>= 1;
            length++;
        }
        if (length > 8 || (preserveMarker && length > 4)) return false;
        value = preserveMarker ? (byte)first : (ulong)(first & (marker - 1));
        for (var index = 1; index < length; index++)
        {
            var next = stream.ReadByte();
            if (next < 0) return false;
            value = (value << 8) | (byte)next;
        }
        return true;
    }

    private static bool TryConvertSize(ulong rawSize, int length, out long size)
    {
        var unknown = (1UL << (7 * length)) - 1;
        if (rawSize == unknown)
        {
            size = -1;
            return true;
        }
        if (rawSize > long.MaxValue)
        {
            size = 0;
            return false;
        }
        size = (long)rawSize;
        return true;
    }

    private static long? CheckedEnd(Stream stream, long size)
    {
        if (size < 0 || size > stream.Length - stream.Position) return null;
        return stream.Position + size;
    }

    private static bool TrySkip(Stream stream, long size)
    {
        var end = CheckedEnd(stream, size);
        if (end is null) return false;
        stream.Position = end.Value;
        return true;
    }

    private sealed record SeekEntry(
        ulong? Target, ulong? Position, long? TargetDataOffset, int TargetDataLength,
        long? PositionDataOffset, int PositionDataLength);
    private sealed record Inspection(MatroskaSeekIndexStatus Status, MatroskaSeekIndexPatch? Patch);
    private sealed record CacheEntry(DateTime LastWriteUtc, long Length, Inspection Inspection);
}
