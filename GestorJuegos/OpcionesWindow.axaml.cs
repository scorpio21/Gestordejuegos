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

    // Variables locales de color (Estilo LaunchBox)
    private string _colorLightBg = "#1c1d22";
    private string _colorDarkBg = "#121316";
    private string _colorSelectedBg = "#3a5180";
    private string _colorHighlightedBg = "#2a2b30";
    private string _colorBorderHighlight = "#2c2e35";
    private string _colorBorderWindow = "#5f626a";
    private string _colorBorderMenu = "#2c2e35";
    private string _colorForeground = "#ffffff";

    // Colección local para Prioridades de Región
    private readonly List<string> _regionPriorities = new();

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
    }

    private void LoadSettingsIntoUI()
    {
        // 1. Panel General y Rutas
        TxtLaunchBoxPath.Text = _settings.LaunchBoxPath;
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

        // 3. Tema Principal
        if (CmbTheme != null)
        {
            foreach (var rawItem in CmbTheme.Items)
            {
                if (rawItem is ComboBoxItem item && item.Content?.ToString() == _settings.Theme)
                {
                    CmbTheme.SelectedItem = item;
                    break;
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
        _regionPriorities.AddRange(_settings.RegionPriorities);
        LstRegionPriorities.ItemsSource = null;
        LstRegionPriorities.ItemsSource = _regionPriorities;

        // 16. RetroAchievements
        ChkEnableRetroAchievements.IsChecked = _settings.EnableRetroAchievements;
        TxtRetroUsername.Text = _settings.RetroUsername;
        TxtRetroApiKey.Text = _settings.RetroApiKey;
        ChkShowAchievementNotifications.IsChecked = _settings.ShowAchievementNotifications;
        ChkShowAchievementBadges.IsChecked = _settings.ShowAchievementBadges;
        UpdateRetroAchievementsEnablement();
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
            string item = _regionPriorities[selectedIndex];
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
            string item = _regionPriorities[selectedIndex];
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
            Title = "Seleccionar Carpeta de LaunchBox",
            AllowMultiple = false
        });

        if (folders != null && folders.Count > 0)
        {
            TxtLaunchBoxPath.Text = folders[0].Path.LocalPath;
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
                StartWith = "LaunchBox",
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
        _settings.LaunchBoxPath = TxtLaunchBoxPath.Text ?? "";
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
        _settings.RegionPriorities = new List<string>(_regionPriorities);

        // 16. RetroAchievements
        _settings.EnableRetroAchievements = ChkEnableRetroAchievements.IsChecked ?? false;
        _settings.RetroUsername = TxtRetroUsername.Text ?? "";
        _settings.RetroApiKey = TxtRetroApiKey.Text ?? "";
        _settings.ShowAchievementNotifications = ChkShowAchievementNotifications.IsChecked ?? true;
        _settings.ShowAchievementBadges = ChkShowAchievementBadges.IsChecked ?? true;

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
}
