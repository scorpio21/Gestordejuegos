using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GestorJuegos.Models;
using System.Text.Json.Serialization;
using System.Linq;

namespace GestorJuegos.Services
{
    public class RetroAchievementsService
    {
        private readonly HttpClient _httpClient;
        private string? _username;
        private string? _apiKey;

        public RetroAchievementsService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://retroachievements.org/API/") };
        }

        public void Initialize(string username, string apiKey)
        {
            _username = username;
            _apiKey = apiKey;
        }

        public async Task<RAUserSummary?> GetUserSummary()
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiKey)) return null;

            try
            {
                return await _httpClient.GetFromJsonAsync<RAUserSummary>(
                    $"API_GetUserSummary.php?u={_username}&z={_username}&y={_apiKey}");
            }
            catch { return null; }
        }

        public async Task<RAGameProgress?> GetGameProgress(int gameId)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiKey)) return null;

            try
            {
                // gameId aquí debe ser el ID de RetroAchievements, no el ID local
                return await _httpClient.GetFromJsonAsync<RAGameProgress>(
                    $"API_GetGameInfoAndUserProgress.php?u={_username}&z={_username}&y={_apiKey}&g={gameId}");
            }
            catch { return null; }
        }

        public async Task<List<Achievement>> GetGameAchievements(int raGameId)
        {
            var progress = await GetGameProgress(raGameId);
            if (progress == null || progress.Achievements == null) return new();

            return progress.Achievements.Values.Select(a => new Achievement
            {
                Title = a.Title ?? "Sin título",
                Description = a.Description ?? "",
                IconUrl = $"https://retroachievements.org/Badge/{a.BadgeName}.png",
                IsUnlocked = !string.IsNullOrEmpty(a.DateAwarded),
                Points = a.Points,
                UnlockDate = string.IsNullOrEmpty(a.DateAwarded) ? null : DateTime.TryParse(a.DateAwarded, out var d) ? d : null,
                Type = a.Type
            }).ToList();
        }

        public async Task<RAGameProgression?> GetGameProgression(int gameId)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiKey)) return null;

            try
            {
                return await _httpClient.GetFromJsonAsync<RAGameProgression>(
                    $"API_GetGameProgression.php?u={_username}&z={_username}&y={_apiKey}&i={gameId}");
            }
            catch { return null; }
        }
    }

    public class RAUserSummary
    {
        [JsonPropertyName("User")]
        public string? User { get; set; }

        [JsonPropertyName("TotalPoints")]
        public int TotalPoints { get; set; }

        [JsonPropertyName("TotalTruePoints")]
        public int TotalTruePoints { get; set; }

        [JsonPropertyName("Rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("UserPic")]
        public string? UserPic { get; set; }
    }

    public class RAGameProgress
    {
        [JsonPropertyName("ID")]
        public int ID { get; set; }

        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("NumAchievements")]
        public int NumAchievements { get; set; }

        [JsonPropertyName("NumAwarded")]
        public int NumAwarded { get; set; }

        [JsonPropertyName("Achievements")]
        public Dictionary<string, RAAchievement>? Achievements { get; set; }
    }

    public class RAAchievement
    {
        [JsonPropertyName("ID")]
        public int ID { get; set; }

        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Points")]
        public int Points { get; set; }

        [JsonPropertyName("BadgeName")]
        public string? BadgeName { get; set; }

        [JsonPropertyName("DateAwarded")]
        public string? DateAwarded { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class RARecentAchievement
    {
        [JsonPropertyName("Date")]
        public string? Date { get; set; }

        [JsonPropertyName("AchievementID")]
        public int AchievementID { get; set; }

        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("GameTitle")]
        public string? GameTitle { get; set; }

        [JsonPropertyName("GameID")]
        public int GameID { get; set; }

        [JsonPropertyName("BadgeName")]
        public string? BadgeName { get; set; }
    }

    public class RAGameProgression
    {
        [JsonPropertyName("ID")]
        public int Id { get; set; }

        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("NumDistinctPlayers")]
        public int NumDistinctPlayers { get; set; }

        [JsonPropertyName("MedianTimeToBeat")]
        public double MedianTimeToBeat { get; set; }

        [JsonPropertyName("MedianTimeToMaster")]
        public double MedianTimeToMaster { get; set; }
    }

}
