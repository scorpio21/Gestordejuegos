using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using GestorVimm.Models;

namespace GestorVimm.Services;

public sealed partial class VimmVaultService : IDisposable
{
    private readonly HttpClient _http;

    private const string VimmBase = "https://vimm.net";

    public VimmVaultService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) GestorVimm/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,*/*");
        _http.DefaultRequestHeaders.Referrer = new Uri(VimmBase + "/");
    }

    public async Task<int?> FindGameIdAsync(string systemCode, RomEntry entry, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(entry.Title);
        var url =
            $"https://vimm.net/vault/?p=list&system={Uri.EscapeDataString(systemCode)}&q={Uri.EscapeDataString(query)}";

        var html = await _http.GetStringAsync(url, ct);
        var results = ParseSearchResults(html);

        if (results.Count == 0)
            return null;

        var candidates = results;
        if (!string.IsNullOrWhiteSpace(entry.Region))
        {
            var byRegion = results
                .Where(r => RegionMatches(entry.Region, r.Region))
                .ToList();
            if (byRegion.Count > 0)
                candidates = byRegion;
        }

        if (!string.IsNullOrWhiteSpace(entry.Languages) && candidates.Count > 1)
        {
            var romLangs = ParseLanguageCodes(entry.Languages);
            var scored = candidates
                .Select(r => (Result: r, Score: LanguageMatchScore(romLangs, r.Languages)))
                .OrderByDescending(x => x.Score)
                .ToList();

            if (scored[0].Score > 0)
                return scored[0].Result.Id;
        }

        return candidates[0].Id;
    }

    public async Task<byte[]?> DownloadBoxArtAsync(int gameId, CancellationToken ct = default)
    {
        var url = $"https://dl.vimm.net/image.php?type=box&id={gameId}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri($"{VimmBase}/vault/{gameId}");

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (!IsValidBoxImage(bytes))
            {
                bytes = await DownloadBoxFromGamePageAsync(gameId, ct);
            }

            return bytes is { Length: > 0 } && IsValidBoxImage(bytes) ? bytes : null;
        }
        catch (HttpRequestException)
        {
            return await DownloadBoxFromGamePageAsync(gameId, ct);
        }
    }

    private async Task<byte[]?> DownloadBoxFromGamePageAsync(int gameId, CancellationToken ct)
    {
        var pageUrl = $"{VimmBase}/vault/{gameId}";
        var html = await _http.GetStringAsync(pageUrl, ct);

        var match = BoxImgPattern().Match(html);
        if (!match.Success)
            return null;

        var imgPath = match.Groups[1].Value.Replace("&amp;", "&");
        var imgUrl = imgPath.StartsWith("//", StringComparison.Ordinal)
            ? "https:" + imgPath
            : imgPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? imgPath
                : VimmBase + imgPath;

        using var request = new HttpRequestMessage(HttpMethod.Get, imgUrl);
        request.Headers.Referrer = new Uri(pageUrl);
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return IsValidBoxImage(bytes) ? bytes : null;
    }

    // Vimm devuelve un banner 400×100 (logo + título) si falta Referer o no hay carátula.
    private static bool IsValidBoxImage(byte[] bytes)
    {
        if (bytes.Length < 500)
            return false;

        if (!TryGetPngSize(bytes, out var w, out var h))
            return true;

        if (w == 400 && h == 100)
            return false;
        if (w <= 100 && h <= 100)
            return false;

        return h >= 120 && w >= 120;
    }

    private static bool TryGetPngSize(byte[] data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 24 || data[0] != 0x89 || data[1] != (byte)'P')
            return false;

        width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return width > 0 && height > 0;
    }

    [GeneratedRegex(@"image\.php\?type=box[^""']+", RegexOptions.IgnoreCase)]
    private static partial Regex BoxImgPattern();

    public static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    private static string BuildSearchQuery(string title)
    {
        var q = title;
        var dashIdx = q.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0)
            q = q[..dashIdx];
        return q.Trim();
    }

    private static List<VaultSearchResult> ParseSearchResults(string html)
    {
        var results = new List<VaultSearchResult>();
        foreach (Match match in SearchRowPattern().Matches(html))
        {
            if (!int.TryParse(match.Groups[1].Value, out var id))
                continue;

            results.Add(new VaultSearchResult(
                id,
                match.Groups[2].Value.Trim(),
                match.Groups[3].Value.Trim(),
                match.Groups[4].Value.Trim()));
        }

        return results;
    }

    private static bool RegionMatches(string romRegion, string vaultRegion) =>
        string.Equals(NormalizeRegion(romRegion), NormalizeRegion(vaultRegion), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRegion(string region)
    {
        region = region.Trim();
        return region.ToUpperInvariant() switch
        {
            "US" or "U" or "USA" => "USA",
            "EU" or "E" or "EUR" => "Europe",
            "J" or "JP" or "JPN" => "Japan",
            "W" or "WORLD" => "World",
            "A" or "ASIA" => "Asia",
            "K" or "KOREA" => "Korea",
            _ => region
        };
    }

    private static HashSet<string> ParseLanguageCodes(string languages) =>
        languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.Length >= 2 ? l[..2].ToLowerInvariant() : l.ToLowerInvariant())
            .ToHashSet();

    private static int LanguageMatchScore(HashSet<string> romLangs, string vaultLangs)
    {
        if (romLangs.Count == 0 || string.IsNullOrWhiteSpace(vaultLangs) || vaultLangs == "-")
            return 0;

        var vault = vaultLangs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.ToLowerInvariant())
            .ToHashSet();

        return romLangs.Count(l => vault.Contains(l));
    }

    [GeneratedRegex(
        @"<tr><td[^>]*><a\s+href\s*=\s*""/vault/(\d+)""[^>]*>([^<]+)</a>[\s\S]*?title=""([^""]+)""[\s\S]*?class=""responsive"">([^<]*)</td>",
        RegexOptions.IgnoreCase)]
    private static partial Regex SearchRowPattern();

    private sealed record VaultSearchResult(int Id, string Title, string Region, string Languages);

    public void Dispose() => _http.Dispose();
}
