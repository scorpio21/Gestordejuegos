using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GestorJuegos.Services
{
    public class IgdbSearchResult
    {
        public string Name { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public List<string> Platforms { get; set; } = new();
    }

    public class IgdbService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private string? _accessToken;
        private DateTime _tokenExpiration;

        public IgdbService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        private async Task EnsureTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
                return;

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 1 minuto de margen
        }

        public async Task<List<IgdbSearchResult>> SearchGamesAsync(string query)
        {
            await EnsureTokenAsync();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", _clientId);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            // IGDB query syntax
            string body = $"search \"{query}\"; fields name, first_release_date, cover.image_id, genres.name, platforms.name; limit 30;";
            var content = new StringContent(body, Encoding.UTF8, "text/plain");

            var response = await client.PostAsync("https://api.igdb.com/v4/games", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = new List<IgdbSearchResult>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var result = new IgdbSearchResult();
                if (element.TryGetProperty("name", out var nameProp))
                {
                    result.Name = nameProp.GetString() ?? "";
                }

                if (element.TryGetProperty("first_release_date", out var dateProp))
                {
                    long unixDate = dateProp.GetInt64();
                    result.Year = DateTimeOffset.FromUnixTimeSeconds(unixDate).Year;
                }

                if (element.TryGetProperty("genres", out var genresProp) && genresProp.ValueKind == JsonValueKind.Array)
                {
                    var genresList = new List<string>();
                    foreach (var genre in genresProp.EnumerateArray())
                    {
                        if (genre.TryGetProperty("name", out var gName))
                        {
                            genresList.Add(gName.GetString() ?? "");
                        }
                    }
                    result.Genre = string.Join(", ", genresList);
                }

                if (element.TryGetProperty("platforms", out var platformsProp) && platformsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in platformsProp.EnumerateArray())
                    {
                        if (p.TryGetProperty("name", out var pName))
                        {
                            result.Platforms.Add(pName.GetString() ?? "");
                        }
                    }
                }

                if (element.TryGetProperty("cover", out var coverProp) && coverProp.TryGetProperty("image_id", out var imageIdProp))
                {
                    string imageId = imageIdProp.GetString() ?? "";
                    result.CoverUrl = $"https://images.igdb.com/igdb/image/upload/t_cover_big/{imageId}.jpg";
                }

                results.Add(result);
            }

            return results;
        }

        public async Task<byte[]> DownloadCoverAsync(string url)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(url);
        }
    }
}
