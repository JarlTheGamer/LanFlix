namespace Lanflix.Infrastructure.Services.Playback.Planning;

/// <summary>A seekable read-only view that overlays a few Matroska header bytes.</summary>
internal sealed class VirtualMatroskaSeekStream(Stream source, MatroskaSeekIndexPatch patch) : Stream
{
    public override bool CanRead => source.CanRead;
    public override bool CanSeek => source.CanSeek;
    public override bool CanWrite => false;
    public override long Length => source.Length;
    public override long Position { get => source.Position; set => source.Position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var start = source.Position;
        var read = source.Read(buffer, offset, count);
        Apply(buffer.AsSpan(offset, read), start);
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var start = source.Position;
        var read = source.Read(buffer);
        Apply(buffer[..read], start);
        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var start = source.Position;
        var read = await source.ReadAsync(buffer, cancellationToken);
        Apply(buffer.Span[..read], start);
        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadArrayAsync(buffer, offset, count, cancellationToken);
    }

    private async Task<int> ReadArrayAsync(
        byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var start = source.Position;
        var read = await source.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        Apply(buffer.AsSpan(offset, read), start);
        return read;
    }

    private void Apply(Span<byte> destination, long sourceOffset)
    {
        var sourceEnd = sourceOffset + destination.Length;
        foreach (var item in patch.Bytes)
        {
            var patchEnd = item.Offset + item.Replacement.Length;
            var overlapStart = Math.Max(sourceOffset, item.Offset);
            var overlapEnd = Math.Min(sourceEnd, patchEnd);
            if (overlapStart >= overlapEnd) continue;

            item.Replacement.AsSpan(
                    (int)(overlapStart - item.Offset), (int)(overlapEnd - overlapStart))
                .CopyTo(destination[(int)(overlapStart - sourceOffset)..]);
        }
    }

    public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);
    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) source.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await source.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
