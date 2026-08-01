using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.LiveTV;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Adapters.LiveTV;

internal sealed partial class LiveTvCatalog(ApplicationDbContext db, IHttpClientFactory clients, ILogger<LiveTvCatalog> logger) : ILiveTvCatalog
{
    private static readonly SemaphoreSlim LeaseLock = new(1, 1);
    public async Task<IReadOnlyList<LiveTvSourceDto>> GetSourcesAsync(CancellationToken ct) => await db.LiveTvSources.AsNoTracking().OrderBy(x => x.Name).Select(x => new LiveTvSourceDto(x.Id, x.Name, x.Kind.ToString(), x.MaxTuners, x.Enabled, x.LastRefreshedUtc, x.LastError)).ToArrayAsync(ct);

    public async Task<IReadOnlyList<LiveTvChannelDto>> GetChannelsAsync(Guid accountId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var enabledSources = await db.LiveTvSources.AsNoTracking().Where(x => x.Enabled).Select(x => x.Id).ToArrayAsync(ct);
        var channels = await db.LiveTvChannels.AsNoTracking().Where(x => x.Enabled && enabledSources.Contains(x.SourceId)).OrderBy(x => x.Number).ThenBy(x => x.Name).ToArrayAsync(ct);
        var ids = channels.Select(x => x.Id).ToArray();
        var favorites = (await db.LiveTvFavorites.AsNoTracking().Where(x => x.AccountId == accountId && ids.Contains(x.ChannelId)).Select(x => x.ChannelId).ToArrayAsync(ct)).ToHashSet();
        var programs = await db.LiveTvPrograms.AsNoTracking().Where(x => ids.Contains(x.ChannelId) && x.EndsAtUtc > now).OrderBy(x => x.StartsAtUtc).ToArrayAsync(ct);
        return channels.Select(channel =>
        {
            var upcoming = programs.Where(x => x.ChannelId == channel.Id).ToArray();
            return MapChannel(channel, favorites.Contains(channel.Id), upcoming.FirstOrDefault(x => x.StartsAtUtc <= now && x.EndsAtUtc > now), upcoming.FirstOrDefault(x => x.StartsAtUtc > now));
        }).OrderByDescending(x => x.Favorite).ThenBy(x => x.Number).ToArray();
    }

    public async Task<LiveTvGuideDto> GetGuideAsync(Guid accountId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var channels = await GetChannelsAsync(accountId, ct);
        var ids = channels.Select(x => x.Id).ToArray();
        var programs = await db.LiveTvPrograms.AsNoTracking().Where(x => ids.Contains(x.ChannelId) && x.EndsAtUtc > fromUtc && x.StartsAtUtc < toUtc).OrderBy(x => x.StartsAtUtc).ToArrayAsync(ct);
        return new(fromUtc, toUtc, channels, programs.GroupBy(x => x.ChannelId).ToDictionary(x => x.Key, x => (IReadOnlyList<LiveTvProgramDto>)x.Select(MapProgram).ToArray()));
    }

    public async Task<LiveTvRefreshResult> RefreshAsync(long sourceId, CancellationToken ct)
    {
        var source = await db.LiveTvSources.SingleOrDefaultAsync(x => x.Id == sourceId, ct);
        if (source is null) return new(0, 0, 0, 0, "Source not found");
        try
        {
            var importedChannels = source.Kind == LiveTvSourceKind.HdHomeRun ? await ReadHdHomeRunAsync(source.SourceUri, ct) : ParseM3u(await ReadTextAsync(source.SourceUri, 50 * 1024 * 1024, ct));
            if (importedChannels.Count == 0) throw new InvalidDataException("The source contained no playable channels.");
            var existing = await db.LiveTvChannels.Where(x => x.SourceId == source.Id).ToDictionaryAsync(x => x.ExternalId, StringComparer.OrdinalIgnoreCase, ct);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var added = 0; var updated = 0;
            foreach (var value in importedChannels)
            {
                if (!IsHttp(value.StreamUri) || !seen.Add(value.ExternalId)) continue;
                if (existing.TryGetValue(value.ExternalId, out var channel)) { channel.Update(value); updated++; }
                else { db.LiveTvChannels.Add(LiveTvChannel.Create(source.Id, value)); added++; }
            }
            var removed = existing.Values.Where(x => !seen.Contains(x.ExternalId)).ToArray();
            if (removed.Length > 0) db.LiveTvChannels.RemoveRange(removed);
            await db.SaveChangesAsync(ct);

            var programCount = 0;
            if (!string.IsNullOrWhiteSpace(source.GuideUri)) programCount = await ImportXmlTvAsync(source, await ReadTextAsync(source.GuideUri, 100 * 1024 * 1024, ct), ct);
            source.RefreshSucceeded(); await db.SaveChangesAsync(ct);
            return new(added, updated, removed.Length, programCount, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Live TV source {SourceId} refresh failed", sourceId);
            source.RefreshFailed(exception.Message); await db.SaveChangesAsync(ct);
            return new(0, 0, 0, 0, exception.Message);
        }
    }

    public async Task<LiveTvStream?> AcquireStreamAsync(long channelId, Guid accountId, CancellationToken ct)
    {
        await LeaseLock.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            db.LiveTvTunerLeases.RemoveRange(db.LiveTvTunerLeases.Where(x => x.ExpiresAtUtc <= now));
            var channel = await db.LiveTvChannels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == channelId && x.Enabled, ct);
            if (channel is null) { await db.SaveChangesAsync(ct); return null; }
            var source = await db.LiveTvSources.AsNoTracking().SingleOrDefaultAsync(x => x.Id == channel.SourceId && x.Enabled, ct);
            if (source is null || await db.LiveTvTunerLeases.CountAsync(x => x.SourceId == source.Id && x.ExpiresAtUtc > now, ct) >= source.MaxTuners) { await db.SaveChangesAsync(ct); return null; }
            var lease = LiveTvTunerLease.Create(source.Id, channel.Id, accountId, TimeSpan.FromHours(4)); db.LiveTvTunerLeases.Add(lease); await db.SaveChangesAsync(ct);
            return new(channel.StreamUri, "video/mp2t", lease.Id);
        }
        finally { LeaseLock.Release(); }
    }

    public async Task ReleaseStreamAsync(Guid leaseId, CancellationToken ct)
    { var lease = await db.LiveTvTunerLeases.SingleOrDefaultAsync(x => x.Id == leaseId, ct); if (lease is not null) { db.LiveTvTunerLeases.Remove(lease); await db.SaveChangesAsync(ct); } }

    private async Task<int> ImportXmlTvAsync(LiveTvSource source, string xml, CancellationToken ct)
    {
        var document = XDocument.Parse(xml, LoadOptions.None);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var channels = await db.LiveTvChannels.Where(x => x.SourceId == source.Id).ToArrayAsync(ct);
        var byExternal = channels.ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);
        var aliases = document.Root?.Elements("channel").Select(x => new { Id = (string?)x.Attribute("id"), Name = (string?)x.Element("display-name") }).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToArray() ?? [];
        foreach (var alias in aliases) if (!byExternal.ContainsKey(alias.Id!) && !string.IsNullOrWhiteSpace(alias.Name)) { var match = channels.FirstOrDefault(x => string.Equals(x.Name, alias.Name, StringComparison.OrdinalIgnoreCase)); if (match is not null) byExternal[alias.Id!] = match; }
        var channelIds = channels.Select(x => x.Id).ToArray();
        db.LiveTvPrograms.RemoveRange(db.LiveTvPrograms.Where(x => channelIds.Contains(x.ChannelId)));
        await db.SaveChangesAsync(ct);
        var existingKeys = new HashSet<string>();
        var count = 0;
        foreach (var element in document.Root?.Elements("programme") ?? [])
        {
            ct.ThrowIfCancellationRequested();
            var externalChannel = (string?)element.Attribute("channel"); if (externalChannel is null || !byExternal.TryGetValue(externalChannel, out var channel)) continue;
            if (!TryXmlTvDate((string?)element.Attribute("start"), out var start) || !TryXmlTvDate((string?)element.Attribute("stop"), out var end) || end <= start) continue;
            var title = element.Element("title")?.Value?.Trim(); if (string.IsNullOrWhiteSpace(title)) continue;
            var externalId = (string?)element.Attribute("id") ?? Hash($"{externalChannel}|{start.Ticks}|{title}");
            if (!existingKeys.Add($"{channel.Id}|{externalId}|{start.Ticks}")) continue;
            db.LiveTvPrograms.Add(LiveTvProgram.Create(channel.Id, new(externalId, title, element.Element("desc")?.Value?.Trim(), element.Element("category")?.Value?.Trim(), element.Element("sub-title")?.Value?.Trim(), (string?)element.Element("icon")?.Attribute("src"), start, end))); count++;
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return count;
    }

    private async Task<IReadOnlyList<ChannelImport>> ReadHdHomeRunAsync(string baseUri, CancellationToken ct)
    {
        var uri = new Uri(new Uri(baseUri.TrimEnd('/') + "/"), "lineup.json");
        using var document = JsonDocument.Parse(await ReadTextAsync(uri.ToString(), 10 * 1024 * 1024, ct));
        return document.RootElement.EnumerateArray().Select((x, index) =>
        {
            var number = Property(x, "GuideNumber") ?? (index + 1).ToString(CultureInfo.InvariantCulture); var name = Property(x, "GuideName") ?? $"Channel {number}"; var stream = Property(x, "URL") ?? string.Empty;
            return new ChannelImport(Property(x, "GuideID") ?? number, number, name, Property(x, "ImageURL"), stream, "HDHomeRun");
        }).Where(x => IsHttp(x.StreamUri)).ToArray();
    }

    internal static IReadOnlyList<ChannelImport> ParseM3u(string text)
    {
        var result = new List<ChannelImport>(); string? info = null; var order = 0;
        foreach (var raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim(); if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase)) { info = line; continue; }
            if (info is null || line.StartsWith('#') || !IsHttp(line)) continue;
            order++; var attributes = Attributes().Matches(info).ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value, StringComparer.OrdinalIgnoreCase);
            var comma = info.LastIndexOf(','); var display = comma >= 0 ? info[(comma + 1)..].Trim() : $"Channel {order}";
            var name = attributes.GetValueOrDefault("tvg-name") ?? display; var number = attributes.GetValueOrDefault("tvg-chno") ?? order.ToString(CultureInfo.InvariantCulture);
            var id = attributes.GetValueOrDefault("tvg-id") ?? Hash($"{name}|{line}");
            result.Add(new(id, number, name, attributes.GetValueOrDefault("tvg-logo"), line, attributes.GetValueOrDefault("group-title"))); info = null;
        }
        return result;
    }

    private async Task<string> ReadTextAsync(string value, int maxBytes, CancellationToken ct)
    {
        if (File.Exists(value)) { var file = new FileInfo(value); if (file.Length > maxBytes) throw new InvalidDataException("Source file is too large."); return await File.ReadAllTextAsync(value, ct); }
        if (!IsHttp(value)) throw new InvalidDataException("Source URI must be HTTP(S) or an existing local file.");
        using var response = await clients.CreateClient("LiveTvMetadata").GetAsync(value, HttpCompletionOption.ResponseHeadersRead, ct); response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maxBytes) throw new InvalidDataException("Source response is too large.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct); using var memory = new MemoryStream();
        var buffer = new byte[81920]; int read; while ((read = await stream.ReadAsync(buffer, ct)) > 0) { if (memory.Length + read > maxBytes) throw new InvalidDataException("Source response is too large."); await memory.WriteAsync(buffer.AsMemory(0, read), ct); }
        return Encoding.UTF8.GetString(memory.ToArray());
    }

    private static LiveTvChannelDto MapChannel(LiveTvChannel c, bool favorite, LiveTvProgram? now, LiveTvProgram? next) => new(c.Id, c.Number, c.Name, c.LogoUrl, c.GroupName, favorite, now is null ? null : MapProgram(now), next is null ? null : MapProgram(next));
    private static LiveTvProgramDto MapProgram(LiveTvProgram x) => new(x.Id, x.Title, x.Description, x.Category, x.EpisodeTitle, x.ArtworkUrl, x.StartsAtUtc, x.EndsAtUtc);
    private static string? Property(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static bool TryXmlTvDate(string? value, out DateTime utc)
    {
        utc = default; if (string.IsNullOrWhiteSpace(value) || value.Length < 14) return false;
        var timestamp = value[..14]; var offset = value.Length >= 20 ? value.Substring(15, 5) : "+0000";
        if (!DateTime.TryParseExact(timestamp, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) return false;
        if (!TimeSpan.TryParseExact(offset, "hhmm", CultureInfo.InvariantCulture, out var span)) { var sign = offset.StartsWith('-') ? -1 : 1; if (!int.TryParse(offset.TrimStart('+', '-'), out var numeric)) return false; span = TimeSpan.FromMinutes(sign * ((numeric / 100) * 60 + numeric % 100)); }
        utc = new DateTimeOffset(local, span).UtcDateTime; return true;
    }
    private static bool IsHttp(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24].ToLowerInvariant();
    [GeneratedRegex("([A-Za-z0-9_-]+)=\"([^\"]*)\"")]
    private static partial Regex Attributes();
}
