using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GestorJuegos.Services
{
    public sealed partial class VimmVaultService : IDisposable
    {
        private readonly HttpClient _http;
        private const string VimmBase = "https://vimm.net";
        
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vimm_debug_log.txt");

        public VimmVaultService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) GestorVimm/1.0");
            _http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,*/*");
            _http.DefaultRequestHeaders.Referrer = new Uri(VimmBase + "/");
            
            Log("--- VimmVaultService Initialized ---");
        }

        public static void Log(string message)
        {
            try
            {
                string entry = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFile, entry);
            }
            catch { }
        }

        public static string? GetSystemCode(string platformName)
        {
            Log($"GetSystemCode para: '{platformName}'");
            string lowerName = platformName.ToLower();

            if (lowerName.Contains("atari 2600")) return "2600";
            if (lowerName.Contains("atari 5200")) return "5200";
            if (lowerName.Contains("atari 7800")) return "7800";
            if (lowerName.Contains("nes") || lowerName.Contains("nintendo entertainment system")) return "NES";
            if (lowerName.Contains("snes") || lowerName.Contains("super nintendo")) return "SNES";
            if (lowerName.Contains("n64") || lowerName.Contains("nintendo 64")) return "N64";
            if (lowerName.Contains("gamecube") || lowerName.Contains("gc")) return "GameCube";
            if (lowerName.Contains("wii") && !lowerName.Contains("wiiu")) return "Wii";
            if (lowerName.Contains("game boy") && !lowerName.Contains("color") && !lowerName.Contains("advance")) return "GB";
            if (lowerName.Contains("game boy color") || lowerName.Contains("gbc")) return "GBC";
            if (lowerName.Contains("game boy advance") || lowerName.Contains("gba")) return "GBA";
            if (lowerName.Contains("ds") && !lowerName.Contains("3ds")) return "DS";
            if (lowerName.Contains("3ds")) return "3DS";
            if (lowerName.Contains("master system")) return "SMS";
            if (lowerName.Contains("genesis") || lowerName.Contains("mega drive")) return "Genesis";
            if (lowerName.Contains("game gear")) return "GG";
            if (lowerName.Contains("saturn")) return "Saturn";
            if (lowerName.Contains("dreamcast")) return "Dreamcast";
            if (lowerName.Contains("ps1") || lowerName.Contains("playstation 1") || (lowerName.Contains("playstation") && !lowerName.Contains("2") && !lowerName.Contains("3") && !lowerName.Contains("portable"))) return "PS1";
            if (lowerName.Contains("ps2") || lowerName.Contains("playstation 2")) return "PS2";
            if (lowerName.Contains("ps3") || lowerName.Contains("playstation 3")) return "PS3";
            if (lowerName.Contains("psp") || lowerName.Contains("playstation portable")) return "PSP";
            if (lowerName.Contains("xbox") && !lowerName.Contains("360")) return "Xbox";
            if (lowerName.Contains("xbox 360")) return "Xbox360";
            if (lowerName.Contains("turbografx-16") || lowerName.Contains("tg16") || lowerName.Contains("pc engine")) return "TG16";
            if (lowerName.Contains("jaguar")) return "Jaguar";
            if (lowerName.Contains("lynx")) return "Lynx";
            if (lowerName.Contains("virtual boy")) return "VB";

            Log($"No se encontró código de sistema para: '{platformName}'");
            return null;
        }

        public async Task<int?> FindGameIdAsync(string systemCode, string gameName, string region = "", string languages = "", CancellationToken ct = default)
        {
            var query = BuildSearchQuery(gameName);
            var url = $"https://vimm.net/vault/?p=list&system={Uri.EscapeDataString(systemCode)}&q={Uri.EscapeDataString(query)}";

            Log($"--- FindGameIdAsync ---");
            Log($"System: {systemCode}, Game: {gameName}, Query: {query}");
            Log($"URL: {url}");

            try
            {
                var html = await _http.GetStringAsync(url, ct);
                Log($"HTML recibido (longitud: {html.Length})");
                
                var results = ParseSearchResults(html);
                Log($"Resultados encontrados en HTML: {results.Count}");

                if (results.Count == 0)
                {
                    Log("No se encontraron resultados en el HTML.");
                    return null;
                }

                foreach (var res in results)
                {
                    Log($"Candidato: ID={res.Id}, Title={res.Title}, Region={res.Region}, Lang={res.Languages}");
                }

                var candidates = results;
                if (!string.IsNullOrWhiteSpace(region))
                {
                    var normRegion = NormalizeRegion(region);
                    Log($"Filtrando por región: {region} -> {normRegion}");
                    var byRegion = results
                        .Where(r => RegionMatches(region, r.Region))
                        .ToList();
                    
                    if (byRegion.Count > 0)
                    {
                        Log($"Coincidencias por región: {byRegion.Count}");
                        candidates = byRegion;
                    }
                    else
                    {
                        Log("No hubo coincidencias exactas por región, manteniendo todos los candidatos.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(languages) && candidates.Count > 1)
                {
                    var romLangs = ParseLanguageCodes(languages);
                    Log($"Puntuando por idiomas: {languages}");
                    var scored = candidates
                        .Select(r => (Result: r, Score: LanguageMatchScore(romLangs, r.Languages)))
                        .OrderByDescending(x => x.Score)
                        .ToList();

                    if (scored[0].Score > 0)
                    {
                        Log($"Mejor puntuación idioma: {scored[0].Result.Id} (Score: {scored[0].Score})");
                        return scored[0].Result.Id;
                    }
                }

                Log($"Seleccionado por defecto: {candidates[0].Id}");
                return candidates[0].Id;
            }
            catch (Exception ex)
            {
                Log($"Error en FindGameIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<byte[]?> DownloadBoxArtAsync(int gameId, CancellationToken ct = default)
        {
            var url = $"https://dl.vimm.net/image.php?type=box&id={gameId}";
            Log($"--- DownloadBoxArtAsync (ID: {gameId}) ---");
            Log($"URL Inicial: {url}");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Referrer = new Uri($"{VimmBase}/vault/{gameId}");

                var response = await _http.SendAsync(request, ct);
                Log($"Respuesta HTTP: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                Log($"Bytes descargados: {bytes.Length}");

                if (!IsValidBoxImage(bytes))
                {
                    Log("La imagen descargada no parece una carátula válida. Intentando desde página del juego...");
                    bytes = await DownloadBoxFromGamePageAsync(gameId, ct);
                }

                if (bytes is { Length: > 0 } && IsValidBoxImage(bytes))
                {
                    Log("Imagen validada con éxito.");
                    return bytes;
                }

                Log("No se pudo obtener una imagen válida.");
                return null;
            }
            catch (Exception ex)
            {
                Log($"Error en DownloadBoxArtAsync: {ex.Message}");
                return await DownloadBoxFromGamePageAsync(gameId, ct);
            }
        }

        private async Task<byte[]?> DownloadBoxFromGamePageAsync(int gameId, CancellationToken ct)
        {
            try
            {
                var pageUrl = $"{VimmBase}/vault/{gameId}";
                Log($"Analizando página del juego: {pageUrl}");
                var html = await _http.GetStringAsync(pageUrl, ct);

                var match = BoxImgPattern().Match(html);
                if (!match.Success)
                {
                    Log("No se encontró el patrón de imagen en el HTML de la página.");
                    return null;
                }

                var imgPath = match.Groups[1].Value.Replace("&amp;", "&");
                var imgUrl = imgPath.StartsWith("//", StringComparison.Ordinal)
                    ? "https:" + imgPath
                    : imgPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? imgPath
                        : VimmBase + imgPath;

                Log($"URL de imagen encontrada en página: {imgUrl}");

                using var request = new HttpRequestMessage(HttpMethod.Get, imgUrl);
                request.Headers.Referrer = new Uri(pageUrl);
                var response = await _http.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                
                if (IsValidBoxImage(bytes))
                {
                    Log("Imagen de página validada.");
                    return bytes;
                }

                Log("Imagen de página tampoco es válida.");
                return null;
            }
            catch (Exception ex)
            {
                Log($"Error en DownloadBoxFromGamePageAsync: {ex.Message}");
                return null;
            }
        }

        private bool IsValidBoxImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 500) return false;

            if (!TryGetPngSize(bytes, out var w, out var h))
            {
                return bytes.Length > 2000; 
            }

            if (w == 400 && h == 100) return false;
            
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

        public async Task<List<IgdbSearchResult>> SearchGamesAsync(string systemCode, string gameName, CancellationToken ct = default)
        {
            var query = BuildSearchQuery(gameName);
            var url = $"https://vimm.net/vault/?p=list&system={Uri.EscapeDataString(systemCode)}&q={Uri.EscapeDataString(query)}";

            Log($"--- SearchGamesAsync ---");
            Log($"System: {systemCode}, Game: {gameName}");
            Log($"URL: {url}");

            try
            {
                var html = await _http.GetStringAsync(url, ct);
                Log($"HTML recibido para búsqueda (longitud: {html.Length})");
                
                var results = ParseSearchResults(html);
                Log($"Resultados parseados: {results.Count}");

                return results.Select(r => new IgdbSearchResult
                {
                    Name = $"{r.Title} [{r.Region}]",
                    CoverUrl = r.Id.ToString(), 
                    Platforms = new List<string> { systemCode },
                    Genre = r.Languages
                }).ToList();
            }
            catch (Exception ex)
            {
                Log($"Error en SearchGamesAsync: {ex.Message}");
                return new List<IgdbSearchResult>();
            }
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
            region = Regex.Replace(region, @"[^\u0000-\u007F]+", "");
            region = region.Trim().ToUpperInvariant();
            
            return region switch
            {
                "US" or "U" or "USA" => "USA",
                "EU" or "E" or "EUR" or "EUROPE" => "Europe",
                "J" or "JP" or "JPN" or "JAPAN" => "Japan",
                "W" or "WORLD" => "World",
                "A" or "ASIA" => "Asia",
                "K" or "KOREA" => "Korea",
                _ => region
            };
        }

        private static HashSet<string> ParseLanguageCodes(string languages) =>
            languages.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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

        public static Dictionary<string, string> GetSupportedPlatforms() => new()
        {
            { "Atari 2600", "2600" },
            { "Atari 5200", "5200" },
            { "Atari 7800", "7800" },
            { "ColecoVision", "ColecoVision" },
            { "Commodore 64", "64" },
            { "Intellivision", "Intellivision" },
            { "Nintendo (NES)", "NES" },
            { "Sega Master System", "SMS" },
            { "Sega Genesis", "Genesis" },
            { "Sega Game Gear", "GG" },
            { "Game Boy", "GB" },
            { "TurboGrafx-16", "TG16" },
            { "Super Nintendo", "SNES" },
            { "Game Boy Color", "GBC" },
            { "Sega Saturn", "Saturn" },
            { "PlayStation", "PS1" },
            { "Virtual Boy", "VB" },
            { "Nintendo 64", "N64" },
            { "Game Boy Advance", "GBA" },
            { "PlayStation 2", "PS2" },
            { "GameCube", "GameCube" },
            { "Xbox", "Xbox" },
            { "Sega Dreamcast", "Dreamcast" },
            { "Nintendo DS", "DS" },
            { "Xbox 360", "Xbox360" },
            { "Nintendo Wii", "Wii" },
            { "PS Portable", "PSP" },
            { "PlayStation 3", "PS3" },
            { "Nintendo 3DS", "3DS" }
        };

        public void Dispose() => _http.Dispose();
    }
}
