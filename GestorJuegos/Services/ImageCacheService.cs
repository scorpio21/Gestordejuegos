using System;
using System.IO;

namespace GestorJuegos.Services
{
    public class ImageCacheService
    {
        private readonly string _cacheDir;

        public ImageCacheService()
        {
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "Covers");
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
        }

        public string GetCachePath(int gameId, string artType)
        {
            // Sanitizar el tipo de arte para usarlo en nombres de archivo
            string safeType = string.IsNullOrEmpty(artType) ? "Default" : artType.Replace(" ", "_").Replace("-", "_");
            return Path.Combine(_cacheDir, $"game_{gameId}_{safeType}.jpg");
        }

        public bool IsCached(int gameId, string artType)
        {
            return File.Exists(GetCachePath(gameId, artType));
        }

        public byte[]? GetFromCache(int gameId, string artType)
        {
            try
            {
                string path = GetCachePath(gameId, artType);
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo de caché: {ex.Message}");
            }
            return null;
        }

        public void SaveToCache(int gameId, string artType, byte[] data)
        {
            if (data == null || data.Length == 0) return;
            
            try
            {
                string path = GetCachePath(gameId, artType);
                File.WriteAllBytes(path, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando en caché: {ex.Message}");
            }
        }

        public void InvalidateCache(int gameId, string? artType = null)
        {
            try
            {
                if (string.IsNullOrEmpty(artType))
                {
                    // Borrar todos los tipos para este juego
                    var files = Directory.GetFiles(_cacheDir, $"game_{gameId}_*.jpg");
                    foreach (var file in files) File.Delete(file);
                }
                else
                {
                    string path = GetCachePath(gameId, artType);
                    if (File.Exists(path)) File.Delete(path);
                }
            }
            catch { }
        }

        public void ClearAll()
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    Directory.Delete(_cacheDir, true);
                    Directory.CreateDirectory(_cacheDir);
                }
            }
            catch { }
        }
    }
}
