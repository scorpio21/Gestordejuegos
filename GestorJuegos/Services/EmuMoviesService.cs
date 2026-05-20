using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;

namespace GestorJuegos.Services
{
    public class EmuMoviesService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private string? _sessionId;
        private const string ApiBaseUrl = "http://api.emumovies.com";
        
        // Clave pública/genérica utilizada en otros proyectos de emulación
        private string _apiKey = "6021464670697368"; 
        private string _productName = "GestorJuegos";

        public bool IsLoggedIn => !string.IsNullOrEmpty(_sessionId);

        public void SetCredentials(string apiKey, string productName)
        {
            _apiKey = apiKey;
            _productName = productName;
        }

        public string? LastErrorMessage { get; private set; }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                LastErrorMessage = null;
                // El endpoint de login de EmuMovies
                string url = $"{ApiBaseUrl}/login.aspx?user={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&api={_apiKey}&product={_productName}";
                
                var response = await _httpClient.GetStringAsync(url);
                var xml = XDocument.Parse(response);
                
                var status = xml.Root?.Element("Status")?.Value;
                if (status == "Success")
                {
                    _sessionId = xml.Root?.Element("SessionID")?.Value;
                    return true;
                }
                else
                {
                    LastErrorMessage = xml.Root?.Element("Message")?.Value ?? "Login Failure";
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<List<EmuMediaResult>> SearchMediaAsync(string gameName, string system, string mediaType = "Video_Snap")
        {
            if (!IsLoggedIn) return new List<EmuMediaResult>();

            try
            {
                string url = $"{ApiBaseUrl}/search.aspx?search={Uri.EscapeDataString(gameName)}&system={Uri.EscapeDataString(system)}&media={mediaType}&sessionid={_sessionId}";
                
                var response = await _httpClient.GetStringAsync(url);
                var xml = XDocument.Parse(response);
                
                var results = xml.Root?.Elements("Result")
                    .Select(r => new EmuMediaResult
                    {
                        FileName = r.Element("FileName")?.Value ?? "",
                        DownloadUrl = r.Element("DownloadURL")?.Value ?? "",
                        System = r.Element("System")?.Value ?? "",
                        Media = r.Element("Media")?.Value ?? ""
                    })
                    .ToList();

                return results ?? new List<EmuMediaResult>();
            }
            catch
            {
                return new List<EmuMediaResult>();
            }
        }

        public async Task<byte[]?> DownloadMediaAsync(string url)
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }
    }

    public class EmuMediaResult
    {
        public string FileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string System { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
    }
}
