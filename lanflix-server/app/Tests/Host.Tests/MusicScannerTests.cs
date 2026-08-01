using Lanflix.Infrastructure.Adapters.Music;
using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class MusicScannerTests
{
    [Fact]
    public async Task Scanner_imports_updates_lyrics_and_removes_missing_audio()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lanflix-music-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var audio = Path.Combine(root, "song.wav");
        var database = Path.Combine(root, "music.db");
        try
        {
            WriteWave(audio);
            WriteTags(audio, "First title");
            await File.WriteAllTextAsync(Path.ChangeExtension(audio, ".lrc"), "[00:01.00]First line");
            await File.WriteAllBytesAsync(Path.Combine(root, "cover.jpg"), [0xFF, 0xD8, 0xFF, 0xD9]);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={database}").Options;
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Music:Folders:0"] = root }).Build();
            var catalog = new LocalMusicCatalog(db, configuration, NullLogger<LocalMusicCatalog>.Instance);

            var first = await catalog.ScanAsync(CancellationToken.None);
            Assert.Equal(1, first.Imported);
            var track = await db.MusicTracks.SingleAsync();
            Assert.Equal("First title", track.Title);
            Assert.Equal("Test Artist", (await db.MusicArtists.SingleAsync()).Name);
            Assert.Equal("Test Album", (await db.MusicAlbums.SingleAsync()).Title);
            Assert.True((await db.MusicLyrics.SingleAsync()).IsSynchronized);
            Assert.True(File.Exists((await db.MusicAlbums.SingleAsync()).ArtworkPath));

            WriteTags(audio, "Changed title");
            File.SetLastWriteTimeUtc(audio, DateTime.UtcNow.AddSeconds(2));
            var second = await catalog.ScanAsync(CancellationToken.None);
            Assert.Equal(1, second.Updated);
            db.ChangeTracker.Clear();
            Assert.Equal("Changed title", (await db.MusicTracks.SingleAsync()).Title);

            File.Delete(audio);
            var third = await catalog.ScanAsync(CancellationToken.None);
            Assert.Equal(1, third.Removed);
            Assert.Empty(await db.MusicTracks.ToListAsync());
            Assert.Empty(await db.MusicAlbums.ToListAsync());
            Assert.Empty(await db.MusicArtists.ToListAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void WriteTags(string path, string title)
    {
        using var media = TagLib.File.Create(path);
        media.Tag.Title = title;
        media.Tag.Performers = ["Test Artist"];
        media.Tag.AlbumArtists = ["Test Artist"];
        media.Tag.Album = "Test Album";
        media.Tag.Year = 2026;
        media.Tag.Track = 1;
        media.Tag.Genres = ["Soundtrack"];
        media.Save();
    }

    private static void WriteWave(string path)
    {
        const int sampleRate = 8000;
        const short channels = 1;
        const short bits = 16;
        var dataLength = sampleRate * channels * bits / 8;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8); writer.Write(36 + dataLength); writer.Write("WAVE"u8);
        writer.Write("fmt "u8); writer.Write(16); writer.Write((short)1); writer.Write(channels);
        writer.Write(sampleRate); writer.Write(sampleRate * channels * bits / 8); writer.Write((short)(channels * bits / 8)); writer.Write(bits);
        writer.Write("data"u8); writer.Write(dataLength); writer.Write(new byte[dataLength]);
    }
}
