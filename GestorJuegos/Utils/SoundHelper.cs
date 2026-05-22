using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace GestorJuegos.Utils
{
    public static class SoundHelper
    {
        private static readonly string SoundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");

        public static void PlayNavigation()
        {
            PlaySound("nav.wav");
        }

        public static void PlaySelect()
        {
            PlaySound("select.wav");
        }

        public static void PlayBack()
        {
            PlaySound("back.wav");
        }

        public static void PlayLaunch()
        {
            PlaySound("launch.wav");
        }

        private static void PlaySound(string fileName)
        {
            Task.Run(() =>
            {
                try
                {
                    string fullPath = Path.Combine(SoundsPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        using (var player = new SoundPlayer(fullPath))
                        {
                            player.Play();
                        }
                    }
                }
                catch { /* Ignorar errores de audio para no bloquear la UI */ }
            });
        }
    }
}
