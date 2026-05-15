using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GestorJuegos.Services
{
    public class GameTdbService
    {
        public async Task<List<IgdbSearchResult>> SearchGamesAsync(string query, string platformName)
        {
            var results = new List<IgdbSearchResult>();
            
            // Only allow specific platforms
            string systemCode = "";
            platformName = platformName.ToLower();
            if (platformName.Contains("wii u")) systemCode = "wiiu";
            else if (platformName.Contains("wii")) systemCode = "wii";
            else if (platformName.Contains("gamecube") || platformName.Contains("game cube")) systemCode = "gc";
            else if (platformName.Contains("3ds")) systemCode = "3ds";
            else if (platformName.Contains("ds")) systemCode = "ds";
            else if (platformName.Contains("switch")) systemCode = "switch";
            else if (platformName.Contains("ps3") || platformName.Contains("playstation 3")) systemCode = "ps3";
            else return results; // Not supported by GameTDB filter requested

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            var url = $"https://www.gametdb.com/Search?q={Uri.EscapeDataString(query)}";
            
            try
            {
                var html = await client.GetStringAsync(url);
                
                // Buscar enlaces como href="/Wii/RMCE01" o class="nav-link"
                // En el HTML de gametdb los juegos listados en búsqueda están en filas de tablas
                // Ejemplo: <a href="/Wii/RMCE01">Mario Kart Wii</a>
                
                var regex = new Regex(@"href=""/([^/]+)/([^""]+)""[^>]*>(.*?)</a>", RegexOptions.IgnoreCase);
                var matches = regex.Matches(html);
                
                var seenIds = new HashSet<string>();

                foreach (Match match in matches)
                {
                    string sys = match.Groups[1].Value.ToLower();
                    string id = match.Groups[2].Value;
                    string name = Regex.Replace(match.Groups[3].Value, "<.*?>", string.Empty).Trim();
                    
                    if (sys == systemCode || string.IsNullOrEmpty(systemCode))
                    {
                        if (seenIds.Contains(id)) continue;
                        seenIds.Add(id);

                        var result = new IgdbSearchResult
                        {
                            Name = name,
                            Year = DateTime.Now.Year, // No tenemos fecha fácil en la búsqueda simple
                            Genre = "",
                            // Generamos URL de carátula genérica (US o EN)
                            // Para Switch es /switch/cover/US/ID.jpg, Wii es /wii/cover/US/ID.png
                            CoverUrl = $"https://art.gametdb.com/{sys}/cover/US/{id}.jpg"
                        };
                        result.Platforms.Add(sys.ToUpper());
                        results.Add(result);
                    }
                }
            }
            catch
            {
                // Ignorar errores de scraping
            }

            return results;
        }

        public async Task<byte[]> DownloadCoverAsync(string url)
        {
            using var client = new HttpClient();
            try
            {
                // En GameTDB a veces es jpg y otras png. Intentamos ambas si falla la primera
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                
                // Si falla, probar con .png o viceversa
                string fallbackUrl = url.EndsWith(".jpg") ? url.Replace(".jpg", ".png") : url.Replace(".png", ".jpg");
                var fbResponse = await client.GetAsync(fallbackUrl);
                if (fbResponse.IsSuccessStatusCode)
                {
                    return await fbResponse.Content.ReadAsByteArrayAsync();
                }

                // Probar región EN
                string enUrl = url.Replace("/US/", "/EN/");
                var enResponse = await client.GetAsync(enUrl);
                if (enResponse.IsSuccessStatusCode)
                {
                    return await enResponse.Content.ReadAsByteArrayAsync();
                }
                
                string enFallback = fallbackUrl.Replace("/US/", "/EN/");
                var enfbResponse = await client.GetAsync(enFallback);
                if (enfbResponse.IsSuccessStatusCode)
                {
                    return await enfbResponse.Content.ReadAsByteArrayAsync();
                }
            }
            catch {}
            
            return new byte[0];
        }
    }
}
