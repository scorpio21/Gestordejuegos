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
        private const string ApiBaseUrl = "https://api.gamesdbase.com";
        
        // Clave pública/genérica y nombre de producto de una herramienta autorizada
        private string _apiKey = "6021464670697368"; 
        private string _productName = "Skyscraper";

        public EmuMoviesService()
        {
            // Añadimos un User-Agent para que el servidor nos acepte como una herramienta legítima
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Skyscraper/3.0 (EmuMovies Integration)");
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(_sessionId);

        public void SetCredentials(string apiKey, string productName)
        {
            _apiKey = apiKey;
            _productName = productName;
        }

        public string? LastErrorMessage { get; private set; }

        public async Task<bool> LoginAsync(string username, string password)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "emumovies_debug.log");
            try
            {
                LastErrorMessage = null;
                // El endpoint de login de EmuMovies usa 'pass' en lugar de 'password'
                string url = $"{ApiBaseUrl}/login.aspx?user={Uri.EscapeDataString(username)}&pass={Uri.EscapeDataString(password)}&api={_apiKey}&product={_productName}";
                
                // Log de la petición (ocultando password)
                string maskedUrl = $"{ApiBaseUrl}/login.aspx?user={Uri.EscapeDataString(username)}&pass=********&api={_apiKey}&product={_productName}";
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Intentando Login: {maskedUrl}{Environment.NewLine}");

                var response = await _httpClient.GetStringAsync(url);
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Respuesta RAW: {response}{Environment.NewLine}");

                var xml = XDocument.Parse(response);
                var resultElement = xml.Root?.Element("Result");
                
                var success = resultElement?.Attribute("Success")?.Value;
                if (success == "True")
                {
                    _sessionId = resultElement?.Attribute("Session")?.Value;
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Login Exitoso. SessionID obtenido.{Environment.NewLine}");
                    return true;
                }
                else
                {
                    LastErrorMessage = resultElement?.Attribute("MSG")?.Value ?? "Login Failure";
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Error en Login: {LastErrorMessage}{Environment.NewLine}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] EXCEPCIÓN: {ex.Message}{Environment.NewLine}");
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
