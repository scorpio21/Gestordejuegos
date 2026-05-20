using System;

namespace GestorJuegos.Models
{
    public class AppSettings
    {
        public string LaunchBoxPath { get; set; } = @"H:\LaunchBox";
        public string PreferredArtType { get; set; } = "Box - Front";
        public bool AutoImportCovers { get; set; } = true;
    }
}
