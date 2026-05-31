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

        // --- Bandeja de Sistema (System Tray) ---
        public bool EnableSystemTray { get; set; } = false;
        public bool MinimizeToSystemTray { get; set; } = false;
        public bool CloseToSystemTray { get; set; } = true;
        public bool ShowNotificationOnTraySend { get; set; } = false;

        // --- Reproducción de Vídeo ---
        public bool UseWindowsMediaPlayer { get; set; } = true;

        // --- Copias de Seguridad ---
        public bool AutoBackupXmlData { get; set; } = true;

        // --- Actualizaciones ---
        public bool EnableAutoUpdates { get; set; } = true;
        public bool EnableBetaUpdates { get; set; } = false;

        // --- Prioridades de Región ---
        public List<string> RegionPriorities { get; set; } = new List<string> 
        { 
            "United States", "Europe", "Japan", "World", "United Kingdom", "Spain", "France", "Germany", "Australia" 
        };

        // --- RetroAchievements ---
        public bool EnableRetroAchievements { get; set; } = false;
        public string RetroUsername { get; set; } = "";
        public string RetroApiKey { get; set; } = "";
        public bool ShowAchievementNotifications { get; set; } = true;
        public bool ShowAchievementBadges { get; set; } = true;
    }
}
