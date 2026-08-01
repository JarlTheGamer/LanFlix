using Lanflix.Infrastructure.Adapters.LiveTV;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.LiveTV;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class LiveTvImportTests
{
    [Fact]
    public async Task M3u_and_xmltv_are_normalized_and_tuner_limit_is_enforced()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lanflix-tv-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        var database = Path.Combine(root, "tv.db"); var playlist = Path.Combine(root, "channels.m3u"); var guide = Path.Combine(root, "guide.xml");
        try
        {
            await File.WriteAllTextAsync(playlist, "#EXTM3U\n#EXTINF:-1 tvg-id=\"news.nl\" tvg-name=\"News NL\" tvg-chno=\"1\" tvg-logo=\"https://example.test/logo.png\" group-title=\"News\",News NL\nhttps://example.test/live/news.ts\n");
            await File.WriteAllTextAsync(guide, "<tv><channel id=\"news.nl\"><display-name>News NL</display-name></channel><programme id=\"bulletin-1\" channel=\"news.nl\" start=\"20260801200000 +0200\" stop=\"20260801203000 +0200\"><title>Evening news</title><desc>Headlines</desc><category>News</category></programme></tv>");
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={database}").Options;
            await using var db = new ApplicationDbContext(options); await db.Database.MigrateAsync();
            var source = LiveTvSource.Create("Test TV", LiveTvSourceKind.M3uXmlTv, playlist, guide, 1); db.LiveTvSources.Add(source); await db.SaveChangesAsync();
            var catalog = new LiveTvCatalog(db, new TestHttpClientFactory(), NullLogger<LiveTvCatalog>.Instance);

            var refresh = await catalog.RefreshAsync(source.Id, CancellationToken.None);
            Assert.Null(refresh.Error); Assert.Equal(1, refresh.ChannelsImported); Assert.Equal(1, refresh.ProgramsImported);
            var channel = await db.LiveTvChannels.SingleAsync(); Assert.Equal("news.nl", channel.ExternalId); Assert.Equal("News", channel.GroupName);
            var program = await db.LiveTvPrograms.SingleAsync(); Assert.Equal("Evening news", program.Title); Assert.Equal(new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc), program.StartsAtUtc);

            var account = Guid.NewGuid(); var first = await catalog.AcquireStreamAsync(channel.Id, account, CancellationToken.None); Assert.NotNull(first);
            Assert.Null(await catalog.AcquireStreamAsync(channel.Id, account, CancellationToken.None));
            await catalog.ReleaseStreamAsync(first!.LeaseId, CancellationToken.None);
            Assert.NotNull(await catalog.AcquireStreamAsync(channel.Id, account, CancellationToken.None));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(); }
}
