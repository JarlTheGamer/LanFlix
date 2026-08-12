using Lanflix.Infrastructure.Services.Playback.Planning;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class MatroskaSeekIndexInspectorTests
{
    [Fact]
    public void Direct_cues_reference_is_compatible()
    {
        using var file = MatroskaWithInitialSeekTarget(0x1C53BB6B);

        Assert.Equal(MatroskaSeekIndexStatus.Compatible, MatroskaSeekIndexInspector.Inspect(file));
    }

    [Fact]
    public void Chained_seek_head_requires_remux()
    {
        using var file = MatroskaWithInitialSeekTarget(0x114D9B74);

        Assert.Equal(MatroskaSeekIndexStatus.MissingDirectCueReference,
            MatroskaSeekIndexInspector.Inspect(file));
    }

    [Fact]
    public void Chained_seek_head_can_be_exposed_as_direct_cues_without_changing_length()
    {
        using var file = MatroskaWithChainedSeekHead();
        var original = file.ToArray();

        var patch = MatroskaSeekIndexInspector.GetVirtualPatch(file);

        Assert.NotNull(patch);
        using var view = new VirtualMatroskaSeekStream(
            new MemoryStream(original, writable: false), patch!);
        Assert.Equal(original.Length, view.Length);
        Assert.Equal(MatroskaSeekIndexStatus.Compatible, MatroskaSeekIndexInspector.Inspect(view));
        Assert.Equal(0x11, original[21]); // The stored file still targets SeekHead.
    }

    [Fact]
    public void Invalid_input_does_not_force_remux()
    {
        using var file = new MemoryStream([0x00, 0x01, 0x02]);

        Assert.Equal(MatroskaSeekIndexStatus.Unknown, MatroskaSeekIndexInspector.Inspect(file));
    }

    private static MemoryStream MatroskaWithInitialSeekTarget(uint target)
    {
        // Minimal EBML header followed by an unknown-length Segment and one
        // SeekHead containing SeekID + SeekPosition.
        var bytes = new List<byte>();
        bytes.AddRange([0x1A, 0x45, 0xDF, 0xA3, 0x80]);
        bytes.AddRange([0x18, 0x53, 0x80, 0x67, 0xFF]);

        var seekEntry = new List<byte>();
        seekEntry.AddRange([0x53, 0xAB, 0x84]);
        seekEntry.AddRange([
            (byte)(target >> 24), (byte)(target >> 16),
            (byte)(target >> 8), (byte)target]);
        seekEntry.AddRange([0x53, 0xAC, 0x81, 0x00]);

        bytes.AddRange([0x11, 0x4D, 0x9B, 0x74, (byte)(0x80 | seekEntry.Count + 3)]);
        bytes.AddRange([0x4D, 0xBB, (byte)(0x80 | seekEntry.Count)]);
        bytes.AddRange(seekEntry);
        return new MemoryStream(bytes.ToArray(), writable: false);
    }

    private static MemoryStream MatroskaWithChainedSeekHead()
    {
        var bytes = new List<byte>();
        bytes.AddRange([0x1A, 0x45, 0xDF, 0xA3, 0x80]);
        bytes.AddRange([0x18, 0x53, 0x80, 0x67, 0xFF]);

        // This first SeekHead occupies 19 bytes and points to the second one.
        bytes.AddRange([
            0x11, 0x4D, 0x9B, 0x74, 0x8E,
            0x4D, 0xBB, 0x8B,
            0x53, 0xAB, 0x84, 0x11, 0x4D, 0x9B, 0x74,
            0x53, 0xAC, 0x81, 0x13
        ]);

        // The second SeekHead points to Cues at a representable position.
        bytes.AddRange([
            0x11, 0x4D, 0x9B, 0x74, 0x8E,
            0x4D, 0xBB, 0x8B,
            0x53, 0xAB, 0x84, 0x1C, 0x53, 0xBB, 0x6B,
            0x53, 0xAC, 0x81, 0x26
        ]);
        bytes.AddRange([0x1C, 0x53, 0xBB, 0x6B, 0x80]);
        return new MemoryStream(bytes.ToArray(), writable: false);
    }
}
