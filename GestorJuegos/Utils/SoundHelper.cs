#pragma warning disable CA1416

using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace GestorJuegos.Utils
{
    public static class SoundHelper
    {
        private static readonly string SoundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
        private static readonly Dictionary<string, SoundPlayer> _soundCache = new Dictionary<string, SoundPlayer>();
        public static bool IsEnabled { get; set; } = true;

        static SoundHelper()
        {
            // Precargar sonidos comunes para evitar latencia de disco
            PreloadSound("nav.wav");
            PreloadSound("select.wav");
            PreloadSound("back.wav");
            PreloadSound("launch.wav");
        }

        private static void PreloadSound(string fileName)
        {
            try
            {
                string fullPath = Path.Combine(SoundsPath, fileName);
                if (File.Exists(fullPath))
                {
                    var player = new SoundPlayer(fullPath);
                    player.Load();
                    _soundCache[fileName] = player;
                }
            }
            catch { /* Ignorar errores de carga */ }
        }

        public static void PlayNavigation()
        {
            if (!IsEnabled) return;
            PlayCachedSound("nav.wav");
        }

        public static void PlaySelect()
        {
            if (!IsEnabled) return;
            PlayCachedSound("select.wav");
        }

        public static void PlayBack()
        {
            if (!IsEnabled) return;
            PlayCachedSound("back.wav");
        }

        public static void PlayLaunch()
        {
            if (!IsEnabled) return;
            PlayCachedSound("launch.wav");
        }

        private static void PlayCachedSound(string fileName)
        {
            Task.Run(() =>
            {
                try
                {
                    if (_soundCache.TryGetValue(fileName, out var player))
                    {
                        player.Play();
                    }
                    else
                    {
                        // Si por algún motivo no estaba en caché, intentar carga directa
                        string fullPath = Path.Combine(SoundsPath, fileName);
                        if (File.Exists(fullPath))
                        {
                            using var fallbackPlayer = new SoundPlayer(fullPath);
                            fallbackPlayer.Play();
                        }
                    }
                }
                catch { /* Ignorar errores de audio para no bloquear la UI */ }
            });
        }
    }
}
