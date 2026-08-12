using System.Collections.Concurrent;

namespace Lanflix.Infrastructure.Services.Playback.Planning;

internal enum MatroskaSeekIndexStatus
{
    Compatible,
    MissingDirectCueReference,
    Unknown
}

/// <summary>
/// Checks the small amount of Matroska metadata Media3 needs in order to
/// expose a seekable timeline. Media3 expects the first SeekHead to reference
/// Cues directly; a chained SeekHead at the end of the file is valid Matroska
/// but is treated as unseekable by its extractor.
/// </summary>
internal sealed class MatroskaSeekIndexInspector
{
    private const ulong EbmlId = 0x1A45DFA3;
    private const ulong SegmentId = 0x18538067;
    private const ulong SeekHeadId = 0x114D9B74;
    private const ulong SeekId = 0x4DBB;
    private const ulong SeekTargetId = 0x53AB;
    private const ulong CuesId = 0x1C53BB6B;
    private const ulong ClusterId = 0x1F43B675;
    private const long MaximumHeaderScanBytes = 4 * 1024 * 1024;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public MatroskaSeekIndexStatus Inspect(string filePath)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists) return MatroskaSeekIndexStatus.Unknown;

        if (_cache.TryGetValue(file.FullName, out var cached) &&
            cached.LastWriteUtc == file.LastWriteTimeUtc && cached.Length == file.Length)
            return cached.Status;

        MatroskaSeekIndexStatus status;
        try
        {
            using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
                64 * 1024, FileOptions.RandomAccess);
            status = Inspect(stream);
        }
        catch (IOException)
        {
            status = MatroskaSeekIndexStatus.Unknown;
        }
        catch (UnauthorizedAccessException)
        {
            status = MatroskaSeekIndexStatus.Unknown;
        }
        catch (InvalidDataException)
        {
            status = MatroskaSeekIndexStatus.Unknown;
        }

        _cache[file.FullName] = new CacheEntry(file.LastWriteTimeUtc, file.Length, status);
        return status;
    }

    internal static MatroskaSeekIndexStatus Inspect(Stream stream)
    {
        if (!stream.CanRead || !stream.CanSeek) return MatroskaSeekIndexStatus.Unknown;
        stream.Position = 0;

        if (!TryReadElementHeader(stream, out var ebml, out var ebmlSize) || ebml != EbmlId || ebmlSize < 0 ||
            !TrySkip(stream, ebmlSize))
            return MatroskaSeekIndexStatus.Unknown;

        if (!TryReadElementHeader(stream, out var segment, out var segmentSize) || segment != SegmentId)
            return MatroskaSeekIndexStatus.Unknown;

        var segmentDataStart = stream.Position;
        var segmentEnd = segmentSize < 0 ? stream.Length : Math.Min(stream.Length, segmentDataStart + segmentSize);
        var scanEnd = Math.Min(segmentEnd, segmentDataStart + MaximumHeaderScanBytes);

        while (stream.Position < scanEnd)
        {
            if (!TryReadElementHeader(stream, out var id, out var size) || size < 0)
                return MatroskaSeekIndexStatus.Unknown;

            if (id == SeekHeadId)
                return SeekHeadReferencesCues(stream, size)
                    ? MatroskaSeekIndexStatus.Compatible
                    : MatroskaSeekIndexStatus.MissingDirectCueReference;

            // Once media clusters begin there cannot be an initial SeekHead
            // that Media3 can use without scanning the file.
            if (id == ClusterId)
                return MatroskaSeekIndexStatus.MissingDirectCueReference;

            if (!TrySkip(stream, size)) return MatroskaSeekIndexStatus.Unknown;
        }

        return MatroskaSeekIndexStatus.Unknown;
    }

    private static bool SeekHeadReferencesCues(Stream stream, long size)
    {
        var end = CheckedEnd(stream, size);
        if (end is null) return false;

        while (stream.Position < end.Value)
        {
            if (!TryReadElementHeader(stream, out var id, out var childSize) || childSize < 0)
                return false;

            if (id == SeekId && SeekEntryTargetsCues(stream, childSize)) return true;
            if (id != SeekId && !TrySkip(stream, childSize)) return false;
        }

        return false;
    }

    private static bool SeekEntryTargetsCues(Stream stream, long size)
    {
        var end = CheckedEnd(stream, size);
        if (end is null) return false;

        while (stream.Position < end.Value)
        {
            if (!TryReadElementHeader(stream, out var id, out var childSize) || childSize < 0)
                return false;

            if (id == SeekTargetId)
            {
                if (childSize is <= 0 or > 8 || stream.Position + childSize > stream.Length) return false;
                ulong target = 0;
                for (var index = 0; index < childSize; index++)
                {
                    var value = stream.ReadByte();
                    if (value < 0) return false;
                    target = (target << 8) | (byte)value;
                }
                if (target == CuesId) return true;
            }
            else if (!TrySkip(stream, childSize))
            {
                return false;
            }
        }

        return false;
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

    private sealed record CacheEntry(DateTime LastWriteUtc, long Length, MatroskaSeekIndexStatus Status);
}
