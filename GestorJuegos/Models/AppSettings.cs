using System;
using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class StartupApplication
    {
        public string AppPath { get; set; } = "";
        public string CommandLineArgs { get; set; } = "";
        public string StartWith { get; set; } = "Biblioteca Externa";
        public bool AllowMultipleInstances { get; set; } = false;
    }

    public class RegionPriorityItem
    {
        public string RegionName { get; set; } = "";
        public bool IsChecked { get; set; } = false;
    }

    public class ProgressStatusGroup
    {
        public string GroupName { get; set; } = "";
        public List<string> Items { get; set; } = new();
    }

    public class GameRelationCriterion
    {
        public string Field { get; set; } = "";
        public string Comparison { get; set; } = "";
        public string ValueType { get; set; } = "";
        public string CustomValue { get; set; } = "";
        public string Weight { get; set; } = "1";
        public string TargetGames { get; set; } = "";
    }

    public class AppSettings
    {
        public string ExternalLibraryPath { get; set; } = @"C:\BibliotecaExterna";
        public string PreferredArtType { get; set; } = "Box - Front";
        public bool AutoImportCovers { get; set; } = true;
        public bool EnableSoundEffects { get; set; } = true;

        // --- Tematización estilo Biblioteca Externa ---
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
        public List<RegionPriorityItem> RegionPriorities { get; set; } = new List<RegionPriorityItem>
        {
            new RegionPriorityItem { RegionName = "North America", IsChecked = true },
            new RegionPriorityItem { RegionName = "United States", IsChecked = true },
            new RegionPriorityItem { RegionName = "Europe", IsChecked = false },
            new RegionPriorityItem { RegionName = "Japan", IsChecked = false },
            new RegionPriorityItem { RegionName = "World", IsChecked = false },
            new RegionPriorityItem { RegionName = "Asia", IsChecked = false },
            new RegionPriorityItem { RegionName = "Australia", IsChecked = false },
            new RegionPriorityItem { RegionName = "Brazil", IsChecked = false },
            new RegionPriorityItem { RegionName = "Canada", IsChecked = false },
            new RegionPriorityItem { RegionName = "China", IsChecked = false },
            new RegionPriorityItem { RegionName = "Finland", IsChecked = false },
            new RegionPriorityItem { RegionName = "France", IsChecked = false },
            new RegionPriorityItem { RegionName = "Germany", IsChecked = false },
            new RegionPriorityItem { RegionName = "Greece", IsChecked = false },
            new RegionPriorityItem { RegionName = "Holland", IsChecked = false },
            new RegionPriorityItem { RegionName = "Hong Kong", IsChecked = false },
            new RegionPriorityItem { RegionName = "Italy", IsChecked = false },
            new RegionPriorityItem { RegionName = "Korea", IsChecked = false },
            new RegionPriorityItem { RegionName = "The Netherlands", IsChecked = false },
            new RegionPriorityItem { RegionName = "Norway", IsChecked = false },
            new RegionPriorityItem { RegionName = "Oceania", IsChecked = false },
            new RegionPriorityItem { RegionName = "Russia", IsChecked = false },
            new RegionPriorityItem { RegionName = "South America", IsChecked = false },
            new RegionPriorityItem { RegionName = "Spain", IsChecked = false },
            new RegionPriorityItem { RegionName = "Sweden", IsChecked = false },
            new RegionPriorityItem { RegionName = "Thailand", IsChecked = false },
            new RegionPriorityItem { RegionName = "United Kingdom", IsChecked = false }
        };

        // --- RetroAchievements ---
        public bool EnableRetroAchievements { get; set; } = false;
        public string RetroUsername { get; set; } = "";
        public string RetroApiKey { get; set; } = "";
        public bool ShowAchievementNotifications { get; set; } = true;
        public bool ShowAchievementBadges { get; set; } = true;

        // --- Game Progress Automation ---
        public bool EnableProgressAutomation { get; set; } = true;
        public string ProgAutoDefault { get; set; } = "Not Started / Unplayed";
        public int ProgAutoPlaytimeMin { get; set; } = 30;
        public string ProgAutoPlaytimeVal { get; set; } = "Active / In Progress";
        public string ProgAutoEarnedAchVal { get; set; } = "Active / In Progress";
        public int ProgAutoInactiveDays { get; set; } = 30;
        public string ProgAutoInactiveVal { get; set; } = "Active / Paused";
        public string ProgAutoSoftcoreBeatenVal { get; set; } = "Done / Beaten";
        public string ProgAutoHardcoreBeatenVal { get; set; } = "Done / Beaten";
        public string ProgAutoSoftcoreCompleteVal { get; set; } = "Done / Completed";
        public string ProgAutoHardcoreMasteredVal { get; set; } = "Done / Mastered";
        public string ProgAutoIncludeStatuses { get; set; } = "Not Started / Want to Play";

        // --- Game Progress Organization ---
        public List<ProgressStatusGroup> ProgressStatusGroups { get; set; } = new List<ProgressStatusGroup>
        {
            new ProgressStatusGroup { GroupName = "Not Started", Items = new() { "Unplayed", "Want to Play", "Won't Play" } },
            new ProgressStatusGroup { GroupName = "Active", Items = new() { "In Progress", "Continuous", "Paused" } },
            new ProgressStatusGroup { GroupName = "Done", Items = new() { "Beaten", "Completed", "Mastered", "Dropped" } }
        };

        // --- Buscar ---
        public bool EnableExternalMetadataSearch { get; set; } = true;
        public bool EnableVirtualKeyboard { get; set; } = true;
        public bool LoadExternalRatings { get; set; } = true;
        public bool UseCommunityRatings { get; set; } = true;
        public int MinCommunityRatings { get; set; } = 5;
        public bool UseAdvancedSearchSyntax { get; set; } = true;

        // --- Juegos Relacionados ---
        // Criterios de Juegos Similares
        public bool SimilarIncludeNonLibrary { get; set; } = true;
        public List<GameRelationCriterion> SimilarGameCriteria { get; set; } = new List<GameRelationCriterion>
        {
            new GameRelationCriterion { Field = "Notas", Comparison = "No está vacío", ValueType = "Valor personalizado", CustomValue = "", Weight = "Requerido", TargetGames = "Solo juegos de..." },
            new GameRelationCriterion { Field = "Tipo de Lanzamiento", Comparison = "Es Igual A", ValueType = "Valor personalizado", CustomValue = "Released", Weight = "Requerido", TargetGames = "Solo juegos de..." },
            new GameRelationCriterion { Field = "Título", Comparison = "No es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "Requerido", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Título", Comparison = "Es similar a", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Nombre Alternativo", Comparison = "Es similar a", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Series", Comparison = "Es similar a", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Género", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "3", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Modo de Juego", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Cantidad Máx. de...", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Plataforma", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Calificación", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Desarrollador", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Editor", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." }
        };

        // Criterios de Juegos Recomendados
        public bool RecommendedIncludeNonLibrary { get; set; } = true;
        public List<GameRelationCriterion> RecommendedGameCriteria { get; set; } = new List<GameRelationCriterion>
        {
            new GameRelationCriterion { Field = "Tipo de Lanzamiento", Comparison = "Es Igual A", ValueType = "Valor personalizado", CustomValue = "Released", Weight = "Requerido", TargetGames = "Solo juegos de..." },
            new GameRelationCriterion { Field = "Título", Comparison = "No es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "Requerido", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Calificación en la Comunidad", Comparison = "Es Mayor Que", ValueType = "Valor personalizado", CustomValue = "3.5", Weight = "Requerido", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Series", Comparison = "No es similar a", ValueType = "Valor del juego", CustomValue = "", Weight = "Requerido", TargetGames = "Solo juegos locales..." },
            new GameRelationCriterion { Field = "Género", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "3", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Modo de Juego", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Cantidad Máx. de...", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Plataforma", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Calificación en la Comunidad", Comparison = "Es Mayor Que", ValueType = "Valor personalizado", CustomValue = "4.1", Weight = "3", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Calificación", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "2", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Desarrollador", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Editor", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "1", TargetGames = "Todos los juegos..." }
        };

        // Criterios de Puertos Posibles
        public bool PossiblePortsIncludeNonLibrary { get; set; } = true;
        public List<GameRelationCriterion> PossiblePortsCriteria { get; set; } = new List<GameRelationCriterion>
        {
            new GameRelationCriterion { Field = "Tipo de Lanzamiento", Comparison = "Es Igual A", ValueType = "Valor personalizado", CustomValue = "Released", Weight = "Requerido", TargetGames = "Solo juegos de..." },
            new GameRelationCriterion { Field = "Plataforma", Comparison = "No es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "Requerido", TargetGames = "Todos los juegos..." },
            new GameRelationCriterion { Field = "Título", Comparison = "Es Igual A", ValueType = "Valor del juego", CustomValue = "", Weight = "Requerido", TargetGames = "Todos los juegos..." }
        };
    }
}
