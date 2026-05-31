using System;
using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class StartupApplication
    {
        public string AppPath { get; set; } = "";
        public string CommandLineArgs { get; set; } = "";
        public string StartWith { get; set; } = "LaunchBox";
        public bool AllowMultipleInstances { get; set; } = false;
    }

    public class AppSettings
    {
        public string LaunchBoxPath { get; set; } = @"H:\LaunchBox";
        public string PreferredArtType { get; set; } = "Box - Front";
        public bool AutoImportCovers { get; set; } = true;
        public bool EnableSoundEffects { get; set; } = true;

        // --- Tematización estilo LaunchBox ---
        public string Theme { get; set; } = "Default";
        public string ColorTheme { get; set; } = "Default";

        // Colores Hexadecimales de la Paleta (Aesthetic & editable)
        public string ColorLightBg { get; set; } = "#1c1d22";
        public string ColorDarkBg { get; set; } = "#121316";
        public string ColorSelectedBg { get; set; } = "#3a5180";
        public string ColorHighlightedBg { get; set; } = "#2a2b30";
        public string ColorBorderHighlight { get; set; } = "#2c2e35";
        public string ColorBorderWindow { get; set; } = "#5f626a";
        public string ColorBorderMenu { get; set; } = "#2c2e35";
        public string ColorForeground { get; set; } = "#ffffff";

        // Características Visuales
        public bool ScrollImmediately { get; set; } = true;
        public bool ColorGameDividers { get; set; } = true;
        public bool ColorScrollbar { get; set; } = true;
        public bool ColorGameSelections { get; set; } = false;
        public bool ColorPopupDetails { get; set; } = false;
        public bool ColorBlurBackground { get; set; } = false;
        public bool UseRandomColorTheme { get; set; } = false;

        // Fuentes Tipográficas
        public string GameFont { get; set; } = "Segoe UI";
        public string SidebarFont { get; set; } = "Segoe UI";
        public string LargeDetailsFont { get; set; } = "Segoe UI";
        public string SmallDetailsFont { get; set; } = "Segoe UI";
        public string TitleDetailsFont { get; set; } = "Segoe UI";

        // --- Depuración ---
        public bool EnableDebugLogs { get; set; } = false;

        // --- Notificaciones ---
        public string NotificationSystem { get; set; } = "Cuadros de mensaje";

        // --- Importaciones Automatizadas ---
        public bool EnableAutomaticRomImports { get; set; } = true;

        // --- Save Management ---
        public bool EnableSaveManagement { get; set; } = true;
        public bool EnableAutoSaveBackups { get; set; } = false;
        public bool BackupOnGameClose { get; set; } = true;
        public bool EnablePeriodicBackups { get; set; } = true;
        public int MaxBackupVersions { get; set; } = 25;

        // --- Aplicaciones de Inicio ---
        public List<StartupApplication> StartupApplications { get; set; } = new List<StartupApplication>();
    }
}
