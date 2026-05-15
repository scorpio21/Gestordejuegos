using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GestorJuegos.Services
{
    public class TheGamesDbService
    {
        private readonly string _apiKey;

        public TheGamesDbService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<List<IgdbSearchResult>> SearchGamesAsync(string query)
        {
            var results = new List<IgdbSearchResult>();
            if (string.IsNullOrEmpty(_apiKey)) return results;

            using var client = new HttpClient();
            var url = $"https://api.thegamesdb.net/v1/Games/ByGameName?apikey={_apiKey}&name={Uri.EscapeDataString(query)}&fields=genres,platforms";
            
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return results;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("data", out var dataElement) && 
                dataElement.TryGetProperty("games", out var gamesElement) && 
                gamesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var game in gamesElement.EnumerateArray())
                {
                    var result = new IgdbSearchResult();
                    
                    if (game.TryGetProperty("game_title", out var titleProp))
                    {
                        result.Name = titleProp.GetString() ?? "";
                    }
                    
                    if (game.TryGetProperty("release_date", out var dateProp))
                    {
                        var dateStr = dateProp.GetString();
                        if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                        {
                            result.Year = date.Year;
                        }
                    }
                    
                    int gameId = 0;
                    if (game.TryGetProperty("id", out var idProp))
                    {
                        gameId = idProp.GetInt32();
                    }

                    if (gameId > 0)
                    {
                        // Prefix to distinguish it in the DownloadCoverAsync method
                        result.CoverUrl = $"tgdb:{gameId}"; 
                    }

                    results.Add(result);
                }
            }

            return results;
        }

        public async Task<byte[]> DownloadCoverAsync(string urlOrId)
        {
            if (string.IsNullOrEmpty(_apiKey)) return new byte[0];

            if (!urlOrId.StartsWith("tgdb:"))
            {
                using var client = new HttpClient();
                return await client.GetByteArrayAsync(urlOrId);
            }

            string gameIdStr = urlOrId.Substring(5);
            using var tgdbClient = new HttpClient();
            var imagesUrl = $"https://api.thegamesdb.net/v1/Games/Images?apikey={_apiKey}&games_id={gameIdStr}";
            
            var response = await tgdbClient.GetAsync(imagesUrl);
            if (!response.IsSuccessStatusCode) return new byte[0];

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("data", out var dataElement))
            {
                string baseUrl = "";
                if (dataElement.TryGetProperty("base_url", out var baseObj) && baseObj.TryGetProperty("original", out var origObj))
                {
                    baseUrl = origObj.GetString() ?? "";
                }
                
                if (dataElement.TryGetProperty("images", out var imagesElement) && imagesElement.TryGetProperty(gameIdStr, out var gameImagesElement))
                {
                    var coverList = new List<string>();
                    foreach (var img in gameImagesElement.EnumerateArray())
                    {
                        if (img.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "boxart" &&
                            img.TryGetProperty("filename", out var fileProp))
                        {
                            coverList.Add(fileProp.GetString() ?? "");
                        }
                    }
                    
                    if (coverList.Count > 0 && !string.IsNullOrEmpty(baseUrl))
                    {
                        string coverFilename = coverList[0];
                        string finalUrl = baseUrl + coverFilename;
                        return await tgdbClient.GetByteArrayAsync(finalUrl);
                    }
                }
            }
            
            return new byte[0];
        }
    }
}
