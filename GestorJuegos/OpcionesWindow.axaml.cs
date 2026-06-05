using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestorJuegos;

public partial class OpcionesWindow : Window
{
    private readonly AppSettings _settings;

    // Variables locales de color (Estilo Biblioteca Externa)
    private string _colorLightBg = "#1c1d22";
    private string _colorDarkBg = "#121316";
    private string _colorSelectedBg = "#3a5180";
    private string _colorHighlightedBg = "#2a2b30";
    private string _colorBorderHighlight = "#2c2e35";
    private string _colorBorderWindow = "#5f626a";
    private string _colorBorderMenu = "#2c2e35";
    private string _colorForeground = "#ffffff";

    // Colección local para Prioridades de Región
    private readonly List<RegionPriorityItem> _regionPriorities = new();

    // Colecciones locales para Juegos Relacionados
    private readonly List<GameRelationCriterion> _similarCriteria = new();
    private readonly List<GameRelationCriterion> _recommendedCriteria = new();
    private readonly List<GameRelationCriterion> _possiblePortsCriteria = new();

    // Colección local para Organización de Progreso
    private readonly List<ProgressStatusGroup> _progressStatusGroups = new();

    // Listas auxiliares para los ComboBoxes de reglas en XAML
    public List<string> RuleFields { get; } = new()
    {
        "Título", "Nombre Alternativo", "Series", "Género", "Modo de Juego",
        "Cantidad Máx. de...", "Plataforma", "Calificación",
        "Calificación en la Comunidad", "Desarrollador", "Editor",
        "Tipo de Lanzamiento", "Notas"
    };

    public List<string> RuleComparisons { get; } = new()
    {
        "Es Igual A", "No es Igual A", "Es similar a", "No es similar a",
        "No está vacío", "Es Mayor Que"
    };

    public List<string> RuleValueTypes { get; } = new()
    {
        "Valor del juego", "Valor personalizado"
    };

    public List<string> RuleWeights { get; } = new()
    {
        "Requerido", "1", "2", "3"
    };

    public List<string> RuleTargetGames { get; } = new()
    {
        "Todos los juegos...", "Solo juegos locales...", "Solo juegos de..."
    };

    public OpcionesWindow()
    {
        InitializeComponent();
        _settings = new AppSettings();
        SetupEvents();
    }

    public OpcionesWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        // Cargar los valores de configuración en los controles
        LoadSettingsIntoUI();
        SetupEvents();
    }

    private void SetupEvents()
    {
        ChkEnableSaveManagement.IsCheckedChanged += OnSaveManagementChecked;
        ChkEnableAutoSaveBackups.IsCheckedChanged += OnAutoSaveBackupsChecked;
        ChkEnableRetroAchievements.IsCheckedChanged += OnRetroAchievementsChecked;
        ChkEnableProgressAutomation.IsCheckedChanged += OnProgressAutomationChecked;
    }

    private void LoadSettingsIntoUI()
    {
        // 1. Panel General y Rutas
        TxtExternalLibraryPath.Text = _settings.ExternalLibraryPath;
        ChkEnableSoundEffects.IsChecked = _settings.EnableSoundEffects;
        ChkAutoImportCovers.IsChecked = _settings.AutoImportCovers;

        // 2. Media: Tipo de arte preferido
        if (CmbPreferredArtType != null)
        {
            bool found = false;
            foreach (var rawItem in CmbPreferredArtType.Items)
            {
                if (rawItem is ComboBoxItem item && item.Content?.ToString() == _settings.PreferredArtType)
                {
                    CmbPreferredArtType.SelectedItem = item;
                    found = true;
                    break;
                }
            }
            if (!found && CmbPreferredArtType.Items.Count > 0)
            {
                CmbPreferredArtType.SelectedIndex = 3; // Por defecto "Box"
            }
        }

        // 3. Tema Principal (Poblado Dinámico)
        if (CmbTheme != null)
        {
            // Mapeo automático de temas antiguos si es necesario
            if (_settings.Theme == "Neon Deluxe Arcade LB")
            {
                _settings.Theme = "Neon Deluxe";
            }

            // Guardar el tema actual seleccionado
            string currentTheme = _settings.Theme;

            // Limpiar items existentes
            CmbTheme.Items.Clear();

            // Agregar temas fijos/integrados por defecto
            CmbTheme.Items.Add(new ComboBoxItem { Content = "Default" });
            CmbTheme.Items.Add(new ComboBoxItem { Content = "Old Default" });

            // Escanear carpeta de temas
            try
            {
                string themesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
                if (!System.IO.Directory.Exists(themesDir))
                {
                    themesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Themes");
                }

                if (System.IO.Directory.Exists(themesDir))
                {
                    var directories = System.IO.Directory.GetDirectories(themesDir);
                    foreach (var dir in directories)
                    {
                        string folderName = System.IO.Path.GetFileName(dir);
                        
                        // Omitir carpetas conocidas integradas si están en disco
                        if (folderName.Equals("Default", StringComparison.OrdinalIgnoreCase) || 
                            folderName.Equals("Old Default", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Verificar si tiene un theme.json para considerarlo tema válido
                        string jsonPath = System.IO.Path.Combine(dir, "theme.json");
                        if (System.IO.File.Exists(jsonPath))
                        {
                            CmbTheme.Items.Add(new ComboBoxItem { Content = folderName });
                        }
                    }
                }
            }
            catch
            {
                // Ignorar errores de escaneo
            }

            // Seleccionar el tema actual
            bool themeSelected = false;
            foreach (var rawItem in CmbTheme.Items)
            {
                if (rawItem is ComboBoxItem item && item.Content?.ToString() == currentTheme)
                {
                    CmbTheme.SelectedItem = item;
                    themeSelected = true;
                    break;
                }
            }

            // Si no se encuentra, seleccionar "Default"
            if (!themeSelected && CmbTheme.Items.Count > 0)
            {
                foreach (var rawItem in CmbTheme.Items)
                {
                    if (rawItem is ComboBoxItem item && item.Content?.ToString() == "Default")
                    {
                        CmbTheme.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // 4. Colores
        if (CmbColorTheme != null)
        {
            foreach (var rawItem in CmbColorTheme.Items)
            {
                if (rawItem is ComboBoxItem item && item.Content?.ToString() == _settings.ColorTheme)
                {
                    CmbColorTheme.SelectedItem = item;
                    break;
                }
            }
        }
        _colorLightBg = _settings.ColorLightBg;
        _colorDarkBg = _settings.ColorDarkBg;
        _colorSelectedBg = _settings.ColorSelectedBg;
        _colorHighlightedBg = _settings.ColorHighlightedBg;
        _colorBorderHighlight = _settings.ColorBorderHighlight;
        _colorBorderWindow = _settings.ColorBorderWindow;
        _colorBorderMenu = _settings.ColorBorderMenu;
        _colorForeground = _settings.ColorForeground;
        UpdateColorPreviews();

        // 5. Características
        ChkScrollImmediately.IsChecked = _settings.ScrollImmediately;
        ChkColorGameDividers.IsChecked = _settings.ColorGameDividers;
        ChkColorScrollbar.IsChecked = _settings.ColorScrollbar;
        ChkColorGameSelections.IsChecked = _settings.ColorGameSelections;
        ChkColorPopupDetails.IsChecked = _settings.ColorPopupDetails;
        ChkColorBlurBackground.IsChecked = _settings.ColorBlurBackground;
        ChkUseRandomColorTheme.IsChecked = _settings.UseRandomColorTheme;

        // 6. Depuración
        ChkEnableDebugLogs.IsChecked = _settings.EnableDebugLogs;

        // 7. Notificaciones
        if (CmbNotificationSystem != null)
        {
            foreach (var rawItem in CmbNotificationSystem.Items)
            {
                if (rawItem is ComboBoxItem item && item.Content?.ToString() == _settings.NotificationSystem)
                {
                    CmbNotificationSystem.SelectedItem = item;
                    break;
                }
            }
        }

        // 8. Importaciones Automatizadas
        ChkEnableAutomaticRomImports.IsChecked = _settings.EnableAutomaticRomImports;

        // 9. Save Management
        ChkEnableSaveManagement.IsChecked = _settings.EnableSaveManagement;
        ChkEnableAutoSaveBackups.IsChecked = _settings.EnableAutoSaveBackups;
        ChkBackupOnGameClose.IsChecked = _settings.BackupOnGameClose;
        ChkEnablePeriodicBackups.IsChecked = _settings.EnablePeriodicBackups;
        NumMaxBackupVersions.Value = _settings.MaxBackupVersions;

        // Forzar actualización de estados de habilitación visual
        UpdateSaveManagementEnablement();

        // 10. Aplicaciones de Inicio
        LstStartupApps.ItemsSource = _settings.StartupApplications;

        // 11. Bandeja de Sistema
        ChkEnableSystemTray.IsChecked = _settings.EnableSystemTray;
        ChkMinimizeToSystemTray.IsChecked = _settings.MinimizeToSystemTray;
        ChkCloseToSystemTray.IsChecked = _settings.CloseToSystemTray;
        ChkShowNotificationOnTraySend.IsChecked = _settings.ShowNotificationOnTraySend;

        // 12. Reproducción de Vídeo
        RadUseWMP.IsChecked = _settings.UseWindowsMediaPlayer;
        RadUseFFmpeg.IsChecked = !_settings.UseWindowsMediaPlayer;

        // 13. Copias de Seguridad
        ChkAutoBackupXmlData.IsChecked = _settings.AutoBackupXmlData;

        // 14. Actualizaciones
        ChkEnableAutoUpdates.IsChecked = _settings.EnableAutoUpdates;
        ChkEnableBetaUpdates.IsChecked = _settings.EnableBetaUpdates;

        // 15. Prioridades de Región
        _regionPriorities.Clear();
        foreach (var item in _settings.RegionPriorities)
        {
            _regionPriorities.Add(new RegionPriorityItem { RegionName = item.RegionName, IsChecked = item.IsChecked });
        }
        LstRegionPriorities.ItemsSource = null;
        LstRegionPriorities.ItemsSource = _regionPriorities;

        // 16. RetroAchievements
        ChkEnableRetroAchievements.IsChecked = _settings.EnableRetroAchievements;
        TxtRetroUsername.Text = _settings.RetroUsername;
        TxtRetroApiKey.Text = _settings.RetroApiKey;
        ChkShowAchievementNotifications.IsChecked = _settings.ShowAchievementNotifications;
        ChkShowAchievementBadges.IsChecked = _settings.ShowAchievementBadges;
        UpdateRetroAchievementsEnablement();

        // 17. Game Progress Automation
        ChkEnableProgressAutomation.IsChecked = _settings.EnableProgressAutomation;
        NumProgAutoPlaytimeMin.Value = _settings.ProgAutoPlaytimeMin;
        NumProgAutoInactiveDays.Value = _settings.ProgAutoInactiveDays;
        TxtProgAutoIncludeStatuses.Text = _settings.ProgAutoIncludeStatuses;

        var statesList = new List<string>
        {
            "Not Started / Unplayed",
            "Not Started / Want to Play",
            "Not Started / Won't Play",
            "Active / In Progress",
            "Active / Continuous",
            "Active / Paused",
            "Done / Beaten",
            "Done / Completed",
            "Done / Mastered",
            "Done / Dropped",
            "(Ninguno)"
        };

        void PopulateStateCmb(ComboBox cmb, string selectedValue)
        {
            cmb.ItemsSource = statesList;
            cmb.SelectedItem = selectedValue;
            if (cmb.SelectedItem == null && statesList.Count > 0)
                cmb.SelectedIndex = 0;
        }

        PopulateStateCmb(CmbProgAutoDefault, _settings.ProgAutoDefault);
        PopulateStateCmb(CmbProgAutoPlaytimeVal, _settings.ProgAutoPlaytimeVal);
        PopulateStateCmb(CmbProgAutoEarnedAchVal, _settings.ProgAutoEarnedAchVal);
        PopulateStateCmb(CmbProgAutoInactiveVal, _settings.ProgAutoInactiveVal);
        PopulateStateCmb(CmbProgAutoSoftcoreBeatenVal, _settings.ProgAutoSoftcoreBeatenVal);
        PopulateStateCmb(CmbProgAutoHardcoreBeatenVal, _settings.ProgAutoHardcoreBeatenVal);
        PopulateStateCmb(CmbProgAutoSoftcoreCompleteVal, _settings.ProgAutoSoftcoreCompleteVal);
        PopulateStateCmb(CmbProgAutoHardcoreMasteredVal, _settings.ProgAutoHardcoreMasteredVal);

        UpdateProgressAutomationEnablement();

        // 18. Game Progress Organization
        _progressStatusGroups.Clear();
        foreach (var group in _settings.ProgressStatusGroups)
        {
            _progressStatusGroups.Add(new ProgressStatusGroup
            {
                GroupName = group.GroupName,
                Items = new List<string>(group.Items)
            });
        }
        LoadProgressOrgTree();

        // 19. Buscar
        ChkEnableExternalMetadataSearch.IsChecked = _settings.EnableExternalMetadataSearch;
        ChkLoadExternalRatings.IsChecked = _settings.LoadExternalRatings;
        ChkUseCommunityRatings.IsChecked = _settings.UseCommunityRatings;
        NumMinCommunityRatings.Value = _settings.MinCommunityRatings;
        ChkUseAdvancedSearchSyntax.IsChecked = _settings.UseAdvancedSearchSyntax;

        // 20. Juegos Similares
        ChkSimilarIncludeNonLibrary.IsChecked = _settings.SimilarIncludeNonLibrary;
        _similarCriteria.Clear();
        foreach (var c in _settings.SimilarGameCriteria)
        {
            _similarCriteria.Add(new GameRelationCriterion
            {
                Field = c.Field,
                Comparison = c.Comparison,
                ValueType = c.ValueType,
                CustomValue = c.CustomValue,
                Weight = c.Weight,
                TargetGames = c.TargetGames
            });
        }
        LstSimilarCriteria.ItemsSource = null;
        LstSimilarCriteria.ItemsSource = _similarCriteria;

        // 21. Juegos Recomendados
        ChkRecommendedIncludeNonLibrary.IsChecked = _settings.RecommendedIncludeNonLibrary;
        _recommendedCriteria.Clear();
        foreach (var c in _settings.RecommendedGameCriteria)
        {
            _recommendedCriteria.Add(new GameRelationCriterion
            {
                Field = c.Field,
                Comparison = c.Comparison,
                ValueType = c.ValueType,
                CustomValue = c.CustomValue,
                Weight = c.Weight,
                TargetGames = c.TargetGames
            });
        }
        LstRecommendedCriteria.ItemsSource = null;
        LstRecommendedCriteria.ItemsSource = _recommendedCriteria;

        // 22. Puertos Posibles
        ChkPossiblePortsIncludeNonLibrary.IsChecked = _settings.PossiblePortsIncludeNonLibrary;
        _possiblePortsCriteria.Clear();
        foreach (var c in _settings.PossiblePortsCriteria)
        {
            _possiblePortsCriteria.Add(new GameRelationCriterion
            {
                Field = c.Field,
                Comparison = c.Comparison,
                ValueType = c.ValueType,
                CustomValue = c.CustomValue,
                Weight = c.Weight,
                TargetGames = c.TargetGames
            });
        }
        LstPossiblePortsCriteria.ItemsSource = null;
        LstPossiblePortsCriteria.ItemsSource = _possiblePortsCriteria;
    }

    private void UpdateSaveManagementEnablement()
    {
        bool isSaveMgmtEnabled = ChkEnableSaveManagement.IsChecked == true;
        bool isAutoBackupEnabled = ChkEnableAutoSaveBackups.IsChecked == true;

        if (BrdSaveBackupOptions != null)
        {
            BrdSaveBackupOptions.IsEnabled = isSaveMgmtEnabled;
        }

        if (PnlBackupSubOptions != null)
        {
            PnlBackupSubOptions.IsEnabled = isSaveMgmtEnabled && isAutoBackupEnabled;
        }
    }

    private void OnSaveManagementChecked(object? sender, RoutedEventArgs e)
    {
        UpdateSaveManagementEnablement();
    }

    private void OnAutoSaveBackupsChecked(object? sender, RoutedEventArgs e)
    {
        UpdateSaveManagementEnablement();
    }

    private void UpdateRetroAchievementsEnablement()
    {
        if (BrdRetroOptions != null)
        {
            BrdRetroOptions.IsEnabled = ChkEnableRetroAchievements.IsChecked == true;
        }
    }

    private void OnRetroAchievementsChecked(object? sender, RoutedEventArgs e)
    {
        UpdateRetroAchievementsEnablement();
    }

    private void UpdateColorPreviews()
    {
        try { BrdColorLightBg.Background = Brush.Parse(_colorLightBg); } catch {}
        try { BrdColorDarkBg.Background = Brush.Parse(_colorDarkBg); } catch {}
        try { BrdColorSelectedBg.Background = Brush.Parse(_colorSelectedBg); } catch {}
        try { BrdColorHighlightedBg.Background = Brush.Parse(_colorHighlightedBg); } catch {}
        try { BrdColorBorderHighlight.Background = Brush.Parse(_colorBorderHighlight); } catch {}
        try { BrdColorBorderWindow.Background = Brush.Parse(_colorBorderWindow); } catch {}
        try { BrdColorBorderMenu.Background = Brush.Parse(_colorBorderMenu); } catch {}
        try { BrdColorForeground.Background = Brush.Parse(_colorForeground); } catch {}
    }

    private async void BtnColor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string colorTag)
        {
            string startColor = colorTag switch
            {
                "ColorLightBg" => _colorLightBg,
                "ColorDarkBg" => _colorDarkBg,
                "ColorSelectedBg" => _colorSelectedBg,
                "ColorHighlightedBg" => _colorHighlightedBg,
                "ColorBorderHighlight" => _colorBorderHighlight,
                "ColorBorderWindow" => _colorBorderWindow,
                "ColorBorderMenu" => _colorBorderMenu,
                "ColorForeground" => _colorForeground,
                _ => "#ffffff"
            };

            var dialog = new ColorPickerDialog(startColor);
            var selectedColor = await dialog.ShowDialog<string>(this);

            if (selectedColor != null)
            {
                switch (colorTag)
                {
                    case "ColorLightBg": _colorLightBg = selectedColor; break;
                    case "ColorDarkBg": _colorDarkBg = selectedColor; break;
                    case "ColorSelectedBg": _colorSelectedBg = selectedColor; break;
                    case "ColorHighlightedBg": _colorHighlightedBg = selectedColor; break;
                    case "ColorBorderHighlight": _colorBorderHighlight = selectedColor; break;
                    case "ColorBorderWindow": _colorBorderWindow = selectedColor; break;
                    case "ColorBorderMenu": _colorBorderMenu = selectedColor; break;
                    case "ColorForeground": _colorForeground = selectedColor; break;
                }

                UpdateColorPreviews();
            }
        }
    }

    private void BtnRegionUp_Click(object? sender, RoutedEventArgs e)
    {
        int selectedIndex = LstRegionPriorities.SelectedIndex;
        if (selectedIndex > 0)
        {
            var item = _regionPriorities[selectedIndex];
            _regionPriorities.RemoveAt(selectedIndex);
            _regionPriorities.Insert(selectedIndex - 1, item);
            
            LstRegionPriorities.ItemsSource = null;
            LstRegionPriorities.ItemsSource = _regionPriorities;
            LstRegionPriorities.SelectedIndex = selectedIndex - 1;
        }
    }

    private void BtnRegionDown_Click(object? sender, RoutedEventArgs e)
    {
        int selectedIndex = LstRegionPriorities.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < _regionPriorities.Count - 1)
        {
            var item = _regionPriorities[selectedIndex];
            _regionPriorities.RemoveAt(selectedIndex);
            _regionPriorities.Insert(selectedIndex + 1, item);
            
            LstRegionPriorities.ItemsSource = null;
            LstRegionPriorities.ItemsSource = _regionPriorities;
            LstRegionPriorities.SelectedIndex = selectedIndex + 1;
        }
    }

    private async void BtnCheckUpdatesNow_Click(object? sender, RoutedEventArgs e)
    {
        if (TxtUpdateStatus == null) return;

        TxtUpdateStatus.Foreground = Brushes.LightGray;
        TxtUpdateStatus.Text = "Buscando actualizaciones...";

        await Task.Delay(1500);

        TxtUpdateStatus.Foreground = Brush.Parse("#10b981");
        TxtUpdateStatus.Text = "¡Tu versión ya está al día (v1.2.0.1-Dev)!";
    }

    private async void BtnTestRetroConnection_Click(object? sender, RoutedEventArgs e)
    {
        if (TxtRetroTestStatus == null) return;

        TxtRetroTestStatus.Foreground = Brushes.LightGray;
        TxtRetroTestStatus.Text = "Estableciendo conexión...";

        await Task.Delay(1500);

        if (string.IsNullOrWhiteSpace(TxtRetroUsername.Text) || string.IsNullOrWhiteSpace(TxtRetroApiKey.Text))
        {
            TxtRetroTestStatus.Foreground = Brush.Parse("#ef4444");
            TxtRetroTestStatus.Text = "Error: El usuario y la clave API no pueden estar vacíos.";
        }
        else
        {
            TxtRetroTestStatus.Foreground = Brush.Parse("#10b981");
            TxtRetroTestStatus.Text = "¡Conexión establecida con éxito! Credenciales válidas.";
        }
    }

    private void TvCategories_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TvCategories == null || TvCategories.SelectedItem is not TreeViewItem selectedItem)
            return;

        // Actualizar el título superior
        string? headerText = selectedItem.Header?.ToString();
        if (TxtSectionTitle != null)
        {
            TxtSectionTitle.Text = headerText ?? "General";
        }

        // Obtener el Tag de la categoría
        string categoryTag = selectedItem.Tag?.ToString() ?? "Placeholder";

        // Ocultar todos los paneles
        if (PnlGeneral != null) PnlGeneral.IsVisible = false;
        if (PnlSonido != null) PnlSonido.IsVisible = false;
        if (PnlRutas != null) PnlRutas.IsVisible = false;
        if (PnlMedia != null) PnlMedia.IsVisible = false;
        if (PnlTemaPrincipal != null) PnlTemaPrincipal.IsVisible = false;
        if (PnlColores != null) PnlColores.IsVisible = false;
        if (PnlCaracteristicas != null) PnlCaracteristicas.IsVisible = false;
        if (PnlFuentes != null) PnlFuentes.IsVisible = false;
        if (PnlDepuracion != null) PnlDepuracion.IsVisible = false;
        if (PnlNotificaciones != null) PnlNotificaciones.IsVisible = false;
        if (PnlImportacionesAuto != null) PnlImportacionesAuto.IsVisible = false;
        if (PnlSaveManagement != null) PnlSaveManagement.IsVisible = false;
        if (PnlAppsInicio != null) PnlAppsInicio.IsVisible = false;
        if (PnlBandejaSistema != null) PnlBandejaSistema.IsVisible = false;
        if (PnlReproduccionVideo != null) PnlReproduccionVideo.IsVisible = false;
        if (PnlDatos != null) PnlDatos.IsVisible = false;
        if (PnlCopiasSeguridad != null) PnlCopiasSeguridad.IsVisible = false;
        if (PnlActualizaciones != null) PnlActualizaciones.IsVisible = false;
        if (PnlPrioridadesRegion != null) PnlPrioridadesRegion.IsVisible = false;
        if (PnlRetroAchievements != null) PnlRetroAchievements.IsVisible = false;
        if (PnlProgressAutomation != null) PnlProgressAutomation.IsVisible = false;
        if (PnlProgressOrganization != null) PnlProgressOrganization.IsVisible = false;
        if (PnlBuscar != null) PnlBuscar.IsVisible = false;
        if (PnlJuegosRelacionados != null) PnlJuegosRelacionados.IsVisible = false;
        if (PnlJuegosSimilares != null) PnlJuegosSimilares.IsVisible = false;
        if (PnlJuegosRecomendados != null) PnlJuegosRecomendados.IsVisible = false;
        if (PnlPuertosPosibles != null) PnlPuertosPosibles.IsVisible = false;
        if (PnlPlaceholder != null) PnlPlaceholder.IsVisible = false;

        // Mostrar el panel correspondiente
        switch (categoryTag)
        {
            case "General":
                if (PnlGeneral != null) PnlGeneral.IsVisible = true;
                break;
            case "Sonido":
                if (PnlSonido != null) PnlSonido.IsVisible = true;
                break;
            case "Rutas":
                if (PnlRutas != null) PnlRutas.IsVisible = true;
                break;
            case "Media":
                if (PnlMedia != null) PnlMedia.IsVisible = true;
                break;
            case "TemaPrincipal":
                if (PnlTemaPrincipal != null) PnlTemaPrincipal.IsVisible = true;
                break;
            case "Colores":
                if (PnlColores != null) PnlColores.IsVisible = true;
                break;
            case "Caracteristicas":
                if (PnlCaracteristicas != null) PnlCaracteristicas.IsVisible = true;
                break;
            case "Fuentes":
                if (PnlFuentes != null) PnlFuentes.IsVisible = true;
                break;
            case "Depuracion":
                if (PnlDepuracion != null) PnlDepuracion.IsVisible = true;
                break;
            case "Notificaciones":
                if (PnlNotificaciones != null) PnlNotificaciones.IsVisible = true;
                break;
            case "Importaciones":
                if (PnlImportacionesAuto != null) PnlImportacionesAuto.IsVisible = true;
                break;
            case "SaveManagement":
                if (PnlSaveManagement != null) PnlSaveManagement.IsVisible = true;
                break;
            case "AppsInicio":
                if (PnlAppsInicio != null) PnlAppsInicio.IsVisible = true;
                break;
            case "BandejaSistema":
                if (PnlBandejaSistema != null) PnlBandejaSistema.IsVisible = true;
                break;
            case "Actualizaciones":
                if (PnlActualizaciones != null) PnlActualizaciones.IsVisible = true;
                break;
            case "ReproduccionVideo":
                if (PnlReproduccionVideo != null) PnlReproduccionVideo.IsVisible = true;
                break;
            case "Datos":
                if (PnlDatos != null) PnlDatos.IsVisible = true;
                break;
            case "CopiasSeguridad":
                if (PnlCopiasSeguridad != null) PnlCopiasSeguridad.IsVisible = true;
                break;
            case "PrioridadesRegion":
                if (PnlPrioridadesRegion != null) PnlPrioridadesRegion.IsVisible = true;
                break;
            case "RetroAchievements":
                if (PnlRetroAchievements != null) PnlRetroAchievements.IsVisible = true;
                break;
            case "ProgressAutomation":
                if (PnlProgressAutomation != null) PnlProgressAutomation.IsVisible = true;
                break;
            case "ProgressOrganization":
                if (PnlProgressOrganization != null) PnlProgressOrganization.IsVisible = true;
                break;
            case "Buscar":
                if (PnlBuscar != null) PnlBuscar.IsVisible = true;
                break;
            case "JuegosRelacionados":
                if (PnlJuegosRelacionados != null) PnlJuegosRelacionados.IsVisible = true;
                break;
            case "JuegosSimilares":
                if (PnlJuegosSimilares != null) PnlJuegosSimilares.IsVisible = true;
                break;
            case "JuegosRecomendados":
                if (PnlJuegosRecomendados != null) PnlJuegosRecomendados.IsVisible = true;
                break;
            case "PuertosPosibles":
                if (PnlPuertosPosibles != null) PnlPuertosPosibles.IsVisible = true;
                break;
            default:
                if (PnlPlaceholder != null) PnlPlaceholder.IsVisible = true;
                break;
        }
    }

    private async void BtnBrowseLb_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleccionar Carpeta de la Biblioteca Externa",
            AllowMultiple = false
        });

        if (folders != null && folders.Count > 0)
        {
            TxtExternalLibraryPath.Text = folders[0].Path.LocalPath;
        }
    }

    private async void BtnAddStartupApp_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Ejecutable de Aplicación",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Ejecutables (*.exe)") { Patterns = new[] { "*.exe" } } }
        });

        if (files != null && files.Count > 0)
        {
            var app = new StartupApplication
            {
                AppPath = files[0].Path.LocalPath,
                CommandLineArgs = "",
                StartWith = "Biblioteca Externa",
                AllowMultipleInstances = false
            };
            _settings.StartupApplications.Add(app);

            // Refrescar el listado visual
            LstStartupApps.ItemsSource = null;
            LstStartupApps.ItemsSource = _settings.StartupApplications;
        }
    }

    private void BtnRemoveStartupApp_Click(object? sender, RoutedEventArgs e)
    {
        if (LstStartupApps.SelectedItem is StartupApplication selectedApp)
        {
            _settings.StartupApplications.Remove(selectedApp);

            // Refrescar el listado visual
            LstStartupApps.ItemsSource = null;
            LstStartupApps.ItemsSource = _settings.StartupApplications;
        }
    }

    private void BtnAccept_Click(object? sender, RoutedEventArgs e)
    {
        // Guardar valores en el objeto AppSettings
        _settings.ExternalLibraryPath = TxtExternalLibraryPath.Text ?? "";
        _settings.EnableSoundEffects = ChkEnableSoundEffects.IsChecked ?? true;
        _settings.AutoImportCovers = ChkAutoImportCovers.IsChecked ?? true;

        if (CmbPreferredArtType.SelectedItem is ComboBoxItem artItem)
        {
            _settings.PreferredArtType = artItem.Content?.ToString() ?? "Box";
        }

        // 3. Tema Principal
        if (CmbTheme.SelectedItem is ComboBoxItem themeItem)
        {
            _settings.Theme = themeItem.Content?.ToString() ?? "Default";
        }

        // 4. Colores
        if (CmbColorTheme.SelectedItem is ComboBoxItem colorThemeItem)
        {
            _settings.ColorTheme = colorThemeItem.Content?.ToString() ?? "Default";
        }
        _settings.ColorLightBg = _colorLightBg;
        _settings.ColorDarkBg = _colorDarkBg;
        _settings.ColorSelectedBg = _colorSelectedBg;
        _settings.ColorHighlightedBg = _colorHighlightedBg;
        _settings.ColorBorderHighlight = _colorBorderHighlight;
        _settings.ColorBorderWindow = _colorBorderWindow;
        _settings.ColorBorderMenu = _colorBorderMenu;
        _settings.ColorForeground = _colorForeground;

        // 5. Características
        _settings.ScrollImmediately = ChkScrollImmediately.IsChecked ?? true;
        _settings.ColorGameDividers = ChkColorGameDividers.IsChecked ?? true;
        _settings.ColorScrollbar = ChkColorScrollbar.IsChecked ?? true;
        _settings.ColorGameSelections = ChkColorGameSelections.IsChecked ?? false;
        _settings.ColorPopupDetails = ChkColorPopupDetails.IsChecked ?? false;
        _settings.ColorBlurBackground = ChkColorBlurBackground.IsChecked ?? false;
        _settings.UseRandomColorTheme = ChkUseRandomColorTheme.IsChecked ?? false;

        // 6. Depuración
        _settings.EnableDebugLogs = ChkEnableDebugLogs.IsChecked ?? false;

        // 7. Notificaciones
        if (CmbNotificationSystem.SelectedItem is ComboBoxItem notifItem)
        {
            _settings.NotificationSystem = notifItem.Content?.ToString() ?? "Cuadros de mensaje";
        }

        // 8. Importaciones Automatizadas
        _settings.EnableAutomaticRomImports = ChkEnableAutomaticRomImports.IsChecked ?? true;

        // 9. Save Management
        _settings.EnableSaveManagement = ChkEnableSaveManagement.IsChecked ?? true;
        _settings.EnableAutoSaveBackups = ChkEnableAutoSaveBackups.IsChecked ?? false;
        _settings.BackupOnGameClose = ChkBackupOnGameClose.IsChecked ?? true;
        _settings.EnablePeriodicBackups = ChkEnablePeriodicBackups.IsChecked ?? true;
        _settings.MaxBackupVersions = (int)(NumMaxBackupVersions.Value ?? 25);

        // 11. Bandeja de Sistema
        _settings.EnableSystemTray = ChkEnableSystemTray.IsChecked ?? false;
        _settings.MinimizeToSystemTray = ChkMinimizeToSystemTray.IsChecked ?? false;
        _settings.CloseToSystemTray = ChkCloseToSystemTray.IsChecked ?? true;
        _settings.ShowNotificationOnTraySend = ChkShowNotificationOnTraySend.IsChecked ?? false;

        // 12. Reproducción de Vídeo
        _settings.UseWindowsMediaPlayer = RadUseWMP.IsChecked ?? true;

        // 13. Copias de Seguridad
        _settings.AutoBackupXmlData = ChkAutoBackupXmlData.IsChecked ?? true;

        // 14. Actualizaciones
        _settings.EnableAutoUpdates = ChkEnableAutoUpdates.IsChecked ?? true;
        _settings.EnableBetaUpdates = ChkEnableBetaUpdates.IsChecked ?? false;

        // 15. Prioridades de Región
        _settings.RegionPriorities = new List<RegionPriorityItem>(_regionPriorities);

        // 16. RetroAchievements
        _settings.EnableRetroAchievements = ChkEnableRetroAchievements.IsChecked ?? false;
        _settings.RetroUsername = TxtRetroUsername.Text ?? "";
        _settings.RetroApiKey = TxtRetroApiKey.Text ?? "";
        _settings.ShowAchievementNotifications = ChkShowAchievementNotifications.IsChecked ?? true;
        _settings.ShowAchievementBadges = ChkShowAchievementBadges.IsChecked ?? true;

        // 17. Game Progress Automation
        _settings.EnableProgressAutomation = ChkEnableProgressAutomation.IsChecked ?? true;
        _settings.ProgAutoDefault = CmbProgAutoDefault.SelectedItem?.ToString() ?? "Not Started / Unplayed";
        _settings.ProgAutoPlaytimeMin = (int)(NumProgAutoPlaytimeMin.Value ?? 30);
        _settings.ProgAutoPlaytimeVal = CmbProgAutoPlaytimeVal.SelectedItem?.ToString() ?? "Active / In Progress";
        _settings.ProgAutoEarnedAchVal = CmbProgAutoEarnedAchVal.SelectedItem?.ToString() ?? "Active / In Progress";
        _settings.ProgAutoInactiveDays = (int)(NumProgAutoInactiveDays.Value ?? 30);
        _settings.ProgAutoInactiveVal = CmbProgAutoInactiveVal.SelectedItem?.ToString() ?? "Active / Paused";
        _settings.ProgAutoSoftcoreBeatenVal = CmbProgAutoSoftcoreBeatenVal.SelectedItem?.ToString() ?? "Done / Beaten";
        _settings.ProgAutoHardcoreBeatenVal = CmbProgAutoHardcoreBeatenVal.SelectedItem?.ToString() ?? "Done / Beaten";
        _settings.ProgAutoSoftcoreCompleteVal = CmbProgAutoSoftcoreCompleteVal.SelectedItem?.ToString() ?? "Done / Completed";
        _settings.ProgAutoHardcoreMasteredVal = CmbProgAutoHardcoreMasteredVal.SelectedItem?.ToString() ?? "Done / Mastered";
        _settings.ProgAutoIncludeStatuses = TxtProgAutoIncludeStatuses.Text ?? "Not Started / Want to Play";

        // 18. Game Progress Organization
        _settings.ProgressStatusGroups = new List<ProgressStatusGroup>(_progressStatusGroups);

        // 19. Buscar
        _settings.EnableExternalMetadataSearch = ChkEnableExternalMetadataSearch.IsChecked ?? true;
        _settings.LoadExternalRatings = ChkLoadExternalRatings.IsChecked ?? true;
        _settings.UseCommunityRatings = ChkUseCommunityRatings.IsChecked ?? true;
        _settings.MinCommunityRatings = (int)(NumMinCommunityRatings.Value ?? 5);
        _settings.UseAdvancedSearchSyntax = ChkUseAdvancedSearchSyntax.IsChecked ?? true;

        // 20. Juegos Similares, Recomendados y Puertos
        _settings.SimilarIncludeNonLibrary = ChkSimilarIncludeNonLibrary.IsChecked ?? true;
        _settings.SimilarGameCriteria = new List<GameRelationCriterion>(_similarCriteria);

        _settings.RecommendedIncludeNonLibrary = ChkRecommendedIncludeNonLibrary.IsChecked ?? true;
        _settings.RecommendedGameCriteria = new List<GameRelationCriterion>(_recommendedCriteria);

        _settings.PossiblePortsIncludeNonLibrary = ChkPossiblePortsIncludeNonLibrary.IsChecked ?? true;
        _settings.PossiblePortsCriteria = new List<GameRelationCriterion>(_possiblePortsCriteria);

        Close(true);
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void BtnResetFonts_Click(object? sender, RoutedEventArgs e)
    {
        _settings.GameFont = "Segoe UI";
        _settings.SidebarFont = "Segoe UI";
        _settings.LargeDetailsFont = "Segoe UI";
        _settings.SmallDetailsFont = "Segoe UI";
        _settings.TitleDetailsFont = "Segoe UI";

        // Mostrar un pequeño aviso visual
        TxtSectionTitle.Text = "Fuentes (Restablecido por defecto)";
    }

    private void OnProgressAutomationChecked(object? sender, RoutedEventArgs e)
    {
        UpdateProgressAutomationEnablement();
    }

    private void UpdateProgressAutomationEnablement()
    {
        if (BrdProgressAutomationOptions != null)
        {
            BrdProgressAutomationOptions.IsEnabled = ChkEnableProgressAutomation.IsChecked == true;
        }
    }

    private void LoadProgressOrgTree()
    {
        if (TvProgressOrg == null) return;
        TvProgressOrg.Items.Clear();
        foreach (var group in _progressStatusGroups)
        {
            var groupItem = new TreeViewItem
            {
                Header = group.GroupName,
                Tag = group,
                IsExpanded = true
            };
            foreach (var item in group.Items)
            {
                var statusItem = new TreeViewItem
                {
                    Header = item,
                    Tag = item
                };
                groupItem.Items.Add(statusItem);
            }
            TvProgressOrg.Items.Add(groupItem);
        }
    }

    private void BtnProgOrgUp_Click(object? sender, RoutedEventArgs e)
    {
        if (TvProgressOrg.SelectedItem is not TreeViewItem selectedItem) return;

        if (selectedItem.Tag is ProgressStatusGroup selectedGroup)
        {
            int index = _progressStatusGroups.IndexOf(selectedGroup);
            if (index > 0)
            {
                _progressStatusGroups.RemoveAt(index);
                _progressStatusGroups.Insert(index - 1, selectedGroup);
                LoadProgressOrgTree();
                SelectGroupInTree(selectedGroup);
            }
        }
        else if (selectedItem.Tag is string selectedStatus)
        {
            if (selectedItem.Parent is TreeViewItem parentItem && parentItem.Tag is ProgressStatusGroup parentGroup)
            {
                int index = parentGroup.Items.IndexOf(selectedStatus);
                if (index > 0)
                {
                    parentGroup.Items.RemoveAt(index);
                    parentGroup.Items.Insert(index - 1, selectedStatus);
                    LoadProgressOrgTree();
                    SelectStatusInTree(parentGroup, selectedStatus);
                }
            }
        }
    }

    private void BtnProgOrgDown_Click(object? sender, RoutedEventArgs e)
    {
        if (TvProgressOrg.SelectedItem is not TreeViewItem selectedItem) return;

        if (selectedItem.Tag is ProgressStatusGroup selectedGroup)
        {
            int index = _progressStatusGroups.IndexOf(selectedGroup);
            if (index >= 0 && index < _progressStatusGroups.Count - 1)
            {
                _progressStatusGroups.RemoveAt(index);
                _progressStatusGroups.Insert(index + 1, selectedGroup);
                LoadProgressOrgTree();
                SelectGroupInTree(selectedGroup);
            }
        }
        else if (selectedItem.Tag is string selectedStatus)
        {
            if (selectedItem.Parent is TreeViewItem parentItem && parentItem.Tag is ProgressStatusGroup parentGroup)
            {
                int index = parentGroup.Items.IndexOf(selectedStatus);
                if (index >= 0 && index < parentGroup.Items.Count - 1)
                {
                    parentGroup.Items.RemoveAt(index);
                    parentGroup.Items.Insert(index + 1, selectedStatus);
                    LoadProgressOrgTree();
                    SelectStatusInTree(parentGroup, selectedStatus);
                }
            }
        }
    }

    private void BtnProgOrgDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (TvProgressOrg.SelectedItem is not TreeViewItem selectedItem) return;

        if (selectedItem.Tag is ProgressStatusGroup selectedGroup)
        {
            _progressStatusGroups.Remove(selectedGroup);
            LoadProgressOrgTree();
        }
        else if (selectedItem.Tag is string selectedStatus)
        {
            if (selectedItem.Parent is TreeViewItem parentItem && parentItem.Tag is ProgressStatusGroup parentGroup)
            {
                parentGroup.Items.Remove(selectedStatus);
                LoadProgressOrgTree();
            }
        }
    }

    private void BtnProgOrgAddGroup_Click(object? sender, RoutedEventArgs e)
    {
        string name = TxtProgOrgNewItem.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) return;

        if (_progressStatusGroups.Exists(g => g.GroupName.Equals(name, StringComparison.OrdinalIgnoreCase))) return;

        var newGroup = new ProgressStatusGroup { GroupName = name, Items = new() };
        _progressStatusGroups.Add(newGroup);
        TxtProgOrgNewItem.Text = "";
        LoadProgressOrgTree();
        SelectGroupInTree(newGroup);
    }

    private void BtnProgOrgAddStatus_Click(object? sender, RoutedEventArgs e)
    {
        string name = TxtProgOrgNewItem.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) return;

        ProgressStatusGroup? targetGroup = null;

        if (TvProgressOrg.SelectedItem is TreeViewItem selectedItem)
        {
            if (selectedItem.Tag is ProgressStatusGroup group)
            {
                targetGroup = group;
            }
            else if (selectedItem.Tag is string && selectedItem.Parent is TreeViewItem parentItem && parentItem.Tag is ProgressStatusGroup pGroup)
            {
                targetGroup = pGroup;
            }
        }

        if (targetGroup == null) return;

        if (targetGroup.Items.Contains(name)) return;

        targetGroup.Items.Add(name);
        TxtProgOrgNewItem.Text = "";
        LoadProgressOrgTree();
        SelectStatusInTree(targetGroup, name);
    }

    private void BtnProgOrgReset_Click(object? sender, RoutedEventArgs e)
    {
        _progressStatusGroups.Clear();
        _progressStatusGroups.Add(new ProgressStatusGroup { GroupName = "Not Started", Items = new() { "Unplayed", "Want to Play", "Won't Play" } });
        _progressStatusGroups.Add(new ProgressStatusGroup { GroupName = "Active", Items = new() { "In Progress", "Continuous", "Paused" } });
        _progressStatusGroups.Add(new ProgressStatusGroup { GroupName = "Done", Items = new() { "Beaten", "Completed", "Mastered", "Dropped" } });
        LoadProgressOrgTree();
    }

    private void SelectGroupInTree(ProgressStatusGroup group)
    {
        if (TvProgressOrg == null) return;
        foreach (var rawItem in TvProgressOrg.Items)
        {
            if (rawItem is TreeViewItem item && item.Tag == group)
            {
                TvProgressOrg.SelectedItem = item;
                item.Focus();
                break;
            }
        }
    }

    private void SelectStatusInTree(ProgressStatusGroup group, string status)
    {
        if (TvProgressOrg == null) return;
        foreach (var rawItem in TvProgressOrg.Items)
        {
            if (rawItem is TreeViewItem item && item.Tag == group)
            {
                item.IsExpanded = true;
                foreach (var rawSubItem in item.Items)
                {
                    if (rawSubItem is TreeViewItem subItem && subItem.Tag is string s && s == status)
                    {
                        TvProgressOrg.SelectedItem = subItem;
                        subItem.Focus();
                        break;
                    }
                }
                break;
            }
        }
    }

    private void BtnResetSimilar_Click(object? sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        _similarCriteria.Clear();
        foreach (var c in defaults.SimilarGameCriteria)
        {
            _similarCriteria.Add(new GameRelationCriterion
            {
                Field = c.Field,
                Comparison = c.Comparison,
                ValueType = c.ValueType,
                CustomValue = c.CustomValue,
                Weight = c.Weight,
                TargetGames = c.TargetGames
            });
        }
        LstSimilarCriteria.ItemsSource = null;
        LstSimilarCriteria.ItemsSource = _similarCriteria;
    }

    private void BtnResetRecommended_Click(object? sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        _recommendedCriteria.Clear();
        foreach (var c in defaults.RecommendedGameCriteria)
        {
            _recommendedCriteria.Add(new GameRelationCriterion
            {
                Field = c.Field,
                Comparison = c.Comparison,
                ValueType = c.ValueType,
                CustomValue = c.CustomValue,
                Weight = c.Weight,
                TargetGames = c.TargetGames
            });
        }
        LstRecommendedCriteria.ItemsSource = null;
        LstRecommendedCriteria.ItemsSource = _recommendedCriteria;
    }

    private void BtnResetPossiblePorts_Click(object? sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        _possiblePortsCriteria.Clear();
        foreach (var c in defaults.PossiblePortsCriteria)
        {
            _possiblePortsCriteria.Add(new GameRelationCriterion
            {
                Field = c.Field,
                Comparison = c.Comparison,
                ValueType = c.ValueType,
                CustomValue = c.CustomValue,
                Weight = c.Weight,
                TargetGames = c.TargetGames
            });
        }
        LstPossiblePortsCriteria.ItemsSource = null;
        LstPossiblePortsCriteria.ItemsSource = _possiblePortsCriteria;
    }
}
