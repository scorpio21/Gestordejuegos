using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GestorJuegos.Services
{
    public class PalSnesCoversService
    {
        private readonly HttpClient _httpClient;
        private List<string>? _cachedFiles = null;

        public PalSnesCoversService()
        {
            _httpClient = new HttpClient();
            // User-Agent es requerido por la API de GitHub
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GestorJuegos-App");
        }

        private async Task FetchFileListAsync()
        {
            if (_cachedFiles != null) return;
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.github.com/repos/nailbomb-rp/pal-snes-covers/contents/");
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<List<GithubFile>>(response, jsonOptions);
                
                if (items != null)
                {
                    _cachedFiles = items.Where(i => i.name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                                        .Select(i => i.name)
                                        .ToList();
                }
            }
            catch (Exception)
            {
                _cachedFiles = new List<string>();
            }
        }

        public async Task<List<IgdbSearchResult>> SearchGamesAsync(string query, string platformName = "")
        {
            await FetchFileListAsync();
            var results = new List<IgdbSearchResult>();
            
            if (_cachedFiles == null || _cachedFiles.Count == 0) return results;

            // Filtro simple: ver si el nombre de archivo contiene la query
            var matches = _cachedFiles.Where(f => f.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var match in matches)
            {
                // Reemplazamos espacios por %20 para la url, pero UrlEncode reemplazaría / por %2F
                string encodedName = Uri.EscapeDataString(match);
                string url = $"https://raw.githubusercontent.com/nailbomb-rp/pal-snes-covers/master/{encodedName}";
                
                // Limpiar nombre para mostrar
                string cleanName = match.Replace(".png", "", StringComparison.OrdinalIgnoreCase);
                
                results.Add(new IgdbSearchResult
                {
                    Name = cleanName,
                    CoverUrl = url,
                    Platforms = new List<string> { "Super Nintendo (SNES)" },
                    Genre = "SNES Cover"
                });
            }

            return results;
        }

        public async Task<byte[]?> DownloadCoverAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            try
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }

        private class GithubFile
        {
            public string name { get; set; } = "";
            public string type { get; set; } = "";
        }
    }
}
