using System;

namespace GestorJuegos.Models
{
    public class AppSettings
    {
        public string LaunchBoxPath { get; set; } = @"H:\LaunchBox";
        public string PreferredArtType { get; set; } = "Box - Front";
        public bool AutoImportCovers { get; set; } = true;
        public string EmuMoviesUser { get; set; } = string.Empty;
        public string EmuMoviesPass { get; set; } = string.Empty;
        public string EmuMoviesApiKey { get; set; } = "D4F5E6A7B8C9D0E1F2"; // Default placeholder
    }
}
