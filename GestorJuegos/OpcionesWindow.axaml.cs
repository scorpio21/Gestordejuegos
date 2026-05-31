using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using System;
using System.IO;
using System.Collections.Generic;

namespace GestorJuegos;

public partial class OpcionesWindow : Window
{
    private readonly AppSettings _settings;

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
        TxtColorLightBg.Text = _settings.ColorLightBg;
        TxtColorDarkBg.Text = _settings.ColorDarkBg;
        TxtColorSelectedBg.Text = _settings.ColorSelectedBg;
        TxtColorHighlightedBg.Text = _settings.ColorHighlightedBg;
        TxtColorBorderHighlight.Text = _settings.ColorBorderHighlight;
        TxtColorBorderWindow.Text = _settings.ColorBorderWindow;
        TxtColorBorderMenu.Text = _settings.ColorBorderMenu;
        TxtColorForeground.Text = _settings.ColorForeground;

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
        _settings.ColorLightBg = TxtColorLightBg.Text ?? "#1c1d22";
        _settings.ColorDarkBg = TxtColorDarkBg.Text ?? "#121316";
        _settings.ColorSelectedBg = TxtColorSelectedBg.Text ?? "#3a5180";
        _settings.ColorHighlightedBg = TxtColorHighlightedBg.Text ?? "#2a2b30";
        _settings.ColorBorderHighlight = TxtColorBorderHighlight.Text ?? "#2c2e35";
        _settings.ColorBorderWindow = TxtColorBorderWindow.Text ?? "#5f626a";
        _settings.ColorBorderMenu = TxtColorBorderMenu.Text ?? "#2c2e35";
        _settings.ColorForeground = TxtColorForeground.Text ?? "#ffffff";

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
