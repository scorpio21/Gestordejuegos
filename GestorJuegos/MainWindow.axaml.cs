using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using GestorJuegos.Utils;

namespace GestorJuegos;

public partial class MainWindow : Window
{
    private readonly GameService _gameService;
    private Platform? _selectedPlatform;
    private string? _selectedCategory;
    private Game? _selectedGame;
    private byte[]? _currentCover;
    private readonly VimmVaultService _vimmService;
    private System.Collections.Generic.List<Game> _currentPlatformGames = new System.Collections.Generic.List<Game>();
    private int _currentPage = 1;
    private const int PageSize = 100;
    private System.Collections.ObjectModel.ObservableCollection<string> _currentRoms = new();
    private System.Threading.CancellationTokenSource? _cts;
    private Action? _onConfirmAction;

    // Variables del nuevo menú horizontal
    private string _currentSortField = "Name";
    private bool _isSortAscending = true;
    private bool _showFavoriteBadge = true;
    private bool _showRegionBadge = true;
    private bool _showStatusBadge = true;

    private List<string> LoadDrossPatterns()
    {
        string drossPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dross_filter.json");
        if (File.Exists(drossPath))
        {
            try
            {
                var json = File.ReadAllText(drossPath);
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch { }
        }
        return new List<string>();
    }

    private AppSettings _settings = new AppSettings();

    private void LoadSettings()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            SoundHelper.IsEnabled = _settings.EnableSoundEffects;
            ApplyTheme();
        }
        catch { _settings = new AppSettings(); }
    }

    private void ApplyTheme()
    {
        try
        {
            // Siempre limpiar/ocultar el fondo al iniciar
            if (ImgThemeBackground != null)
            {
                ImgThemeBackground.IsVisible = false;
                ImgThemeBackground.Source = null;
            }

            string themeName = _settings.Theme;
            // Mapeo automático para mantener compatibilidad con la configuración anterior
            if (themeName == "Neon Deluxe Arcade LB")
            {
                themeName = "Neon Deluxe";
            }

            bool themeLoaded = false;

            if (!string.IsNullOrWhiteSpace(themeName))
            {
                try
                {
                    string themeFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", themeName);
                    if (!Directory.Exists(themeFolder))
                    {
                        themeFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Themes", themeName);
                    }

                    if (Directory.Exists(themeFolder))
                    {
                        string jsonPath = Path.Combine(themeFolder, "theme.json");
                        if (File.Exists(jsonPath))
                        {
                            string jsonText = File.ReadAllText(jsonPath);
                            var themeConfig = System.Text.Json.JsonSerializer.Deserialize<ThemeConfig>(jsonText);
                            if (themeConfig != null)
                            {
                                // 1. Colores
                                if (themeConfig.Colors != null)
                                {
                                    foreach (var colorPair in themeConfig.Colors)
                                    {
                                        if (!string.IsNullOrWhiteSpace(colorPair.Value))
                                        {
                                            var parsedColor = Avalonia.Media.Color.Parse(colorPair.Value);
                                            this.Resources[colorPair.Key] = new Avalonia.Media.SolidColorBrush(parsedColor);
                                            
                                            if (colorPair.Key == "DeepDarkBrush")
                                            {
                                                this.Resources["DeepDarkColor"] = parsedColor;
                                            }
                                        }
                                    }
                                }

                                // 2. Fuentes dinámicas
                                if (themeConfig.Fonts != null)
                                {
                                    foreach (var fontPair in themeConfig.Fonts)
                                    {
                                        string fontFile = fontPair.Value;
                                        if (string.IsNullOrWhiteSpace(fontFile)) continue;

                                        string fontPath = Path.Combine(themeFolder, fontFile);
                                        if (File.Exists(fontPath))
                                        {
                                            try
                                            {
                                                // Crear URI para la fuente (file://...)
                                                var fontUri = new Uri($"file://{fontPath.Replace("\\", "/")}");
                                                var fontFamily = new Avalonia.Media.FontFamily(fontUri, Path.GetFileNameWithoutExtension(fontFile));

                                                // VALIDACIÓN CRÍTICA: Verificar si Avalonia puede crear el GlyphTypeface
                                                var typeface = new Avalonia.Media.Typeface(fontFamily);
                                                if (typeface.GlyphTypeface != null)
                                                {
                                                    this.Resources[fontPair.Key] = fontFamily;
                                                }
                                                else
                                                {
                                                    // Si falla, usar fallback
                                                    this.Resources[fontPair.Key] = Avalonia.Media.FontFamily.Default;
                                                }
                                            }
                                            catch { /* Ignorar fuente inválida */ }
                                        }
                                    }
                                }

                                // 3. Métricas (CornerRadius)
                                if (themeConfig.Metrics != null)
                                {
                                    if (themeConfig.Metrics.TryGetValue("CornerRadius", out string? radiusStr) && double.TryParse(radiusStr, out double radius))
                                    {
                                        this.Resources["ThemeCornerRadius"] = new Avalonia.CornerRadius(radius);
                                    }
                                }

                                // 4. Imagen de fondo
                                if (!string.IsNullOrWhiteSpace(themeConfig.BackgroundImage) && ImgThemeBackground != null)
                                {
                                    string bgPath = Path.Combine(themeFolder, themeConfig.BackgroundImage);
                                    if (File.Exists(bgPath))
                                    {
                                        ImgThemeBackground.Source = new Avalonia.Media.Imaging.Bitmap(bgPath);
                                        ImgThemeBackground.IsVisible = true;
                                    }
                                }

                                // 5. Imagen de Overlay (Vignette/Efectos)
                                if (!string.IsNullOrWhiteSpace(themeConfig.OverlayImage) && ImgThemeOverlay != null)
                                {
                                    string overlayPath = Path.Combine(themeFolder, themeConfig.OverlayImage);
                                    if (File.Exists(overlayPath))
                                    {
                                        ImgThemeOverlay.Source = new Avalonia.Media.Imaging.Bitmap(overlayPath);
                                        ImgThemeOverlay.IsVisible = true;
                                    }
                                }

                                // 6. Logo de la Aplicación (Personalizado por tema)
                                if (ImgAppLogo != null)
                                {
                                    string logoPath = Path.Combine(themeFolder, "Images", "Logo.png");
                                    if (File.Exists(logoPath))
                                    {
                                        ImgAppLogo.Source = new Avalonia.Media.Imaging.Bitmap(logoPath);
                                        ImgAppLogo.IsVisible = true;
                                    }
                                    else
                                    {
                                        // Ocultar si el tema no tiene logo personalizado
                                        ImgAppLogo.IsVisible = false;
                                    }
                                }

                                // 7. Vista Preferida (Grid vs List vs Wheel)
                                if (!string.IsNullOrEmpty(themeConfig.PreferredView))
                                {
                                    if (themeConfig.PreferredView.Equals("List", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (BtnViewList != null) BtnViewList_Click(null, new RoutedEventArgs());
                                    }
                                    else if (themeConfig.PreferredView.Equals("Wheel", StringComparison.OrdinalIgnoreCase) || 
                                             themeConfig.PreferredView.Equals("VerticalWheel", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (BtnViewWheelVertical != null) BtnViewWheelVertical_Click(null, new RoutedEventArgs());
                                    }
                                    else if (themeConfig.PreferredView.Equals("HorizontalWheel", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal_Click(null, new RoutedEventArgs());
                                    }
                                    else
                                    {
                                        if (BtnViewGrid != null) BtnViewGrid_Click(null, new RoutedEventArgs());
                                    }
                                }

                                // 8. Carga Dinámica de Efectos Hover
                                if (themeConfig != null)
                                {
                                    // Determinar el color del hover (si no está definido en el tema, se usa el color de acento)
                                    if (!themeConfig.Colors.TryGetValue("HoverBorderBrush", out string? hoverColorStr))
                                    {
                                        this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
                                    }
                                    else
                                    {
                                        this.Resources["HoverBorderBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(hoverColorStr));
                                    }

                                    // Determinar el desenfoque de brillo hover (HoverGlowBlur) desde Metrics
                                    double glowBlur = 12; // Valor por defecto
                                    if (themeConfig.Metrics != null && themeConfig.Metrics.TryGetValue("HoverGlowBlur", out string? glowStr) && double.TryParse(glowStr, out double gb))
                                    {
                                        glowBlur = gb;
                                    }

                                    var hoverBrush = (Avalonia.Media.SolidColorBrush)this.Resources["HoverBorderBrush"];
                                    if (glowBlur > 0)
                                    {
                                        this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                                        {
                                            Blur = glowBlur,
                                            Spread = 2,
                                            Color = hoverBrush.Color,
                                            OffsetY = 0
                                        });
                                    }
                                    else
                                    {
                                        // Brillo desactivado (sombra transparente)
                                        this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                                        {
                                            Blur = 0,
                                            Spread = 0,
                                            Color = Avalonia.Media.Colors.Transparent,
                                            OffsetY = 0
                                        });
                                    }
                                }

                                themeLoaded = true;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignorar errores individuales para no bloquear el arranque y usar fallbacks
                }
            }

            if (!themeLoaded)
            {
                // Limpiar imagen de overlay si no hay tema
                if (ImgThemeOverlay != null) ImgThemeOverlay.IsVisible = false;

                // Ocultar Logo si no hay tema cargado
                if (ImgAppLogo != null) ImgAppLogo.IsVisible = false;

                // Limpiar recursos si no se cargó tema
                this.Resources["MainFont"] = Avalonia.Media.FontFamily.Default;
                this.Resources["HeaderFont"] = Avalonia.Media.FontFamily.Default;
                this.Resources["ThemeCornerRadius"] = new Avalonia.CornerRadius(8);

                if (themeName == "Old Default")
                {
                    var accentColor = Avalonia.Media.Color.Parse("#3b82f6");
                    var deepDarkColor = Avalonia.Media.Color.Parse("#111827");
                    var panelColor = Avalonia.Media.Color.Parse("#1f2937");
                    var borderColor = Avalonia.Media.Color.Parse("#374151");

                    this.Resources["AccentBrush"] = new Avalonia.Media.SolidColorBrush(accentColor);
                    this.Resources["DeepDarkBrush"] = new Avalonia.Media.SolidColorBrush(deepDarkColor);
                    this.Resources["DeepDarkColor"] = deepDarkColor;
                    this.Resources["PanelBrush"] = new Avalonia.Media.SolidColorBrush(panelColor);
                    this.Resources["BorderBrush"] = new Avalonia.Media.SolidColorBrush(borderColor);

                    // Fallback para Hover
                    this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
                    this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                    {
                        Blur = 12,
                        Spread = 2,
                        Color = accentColor,
                        OffsetY = 0
                    });
                }
                else
                {
                    // Tema por defecto o personalizado
                    if (_settings.ColorTheme == "Personalizado")
                    {
                        var accentColor = Avalonia.Media.Color.Parse(_settings.ColorSelectedBg);
                        var deepDarkColor = Avalonia.Media.Color.Parse(_settings.ColorDarkBg);
                        var panelColor = Avalonia.Media.Color.Parse(_settings.ColorLightBg);
                        var borderColor = Avalonia.Media.Color.Parse(_settings.ColorBorderWindow);

                        this.Resources["AccentBrush"] = new Avalonia.Media.SolidColorBrush(accentColor);
                        this.Resources["DeepDarkBrush"] = new Avalonia.Media.SolidColorBrush(deepDarkColor);
                        this.Resources["DeepDarkColor"] = deepDarkColor;
                        this.Resources["PanelBrush"] = new Avalonia.Media.SolidColorBrush(panelColor);
                        this.Resources["BorderBrush"] = new Avalonia.Media.SolidColorBrush(borderColor);

                        // Fallback para Hover personalizado
                        this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
                        this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                        {
                            Blur = 12,
                            Spread = 2,
                            Color = accentColor,
                            OffsetY = 0
                        });
                    }
                    else
                    {
                        var accentColor = Avalonia.Media.Color.Parse("#10b981");
                        var deepDarkColor = Avalonia.Media.Color.Parse("#0f172a");
                        var panelColor = Avalonia.Media.Color.Parse("#1e293b");
                        var borderColor = Avalonia.Media.Color.Parse("#334155");

                        this.Resources["AccentBrush"] = new Avalonia.Media.SolidColorBrush(accentColor);
                        this.Resources["DeepDarkBrush"] = new Avalonia.Media.SolidColorBrush(deepDarkColor);
                        this.Resources["DeepDarkColor"] = deepDarkColor;
                        this.Resources["PanelBrush"] = new Avalonia.Media.SolidColorBrush(panelColor);
                        this.Resources["BorderBrush"] = new Avalonia.Media.SolidColorBrush(borderColor);

                        // Fallback para Hover por defecto
                        this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
                        this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                        {
                            Blur = 12,
                            Spread = 2,
                            Color = accentColor,
                            OffsetY = 0
                        });
                    }
                }
            }
        }
        catch
        {
            // Fallback robusto en caso de error crítico
            var accentColor = Avalonia.Media.Color.Parse("#10b981");
            var deepDarkColor = Avalonia.Media.Color.Parse("#0f172a");
            var panelColor = Avalonia.Media.Color.Parse("#1e293b");
            var borderColor = Avalonia.Media.Color.Parse("#334155");

            this.Resources["AccentBrush"] = new Avalonia.Media.SolidColorBrush(accentColor);
            this.Resources["DeepDarkBrush"] = new Avalonia.Media.SolidColorBrush(deepDarkColor);
            this.Resources["DeepDarkColor"] = deepDarkColor;
            this.Resources["PanelBrush"] = new Avalonia.Media.SolidColorBrush(panelColor);
            this.Resources["BorderBrush"] = new Avalonia.Media.SolidColorBrush(borderColor);

            this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
            this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
            {
                Blur = 12,
                Spread = 2,
                Color = accentColor,
                OffsetY = 0
            });
        }
    }

    private class ThemeConfig
    {
        public Dictionary<string, string> Colors { get; set; } = new();
        public Dictionary<string, string> Fonts { get; set; } = new();
        public Dictionary<string, string> Metrics { get; set; } = new();
        public string BackgroundImage { get; set; } = "";
        public string OverlayImage { get; set; } = "";
        public string PreferredView { get; set; } = "Grid"; // Grid, List, Wheel, HorizontalWheel
    }


    private void SaveSettings()
    {
        try
        {
            SoundHelper.IsEnabled = _settings.EnableSoundEffects;
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(configPath, json);
        }
        catch { }
    }

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();

        // Inicialización de Base de Datos con la nueva estructura 100% Biblioteca Externa
        try
        {
            using (var context = new GestorJuegos.Data.AppDbContext())
            {
                context.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al inicializar BD: {ex.Message}");
        }

        _gamepadTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gamepadTimer.Tick += GamepadTimer_Tick;
        _gamepadTimer.Start();
        
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            Title = $"Gestor de Juegos v1.1.0.2";
        }

        _gameService = new GameService();
        _vimmService = new VimmVaultService();

        LoadPlatforms();
        ImportExternalAssets();
        LoadDashboard();

        AddHandler(DragDrop.DropEvent, Window_Drop);

        BtnAddGame.Click += BtnAddGame_Click;
        BtnEditGame.Click += BtnEditGame_Click;
        BtnCancelEditGame.Click += BtnCancelEditGame_Click;
        BtnSave.Click += BtnSave_Click;
        BtnDelete.Click += BtnDelete_Click;
        BtnSelectCover.Click += BtnSelectCover_Click;
        BtnClearCover.Click += BtnClearCover_Click;
        CmbEditArtType.SelectionChanged += CmbEditArtType_SelectionChanged;
        
        BtnCancelProgress.Click += (s, e) => _cts?.Cancel();

        LstGames.SelectionChanged += LstGames_SelectionChanged;
        LstGamesGrid.SelectionChanged += LstGames_SelectionChanged;
        LstGamesWheelVertical.SelectionChanged += LstGames_SelectionChanged;
        LstGamesWheelHorizontal.SelectionChanged += LstGames_SelectionChanged;

        // Registrar eventos de renderizado/layout para actualizar la curvatura
        LstGamesWheelVertical.EffectiveViewportChanged += (s, e) => UpdateWheelCurvature();
        LstGamesWheelVertical.LayoutUpdated += (s, e) => UpdateWheelCurvature();
        LstGamesWheelHorizontal.EffectiveViewportChanged += (s, e) => UpdateHorizontalWheelCurvature();
        LstGamesWheelHorizontal.LayoutUpdated += (s, e) => UpdateHorizontalWheelCurvature();
        
        LstPlatformsWall.SelectionChanged += LstPlatformsWall_SelectionChanged;
        TvSidebar.SelectionChanged += TvSidebar_SelectionChanged;
        CmbSidebarView.SelectionChanged += CmbSidebarView_SelectionChanged;

        BtnShowPlatformsWall.Click += (s, e) => LoadPlatformsWall();

        BtnClosePlatformsWall.Click += (s, e) => { GestorJuegos.Utils.SoundHelper.PlayBack(); OverlayPlatformsWall.IsVisible = false; };
        
        BtnShowStats.Click += (s, e) => { GestorJuegos.Utils.SoundHelper.PlaySelect(); ShowFullStats(); };
        BtnCloseFullStats.Click += (s, e) => { GestorJuegos.Utils.SoundHelper.PlayBack(); OverlayFullStats.IsVisible = false; };
        
        BtnViewList.Click += BtnViewList_Click;
        BtnViewGrid.Click += BtnViewGrid_Click;
        BtnPrevPage.Click += BtnPrevPage_Click;
        BtnNextPage.Click += BtnNextPage_Click;

        BtnAddPlatform.Click += BtnAddPlatform_Click;
        BtnCancelPlatform.Click += BtnCancelPlatform_Click;
        BtnSavePlatform.Click += BtnSavePlatform_Click;
        
        BtnManagePlatforms.Click += BtnManagePlatforms_Click;
        BtnCloseManagePlatforms.Click += (s, e) => { GestorJuegos.Utils.SoundHelper.PlayBack(); OverlayManagePlatforms.IsVisible = false; };
        BtnSaveEditPlatform.Click += BtnSaveEditPlatform_Click;
        BtnDeletePlatform.Click += BtnDeletePlatform_Click;
        LstManagePlatforms.SelectionChanged += LstManagePlatforms_SelectionChanged;
        BtnSelectEmulator.Click += BtnSelectEmulator_Click;
        BtnEditPlatformQuick.Click += BtnEditPlatformQuick_Click;
        BtnClosePlatformDetails.Click += BtnClosePlatformDetails_Click;

        BtnOpenFolder.Click += (s, e) => {
            if (_selectedGame != null && !string.IsNullOrEmpty(_selectedGame.RomPath))
            {
                string? dir = Path.GetDirectoryName(_selectedGame.RomPath);
                if (Directory.Exists(dir)) Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
            }
        };

        UpdateMenuCheckmarks();
        BtnToggleFilters.Click += BtnToggleFilters_Click;
        BtnApplyFilters.Click += BtnApplyFilters_Click;
        BtnClearFilters.Click += BtnClearFilters_Click;
    }

    private string GetExternalFolderName(string friendlyName)
    {
        return friendlyName switch
        {
            "Advert" => "Advert",
            "Artwork Preview" => "Artwork_Preview",
            "Background" => "Background",
            "Box" => "Box",
            "Box 3D" => "Box_3D",
            "Box Full" => "Box_Full",
            "Box - Back" => "Box_Back",
            "Box - Spine" => "Box_Spine",
            "Cart - Front" => "Cart_Front",
            "Cart - 3D" => "Cart_3D",
            "Cart - Back" => "Cart_Back",
            "Support" => "Disc",
            "Cabinet" => "Cabinet",
            "Logos" => "Logos",
            "Marquee" => "Marquee",
            "Snap" => "Snap",
            "System Logo" => "System_Logo",
            "Title" => "Title",
            "Fanart" => "Fanart - Background",
            "Clear Logo" => "Clear Logo",
            _ => "Box" // Por defecto
        };
    }

    private bool _isSelectingGame = false;
    private void LoadArtTypeImage(string friendlyName)
    {
        if (_selectedGame == null) return;
        
        string dbTypeName = GetExternalFolderName(friendlyName);
        LogDebug($"Buscando arte en DB: {friendlyName} (Tipo: {dbTypeName})");

        // Efecto Fade Out
        ImgCover.Opacity = 0;
        ImgEditCover.Opacity = 0;

        // 1. Intentar cargar el tipo específico de la base de datos (extra images)
        byte[]? dbImage = _gameService.GetGameExtraImage(_selectedGame.Id, dbTypeName);

        if (dbImage != null && dbImage.Length > 0)
        {
            _currentCover = dbImage;
        }
        else
        {
            // Sin Fallback: Si se elige un tipo específico y no existe, mostrar vacío
            LogDebug($"Tipo {friendlyName} no encontrado en DB.");
            _currentCover = null;
        }
        UpdateCoverImage();
        // Fade In
        ImgCover.Opacity = 1;
        ImgEditCover.Opacity = 1;
    }

    private void CmbArtType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null || _isSelectingGame) return;
        
        string friendlyName = (CmbArtType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Box";
        
        // Solo actualizar y guardar si el tipo ha cambiado realmente
        if (_selectedGame.SelectedArtType != friendlyName)
        {
            _selectedGame.SelectedArtType = friendlyName;
            _gameService.UpdateGameMetadata(_selectedGame); // Persistir en DB principal
            
            // ACTUALIZAR MINIATURA EN EL LISTADO CENTRAL EN TIEMPO REAL
            string artFolder = GetExternalFolderName(friendlyName);
            _selectedGame.Cover = _gameService.GetGameThumbnail(_selectedGame.Id, artFolder);
        }
        
        LoadArtTypeImage(friendlyName);
    }

    private void CmbEditArtType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_selectedGame == null || _isSelectingGame) return;

        string friendlyName = (CmbEditArtType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Box 3D";
        string artFolderName = GetExternalFolderName(friendlyName);

        // Cargar imagen de la DB para previsualización (independiente de _currentCover)
        byte[]? dbImage = _gameService.GetGameExtraImage(_selectedGame.Id, artFolderName);
        
        if (dbImage != null && dbImage.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(dbImage);
                ImgEditCover.Source = new Bitmap(ms);
            }
            catch { ImgEditCover.Source = null; }
        }
        else
        {
            // Si no existe, intentar la carátula principal como fallback visual pero NO actualizar _currentCover
            byte[]? mainCover = _gameService.GetGameFullCover(_selectedGame.Id);
            if (mainCover != null)
            {
                try
                {
                    using var ms = new MemoryStream(mainCover);
                    ImgEditCover.Source = new Bitmap(ms);
                }
                catch { ImgEditCover.Source = null; }
            }
            else { ImgEditCover.Source = null; }
        }
    }

    private void LstGlobalSearchResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstGlobalSearchResults.SelectedItem is Game game)
        {
            // Al seleccionar un juego de la búsqueda global, cargamos su plataforma
            _selectedPlatform = game.Platform;
            PnlGlobalSearch.IsVisible = false;
            PnlDashboard.IsVisible = false;
            // PnlHeaderToggles.IsVisible = true; (Eliminado)
            PnlPagination.IsVisible = true;
            
            TxtSelectedPlatform.Text = $"Plataforma: {_selectedPlatform.Name}";
            
            // Forzar vista de lista para mostrar la selección
            BtnViewList_Click(null, new RoutedEventArgs());
            
            LoadGames();
            
            // Buscar el juego en la lista cargada y seleccionarlo
            var loadedGame = _currentPlatformGames.FirstOrDefault(g => g.Id == game.Id);
            if (loadedGame != null)
            {
                // Calcular en qué página está (opcional, por ahora lo seleccionamos si está en la 1)
                LstGames.SelectedItem = loadedGame;
                LstGames.ScrollIntoView(loadedGame);
            }
        }
    }

    private async void BtnConfirmVimm_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        if (CmbVimmSystem.SelectedItem is KeyValuePair<string, string> selected)
        {
            // Validar existencia rápida antes de empezar el lote
            BtnConfirmVimm.IsEnabled = false;
            var testGame = _currentPlatformGames.FirstOrDefault()?.Name ?? "Sonic";
            var testResult = await _vimmService.FindGameIdAsync(selected.Value, testGame);
            
            if (testResult == null)
            {
                // Si falla el primero, probamos con una búsqueda genérica para confirmar si el sistema existe
                var checkSystem = await _vimmService.SearchGamesAsync(selected.Value, "A");
                if (checkSystem.Count == 0)
                {
                    ShowMessage($"El sistema '{selected.Key}' no parece devolver resultados en Vimm's Lair.\nVerifique la selección.");
                    BtnConfirmVimm.IsEnabled = true;
                    return;
                }
            }

            OverlayVimmSystem.IsVisible = false;
            BtnConfirmVimm.IsEnabled = true;
            StartVimmBatchScrape(selected.Value);
        }
    }

    private async void StartVimmBatchScrape(string systemCode)
    {
        if (_selectedPlatform == null) return;

        var gamesWithoutCover = _gameService.GetGamesByPlatform(_selectedPlatform.Id)
                                            .Where(g => g.Cover == null || g.Cover.Length == 0)
                                            .ToList();

        if (gamesWithoutCover.Count == 0)
        {
            ShowMessage("Todos los juegos de esta plataforma ya tienen carátula.");
            return;
        }

        ShowMessage($"Iniciando descarga desde Vimm's Lair ({systemCode}) para {gamesWithoutCover.Count} juegos...");
        BtnCloseMessage.IsEnabled = false;

        await System.Threading.Tasks.Task.Run(async () =>
        {
            int successCount = 0;
            for (int i = 0; i < gamesWithoutCover.Count; i++)
            {
                var game = gamesWithoutCover[i];
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    TxtMessageContent.Text = $"[Vimm] Descargando '{game.Name}' ({i + 1}/{gamesWithoutCover.Count})...";
                });

                try
                {
                    var vimmId = await _vimmService.FindGameIdAsync(systemCode, game.Name, game.Region, game.Languages);
                    if (vimmId.HasValue)
                    {
                        var coverData = await _vimmService.DownloadBoxArtAsync(vimmId.Value);
                        if (coverData != null && coverData.Length > 0)
                        {
                            game.Cover = coverData;
                            using (var context = new GestorJuegos.Data.AppDbContext())
                            {
                                context.Games.Update(game);
                                context.SaveChanges();
                            }
                            successCount++;
                        }
                    }
                    await System.Threading.Tasks.Task.Delay(500); // Respetar servidor
                }
                catch { }
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                BtnCloseMessage.IsEnabled = true;
                LoadGames();
                ShowMessage($"¡Proceso Vimm completado! Se descargaron {successCount} carátulas.");
            });
        });
    }

    private string[][] _keyboardLayout = new string[][]
    {
        new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" },
        new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", "-" },
        new string[] { "Z", "X", "C", "V", "B", "N", "M", "ESP", "DEL", "OK" }
    };
    
    private int _kbdX = 0;
    private int _kbdY = 0;
    private Avalonia.Controls.Border[,] _keyboardBorders = new Avalonia.Controls.Border[4, 10];
    private bool _gamepadInHeader = false;

    private void InitVirtualKeyboard()
    {
        GridKeyboard.RowDefinitions.Clear();
        GridKeyboard.ColumnDefinitions.Clear();
        for (int i = 0; i < 4; i++) GridKeyboard.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(Avalonia.Controls.GridLength.Auto));
        for (int i = 0; i < 10; i++) GridKeyboard.ColumnDefinitions.Add(new Avalonia.Controls.ColumnDefinition(Avalonia.Controls.GridLength.Auto));

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                var border = new Avalonia.Controls.Border
                {
                    Background = Avalonia.Media.Brush.Parse("#1e293b"),
                    CornerRadius = new Avalonia.CornerRadius(4),
                    Margin = new Avalonia.Thickness(4),
                    Padding = new Avalonia.Thickness(20, 15),
                    Child = new Avalonia.Controls.TextBlock 
                    { 
                        Text = _keyboardLayout[y][x], 
                        Foreground = Avalonia.Media.Brushes.White,
                        FontSize = 20,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    }
                };
                Avalonia.Controls.Grid.SetRow(border, y);
                Avalonia.Controls.Grid.SetColumn(border, x);
                GridKeyboard.Children.Add(border);
                _keyboardBorders[y, x] = border;
            }
        }
    }

    private void UpdateKeyboardHighlight()
    {
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                _keyboardBorders[y, x].Background = (x == _kbdX && y == _kbdY) 
                    ? Avalonia.Media.Brush.Parse("#10b981") 
                    : Avalonia.Media.Brush.Parse("#1e293b");
            }
        }
    }

    private void BtnToggleGamepad_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (BtnToggleGamepad.IsChecked == true)
        {
            BtnToggleGamepad.Content = "🎮 Mando: ON";
            BtnToggleGamepad.Foreground = Avalonia.Media.Brush.Parse("#10b981");
            _gamepadTimer?.Start();
        }
        else
        {
            BtnToggleGamepad.Content = "🎮 Mando: OFF";
            BtnToggleGamepad.Foreground = Avalonia.Media.Brush.Parse("#ef4444");
            _gamepadTimer?.Stop();
        }
    }

    private int _gamepadRepeatDelay = 0;
    private Avalonia.Threading.DispatcherTimer? _gamepadTimer;
    private Vortice.XInput.GamepadButtons _previousGamepadButtons;

    private void SimulateKey(Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers = Avalonia.Input.KeyModifiers.None)
    {
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
        var focusedElement = topLevel?.FocusManager?.GetFocusedElement() as Avalonia.Controls.Control;
        if (focusedElement == null) focusedElement = this;

        var e = new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers,
            Source = focusedElement
        };
        focusedElement.RaiseEvent(e);
        
        var eUp = new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyUpEvent,
            Key = key,
            KeyModifiers = modifiers,
            Source = focusedElement
        };
        focusedElement.RaiseEvent(eUp);
    }

    private void GamepadTimer_Tick(object? sender, EventArgs e)
    {
        if (Vortice.XInput.XInput.GetState(0, out var state))
        {
            var buttons = state.Gamepad.Buttons;
            
            if (state.Gamepad.LeftThumbY > 16000) buttons |= Vortice.XInput.GamepadButtons.DPadUp;
            if (state.Gamepad.LeftThumbY < -16000) buttons |= Vortice.XInput.GamepadButtons.DPadDown;
            if (state.Gamepad.LeftThumbX > 16000) buttons |= Vortice.XInput.GamepadButtons.DPadRight;
            if (state.Gamepad.LeftThumbX < -16000) buttons |= Vortice.XInput.GamepadButtons.DPadLeft;

            var pressedButtons = buttons & ~_previousGamepadButtons;

            if (pressedButtons != Vortice.XInput.GamepadButtons.None)
            {
                _gamepadRepeatDelay = 25; // Initial delay before repeating
                HandleGamepadInput(pressedButtons);
            }
            else if (buttons != Vortice.XInput.GamepadButtons.None)
            {
                var dirButtons = buttons & (Vortice.XInput.GamepadButtons.DPadUp | Vortice.XInput.GamepadButtons.DPadDown | Vortice.XInput.GamepadButtons.DPadLeft | Vortice.XInput.GamepadButtons.DPadRight);
                if (dirButtons != Vortice.XInput.GamepadButtons.None)
                {
                    if (_gamepadRepeatDelay > 0)
                        _gamepadRepeatDelay--;
                    else
                    {
                        _gamepadRepeatDelay = 6; // slightly slower repeat speed (100ms)
                        HandleGamepadInput(dirButtons);
                    }
                }
            }
            
            _previousGamepadButtons = buttons;
        }
    }

    private void HandleGamepadInput(Vortice.XInput.GamepadButtons buttons)
    {
        if (OverlayKeyboard.IsVisible)
        {
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadUp)) { _kbdY--; if (_kbdY < 0) _kbdY = 3; UpdateKeyboardHighlight(); }
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadDown)) { _kbdY++; if (_kbdY > 3) _kbdY = 0; UpdateKeyboardHighlight(); }
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadLeft)) { _kbdX--; if (_kbdX < 0) _kbdX = 9; UpdateKeyboardHighlight(); }
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadRight)) { _kbdX++; if (_kbdX > 9) _kbdX = 0; UpdateKeyboardHighlight(); }
            
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.A))
            {
                string key = _keyboardLayout[_kbdY][_kbdX];
                if (key == "ESP") TxtKeyboardInput.Text += " ";
                else if (key == "DEL") 
                {
                    if (TxtKeyboardInput.Text?.Length > 0)
                        TxtKeyboardInput.Text = TxtKeyboardInput.Text.Substring(0, TxtKeyboardInput.Text.Length - 1);
                }
                else if (key == "OK")
                {
                    TxtSearchGame.Text = TxtKeyboardInput.Text;
                    OverlayKeyboard.IsVisible = false;
                }
                else
                {
                    TxtKeyboardInput.Text += key;
                }
            }
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.B))
            {
                OverlayKeyboard.IsVisible = false;
            }
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.Start))
            {
                TxtSearchGame.Text = TxtKeyboardInput.Text;
                OverlayKeyboard.IsVisible = false;
            }
            return;
        }

        if (buttons.HasFlag(Vortice.XInput.GamepadButtons.X))
        {
            BtnQuickFavorite.IsChecked = !BtnQuickFavorite.IsChecked;
            BtnQuickFavorite_Click(null, new RoutedEventArgs());
            return;
        }
        if (buttons.HasFlag(Vortice.XInput.GamepadButtons.Y))
        {
            BtnToggleFilters_Click(null, new RoutedEventArgs());
            return;
        }

        if (buttons.HasFlag(Vortice.XInput.GamepadButtons.A))
        {
            if (OverlayMessage.IsVisible)
            {
                OverlayMessage.IsVisible = false;
                return;
            }
            if (OverlayIgdbSearch.IsVisible)
            {
                BtnSelectIgdb_Click(null, new RoutedEventArgs());
                return;
            }
            if (_gamepadInHeader)
            {
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
                var focusedElement = topLevel?.FocusManager?.GetFocusedElement() as Avalonia.Controls.Control;

                if (focusedElement == TxtSearchGame)
                {
                    _kbdX = 0; _kbdY = 0;
                    TxtKeyboardInput.Text = TxtSearchGame.Text;
                    UpdateKeyboardHighlight();
                    OverlayKeyboard.IsVisible = true;
                }
                else if (focusedElement is Avalonia.Controls.Primitives.ToggleButton tBtn)
                {
                    tBtn.IsChecked = (tBtn.IsChecked != true);
                    tBtn.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                }
                else if (focusedElement is Avalonia.Controls.Button btn)
                {
                    btn.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                }
                else if (focusedElement is Avalonia.Controls.MenuItem mi)
                {
                    topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Down);
                }
                else if (focusedElement is Avalonia.Controls.ComboBox cb)
                {
                    cb.IsDropDownOpen = !cb.IsDropDownOpen;
                }
                return;
            }

            if (PnlGameDetails.IsVisible && _selectedGame != null && BtnLaunchGame.IsVisible)
            {
                BtnLaunchGame_Click(null, new RoutedEventArgs());
            }
            return;
        }

        if (buttons.HasFlag(Vortice.XInput.GamepadButtons.B))
        {
            if (OverlayMessage.IsVisible)
            {
                OverlayMessage.IsVisible = false;
                return;
            }
            if (OverlayIgdbSearch.IsVisible)
            {
                OverlayIgdbSearch.IsVisible = false;
                return;
            }
            if (OverlayAddPlatform.IsVisible)
            {
                OverlayAddPlatform.IsVisible = false;
                return;
            }
            if (OverlayManagePlatforms.IsVisible)
            {
                OverlayManagePlatforms.IsVisible = false;
                return;
            }

            if (_gamepadInHeader)
            {
                var topLvl = Avalonia.Controls.TopLevel.GetTopLevel(this);
                var fElement = topLvl?.FocusManager?.GetFocusedElement() as Avalonia.Controls.Control;
                if (fElement is Avalonia.Controls.ComboBox cb && cb.IsDropDownOpen)
                {
                    cb.IsDropDownOpen = false;
                    return;
                }

                _gamepadInHeader = false;
                Avalonia.Controls.ListBox? aList = LstGames.IsVisible ? LstGames : (LstGamesGrid.IsVisible ? LstGamesGrid : null);
                if (aList != null && aList.ItemCount > 0)
                {
                    aList.Focus();
                }
                return;
            }

            if (PnlGameDetails.IsVisible)
            {
                PnlGameDetails.IsVisible = false;
                LstGames.SelectedItem = null;
                LstGamesGrid.SelectedItem = null;
                _selectedGame = null;
            }
            return;
        }

        if (buttons.HasFlag(Vortice.XInput.GamepadButtons.LeftShoulder))
        {
            if (BtnPrevPage.IsVisible) BtnPrevPage_Click(null, new RoutedEventArgs());
        }
        else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.RightShoulder))
        {
            if (BtnNextPage.IsVisible) BtnNextPage_Click(null, new RoutedEventArgs());
        }

        Avalonia.Controls.ListBox? activeList = LstGames.IsVisible ? LstGames : (LstGamesGrid.IsVisible ? LstGamesGrid : null);
        
        if (_gamepadInHeader)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement() as Avalonia.Controls.Control;

            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadDown))
            {
                if (focusedElement is Avalonia.Controls.MenuItem)
                {
                    topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Down);
                }
                else if (focusedElement is Avalonia.Controls.ComboBox cb && cb.IsDropDownOpen)
                {
                    // Simulate Down key to move in popup list
                    SimulateKey(Avalonia.Input.Key.Down);
                }
                else if (focusedElement is Avalonia.Controls.NumericUpDown num)
                {
                    SimulateKey(Avalonia.Input.Key.Down);
                }
                else
                {
                    _gamepadInHeader = false;
                    if (activeList != null && activeList.ItemCount > 0)
                    {
                        activeList.Focus();
                        activeList.SelectedIndex = 0;
                        var item = activeList.Items.Cast<object>().ElementAtOrDefault(0);
                        if (item != null) activeList.ScrollIntoView(item);
                    }
                }
            }
            else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadUp))
            {
                if (focusedElement is Avalonia.Controls.ComboBox cb && cb.IsDropDownOpen)
                {
                    SimulateKey(Avalonia.Input.Key.Up);
                }
                else if (focusedElement is Avalonia.Controls.NumericUpDown num)
                {
                    SimulateKey(Avalonia.Input.Key.Up);
                }
                else
                {
                    topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Up);
                }
            }
            else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadRight))
            {
                if (focusedElement is Avalonia.Controls.MenuItem) topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Right);
                else topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Next);
            }
            else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadLeft))
            {
                if (focusedElement is Avalonia.Controls.MenuItem) topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Left);
                else topLevel?.FocusManager?.TryMoveFocus(Avalonia.Input.NavigationDirection.Previous);
            }

            return;
        }

        if (activeList != null && activeList.ItemCount > 0)
        {
            int maxIndex = activeList.ItemCount - 1;
            int currentIndex = activeList.SelectedIndex;
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = currentIndex;

            if (activeList == LstGames)
            {
                if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadDown)) newIndex++;
                else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadUp)) newIndex--;
            }
            else
            {
                int cols = Math.Max(1, (int)(LstGamesGrid.Bounds.Width / 160));
                if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadRight)) newIndex++;
                else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadLeft)) newIndex--;
                else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadDown)) newIndex += cols;
                else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadUp)) newIndex -= cols;
            }

            if (newIndex < 0 && !_gamepadInHeader)
            {
                // Move focus to Header (Search)
                _gamepadInHeader = true;
                TxtSearchGame.Focus(); // Set native focus
                activeList.SelectedIndex = -1;
                return;
            }

            if (newIndex < 0) newIndex = 0;
            if (newIndex > maxIndex) newIndex = maxIndex;

            if (newIndex != activeList.SelectedIndex)
            {
                activeList.SelectedIndex = newIndex;
                var item = activeList.Items.Cast<object>().ElementAtOrDefault(newIndex);
                if (item != null) activeList.ScrollIntoView(item);
            }
        }
    }

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        var filesData = e.DataTransfer.TryGetFiles();
        if (filesData == null) return;
        
        var dropPaths = filesData.Select(f => f.TryGetLocalPath() ?? f.Name).Where(f => !string.IsNullOrEmpty(f)).ToList();
        if (dropPaths.Count == 0) return;

        var romExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        { 
            ".zip", ".7z", ".rar", ".iso", ".bin", ".cue", ".n64", ".v64", ".z64", 
            ".sfc", ".smc", ".nes", ".gb", ".gbc", ".gba", ".nds", ".3ds", ".cia", 
            ".pbp", ".cso", ".rvz", ".wbfs", ".gcm", ".gdi", ".chd", ".m3u" 
        };

        int totalAdded = 0;
        int totalSkipped = 0;
        var drossPatterns = LoadDrossPatterns();
        var gamesToImport = new List<Game>();

        foreach (var path in dropPaths)
        {
            Platform? targetPlatform = null;

            if (Directory.Exists(path))
            {
                // Es una carpeta: Crear o buscar plataforma con el nombre de la carpeta
                string platformName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                using (var context = new GestorJuegos.Data.AppDbContext())
                {
                    targetPlatform = context.Platforms.FirstOrDefault(p => p.Name == platformName);
                    if (targetPlatform == null)
                    {
                        targetPlatform = new Platform { Name = platformName, Category = DetectCategory(platformName) };
                        context.Platforms.Add(targetPlatform);
                        context.SaveChanges();
                    }
                }

                if (targetPlatform == null) continue;

                // Escaneo recursivo de archivos dentro de la carpeta
                var allRomFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => romExtensions.Contains(Path.GetExtension(f)))
                    .ToList();

                using (var context = new GestorJuegos.Data.AppDbContext())
                {
                    var existingNames = new HashSet<string>(context.Games
                        .Where(g => g.PlatformId == targetPlatform.Id)
                        .Select(g => $"{g.Name}|{g.Region}"), StringComparer.OrdinalIgnoreCase);

                    foreach (var romPath in allRomFiles)
                    {
                        string fileName = Path.GetFileName(romPath);
                        if (ImportService.IsDross(fileName, drossPatterns))
                        {
                            totalSkipped++;
                            continue;
                        }

                        var game = ImportService.ParseGameLine(fileName, targetPlatform.Id);
                        game.RomPath = romPath;
                        game.DateAdded = DateTime.Now;
                        
                        // Enriquecer con metadatos de Biblioteca Externa
                        _gameService.EnrichGameWithMetadata(game, targetPlatform.Name);

                        string uniqueKey = $"{game.Name}|{game.Region}";
                        
                        if (!existingNames.Contains(uniqueKey))
                        {
                            gamesToImport.Add(game);
                            existingNames.Add(uniqueKey);
                            totalAdded++;
                        }
                        else
                        {
                            totalSkipped++;
                        }
                    }
                }
            }
            else if (File.Exists(path))
            {
                // Es un archivo suelto: Requiere plataforma seleccionada
                if (_selectedPlatform == null)
                {
                    ShowMessage("Para importar archivos sueltos, primero selecciona una plataforma en el menú superior.");
                    continue;
                }

                targetPlatform = _selectedPlatform;
                string ext = Path.GetExtension(path);
                
                if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    // Procesar lista TXT
                    try
                    {
                        string content = await File.ReadAllTextAsync(path);
                        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        using (var context = new GestorJuegos.Data.AppDbContext())
                        {
                            var existingNames = new HashSet<string>(context.Games
                                .Where(g => g.PlatformId == targetPlatform.Id)
                                .Select(g => $"{g.Name}|{g.Region}"), StringComparer.OrdinalIgnoreCase);

                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Plataforma:", StringComparison.OrdinalIgnoreCase)) continue;
                                var game = ImportService.ParseGameLine(line, targetPlatform.Id);
                                if (string.IsNullOrEmpty(game.Name)) continue;
                                game.DateAdded = DateTime.Now;

                                string uniqueKey = $"{game.Name}|{game.Region}";
                                if (!existingNames.Contains(uniqueKey))
                                {
                                    gamesToImport.Add(game);
                                    existingNames.Add(uniqueKey);
                                    totalAdded++;
                                }
                                else { totalSkipped++; }
                            }
                        }
                    }
                    catch { }
                }
                else if (romExtensions.Contains(ext))
                {
                    // Procesar ROM individual
                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        var existingNames = new HashSet<string>(context.Games
                            .Where(g => g.PlatformId == targetPlatform.Id)
                            .Select(g => $"{g.Name}|{g.Region}"), StringComparer.OrdinalIgnoreCase);

                        string fileName = Path.GetFileName(path);
                        if (!ImportService.IsDross(fileName, drossPatterns))
                        {
                            var game = ImportService.ParseGameLine(fileName, targetPlatform.Id);
                            game.RomPath = path;
                            game.DateAdded = DateTime.Now;
                            string uniqueKey = $"{game.Name}|{game.Region}";
                            if (!existingNames.Contains(uniqueKey))
                            {
                                gamesToImport.Add(game);
                                totalAdded++;
                            }
                            else { totalSkipped++; }
                        }
                    }
                }
            }
        }

        if (gamesToImport.Any())
        {
            _gameService.AddGamesBatch(gamesToImport);
        }

        if (totalAdded > 0)
        {
            LoadPlatforms();
            if (_selectedPlatform != null) LoadGames();
            LoadDashboard();
            string msg = $"¡Importación completada! Se añadieron {totalAdded} juegos.";
            if (totalSkipped > 0) msg += $" Se omitieron {totalSkipped} duplicados o archivos filtrados.";
            ShowMessage(msg);
        }
        else if (totalSkipped > 0)
        {
            ShowMessage($"No se añadieron juegos nuevos (se detectaron {totalSkipped} duplicados o archivos filtrados).");
        }
    }

    private async void MenuImportExternalLib_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        string lbPath = _settings.ExternalLibraryPath;
        
        if (!Directory.Exists(lbPath) || !Directory.Exists(Path.Combine(lbPath, "Data", "Platforms")))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta Raíz de la Biblioteca Externa (Ej: C:\\Users\\Nombre\\BibliotecaExterna)",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                lbPath = folders[0].Path.LocalPath;
                // Validar que es una carpeta de biblioteca externa
                if (!Directory.Exists(Path.Combine(lbPath, "Data", "Platforms")))
                {
                    ShowMessage("La carpeta seleccionada no parece ser una instalación válida de una Biblioteca Externa (No se encontró 'Data\\Platforms').");
                    return;
                }
                _settings.ExternalLibraryPath = lbPath;
                SaveSettings();
            }
            else return;
        }

        string platformsPath = Path.Combine(lbPath, "Data", "Platforms");
        if (!Directory.Exists(platformsPath))
        {
            ShowMessage("No se encontró la carpeta 'Data\\Platforms' en la ruta de la biblioteca externa seleccionada.");
            return;
        }

        try
        {
            OverlayProgress.IsVisible = true;
            ProgBarImport.Value = 0;
            TxtProgressTitle.Text = "Importando desde Biblioteca Externa...";
            TxtProgressDetail.Text = "Escaneando archivos de plataforma...";
            
            _cts = new System.Threading.CancellationTokenSource();

            await System.Threading.Tasks.Task.Run(() =>
            {
                var xmlFiles = Directory.GetFiles(platformsPath, "*.xml");
                int totalGamesAdded = 0;
                bool cancelled = false;

                for (int i = 0; i < xmlFiles.Length; i++)
                {
                    if (_cts.IsCancellationRequested) { cancelled = true; break; }
                    
                    string xmlFile = xmlFiles[i];
                    string platformName = Path.GetFileNameWithoutExtension(xmlFile);
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                        ProgBarImport.Value = (i * 100) / xmlFiles.Length;
                        TxtProgressDetail.Text = $"Procesando plataforma: {platformName}...";
                    });

                    try
                    {
                        Platform? platform;
                        using (var context = new GestorJuegos.Data.AppDbContext())
                        {
                            platform = context.Platforms.FirstOrDefault(p => p.Name == platformName);
                            if (platform == null)
                            {
                                platform = new Platform { Name = platformName, Category = DetectCategory(platformName) };
                                context.Platforms.Add(platform);
                                context.SaveChanges();
                            }
                        }

                        var doc = XDocument.Load(xmlFile);
                        var gamesNodes = doc.Descendants("Game").ToList();
                        var gamesToImport = new List<Game>();
                        
                        using (var context = new GestorJuegos.Data.AppDbContext())
                        {
                            var existingGameKeys = new HashSet<string>(context.Games
                                .Where(g => g.PlatformId == platform.Id)
                                .Select(g => $"{g.Name}|{g.Region}"), StringComparer.OrdinalIgnoreCase);

                            foreach (var node in gamesNodes)
                            {
                                if (_cts.IsCancellationRequested) break;

                                string title = node.Element("Title")?.Value ?? "";
                                if (string.IsNullOrEmpty(title)) continue;

                                string region = node.Element("Region")?.Value ?? "🌎 World";
                                if (string.IsNullOrEmpty(region)) region = "🌎 World";
                                else if (region.Contains("Japan", StringComparison.OrdinalIgnoreCase)) region = "🇯🇵 JP";
                                else if (region.Contains("United States", StringComparison.OrdinalIgnoreCase) || region.Contains("North America", StringComparison.OrdinalIgnoreCase)) region = "🇺🇸 US";
                                else if (region.Contains("Europe", StringComparison.OrdinalIgnoreCase)) region = "🇪🇺 EU";
                                else if (region.Contains("Spain", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";

                                string uniqueKey = $"{title}|{region}";
                                if (existingGameKeys.Contains(uniqueKey)) continue;

                                string genre = node.Element("Genre")?.Value ?? "";
                                string appPath = node.Element("ApplicationPath")?.Value ?? "";
                                
                                // Resolve path if relative
                                if (!string.IsNullOrEmpty(appPath) && !Path.IsPathRooted(appPath))
                                {
                                    appPath = Path.GetFullPath(Path.Combine(lbPath, appPath));
                                }

                                int year = 0;
                                string relDate = node.Element("ReleaseDate")?.Value ?? "";
                                if (!string.IsNullOrEmpty(relDate) && DateTime.TryParse(relDate, out var dt))
                                {
                                    year = dt.Year;
                                }

                                bool isFavorite = (node.Element("Favorite")?.Value ?? "").Equals("true", StringComparison.OrdinalIgnoreCase);

                                byte[]? coverData = null;
                                var extraImages = new List<GameImage>();

                                if (_settings.AutoImportCovers)
                                {
                                    try
                                    {
                                        string[] artTypes = { 
                                            "Box", "Box_3D", "Box_Full", "Logos", "Background", "Snap", 
                                            "Title", "Marquee", "Cabinet", "Advert", "Artwork_Preview", "System_Logo",
                                            "Box - Front", "Box - 3D", "Box - Back", "Box - Spine", "Box - Full",
                                            "Cart - Front", "Cart - 3D", "Cart - Back",
                                            "Clear Logo", "Fanart - Background", "Screenshot - Gameplay", 
                                            "Screenshot - Game Title", "Arcade - Marquee", "Disc", "Steam Banner" 
                                        };
                                        
                                        foreach (var type in artTypes)
                                        {
                                            string imagesPlatformPath = Path.Combine(lbPath, "Images", platformName, type);
                                            if (!Directory.Exists(imagesPlatformPath)) continue;

                                            string imgPath = Path.Combine(imagesPlatformPath, $"{title}.jpg");
                                            if (!File.Exists(imgPath)) imgPath = Path.Combine(imagesPlatformPath, $"{title}.png");
                                            
                                            if (!File.Exists(imgPath))
                                            {
                                                var files = Directory.GetFiles(imagesPlatformPath, $"{title}*.*");
                                                imgPath = files.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                                                   f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) ?? "";
                                            }

                                            if (File.Exists(imgPath))
                                            {
                                                byte[] data = File.ReadAllBytes(imgPath);
                                                // Si es el tipo preferido, va a la carátula principal
                                                if (type == GetExternalFolderName(_settings.PreferredArtType))
                                                {
                                                    coverData = data;
                                                }
                                                else
                                                {
                                                    extraImages.Add(new GameImage { ImageType = type, ImageData = data });
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                gamesToImport.Add(new Game
                                {
                                    Name = title,
                                    PlatformId = platform.Id,
                                    Genre = genre,
                                    Year = year,
                                    Region = region,
                                    RomPath = appPath,
                                    IsFavorite = isFavorite,
                                    DateAdded = DateTime.Now,
                                    Cover = coverData,
                                    CoverType = GetExternalFolderName(_settings.PreferredArtType),
                                    ExtraImages = extraImages
                                });
                                
                                existingGameKeys.Add(uniqueKey);
                            }
                        }

                        if (gamesToImport.Any())
                        {
                            _gameService.AddGamesBatch(gamesToImport);
                            totalGamesAdded += gamesToImport.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log el error pero continuar con la siguiente plataforma
                        try { File.AppendAllText("import_error.log", $"[{DateTime.Now}] Error importando {platformName}: {ex.Message}{Environment.NewLine}"); } catch { }
                    }
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    OverlayProgress.IsVisible = false;
                    LoadPlatforms();
                    LoadDashboard();
                    string status = cancelled ? "Importación CANCELADA" : "¡Importación desde Biblioteca Externa finalizada!";
                    ShowMessage($"{status}\n\nSe han procesado las plataformas y juegos correctamente.\nTotal de juegos nuevos añadidos: {totalGamesAdded}");
                });
            });
        }
        catch (Exception ex)
        {
            OverlayProgress.IsVisible = false;
            ShowMessage($"Error durante la importación de la Biblioteca Externa: {ex.Message}");
        }
    }

    private void ShowMessage(string message)
    {
        TxtMessageContent.Text = message;
        OverlayMessage.IsVisible = true;
    }

    private void BtnCloseMessage_Click(object? sender, RoutedEventArgs e)
    {
        OverlayMessage.IsVisible = false;
    }

    private void BtnAddPlatform_Click(object? sender, RoutedEventArgs e)
    {
        TxtNewPlatformName.Text = string.Empty;
        CmbNewPlatformCategory.SelectedIndex = 0; // Default to Consoles
        OverlayAddPlatform.IsVisible = true;
    }

    private void BtnCancelPlatform_Click(object? sender, RoutedEventArgs e)
    {
        OverlayAddPlatform.IsVisible = false;
    }

    private string DetectCategory(string platformName)
    {
        string name = platformName.ToLower();
        
        // Portátiles
        if (name.Contains("game boy") || name.Contains("gameboy") || name.Contains("psp") || 
            name.Contains("nintendo ds") || name.Contains("nintendo 3ds") || name.Contains("game gear") || 
            name.Contains("lynx") || name.Contains("vita") || name.Contains("wonderswan") || name.Contains("ngp") ||
            name.Contains("pocket"))
            return "Handhelds";

        // Ordenadores
        if (name.Contains("amiga") || name.Contains("commodore") || name.Contains("msx") || 
            name.Contains("amstrad") || name.Contains("spectrum") || name.Contains("atari st") || 
            name.Contains("dos") || name.Contains("windows") || name.Contains("mac") || 
            name.Contains("pc") || name.Contains("apple") || name.Contains("sharp x68000") ||
            name.Contains("nec pc") || name.Contains("scummvm"))
            return "Computers";

        // Arcade
        if (name.Contains("arcade") || name.Contains("mame") || name.Contains("neogeo") || 
            name.Contains("cps") || name.Contains("finalburn") || name.Contains("taito") || name.Contains("sega model") ||
            name.Contains("naomi") || name.Contains("atomiswave"))
            return "Arcade";

        // Por defecto Consolas
        return "Consoles";
    }

    private void BtnSavePlatform_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var platformName = TxtNewPlatformName.Text?.Trim();
            if (!string.IsNullOrEmpty(platformName))
            {
                string category = (CmbNewPlatformCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Consoles";
                _gameService.AddPlatform(new Platform { Name = platformName, Category = category });
                LoadPlatforms();
            }
            OverlayAddPlatform.IsVisible = false;
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al guardar la plataforma: {ex.Message}");
        }
    }

    private void BtnViewList_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayNavigation();
        if (BtnViewList != null) BtnViewList.IsChecked = true;
        if (BtnViewGrid != null) BtnViewGrid.IsChecked = false;
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.IsChecked = false;
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.IsChecked = false;
        
        LstGames.IsVisible = true;
        LstGamesGrid.IsVisible = false;
        LstGamesWheelVertical.IsVisible = false;
        LstGamesWheelHorizontal.IsVisible = false;
        
        if (BtnViewList != null) BtnViewList.Background = Avalonia.Media.Brush.Parse("#444444");
        if (BtnViewGrid != null) BtnViewGrid.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.Background = Avalonia.Media.Brush.Parse("#222222");
    }

    private void BtnViewGrid_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayNavigation();
        if (BtnViewGrid != null) BtnViewGrid.IsChecked = true;
        if (BtnViewList != null) BtnViewList.IsChecked = false;
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.IsChecked = false;
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.IsChecked = false;
        
        LstGames.IsVisible = false;
        LstGamesGrid.IsVisible = true;
        LstGamesWheelVertical.IsVisible = false;
        LstGamesWheelHorizontal.IsVisible = false;
        
        if (BtnViewList != null) BtnViewList.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewGrid != null) BtnViewGrid.Background = Avalonia.Media.Brush.Parse("#444444");
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.Background = Avalonia.Media.Brush.Parse("#222222");
    }

    private void BtnViewWheelVertical_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayNavigation();
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.IsChecked = true;
        if (BtnViewGrid != null) BtnViewGrid.IsChecked = false;
        if (BtnViewList != null) BtnViewList.IsChecked = false;
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.IsChecked = false;

        LstGames.IsVisible = false;
        LstGamesGrid.IsVisible = false;
        LstGamesWheelVertical.IsVisible = true;
        LstGamesWheelHorizontal.IsVisible = false;

        if (BtnViewList != null) BtnViewList.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewGrid != null) BtnViewGrid.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.Background = Avalonia.Media.Brush.Parse("#444444");
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.Background = Avalonia.Media.Brush.Parse("#222222");
    }

    private void BtnViewWheelHorizontal_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayNavigation();
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.IsChecked = true;
        if (BtnViewGrid != null) BtnViewGrid.IsChecked = false;
        if (BtnViewList != null) BtnViewList.IsChecked = false;
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.IsChecked = false;

        LstGames.IsVisible = false;
        LstGamesGrid.IsVisible = false;
        LstGamesWheelVertical.IsVisible = false;
        LstGamesWheelHorizontal.IsVisible = true;

        if (BtnViewList != null) BtnViewList.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewGrid != null) BtnViewGrid.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewWheelVertical != null) BtnViewWheelVertical.Background = Avalonia.Media.Brush.Parse("#222222");
        if (BtnViewWheelHorizontal != null) BtnViewWheelHorizontal.Background = Avalonia.Media.Brush.Parse("#444444");
    }

    private void RestoreActiveViewVisibility()
    {
        if (BtnViewList?.IsChecked == true) BtnViewList_Click(null, new RoutedEventArgs());
        else if (BtnViewWheelVertical?.IsChecked == true) BtnViewWheelVertical_Click(null, new RoutedEventArgs());
        else if (BtnViewWheelHorizontal?.IsChecked == true) BtnViewWheelHorizontal_Click(null, new RoutedEventArgs());
        else BtnViewGrid_Click(null, new RoutedEventArgs());
    }

    private void LoadPlatforms()
    {
        int viewIndex = CmbSidebarView != null ? CmbSidebarView.SelectedIndex : 0;
        string lbPath = _settings?.ExternalLibraryPath ?? "";

        System.Threading.Tasks.Task.Run(() => {
            try
            {
                using var context = new GestorJuegos.Data.AppDbContext();
                int totalGames = context.Games.Count();
                int favoritesCount = context.Games.Count(g => g.IsFavorite);

                // Cargar ruta e icono de Biblioteca Externa si están disponibles
                byte[]? allGamesIcon = null;
                if (!string.IsNullOrEmpty(lbPath) && Directory.Exists(lbPath))
                {
                    string allGamesPath = Path.Combine(lbPath, "Images", "Platform Icons", "All Games.png");
                    if (File.Exists(allGamesPath))
                    {
                        try { allGamesIcon = File.ReadAllBytes(allGamesPath); } catch { }
                    }
                }

                var sidebarNodes = new List<SidebarNode>();

                // Nodo raíz "Todo" que siempre se muestra arriba
                var rootNode = new SidebarNode
                {
                    Name = "Todo",
                    Count = totalGames,
                    Tag = "ALL",
                    IconBytes = allGamesIcon,
                    ResortIcon = "🏠",
                    IsExpanded = true
                };
                sidebarNodes.Add(rootNode);

                if (viewIndex == 0) // --- VISTA: CATEGORÍA DE PLATAFORMA (Estilo Biblioteca Externa capturas) ---
                {
                    // Asegurar categorías por defecto persistidas si la tabla está vacía
                    var dbCategories = context.PlatformCategories.ToList();
                    if (dbCategories.Count == 0)
                    {
                        var defaultCats = new List<PlatformCategory>
                        {
                            new PlatformCategory { Name = "Computers" },
                            new PlatformCategory { Name = "Consoles" },
                            new PlatformCategory { Name = "Handhelds" },
                            new PlatformCategory { Name = "Arcade" }
                        };
                        context.PlatformCategories.AddRange(defaultCats);
                        context.SaveChanges();
                        dbCategories = context.PlatformCategories.ToList();
                    }

                    // Cargar estadísticas y objetos de plataformas
                    var platforms = context.Platforms.ToList();

                    // Asegurar que cualquier categoría existente en la tabla de plataformas esté registrada en PlatformCategories
                    var platformCategoriesInUse = platforms
                        .Select(p => p.Category)
                        .Where(cat => !string.IsNullOrEmpty(cat))
                        .Distinct()
                        .ToList();

                    bool dbChanged = false;
                    foreach (var catName in platformCategoriesInUse)
                    {
                        if (!dbCategories.Any(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase)))
                        {
                            var newCat = new PlatformCategory { Name = catName };
                            
                            // Intentar cargar recursos de Biblioteca Externa inmediatamente si la ruta es válida
                            if (!string.IsNullOrEmpty(lbPath) && Directory.Exists(lbPath))
                            {
                                // Icono pixel-art
                                string iconPath = Path.Combine(lbPath, "Images", "Platform Icons", "Platform Categories", $"{catName}.png");
                                if (File.Exists(iconPath))
                                {
                                    try { newCat.Icon = File.ReadAllBytes(iconPath); } catch { }
                                }
                                
                                // Clear Logo
                                string graphicPath = Path.Combine(lbPath, "Images", "Platform Categories", catName, "Clear Logo", $"{catName}.png");
                                if (File.Exists(graphicPath))
                                {
                                    try { newCat.Graphic = File.ReadAllBytes(graphicPath); } catch { }
                                }
                            }

                            // Notas/descripción por defecto según la categoría
                            if (catName.Equals("Arcade", StringComparison.OrdinalIgnoreCase))
                            {
                                newCat.Notes = "Arcade games are coin-operated entertainment machines, typically installed in public businesses such as restaurants, bars, and amusement arcades. They reached their golden age from the late 1970s to the mid-1980s, offering high-quality graphics and immersive hardware for their time.";
                            }

                            context.PlatformCategories.Add(newCat);
                            dbCategories.Add(newCat);
                            dbChanged = true;
                        }
                    }
                    if (dbChanged)
                    {
                        context.SaveChanges();
                    }

                    var gamesCount = _gameService.GetGamesCountByPlatform() ?? new Dictionary<string, int>();

                    // Crear los nodos de categoría
                    var categoryNodes = new Dictionary<string, SidebarNode>();
                    foreach (var cat in dbCategories.OrderBy(c => c.Name))
                    {
                        string emoji = cat.Name == "Computers" ? "🖥️" : 
                                       (cat.Name == "Handhelds" ? "📟" : 
                                       (cat.Name == "Arcade" ? "🕹️" : "🎮"));
                        var catNode = new SidebarNode
                        {
                            Name = cat.Name,
                            Tag = ("CATEGORY", cat.Name),
                            IconBytes = cat.Icon,
                            ResortIcon = emoji,
                            IsExpanded = true
                        };
                        categoryNodes[cat.Name] = catNode;
                    }

                    // Clasificar plataformas en sus respectivas categorías
                    foreach (var platform in platforms.OrderBy(p => p.Name))
                    {
                        int pCount = gamesCount.ContainsKey(platform.Name) ? gamesCount[platform.Name] : 0;
                        string categoryName = platform.Category;

                        // Si la categoría no está en nuestro diccionario (raro si dbCategories está completa, pero por seguridad)
                        if (!categoryNodes.ContainsKey(categoryName))
                        {
                            string emoji = categoryName == "Computers" ? "🖥️" : (categoryName == "Handhelds" ? "📟" : "🎮");
                            categoryNodes[categoryName] = new SidebarNode
                            {
                                Name = categoryName,
                                Tag = ("CATEGORY", categoryName),
                                ResortIcon = emoji,
                                IsExpanded = true
                            };
                        }

                        var childNode = new SidebarNode
                        {
                            Name = platform.Name,
                            Count = pCount,
                            Tag = ("PLATFORM", platform.Name),
                            IconBytes = platform.Icon, // Icono pixel-art de la plataforma desde SQLite
                            ResortIcon = "🎮",
                            IsExpanded = true
                        };

                        categoryNodes[categoryName].Children.Add(childNode);
                    }

                    // Añadir al árbol solo las categorías que tengan hijos (plataformas)
                    foreach (var catEntry in categoryNodes.OrderBy(c => c.Key))
                    {
                        var catNode = catEntry.Value;
                        if (catNode.Children.Count > 0)
                        {
                            catNode.Count = catNode.Children.Sum(child => child.Count);
                            sidebarNodes.Add(catNode);
                        }
                    }
                }
                else if (viewIndex == 1) // --- VISTA: PLATAFORMAS (Lista plana) ---
                {
                    var platforms = context.Platforms.ToList();
                    var gamesCount = _gameService.GetGamesCountByPlatform() ?? new Dictionary<string, int>();

                    foreach (var platform in platforms.OrderBy(p => p.Name))
                    {
                        int pCount = gamesCount.ContainsKey(platform.Name) ? gamesCount[platform.Name] : 0;
                        sidebarNodes.Add(new SidebarNode
                        {
                            Name = platform.Name,
                            Count = pCount,
                            Tag = ("PLATFORM", platform.Name),
                            IconBytes = platform.Icon,
                            ResortIcon = "🎮"
                        });
                    }
                }
                else if (viewIndex == 2) // --- VISTA: GÉNEROS ---
                {
                    var genreStats = _gameService.GetGenresWithCount();
                    if (genreStats != null)
                    {
                        foreach (var g in genreStats.OrderByDescending(x => x.Value).Take(20))
                        {
                            sidebarNodes.Add(new SidebarNode
                            {
                                Name = g.Key,
                                Count = g.Value,
                                Tag = ("GENRE", g.Key),
                                ResortIcon = "📁"
                            });
                        }
                    }
                }
                else if (viewIndex == 3) // --- VISTA: REGIONES ---
                {
                    var regionStats = _gameService.GetRegionsWithCount();
                    if (regionStats != null)
                    {
                        foreach (var r in regionStats.OrderByDescending(x => x.Value))
                        {
                            sidebarNodes.Add(new SidebarNode
                            {
                                Name = r.Key,
                                Count = r.Value,
                                Tag = ("REGION", r.Key),
                                ResortIcon = "🌎"
                            });
                        }
                    }
                }
                else if (viewIndex == 4) // --- VISTA: BIBLIOTECA ---
                {
                    sidebarNodes.Add(new SidebarNode
                    {
                        Name = "Favoritos",
                        Count = favoritesCount,
                        Tag = "FAVORITES",
                        ResortIcon = "⭐"
                    });
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    TvSidebar.ItemsSource = sidebarNodes;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en LoadPlatforms: {ex.Message}");
            }
        });
    }

    private void CmbSidebarView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadPlatforms();
    }

    private async void ImportExternalAssets()
    {
        string lbPath = _settings.ExternalLibraryPath;
        if (string.IsNullOrEmpty(lbPath) || !Directory.Exists(lbPath))
        {
            // Intentar con la ruta por defecto F:\Biblioteca Externa si no está configurada o si la de configuración no existe
            if (Directory.Exists(@"F:\Biblioteca Externa"))
            {
                lbPath = @"F:\Biblioteca Externa";
                _settings.ExternalLibraryPath = lbPath;
                SaveSettings();
            }
            else
            {
                return; // No se puede importar si no existe Biblioteca Externa
            }
        }

        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                using var context = new GestorJuegos.Data.AppDbContext();

                // 1. Importar/actualizar iconos y gráficos de todas las categorías en la DB
                var categories = context.PlatformCategories.Select(c => c.Name).ToList();
                if (!categories.Contains("Arcade")) categories.Add("Arcade");

                foreach (var catName in categories)
                {
                    var dbCat = context.PlatformCategories.FirstOrDefault(c => c.Name == catName);
                    bool isNew = false;
                    if (dbCat == null)
                    {
                        dbCat = new PlatformCategory { Name = catName };
                        isNew = true;
                    }

                    // Cargar icono pixel-art de la categoría
                    string iconPath = Path.Combine(lbPath, "Images", "Platform Icons", "Platform Categories", $"{catName}.png");
                    if (File.Exists(iconPath))
                    {
                        try { dbCat.Icon = File.ReadAllBytes(iconPath); } catch { }
                    }

                    // Cargar gráfico Clear Logo grande de la categoría
                    string graphicPath = Path.Combine(lbPath, "Images", "Platform Categories", catName, "Clear Logo", $"{catName}.png");
                    if (File.Exists(graphicPath))
                    {
                        try { dbCat.Graphic = File.ReadAllBytes(graphicPath); } catch { }
                    }

                    // Guardar notas/descripción por defecto de la categoría
                    if (string.IsNullOrEmpty(dbCat.Notes))
                    {
                        if (catName == "Computers")
                        {
                            dbCat.Notes = "Home computers were a class of microcomputers entering the market in 1977, and becoming common during the 1980s. They were marketed to consumers as affordable and accessible computers that, for the first time, were intended for the use of a single nontechnical user. These computers were a distinct market segment that typically cost much less than business, scientific or engineering-oriented computers of the time such as the IBM PC, and were generally less powerful in terms of memory and expandability. However, a home computer often had better graphics and sound than contemporaneous business computers. Their most common uses were playing video games, but they were also regularly used for word processing, doing homework, and programming.";
                        }
                        else if (catName == "Handhelds")
                        {
                            dbCat.Notes = "Handheld game consoles are lightweight, portable devices with a built-in screen, game controls, and speakers. They allowed players to carry their gaming collections anywhere, becoming a dominant force in the industry with systems like the Nintendo Game Boy and PlayStation Portable.";
                        }
                        else if (catName == "Consoles")
                        {
                            dbCat.Notes = "Video game consoles are standardized devices designed for interactive entertainment, usually played on a television screen or monitor. They emerged in the early 1970s and evolved through generations of dedicated hardware, offering highly optimized gaming experiences in the living room.";
                        }
                    }

                    if (isNew)
                        context.PlatformCategories.Add(dbCat);
                    else
                        context.PlatformCategories.Update(dbCat);
                }
                context.SaveChanges();

                // 2. Importar/actualizar iconos, logos y datos de plataforma en la DB
                var dbPlatforms = context.Platforms.ToList();
                bool anyPlatformUpdated = false;
                var metadataService = new ExternalMetadataService();

                foreach (var platform in dbPlatforms)
                {
                    bool isUpdated = false;

                    // Si no tiene icono pixel-art guardado en DB, intentar cargarlo de Biblioteca Externa
                    if (platform.Icon == null || platform.Icon.Length == 0)
                    {
                        string platIconPath = Path.Combine(lbPath, "Images", "Platform Icons", "Platforms", $"{platform.Name}.png");
                        if (File.Exists(platIconPath))
                        {
                            try
                            {
                                platform.Icon = File.ReadAllBytes(platIconPath);
                                isUpdated = true;
                            }
                            catch { }
                        }
                    }

                    // Si no tiene logo/Clear Logo en la DB, intentar cargarlo
                    if (platform.Logo == null || platform.Logo.Length == 0)
                    {
                        string logoPath = Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Clear Logo", $"{platform.Name}.png");
                        if (File.Exists(logoPath))
                        {
                            try
                            {
                                platform.Logo = File.ReadAllBytes(logoPath);
                                isUpdated = true;
                            }
                            catch { }
                        }
                    }

                    // Cargar foto física de la consola (HardwareImage)
                    if (platform.HardwareImage == null || platform.HardwareImage.Length == 0)
                    {
                        string consoleImgPath = Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Console", $"{platform.Name}.png");
                        if (!File.Exists(consoleImgPath))
                            consoleImgPath = Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Console", $"{platform.Name}.jpg");
                        
                        if (!File.Exists(consoleImgPath) && Directory.Exists(Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Console")))
                        {
                            try
                            {
                                var files = Directory.GetFiles(Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Console"), "*.*");
                                consoleImgPath = files.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                                                          f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) ?? "";
                            }
                            catch { }
                        }

                        if (File.Exists(consoleImgPath))
                        {
                            try
                            {
                                platform.HardwareImage = File.ReadAllBytes(consoleImgPath);
                                isUpdated = true;
                            }
                            catch { }
                        }
                    }

                    // Cargar fondo de la plataforma (Fanart)
                    if (platform.Graphics == null || platform.Graphics.Length == 0) // Uso Graphics temporalmente para el fondo si no hay campo dedicado
                    {
                        // En realidad, para el fondo solemos usar Fanart o una imagen de hardware
                        // Pero Biblioteca Externa tiene fondos de plataforma específicos
                        string backgroundPath = Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Fanart", $"{platform.Name}.png");
                        if (!File.Exists(backgroundPath))
                             backgroundPath = Path.Combine(lbPath, "Images", "Platforms", platform.Name, "Fanart", $"{platform.Name}.jpg");

                        if (File.Exists(backgroundPath))
                        {
                            try
                            {
                                // Podríamos añadir un campo 'Background' a Platform, pero por ahora usemos HardwareImage si es lo único que hay
                                // O mejor, vamos a cargar HardwareImage y el Logo que ya tenemos.
                            }
                            catch { }
                        }
                    }

                    // Usar el servicio de metadatos para enriquecer especificaciones técnicas
                    if (string.IsNullOrEmpty(platform.Notes) || string.IsNullOrEmpty(platform.Cpu))
                    {
                        string? oldCpu = platform.Cpu;
                        _gameService.EnrichPlatformWithMetadata(platform);
                        if (platform.Cpu != oldCpu) isUpdated = true;
                    }

                    // --- CLASIFICACIÓN DE CATEGORÍA AUTOMÁTICA EN LA DB SI ES DEFAULT ---
                    // Si tiene la categoría default "Consoles", la reclasificamos de forma inteligente para que persista
                    if (platform.Category == "Consoles")
                    {
                        string nameLower = platform.Name.ToLower();
                        string newCategory = "Consoles";

                        if (nameLower.Contains("amiga") || nameLower.Contains("spectrum") || nameLower.Contains("amstrad") || 
                            nameLower.Contains("msx") || nameLower.Contains("x68000") || nameLower.Contains("windows") || 
                            nameLower.Contains("dos") || nameLower.Contains("ibm") || nameLower.Contains("pc") || 
                            nameLower.Contains("commodore") || nameLower.Contains("atari st"))
                        {
                            newCategory = "Computers";
                        }
                        else if (nameLower.Contains("game boy") || nameLower.Contains("gameboy") || nameLower.Contains("advance") || 
                                 nameLower.Contains("color") || nameLower.Contains("psp") || nameLower.Contains("portable") || 
                                 nameLower.Contains("ds") || nameLower.Contains("3ds") || nameLower.Contains("game gear") || 
                                 nameLower.Contains("lynx") || nameLower.Contains("wonderswan") || nameLower.Contains("pocket"))
                        {
                            newCategory = "Handhelds";
                        }

                        if (newCategory != platform.Category)
                        {
                            platform.Category = newCategory;
                            isUpdated = true;
                        }
                    }

                    if (isUpdated)
                    {
                        context.Platforms.Update(platform);
                        anyPlatformUpdated = true;
                    }
                }

                if (anyPlatformUpdated)
                {
                    context.SaveChanges();
                    // Refrescar el árbol en el hilo de UI
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => LoadPlatforms());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ImportExternalAssets: {ex.Message}");
            }
        });
    }

    private void TvSidebar_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TvSidebar.SelectedItem is SidebarNode item)
        {
            GestorJuegos.Utils.SoundHelper.PlaySelect();
            _currentPage = 1;
            PnlDashboard.IsVisible = false;
            PnlPagination.IsVisible = true;
            
            // Cargar los detalles de la plataforma/categoría en el panel derecho
            LoadPlatformOrCategoryDetails(item);
            
            // Restaurar la visibilidad de la vista activa (Grid, List o Wheels)
            RestoreActiveViewVisibility();

            System.Threading.Tasks.Task.Run(() => {
                using var context = new GestorJuegos.Data.AppDbContext();
                var query = context.Games.Include(g => g.Platform).AsQueryable();

                _selectedCategory = null;
                if (item.Tag is string simpleTag)
                {
                    if (simpleTag == "ALL") { /* No filter */ }
                    else if (simpleTag == "FAVORITES") { query = query.Where(g => g.IsFavorite); }
                }
                else if (item.Tag is ValueTuple<string, string> filter)
                {
                    if (filter.Item1 == "PLATFORM") 
                    { 
                        query = query.Where(g => g.Platform.Name == filter.Item2); 
                    }
                    else if (filter.Item1 == "CATEGORY") 
                    { 
                        _selectedCategory = filter.Item2;
                        // Filtrar por todas las plataformas que correspondan a esta categoría
                        var platformNames = context.Platforms
                            .Where(p => p.Category == filter.Item2)
                            .Select(p => p.Name)
                            .ToList();
                        query = query.Where(g => platformNames.Contains(g.Platform.Name)); 
                    }
                    else if (filter.Item1 == "GENRE") 
                    { 
                        query = query.Where(g => g.Genre.Contains(filter.Item2)); 
                    }
                    else if (filter.Item1 == "REGION") 
                    { 
                        query = query.Where(g => g.Region == filter.Item2); 
                    }
                }

                var games = query.ToList();

                // Obtener la plataforma antes de cerrar el contexto para evitar ObjectDisposedException
                Platform? platformToSet = null;
                if (item.Tag is ValueTuple<string, string> f && f.Item1 == "PLATFORM")
                {
                    platformToSet = context.Platforms.FirstOrDefault(p => p.Name == f.Item2);
                }
                else
                {
                    platformToSet = games.FirstOrDefault()?.Platform;
                }
                
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    _selectedPlatform = platformToSet;
                    _currentPlatformGames = games;

                    // Si no hay juegos, mostrar dashboard o mensaje
                    if (_currentPlatformGames.Count == 0)
                    {
                        ShowMessage($"No hay juegos en la categoría '{item.Name}'.");
                        LoadDashboard();
                    }
                    else
                    {
                        ApplySearchFilter();
                    }
                });
            });
        }
    }

    private void SelectSidebarNodeByTag(object tag)
    {
        var items = TvSidebar.ItemsSource as IEnumerable<SidebarNode>;
        if (items == null) return;

        foreach (var node in items)
        {
            if (Equals(node.Tag, tag))
            {
                TvSidebar.SelectedItem = node;
                return;
            }
            foreach (var child in node.Children)
            {
                if (Equals(child.Tag, tag))
                {
                    TvSidebar.SelectedItem = child;
                    return;
                }
            }
        }
    }

    private void LoadPlatformOrCategoryDetails(SidebarNode item)
    {
        if (PnlNoGameSelected != null) PnlNoGameSelected.IsVisible = false;
        if (PnlGameDetails != null) PnlGameDetails.IsVisible = false;
        if (PnlPlatformDetails == null) return;

        PnlPlatformDetails.IsVisible = true;

        using var context = new GestorJuegos.Data.AppDbContext();

        // Limpiar controles
        ImgPlatformLogo.Source = null;
        ImgPlatformHardware.Source = null;
        if (BrdPlatformHardware != null) BrdPlatformHardware.IsVisible = false;
        TxtPlatformHardwarePlaceholder.IsVisible = false;
        TxtPlatformReleaseDate.IsVisible = false;
        TxtPlatDescription.Text = string.Empty;

        // Por defecto, mostrar todas las filas de la tabla
        RowPlatDeveloper.IsVisible = true;
        RowPlatManufacturer.IsVisible = true;
        RowPlatCpu.IsVisible = true;
        RowPlatMemory.IsVisible = true;
        RowPlatGraphics.IsVisible = true;
        RowPlatSound.IsVisible = true;
        RowPlatDisplay.IsVisible = true;
        RowPlatMedia.IsVisible = true;
        RowPlatLastPlayedGame.IsVisible = true;
        RowPlatMostPlayedGame.IsVisible = true;

        if (item.Tag is ValueTuple<string, string> filter)
        {
            string filterType = filter.Item1;
            string filterValue = filter.Item2;

            if (filterType == "PLATFORM")
            {
                var platform = context.Platforms.FirstOrDefault(p => p.Name == filterValue);
                if (platform != null)
                {
                    // 1. Mostrar Logo
                    if (platform.Logo != null && platform.Logo.Length > 0)
                    {
                        try
                        {
                            using var ms = new MemoryStream(platform.Logo);
                            ImgPlatformLogo.Source = new Bitmap(ms);
                            BrdPlatformLogo.IsVisible = true;
                        }
                        catch { BrdPlatformLogo.IsVisible = false; }
                    }
                    else
                    {
                        BrdPlatformLogo.IsVisible = false;
                    }

                    // 2. Fecha de Estreno
                    if (!string.IsNullOrEmpty(platform.ReleaseDate))
                    {
                        if (DateTime.TryParse(platform.ReleaseDate, out var dt))
                        {
                            TxtPlatformReleaseDate.Text = $"Estrenado en {dt:dd/MM/yyyy}";
                        }
                        else
                        {
                            TxtPlatformReleaseDate.Text = $"Estrenado en {platform.ReleaseDate}";
                        }
                        TxtPlatformReleaseDate.IsVisible = true;
                    }

                    // 3. Foto de Consola (HardwareImage)
                    if (platform.HardwareImage != null && platform.HardwareImage.Length > 0)
                    {
                        try
                        {
                            using var ms = new MemoryStream(platform.HardwareImage);
                            ImgPlatformHardware.Source = new Bitmap(ms);
                            ImgPlatformHardware.IsVisible = true;
                            if (BrdPlatformHardware != null) BrdPlatformHardware.IsVisible = true;
                            TxtPlatformHardwarePlaceholder.IsVisible = false;
                        }
                        catch 
                        { 
                            ImgPlatformHardware.IsVisible = false;
                            if (BrdPlatformHardware != null) BrdPlatformHardware.IsVisible = true;
                            TxtPlatformHardwarePlaceholder.IsVisible = true; 
                        }
                    }
                    else
                    {
                        ImgPlatformHardware.IsVisible = false;
                        if (BrdPlatformHardware != null) BrdPlatformHardware.IsVisible = true;
                        TxtPlatformHardwarePlaceholder.IsVisible = true;
                    }

                    // 4. Datos Técnicos
                    TxtPlatDeveloper.Text = string.IsNullOrEmpty(platform.Developer) ? "--" : platform.Developer;
                    TxtPlatManufacturer.Text = string.IsNullOrEmpty(platform.Manufacturer) ? "--" : platform.Manufacturer;
                    TxtPlatCpu.Text = string.IsNullOrEmpty(platform.Cpu) ? "--" : platform.Cpu;
                    TxtPlatMemory.Text = string.IsNullOrEmpty(platform.Memory) ? "--" : platform.Memory;
                    TxtPlatGraphics.Text = string.IsNullOrEmpty(platform.Graphics) ? "--" : platform.Graphics;
                    TxtPlatSound.Text = string.IsNullOrEmpty(platform.Sound) ? "--" : platform.Sound;
                    TxtPlatDisplay.Text = string.IsNullOrEmpty(platform.Display) ? "--" : platform.Display;
                    TxtPlatMedia.Text = string.IsNullOrEmpty(platform.Media) ? "--" : platform.Media;

                    // 5. Estadísticas Locales
                    var games = context.Games.Where(g => g.PlatformId == platform.Id).ToList();
                    int totalGames = games.Count;
                    int completedGames = games.Count(g => g.PlayStatus == "Completado");
                    int totalPlayCount = games.Sum(g => g.PlayCount);
                    int totalPlayTimeSeconds = games.Sum(g => g.PlayTime);

                    int hours = totalPlayTimeSeconds / 3600;
                    int minutes = (totalPlayTimeSeconds % 3600) / 60;
                    int seconds = totalPlayTimeSeconds % 60;
                    
                    TxtPlatTotalGames.Text = totalGames.ToString();
                    TxtPlatCompletedGames.Text = completedGames.ToString();
                    TxtPlatPlayCount.Text = totalPlayCount.ToString();
                    TxtPlatPlayTime.Text = $"{hours}h {minutes:00}m {seconds:00}s";

                    var playedGames = games.Where(g => g.PlayCount > 0).ToList();
                    if (playedGames.Any())
                    {
                        TxtPlatLastPlayed.Text = "Recientemente";
                        var lastPlayedGame = playedGames.OrderByDescending(g => g.PlayCount).First();
                        TxtPlatLastPlayedGame.Text = lastPlayedGame.Name;
                    }
                    else
                    {
                        TxtPlatLastPlayed.Text = "Nunca";
                        TxtPlatLastPlayedGame.Text = "--";
                    }

                    var mostPlayedGame = games.OrderByDescending(g => g.PlayCount).FirstOrDefault();
                    TxtPlatMostPlayedGame.Text = (mostPlayedGame != null && mostPlayedGame.PlayCount > 0) ? mostPlayedGame.Name : "--";

                    // 6. Descripción
                    TxtPlatDescription.Text = string.IsNullOrEmpty(platform.Notes) ? "Sin descripción histórica disponible." : platform.Notes;
                }
            }
            else if (filterType == "CATEGORY")
            {
                var dbCat = context.PlatformCategories.FirstOrDefault(c => c.Name == filterValue);

                RowPlatDeveloper.IsVisible = false;
                RowPlatManufacturer.IsVisible = false;
                RowPlatCpu.IsVisible = false;
                RowPlatMemory.IsVisible = false;
                RowPlatGraphics.IsVisible = false;
                RowPlatSound.IsVisible = false;
                RowPlatDisplay.IsVisible = false;
                RowPlatMedia.IsVisible = false;

                // 1. Logo
                if (dbCat != null && dbCat.Graphic != null && dbCat.Graphic.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(dbCat.Graphic);
                        ImgPlatformLogo.Source = new Bitmap(ms);
                        BrdPlatformLogo.IsVisible = true;
                    }
                    catch { BrdPlatformLogo.IsVisible = false; }
                }
                else
                {
                    BrdPlatformLogo.IsVisible = false;
                }

                ImgPlatformHardware.IsVisible = false;
                if (BrdPlatformHardware != null) BrdPlatformHardware.IsVisible = false;
                TxtPlatformHardwarePlaceholder.IsVisible = false;
                TxtPlatformReleaseDate.IsVisible = false;

                // 2. Estadísticas
                var platformIds = context.Platforms.Where(p => p.Category == filterValue).Select(p => p.Id).ToList();
                var games = context.Games.Where(g => platformIds.Contains(g.PlatformId)).ToList();
                int totalGames = games.Count;
                int completedGames = games.Count(g => g.PlayStatus == "Completado");
                int totalPlayCount = games.Sum(g => g.PlayCount);
                int totalPlayTimeSeconds = games.Sum(g => g.PlayTime);

                int hours = totalPlayTimeSeconds / 3600;
                int minutes = (totalPlayTimeSeconds % 3600) / 60;
                int seconds = totalPlayTimeSeconds % 60;

                TxtPlatTotalGames.Text = totalGames.ToString();
                TxtPlatCompletedGames.Text = completedGames.ToString();
                TxtPlatPlayCount.Text = totalPlayCount.ToString();
                TxtPlatPlayTime.Text = $"{hours}h {minutes:00}m {seconds:00}s";

                var playedGames = games.Where(g => g.PlayCount > 0).ToList();
                if (playedGames.Any())
                {
                    TxtPlatLastPlayed.Text = "Recientemente";
                    var lastPlayedGame = playedGames.OrderByDescending(g => g.PlayCount).First();
                    TxtPlatLastPlayedGame.Text = lastPlayedGame.Name;
                }
                else
                {
                    TxtPlatLastPlayed.Text = "Nunca";
                    TxtPlatLastPlayedGame.Text = "--";
                }

                var mostPlayedGame = games.OrderByDescending(g => g.PlayCount).FirstOrDefault();
                TxtPlatMostPlayedGame.Text = (mostPlayedGame != null && mostPlayedGame.PlayCount > 0) ? mostPlayedGame.Name : "--";

                // 3. Descripción
                TxtPlatDescription.Text = (dbCat != null && !string.IsNullOrEmpty(dbCat.Notes)) ? dbCat.Notes : "Sin descripción de categoría disponible.";
            }
        }
        else if (item.Tag is string simpleTag && simpleTag == "ALL")
        {
            RowPlatDeveloper.IsVisible = false;
            RowPlatManufacturer.IsVisible = false;
            RowPlatCpu.IsVisible = false;
            RowPlatMemory.IsVisible = false;
            RowPlatGraphics.IsVisible = false;
            RowPlatSound.IsVisible = false;
            RowPlatDisplay.IsVisible = false;
            RowPlatMedia.IsVisible = false;
            
            BrdPlatformLogo.IsVisible = false;
            ImgPlatformHardware.IsVisible = false;
            TxtPlatformHardwarePlaceholder.IsVisible = false;
            TxtPlatformReleaseDate.IsVisible = false;

            var games = context.Games.ToList();
            int totalGames = games.Count;
            int completedGames = games.Count(g => g.PlayStatus == "Completado");
            int totalPlayCount = games.Sum(g => g.PlayCount);
            int totalPlayTimeSeconds = games.Sum(g => g.PlayTime);

            int hours = totalPlayTimeSeconds / 3600;
            int minutes = (totalPlayTimeSeconds % 3600) / 60;
            int seconds = totalPlayTimeSeconds % 60;

            TxtPlatTotalGames.Text = totalGames.ToString();
            TxtPlatCompletedGames.Text = completedGames.ToString();
            TxtPlatPlayCount.Text = totalPlayCount.ToString();
            TxtPlatPlayTime.Text = $"{hours}h {minutes:00}m {seconds:00}s";

            var playedGames = games.Where(g => g.PlayCount > 0).ToList();
            if (playedGames.Any())
            {
                TxtPlatLastPlayed.Text = "Recientemente";
                var lastPlayedGame = playedGames.OrderByDescending(g => g.PlayCount).First();
                TxtPlatLastPlayedGame.Text = lastPlayedGame.Name;
            }
            else
            {
                TxtPlatLastPlayed.Text = "Nunca";
                TxtPlatLastPlayedGame.Text = "--";
            }

            var mostPlayedGame = games.OrderByDescending(g => g.PlayCount).FirstOrDefault();
            TxtPlatMostPlayedGame.Text = (mostPlayedGame != null && mostPlayedGame.PlayCount > 0) ? mostPlayedGame.Name : "--";

            TxtPlatDescription.Text = "Esta vista muestra el compendio general de todos los videojuegos importados y catalogados en tu biblioteca local de Gestor de Juegos, ofreciendo métricas de juego consolidadas para toda tu colección multimedia.";
        }
        else
        {
            PnlPlatformDetails.IsVisible = false;
            if (PnlNoGameSelected != null) PnlNoGameSelected.IsVisible = true;
        }
    }

    private void BtnClosePlatformDetails_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayBack();
        if (PnlPlatformDetails != null) PnlPlatformDetails.IsVisible = false;
        if (PnlNoGameSelected != null) PnlNoGameSelected.IsVisible = true;
    }

    private void BtnEditPlatformQuick_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform != null)
        {
            var platforms = _gameService.GetPlatforms();
            LstManagePlatforms.ItemsSource = platforms;
            OverlayManagePlatforms.IsVisible = true;
            
            var matched = platforms.FirstOrDefault(p => p.Id == _selectedPlatform.Id);
            if (matched != null)
            {
                LstManagePlatforms.SelectedItem = matched;
            }
        }
        else
        {
            BtnManagePlatforms_Click(sender, e);
        }
    }

    private void PlatformMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Platform platform)
        {
            SelectSidebarNodeByTag(("PLATFORM", platform.Name));
        }
    }

    private void MenuEditPlatformSidebar_Click(object? sender, RoutedEventArgs e)
    {
        if (TvSidebar.SelectedItem is SidebarNode node)
        {
            if (node.Tag is ValueTuple<string, string> filter && filter.Item1 == "PLATFORM")
            {
                string platformName = filter.Item2;
                
                // Abrir el gestor de plataformas
                var platforms = _gameService.GetPlatforms();
                LstManagePlatforms.ItemsSource = platforms;
                OverlayManagePlatforms.IsVisible = true;
                
                // Buscar y seleccionar esta plataforma en la lista del gestor
                var platformToSelect = platforms.FirstOrDefault(p => p.Name == platformName);
                if (platformToSelect != null)
                {
                    // Buscar el objeto exacto en la lista asignada
                    var matched = platforms.FirstOrDefault(p => p.Id == platformToSelect.Id);
                    if (matched != null)
                    {
                        LstManagePlatforms.SelectedItem = matched;
                    }
                }
            }
            else
            {
                ShowMessage("Solo puedes editar nodos que representen a una Plataforma.");
            }
        }
    }

    private void MenuDeletePlatformSidebar_Click(object? sender, RoutedEventArgs e)
    {
        if (TvSidebar.SelectedItem is SidebarNode node)
        {
            if (node.Tag is ValueTuple<string, string> filter && filter.Item1 == "PLATFORM")
            {
                string platformName = filter.Item2;
                var platforms = _gameService.GetPlatforms();
                var platformToDelete = platforms.FirstOrDefault(p => p.Name == platformName);
                if (platformToDelete != null)
                {
                    // Abrir el gestor de plataformas con la plataforma seleccionada para proceder
                    LstManagePlatforms.ItemsSource = platforms;
                    OverlayManagePlatforms.IsVisible = true;
                    
                    var matched = platforms.FirstOrDefault(p => p.Id == platformToDelete.Id);
                    if (matched != null)
                    {
                        LstManagePlatforms.SelectedItem = matched;
                    }
                }
            }
            else
            {
                ShowMessage("Solo puedes eliminar nodos que representen a una Plataforma.");
            }
        }
    }

    private void BtnManagePlatforms_Click(object? sender, RoutedEventArgs e)
    {
        LstManagePlatforms.ItemsSource = _gameService.GetPlatforms();
        PnlEditPlatform.IsVisible = false;
        OverlayManagePlatforms.IsVisible = true;
    }

    private void BtnCloseManagePlatforms_Click(object? sender, RoutedEventArgs e)
    {
        OverlayManagePlatforms.IsVisible = false;
    }

    private void BtnGoDashboard_Click(object? sender, RoutedEventArgs e)
    {
        _selectedPlatform = null;
        LoadDashboard();
    }

    private void LstPlatformsWall_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstPlatformsWall.SelectedItem is Platform platform)
        {
            SelectSidebarNodeByTag(("PLATFORM", platform.Name));
            
            OverlayPlatformsWall.IsVisible = false;
            LstPlatformsWall.SelectedItem = null;
        }
    }

    private void LoadPlatformsWall()
    {
        var platforms = _gameService.GetPlatforms();
        string lbPath = _settings.ExternalLibraryPath;

        foreach (var p in platforms)
        {
            // Intentar cargar logo de Biblioteca Externa
            if (Directory.Exists(lbPath))
            {
                string logoPath = Path.Combine(lbPath, "Images", "Platforms", p.Name, "Clear Logo", $"{p.Name}.png");
                if (File.Exists(logoPath))
                {
                    try { p.Logo = File.ReadAllBytes(logoPath); } catch { }
                }
            }
        }

        LstPlatformsWall.ItemsSource = platforms;
        OverlayPlatformsWall.IsVisible = true;
    }

    private void ShowFullStats()
    {
        using var context = new GestorJuegos.Data.AppDbContext();
        int totalGames = context.Games.Count();
        int totalPlatforms = context.Platforms.Count();
        int totalGenres = context.Games.Select(g => g.Genre).Distinct().Count();

        FullStatsTotalGames.Text = totalGames.ToString();
        FullStatsTotalPlatforms.Text = totalPlatforms.ToString();
        FullStatsTotalGenres.Text = totalGenres.ToString();

        var platformStats = _gameService.GetGamesCountByPlatform()
            .Select(p => new { Key = p.Key, Value = p.Value })
            .OrderByDescending(p => p.Value)
            .ToList();
        FullStatsPlatformList.ItemsSource = platformStats;

        var regionStats = context.Games
            .Where(g => !string.IsNullOrEmpty(g.Region))
            .GroupBy(g => g.Region)
            .OrderByDescending(g => g.Count())
            .Select(g => new { Key = g.Key, Value = g.Count() })
            .ToList();
        FullStatsRegionList.ItemsSource = regionStats;

        OverlayFullStats.IsVisible = true;
    }

    private void LoadDashboard()
    {
        PnlDashboard.IsVisible = true;
        // PnlHeaderToggles.IsVisible = false; (Eliminado)
        PnlPagination.IsVisible = false;
        LstGames.IsVisible = false;
        LstGamesGrid.IsVisible = false;
        LstGamesWheelVertical.IsVisible = false;
        LstGamesWheelHorizontal.IsVisible = false;
        PnlGameDetails.IsVisible = false;
        
        LoadPlatforms();

        using var context = new GestorJuegos.Data.AppDbContext();
        context.Database.EnsureCreated();

        try
        {
            int allGamesCount = context.Games.Count();
            if (TxtGameStatusInfo != null)
            {
                TxtGameStatusInfo.Text = $"Mostrando 0 de {allGamesCount} del total de juegos.";
            }
        }
        catch { }
    }

    private void MenuExportDB_Click(object? sender, RoutedEventArgs e)
    {
        OverlayExportOptions.IsVisible = true;
    }

    private async void BtnConfirmExport_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        OverlayExportOptions.IsVisible = false;
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        bool exportGames = ChkExportGames.IsChecked ?? false;
        bool exportCovers = ChkExportCovers.IsChecked ?? false;

        if (!exportGames && !exportCovers)
        {
            ShowMessage("No se ha seleccionado nada para exportar.");
            return;
        }

        try
        {
            int exportsDone = 0;

            // 1. Exportar Base de Datos Principal
            if (exportGames)
            {
                var fileData = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Exportar Base de Datos de Juegos",
                    SuggestedFileName = "GestorJuegos_Backup.db",
                    FileTypeChoices = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
                });

                if (fileData != null)
                {
                    string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");
                    if (File.Exists(dbPath))
                    {
                        using (var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var destinationStream = await fileData.OpenWriteAsync())
                        {
                            await sourceStream.CopyToAsync(destinationStream);
                        }
                        exportsDone++;
                    }
                }
            }

            // 2. Exportar Base de Datos de Carátulas
            if (exportCovers)
            {
                var fileCovers = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Exportar Base de Datos de Carátulas",
                    SuggestedFileName = "GestorCovers_Backup.db",
                    FileTypeChoices = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
                });

                if (fileCovers != null)
                {
                    string coversDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorCovers.db");
                    if (File.Exists(coversDbPath))
                    {
                        using (var sourceStream = new FileStream(coversDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var destinationStream = await fileCovers.OpenWriteAsync())
                        {
                            await sourceStream.CopyToAsync(destinationStream);
                        }
                        exportsDone++;
                    }
                }
            }

            if (exportsDone > 0)
            {
                ShowMessage($"Respaldo completado: Se han exportado {exportsDone} archivos con éxito.");
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al exportar: {ex.Message}");
        }
    }

    private async void MenuImportDB_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // 1. Importar Base de Datos Principal
        var filesData = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar Base de Datos de Juegos (GestorJuegos.db)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
        });

        if (filesData.Count > 0)
        {
            try
            {
                var fileData = filesData[0];
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");

                using (var sourceStream = await fileData.OpenReadAsync())
                using (var destinationStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                // 2. Importar Base de Datos de Carátulas
                var filesCovers = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Importar Base de Datos de Carátulas (GestorCovers.db)",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
                });

                if (filesCovers.Count > 0)
                {
                    var fileCovers = filesCovers[0];
                    string coversDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorCovers.db");

                    using (var sourceStream = await fileCovers.OpenReadAsync())
                    using (var destinationStream = new FileStream(coversDbPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                    ShowMessage("Restauración completa: Se han importado los juegos y las carátulas.");
                }
                else
                {
                    ShowMessage("Se importaron los juegos, pero no se seleccionó base de datos de carátulas.");
                }

                // Recargar UI
                _selectedPlatform = null;
                TxtSelectedPlatform.Text = "Seleccione una plataforma";
                LstGames.ItemsSource = null;
                LstGamesGrid.ItemsSource = null;
                PnlGameDetails.IsVisible = false;
                LoadPlatforms();
                LoadDashboard();
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al importar: {ex.Message}. Asegúrese de cerrar el programa si el archivo está bloqueado.");
            }
        }
    }

    private void LogDebug(string message)
    {
        try
        {
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vimm_debug_log.txt"), $"[UI Debug] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private void RunBatchScrape(string source)
    {
        LogDebug($"RunBatchScrape llamado con source: {source}");
        if (_selectedPlatform == null)
        {
            LogDebug("Error: _selectedPlatform es nulo");
            ShowMessage("Por favor, selecciona primero la plataforma a la que quieres descargar carátulas.");
            return;
        }

        if (source == "Vimm's Lair")
        {
            LogDebug("Iniciando flujo de Batch Scrape para Vimm's Lair");
            var platforms = VimmVaultService.GetSupportedPlatforms();
            CmbVimmSystem.ItemsSource = platforms;
            
            // Intentar pre-seleccionar la mejor coincidencia
            var currentName = _selectedPlatform.Name.ToLower();
            var bestMatch = platforms.FirstOrDefault(p => currentName.Contains(p.Key.ToLower()) || p.Key.ToLower().Contains(currentName));
            
            if (bestMatch.Key != null)
            {
                LogDebug($"Pre-seleccionando sistema Vimm: {bestMatch.Key} para plataforma: {_selectedPlatform.Name}");
                CmbVimmSystem.SelectedItem = bestMatch;
            }
            else 
            {
                LogDebug($"No se encontró coincidencia automática para: {_selectedPlatform.Name}. Seleccionando primero por defecto.");
                CmbVimmSystem.SelectedIndex = 0;
            }

            OverlayVimmSystem.IsVisible = true;
            return;
        }
    }

    private async void MenuImportDat_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero la plataforma a la que quieres importar los juegos en el menú superior.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar archivo DAT/XML de No-Intro",
            AllowMultiple = false,
            FileTypeFilter = new[] { 
                new FilePickerFileType("Archivos XML/DAT") { Patterns = new[] { "*.xml", "*.dat" } }
            }
        });

        if (files.Count > 0)
        {
            try
            {
                var file = files[0];
                ShowMessage("Importando juegos, por favor espera...");
                
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    using var stream = await file.OpenReadAsync();
                    var doc = XDocument.Load(stream);
                    
                    int count = 0;
                    var gamesNodes = doc.Descendants("game").ToList(); // Formato No-Intro
                    bool isExternalLib = false;
                    bool isExternalLibMame = false;

                    if (!gamesNodes.Any())
                    {
                        gamesNodes = doc.Descendants("Game").ToList(); // Formato Biblioteca Externa Estándar
                        isExternalLib = gamesNodes.Any();
                        
                        if (!isExternalLib)
                        {
                            gamesNodes = doc.Descendants("MameFile").ToList(); // Formato Biblioteca Externa MAME
                            isExternalLibMame = gamesNodes.Any();
                        }
                    }

                    var newGames = new System.Collections.Generic.List<Game>();
                    
                    var existingNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        var platformGames = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).Select(g => new { g.Name, g.Region }).ToList();
                        foreach(var g in platformGames) 
                            if(g.Name != null) existingNames.Add($"{g.Name}|{g.Region}");
                    }
                    
                    int skippedCount = 0;
                    var drossPatterns = LoadDrossPatterns();

                    foreach (var gameNode in gamesNodes)
                    {
                        string name = "";
                        string shortName = "";
                        string region = "🌎 World";
                        string genre = "";
                        string developer = "";
                        string publisher = "";
                        int year = DateTime.Now.Year;

                        if (isExternalLib)
                        {
                            name = gameNode.Element("Title")?.Value ?? "";
                            region = gameNode.Element("Region")?.Value ?? "🌎 World";
                            genre = gameNode.Element("Genre")?.Value ?? "";
                            developer = gameNode.Element("Developer")?.Value ?? "";
                            publisher = gameNode.Element("Publisher")?.Value ?? "";
                            string relDate = gameNode.Element("ReleaseDate")?.Value ?? "";
                            if (!string.IsNullOrEmpty(relDate) && DateTime.TryParse(relDate, out var dt))
                            {
                                year = dt.Year;
                            }
                        }
                        else if (isExternalLibMame)
                        {
                            name = gameNode.Element("Name")?.Value ?? "";
                            shortName = gameNode.Element("FileName")?.Value ?? "";
                            region = gameNode.Element("Region")?.Value ?? "🌎 World";
                            genre = gameNode.Element("Genre")?.Value ?? "";
                            developer = gameNode.Element("Developer")?.Value ?? "";
                            publisher = gameNode.Element("Publisher")?.Value ?? "";
                            string yearStr = gameNode.Element("Year")?.Value ?? "";
                            if (int.TryParse(yearStr, out var y)) year = y;
                        }
                        else
                        {
                            // No-Intro: El atributo name suele ser el nombre descriptivo o el filename. 
                            // Si tiene un hijo <description>, ese es el nombre real.
                            shortName = gameNode.Attribute("name")?.Value ?? "";
                            name = gameNode.Element("description")?.Value ?? shortName;
                        }

                        if (string.IsNullOrEmpty(name)) continue;

                        if (ImportService.IsDross(name, drossPatterns))
                        {
                            skippedCount++;
                            continue;
                        }

                        // Normalización de Región
                        if (region == "🌎 World" || string.IsNullOrEmpty(region))
                        {
                            if (name.Contains("(JP") || name.Contains("(Japan")) region = "🇯🇵 JP";
                            else if (name.Contains("(US") || name.Contains("(USA")) region = "🇺🇸 US";
                            else if (name.Contains("(EU") || name.Contains("(Europe")) region = "🇪🇺 EU";
                            else if (name.Contains("(Spain", StringComparison.OrdinalIgnoreCase) || name.Contains("(España", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es)", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es-Es)", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es - Es)", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";
                            else region = "🌎 World";
                        }
                        else
                        {
                            if (region.Contains("Japan", StringComparison.OrdinalIgnoreCase)) region = "🇯🇵 JP";
                            else if (region.Contains("United States", StringComparison.OrdinalIgnoreCase) || region.Contains("North America", StringComparison.OrdinalIgnoreCase)) region = "🇺🇸 US";
                            else if (region.Contains("Europe", StringComparison.OrdinalIgnoreCase)) region = "🇪🇺 EU";
                            else if (region.Contains("Spain", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";
                        }

                        string cleanName = name;
                        if (!isExternalLib && !isExternalLibMame)
                        {
                            int bracketIndex = name.IndexOf('(');
                            if (bracketIndex > 0)
                            {
                                cleanName = name.Substring(0, bracketIndex).Trim();
                            }
                            
                            if (cleanName.Contains("•"))
                            {
                                cleanName = cleanName.Split('•')[0].Trim();
                            }
                        }

                        string uniqueKey = $"{cleanName}|{region}";
                        if (existingNames.Contains(uniqueKey))
                        {
                            skippedCount++;
                        }
                        else
                        {
                            existingNames.Add(uniqueKey);
                            newGames.Add(new Game
                            {
                                PlatformId = _selectedPlatform.Id,
                                Name = cleanName,
                                ShortName = shortName,
                                Region = region,
                                Genre = genre,
                                Developer = developer,
                                Publisher = publisher,
                                Year = year,
                                DateAdded = DateTime.Now
                            });
                        }
                    }

                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        context.Platforms.Update(_selectedPlatform);
                        context.SaveChanges();
                    }

                    if (newGames.Any())
                    {
                        _gameService.AddGamesBatch(newGames);
                        count = newGames.Count;
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        LoadGames();
                        string msg = $"¡Importación completada! Se añadieron {count} juegos.";
                        if (skippedCount > 0) msg += $" Se omitieron {skippedCount} que ya existían.";
                        ShowMessage(msg);
                    });
                });
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ShowMessage($"Error al importar el archivo: {ex.Message}");
                });
            }
        }
    }

    private async void MenuSyncExternalLib_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero la plataforma en el menú lateral.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar XML de Plataforma de Biblioteca Externa (ej: arcade.xml)",
            AllowMultiple = false,
            FileTypeFilter = new[] {
                new FilePickerFileType("Biblioteca Externa Platform XML") { Patterns = new[] { "*.xml" } }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            
            await System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using var stream = await file.OpenReadAsync();
                    var doc = XDocument.Load(stream);
                    
                    var gameNodes = doc.Descendants("Game").ToList();
                    if (!gameNodes.Any())
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowMessage("El archivo no parece ser un XML de plataforma de Biblioteca Externa válido (no se encontraron etiquetas <Game>)."));
                        return;
                    }

                    // Mapas para búsqueda rápida
                    var titleToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var shortNameToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var node in gameNodes)
                    {
                        string title = node.Element("Title")?.Value ?? "";
                        string path = node.Element("ApplicationPath")?.Value ?? "";
                        
                        if (!string.IsNullOrEmpty(path))
                        {
                            // 1. Mapeo por Título
                            if (!string.IsNullOrEmpty(title) && !titleToPath.ContainsKey(title))
                                titleToPath.Add(title, path);
                            
                            // 2. Mapeo por Nombre de Archivo (ShortName)
                            try
                            {
                                string fileName = Path.GetFileNameWithoutExtension(path);
                                if (!string.IsNullOrEmpty(fileName) && !shortNameToPath.ContainsKey(fileName))
                                    shortNameToPath.Add(fileName, path);
                            }
                            catch { }
                        }
                    }

                    // Actualizar juegos en la base de datos
                    using var context = new GestorJuegos.Data.AppDbContext();
                    var gamesToUpdate = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).ToList();
                    int updatedCount = 0;

                    foreach (var game in gamesToUpdate)
                    {
                        // Buscar nodo en el XML de Biblioteca Externa para obtener metadatos completos
                        var node = gameNodes.FirstOrDefault(n => 
                            (n.Element("Title")?.Value?.Equals(game.Name, StringComparison.OrdinalIgnoreCase) == true) ||
                            (Path.GetFileNameWithoutExtension(n.Element("ApplicationPath")?.Value ?? "")?.Equals(game.ShortName, StringComparison.OrdinalIgnoreCase) == true)
                        );

                        if (node != null)
                        {
                            // 1. Ruta (ROM)
                            string path = node.Element("ApplicationPath")?.Value ?? "";
                            if (!string.IsNullOrEmpty(path)) game.RomPath = path;

                            // 2. Metadatos
                            game.Description = node.Element("Notes")?.Value ?? game.Description;
                            game.Developer = node.Element("Developer")?.Value ?? game.Developer;
                            game.Publisher = node.Element("Publisher")?.Value ?? game.Publisher;
                            game.Genre = node.Element("Genre")?.Value ?? game.Genre;
                            game.Version = node.Element("Version")?.Value ?? game.Version;
                            game.ExternalDbId = node.Element("DatabaseID")?.Value ?? game.ExternalDbId;
                            
                            string relDate = node.Element("ReleaseDate")?.Value ?? "";
                            if (!string.IsNullOrEmpty(relDate) && DateTime.TryParse(relDate, out var dt))
                            {
                                game.Year = dt.Year;
                            }

                            string starRating = node.Element("StarRating")?.Value ?? "0";
                            if (float.TryParse(starRating, out var rating))
                            {
                                game.Rating = (int)(rating * 20); // Escala 0-5 a 0-100
                            }

                            updatedCount++;
                        }
                    }

                    if (updatedCount > 0)
                    {
                        context.UpdateRange(gamesToUpdate);
                        context.SaveChanges();
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        ShowMessage($"Sincronización Inteligente completada.\nSe han vinculado las rutas de {updatedCount} juegos.");
                        LoadGames();
                    });
                }
                catch (Exception ex)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowMessage($"Error durante la sincronización: {ex.Message}"));
                }
            });
        }
    }

    private async void MenuSyncMasterDb_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null && string.IsNullOrEmpty(_selectedCategory))
        {
            ShowMessage("Por favor, selecciona primero una plataforma o categoría en el menú lateral.");
            return;
        }

        var metadataService = new ExternalMetadataService();

        if (!metadataService.IsDatabaseAvailable)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar Base de Datos Maestra (Metadata.db)",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
            });

            if (files.Count > 0)
            {
                string path = files[0].Path.LocalPath;
                metadataService = new ExternalMetadataService(path);
            }
            else return;
        }

        // Configurar UI de progreso
        _cts = new System.Threading.CancellationTokenSource();
        OverlayProgress.IsVisible = true;
        TxtProgressTitle.Text = "Importando Metadatos de Base Maestra";
        TxtProgressDetail.Text = "Iniciando proceso...";
        ProgBarImport.Value = 0;
        ProgBarImport.Minimum = 0;
        
        var cancelHandler = new EventHandler<RoutedEventArgs>((s, args) => _cts.Cancel());
        BtnCancelProgress.Click += cancelHandler;

        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                using var context = new GestorJuegos.Data.AppDbContext();
                var platformsToSync = new List<Platform>();

                if (!string.IsNullOrEmpty(_selectedCategory))
                {
                    platformsToSync = context.Platforms.Where(p => p.Category == _selectedCategory).ToList();
                }
                else if (_selectedPlatform != null)
                {
                    platformsToSync = new List<Platform> { context.Platforms.First(p => p.Id == _selectedPlatform.Id) };
                }

                int updatedCount = 0;
                int currentGameIndex = 0;

                // Primero contar el total de juegos para el progreso
                var allGamesToUpdate = new List<(Game game, string platformName)>();
                foreach (var platform in platformsToSync)
                {
                    var games = context.Games.Where(g => g.PlatformId == platform.Id).ToList();
                    foreach (var g in games) allGamesToUpdate.Add((g, platform.Name));
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                {
                    ProgBarImport.Maximum = allGamesToUpdate.Count;
                    TxtProgressDetail.Text = $"Preparando {allGamesToUpdate.Count} juegos...";
                });

                foreach (var item in allGamesToUpdate)
                {
                    if (_cts.IsCancellationRequested) break;

                    currentGameIndex++;
                    var game = item.game;
                    var platformName = item.platformName;

                    Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                    {
                        ProgBarImport.Value = currentGameIndex;
                        TxtProgressDetail.Text = $"[{currentGameIndex}/{allGamesToUpdate.Count}] {game.Name}";
                    });

                    var oldId = game.ExternalDbId;
                    _gameService.EnrichGameWithMetadata(game, platformName);
                    
                    if (game.ExternalDbId != oldId || !string.IsNullOrEmpty(game.Description))
                    {
                        updatedCount++;
                    }

                    // Guardar cada 50 juegos para no saturar y no perder progreso si se cancela
                    if (currentGameIndex % 50 == 0)
                    {
                        context.UpdateRange(allGamesToUpdate.Take(currentGameIndex).Select(x => x.game));
                        context.SaveChanges();
                    }
                }

                // Guardado final
                if (!_cts.IsCancellationRequested)
                {
                    context.UpdateRange(allGamesToUpdate.Select(x => x.game));
                    context.SaveChanges();
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    OverlayProgress.IsVisible = false;
                    BtnCancelProgress.Click -= cancelHandler;

                    if (_cts.IsCancellationRequested)
                    {
                        ShowMessage("Proceso cancelado por el usuario.");
                    }
                    else
                    {
                        string scope = !string.IsNullOrEmpty(_selectedCategory) ? $"la categoría '{_selectedCategory}'" : $"la plataforma '{_selectedPlatform?.Name}'";
                        ShowMessage($"Importación de Metadatos para {scope} completada.\nSe han enriquecido {updatedCount} juegos de {allGamesToUpdate.Count} procesados.");
                    }
                    
                    // Refrescar la vista actual
                    TvSidebar_SelectionChanged(null, new SelectionChangedEventArgs(null, new List<object>(), new List<object>()));
                });
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                {
                    OverlayProgress.IsVisible = false;
                    BtnCancelProgress.Click -= cancelHandler;
                    ShowMessage($"Error al leer la base de datos maestra: {ex.Message}");
                });
            }
        });
    }

    private void BtnSyncMasterDbLocal_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;

        GestorJuegos.Utils.SoundHelper.PlaySelect();
        var metadataService = new ExternalMetadataService();

        if (!metadataService.IsDatabaseAvailable)
        {
            ShowMessage("La base de datos maestra no se encontró en RevisaDB ni en la instalación externa.\nPor favor, asegúrate de que Biblioteca Externa.Metadata.db esté en K:\\GestorJuegos\\RevisaDB\\");
            return;
        }

        try
        {
            // Creamos un clon temporal para no ensuciar el objeto real hasta confirmar
            var tempGame = new Models.Game { Name = TxtName.Text ?? _selectedGame.Name };
            _gameService.EnrichGameWithMetadata(tempGame, _selectedPlatform.Name);

            if (!string.IsNullOrEmpty(tempGame.Description))
            {
                TxtDescription.Text = tempGame.Description;
                TxtGenre.Text = tempGame.Genre;
                TxtDeveloper.Text = tempGame.Developer;
                TxtPublisher.Text = tempGame.Publisher;
                NumYear.Value = tempGame.Year > 0 ? tempGame.Year : NumYear.Value;
                
                // Nuevos campos en el formulario (si los hubiera, si no se guardan en el objeto al dar a guardar)
                _selectedGame.ExternalDbId = tempGame.ExternalDbId;
                _selectedGame.ReleaseDate = tempGame.ReleaseDate;
                _selectedGame.MaxPlayers = tempGame.MaxPlayers;
                _selectedGame.Cooperative = tempGame.Cooperative;
                _selectedGame.VideoURL = tempGame.VideoURL;
                _selectedGame.WikipediaURL = tempGame.WikipediaURL;
                _selectedGame.ESRB = tempGame.ESRB;
                _selectedGame.CommunityRating = tempGame.CommunityRating;
                _selectedGame.CommunityRatingCount = tempGame.CommunityRatingCount;

                ShowMessage($"Metadatos encontrados para '{tempGame.Name}'.\nLos campos han sido actualizados en el formulario.");
            }
            else
            {
                ShowMessage("No se encontraron metadatos para este juego en la base de datos local.");
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al buscar en BD local: {ex.Message}");
        }
    }

    private async void BtnSearchIgdb_Click(object? sender, RoutedEventArgs e)
    {
        string query = TxtName.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            ShowMessage("Por favor, escriba el nombre del juego antes de buscar.");
            return;
        }

        GestorJuegos.Utils.SoundHelper.PlaySelect();

        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero la plataforma principal.");
            return;
        }

        var systemCode = VimmVaultService.GetSystemCode(_selectedPlatform.Name);
        if (string.IsNullOrEmpty(systemCode))
        {
            ShowMessage($"Plataforma '{_selectedPlatform.Name}' no soportada por Vimm's Lair.");
            return;
        }

        OverlayIgdbSearch.IsVisible = true;
        TxtIgdbStatus.Text = $"Buscando '{query}' en Vimm's Lair...";
        LstIgdbResults.ItemsSource = null;

        try
        {
            var results = await _vimmService.SearchGamesAsync(systemCode, query);
            LstIgdbResults.ItemsSource = results;
            if (results.Count == 0)
            {
                TxtIgdbStatus.Text = "No se encontraron resultados.";
            }
            else
            {
                TxtIgdbStatus.Text = "Resultados de Búsqueda (Vimm's Lair)";
            }
        }
        catch (Exception ex)
        {
            TxtIgdbStatus.Text = "Error al buscar en Vimm's Lair.";
            ShowMessage($"Error de API: {ex.Message}");
        }
    }

    private void BtnCancelIgdb_Click(object? sender, RoutedEventArgs e)
    {
        OverlayIgdbSearch.IsVisible = false;
    }

    private async void BtnSelectIgdb_Click(object? sender, RoutedEventArgs e)
    {
        if (LstIgdbResults.SelectedItem is IgdbSearchResult result)
        {
            TxtName.Text = result.Name;
            OverlayIgdbSearch.IsVisible = false;

            if (!string.IsNullOrEmpty(result.CoverUrl) && int.TryParse(result.CoverUrl, out var vimmId))
            {
                try
                {
                    ShowMessage("Descargando carátula...");
                    _currentCover = await _vimmService.DownloadBoxArtAsync(vimmId);
                    UpdateCoverImage();
                    OverlayMessage.IsVisible = false;
                }
                catch
                {
                    ShowMessage("No se pudo descargar la carátula.");
                }
            }
        }
    }

    private void LstManagePlatforms_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstManagePlatforms.SelectedItem is Platform platform)
        {
            TxtEditPlatformName.Text = platform.Name;
            TxtEmulatorPath.Text = platform.EmulatorPath;
            TxtLaunchArgs.Text = platform.LaunchArguments;
            
            // Seleccionar la categoría actual
            var categoryItem = CmbEditPlatformCategory.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content?.ToString() == platform.Category);
            if (categoryItem != null) CmbEditPlatformCategory.SelectedItem = categoryItem;
            else CmbEditPlatformCategory.SelectedIndex = 0;

            PnlEditPlatform.IsVisible = true;
        }
    }

    private void BtnSaveEditPlatform_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (LstManagePlatforms.SelectedItem is Platform platform)
            {
                var newName = TxtEditPlatformName.Text?.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    platform.Name = newName;
                    platform.EmulatorPath = TxtEmulatorPath.Text?.Trim() ?? "";
                    platform.LaunchArguments = TxtLaunchArgs.Text?.Trim() ?? "";
                    platform.Category = (CmbEditPlatformCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Consoles";
                    
                    _gameService.UpdatePlatform(platform);
                    LoadPlatforms();
                    LstManagePlatforms.ItemsSource = _gameService.GetPlatforms();
                    
                    // Update currently selected text if it was modified
                    if (_selectedPlatform?.Id == platform.Id)
                    {
                        TxtSelectedPlatform.Text = $"Plataforma: {platform.Name}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al actualizar la plataforma: {ex.Message}");
        }
    }

    private void BtnDeletePlatform_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (LstManagePlatforms.SelectedItem is Platform platform)
            {
                _gameService.DeletePlatform(platform.Id);
                LoadPlatforms();
                LstManagePlatforms.ItemsSource = _gameService.GetPlatforms();
                PnlEditPlatform.IsVisible = false;

                // Reset current view if the deleted platform was the active one
                if (_selectedPlatform?.Id == platform.Id)
                {
                    _selectedPlatform = null;
                    TxtSelectedPlatform.Text = "Seleccione una plataforma";
                    LstGames.ItemsSource = null;
                    LstGamesGrid.ItemsSource = null;
                    PnlGameDetails.IsVisible = false;
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al eliminar la plataforma: {ex.Message}");
        }
    }

    private void GameItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        SoundHelper.PlayNavigation();
    }

    private void SidebarItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        SoundHelper.PlayNavigation();
    }

    private void LoadGames()
    {
        if (_selectedPlatform == null)
        {
            _currentPlatformGames.Clear();
            LstGames.ItemsSource = null;
            LstGamesGrid.ItemsSource = null;
            return;
        }

        _currentPage = 1;
        
        // Optimización: Cargar solo lo necesario de la DB principal
        _currentPlatformGames = _gameService.GetGamesByPlatform(_selectedPlatform.Id);
        
        // Ejecutar ApplySearchFilter de forma asíncrona para no bloquear la UI si hay muchos juegos
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        if (_currentPlatformGames == null) return;
        
        var queryStr = TxtSearchGame?.Text?.Trim().ToLower() ?? "";
        var filtered = _currentPlatformGames.AsEnumerable();

        if (!string.IsNullOrEmpty(queryStr))
        {
            filtered = filtered.Where(g => g.Name.ToLower().Contains(queryStr) || (g.Genre != null && g.Genre.ToLower().Contains(queryStr)));
        }

        if (BtnQuickFavorite?.IsChecked == true)
        {
            filtered = filtered.Where(g => g.IsFavorite);
        }

        if (CmbFilterRegion?.SelectedIndex > 0)
        {
            string reg = (CmbFilterRegion.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            filtered = filtered.Where(g => g.Region != null && g.Region.Contains(reg));
        }

        if (NumFilterYear?.Value > 0)
        {
            filtered = filtered.Where(g => g.Year == (int)NumFilterYear.Value);
        }

        // Aplicar Ordenación Dinámica y Generalizada
        if (!string.IsNullOrEmpty(_currentSortField))
        {
            switch (_currentSortField)
            {
                case "Name":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Name) : filtered.OrderByDescending(g => g.Name);
                    break;
                case "Year":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Year) : filtered.OrderByDescending(g => g.Year);
                    break;
                case "DateAdded":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.DateAdded).ThenBy(g => g.Name) : filtered.OrderByDescending(g => g.DateAdded).ThenBy(g => g.Name);
                    break;
                case "Developer":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Developer) : filtered.OrderByDescending(g => g.Developer);
                    break;
                case "IsFavorite":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.IsFavorite) : filtered.OrderByDescending(g => g.IsFavorite);
                    break;
                case "Genre":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Genre) : filtered.OrderByDescending(g => g.Genre);
                    break;
                case "LastPlayed":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.LastPlayed) : filtered.OrderByDescending(g => g.LastPlayed);
                    break;
                case "ExternalDbId":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.ExternalDbId) : filtered.OrderByDescending(g => g.ExternalDbId);
                    break;
                case "MaxPlayers":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.MaxPlayers) : filtered.OrderByDescending(g => g.MaxPlayers);
                    break;
                case "Platform":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Platform != null ? g.Platform.Name : "") : filtered.OrderByDescending(g => g.Platform != null ? g.Platform.Name : "");
                    break;
                case "PlayCount":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.PlayCount) : filtered.OrderByDescending(g => g.PlayCount);
                    break;
                case "PlayTime":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.PlayTime) : filtered.OrderByDescending(g => g.PlayTime);
                    break;
                case "Rating":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Rating) : filtered.OrderByDescending(g => g.Rating);
                    break;
                case "Region":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Region) : filtered.OrderByDescending(g => g.Region);
                    break;
                case "ReleaseDate":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.ReleaseDate) : filtered.OrderByDescending(g => g.ReleaseDate);
                    break;
                case "PlayStatus":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.PlayStatus) : filtered.OrderByDescending(g => g.PlayStatus);
                    break;
                case "Version":
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Version) : filtered.OrderByDescending(g => g.Version);
                    break;
                default:
                    filtered = _isSortAscending ? filtered.OrderBy(g => g.Name) : filtered.OrderByDescending(g => g.Name);
                    break;
            }
        }
        else
        {
            filtered = filtered.OrderBy(g => g.Name);
        }

        var filteredList = filtered.ToList();

        int totalItems = filteredList.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (totalPages == 0) totalPages = 1;
        if (_currentPage > totalPages) _currentPage = totalPages;

        var paginated = filteredList.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();

        // Limpiar para forzar refresco total
        LstGames.ItemsSource = null;
        LstGamesGrid.ItemsSource = null;
        LstGamesWheelVertical.ItemsSource = null;
        LstGamesWheelHorizontal.ItemsSource = null;

        // Cargar miniaturas para la página actual
        foreach (var game in paginated)
        {
            string gameArtType = !string.IsNullOrEmpty(game.SelectedArtType) ? game.SelectedArtType : _settings.PreferredArtType;
            string artFolder = GetExternalFolderName(gameArtType);
            game.Cover = _gameService.GetGameThumbnail(game.Id, artFolder);
            
            // Si el toggle desactivó la visualización de favoritos, ocultamos visualmente en el listado
            if (!_showFavoriteBadge) game.IsFavorite = false;
        }

        LstGames.ItemsSource = paginated;
        LstGamesGrid.ItemsSource = paginated;
        LstGamesWheelVertical.ItemsSource = paginated;
        LstGamesWheelHorizontal.ItemsSource = paginated;

        // Forzar actualización de la curvatura en segundo plano tras renderizar
        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateWheels, Avalonia.Threading.DispatcherPriority.Background);
        
        if (_selectedPlatform != null)
        {
            TxtSelectedPlatform.Text = $"{_selectedPlatform.Name} ({totalItems} juegos)";
        }
        
        TxtPageInfo.Text = $"Página {_currentPage} de {totalPages}";
        BtnPrevPage.IsEnabled = _currentPage > 1;
        BtnNextPage.IsEnabled = _currentPage < totalPages;

        // Actualizar contador del menú superior horizontal en tiempo real
        try
        {
            int allGamesCount = 0;
            using (var context = new GestorJuegos.Data.AppDbContext())
            {
                allGamesCount = context.Games.Count();
            }
            if (TxtGameStatusInfo != null)
            {
                TxtGameStatusInfo.Text = $"Mostrando {totalItems} de {allGamesCount} del total de juegos.";
            }
        }
        catch { }
    }

    private void TxtSearchGame_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        ApplySearchFilter();
    }

    private void BtnToggleFilters_Click(object? sender, RoutedEventArgs e)
    {
        if (PnlFilters != null)
            PnlFilters.IsVisible = !PnlFilters.IsVisible;
    }

    private void BtnApplyFilters_Click(object? sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplySearchFilter();
    }

    private void BtnClearFilters_Click(object? sender, RoutedEventArgs e)
    {
        if (BtnQuickFavorite != null) BtnQuickFavorite.IsChecked = false;
        if (CmbFilterRegion != null) CmbFilterRegion.SelectedIndex = 0;
        if (NumFilterYear != null) NumFilterYear.Value = 0;
        
        _currentPage = 1;
        ApplySearchFilter();
    }

    private void BtnQuickFavorite_Click(object? sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplySearchFilter();
    }

    private void BtnPrevPage_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            GestorJuegos.Utils.SoundHelper.PlayNavigation();
            _currentPage--;
            ApplySearchFilter();
            
            // Scroll to top
            var firstList = LstGames.Items.Cast<object>().FirstOrDefault();
            if (LstGames.IsVisible && firstList != null) LstGames.ScrollIntoView(firstList);
            
            var firstGrid = LstGamesGrid.Items.Cast<object>().FirstOrDefault();
            if (LstGamesGrid.IsVisible && firstGrid != null) LstGamesGrid.ScrollIntoView(firstGrid);
        }
    }

    private void BtnNextPage_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayNavigation();
        _currentPage++;
        ApplySearchFilter();
        
        // Scroll to top
        var firstList = LstGames.Items.Cast<object>().FirstOrDefault();
        if (LstGames.IsVisible && firstList != null) LstGames.ScrollIntoView(firstList);
        
        var firstGrid = LstGamesGrid.Items.Cast<object>().FirstOrDefault();
        if (LstGamesGrid.IsVisible && firstGrid != null) LstGamesGrid.ScrollIntoView(firstGrid);
    }

    private bool _isSyncingSelection = false;
    private void LstGames_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection) return;

        var listBox = sender as ListBox;
        if (listBox?.SelectedItem is Game game)
        {
            _isSyncingSelection = true;
            try
            {
                // Sincronizar selección entre todas las vistas de juegos sin disparar eventos recursivos
                if (LstGames != listBox && LstGames != null) LstGames.SelectedItem = game;
                if (LstGamesGrid != listBox && LstGamesGrid != null) LstGamesGrid.SelectedItem = game;
                if (LstGamesWheelVertical != listBox && LstGamesWheelVertical != null) LstGamesWheelVertical.SelectedItem = game;
                if (LstGamesWheelHorizontal != listBox && LstGamesWheelHorizontal != null) LstGamesWheelHorizontal.SelectedItem = game;
            }
            finally
            {
                _isSyncingSelection = false;
            }

            // Actualizar la curvatura tras la selección
            Avalonia.Threading.Dispatcher.UIThread.Post(UpdateWheels, Avalonia.Threading.DispatcherPriority.Background);

            // Visibilidad de Paneles inmediata
            if (PnlNoGameSelected != null) PnlNoGameSelected.IsVisible = false;
            if (PnlPlatformDetails != null) PnlPlatformDetails.IsVisible = false;
            
            PnlGameDetails.IsVisible = true;
            PnlGameDetails.Opacity = 1.0;

            // SONIDO: Navegación
            GestorJuegos.Utils.SoundHelper.PlayNavigation();

            _selectedGame = game;
            _isSelectingGame = true; // Evitar disparar eventos de guardado durante la carga

            // --- FONDO DINÁMICO (Fase B) ---
            UpdateDynamicBackground(game);

            // --- Llenar Panel Informativo (Derecha estilo Biblioteca Externa) ---
            TxtInfoName.Text = game.Name;
            TxtInfoPlatform.Text = game.Platform != null ? game.Platform.Name.ToUpper() : "DESCONOCIDO";

            // Calificación (Prioriza la calificación personal del usuario, de lo contrario muestra la de la comunidad)
            double ratingVal = 0;
            if (game.Rating > 0)
            {
                ratingVal = game.Rating / 20.0;
            }
            else if (!string.IsNullOrEmpty(game.CommunityRating))
            {
                string cleanRating = game.CommunityRating.Replace(',', '.').Trim();
                if (double.TryParse(cleanRating, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double commRating))
                {
                    ratingVal = commRating;
                }
            }
            TxtInfoRatingText.Text = ratingVal > 0 ? ratingVal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "0.0";
            int starCount = (int)Math.Round(ratingVal);
            TxtInfoRatingStars.Text = new string('★', starCount) + new string('☆', 5 - starCount);

            // ToolTip de Calificación estilo Biblioteca Externa
            string userRatingStr = game.Rating > 0 ? (game.Rating / 20.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "Ninguno";
            string communityRatingStr = "--";
            if (!string.IsNullOrEmpty(game.CommunityRating))
            {
                string cleanRating = game.CommunityRating.Replace(',', '.').Trim();
                if (double.TryParse(cleanRating, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double commRating))
                {
                    communityRatingStr = commRating.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);
                }
                else
                {
                    communityRatingStr = game.CommunityRating;
                }
            }
            string ratingTooltip = $"Tu Calificación en Estrellas: {userRatingStr}\n" +
                                   $"Calificación en Estrellas de la Comunidad: {communityRatingStr}\n" +
                                   $"Votos Totales de la Calificación en Estrellas de la Comunidad: {game.CommunityRatingCount}";
            ToolTip.SetTip(PnlRatingContainer, ratingTooltip);

            // ToolTip de Progreso rápido
            ToolTip.SetTip(BtnProgressQuick, string.IsNullOrEmpty(game.PlayStatus) ? "No Jugado" : game.PlayStatus);

            // Favorito
            UpdateFavoriteUI();

            // Info Básica
            TxtBaseReleaseDate.Text = game.Year > 0 ? game.Year.ToString() : "--";
            TxtBaseDeveloper.Text = string.IsNullOrEmpty(game.Developer) ? "--" : game.Developer;
            TxtBasePublisher.Text = string.IsNullOrEmpty(game.Publisher) ? "--" : game.Publisher;
            TxtBasePlaytime.Text = $"{game.PlayCount} partidas ({game.PlayStatus})";

            // Bloque INFORMACIÓN (Réplica exacta de Biblioteca Externa)
            
            // 1. Clasificación:
            TxtInfoEsrb.Text = string.IsNullOrEmpty(game.ESRB) ? "--" : game.ESRB;
            
            // 2. Género:
            TxtInfoGenre.Text = string.IsNullOrEmpty(game.Genre) ? "--" : game.Genre;

            // 3. Modo de Juego:
            string playMode = "Un Jugador";
            if (game.Cooperative)
            {
                playMode = "Cooperativo";
            }
            else if (game.MaxPlayers.HasValue && game.MaxPlayers.Value > 1)
            {
                playMode = $"Multijugador ({game.MaxPlayers.Value} jugadores)";
            }
            TxtInfoPlayMode.Text = playMode;

            // 4. Progress:
            TxtInfoProgress.Text = string.IsNullOrEmpty(game.PlayStatus) ? "No Jugado" : game.PlayStatus;
            TxtInfoProgress.Foreground = game.PlayStatus switch
            {
                "Completado" => Avalonia.Media.Brushes.LightGreen,
                "Jugando" => Avalonia.Media.Brushes.LightSkyBlue,
                _ => Avalonia.Media.Brushes.Orange
            };

            // 5. Región:
            TxtInfoRegion.Text = string.IsNullOrEmpty(game.Region) ? "--" : game.Region;

            // 6. Estado:
            TxtInfoStatus.Text = !string.IsNullOrEmpty(game.RomPath) ? "ROM importado" : "No instalado";

            // 7. Portable:
            TxtInfoPortable.Text = "No";

            // 8. Archivo:
            TxtInfoFile.Text = string.IsNullOrEmpty(game.RomPath) ? "--" : Path.GetFileName(game.RomPath);

            // 9. Última vez jugado:
            TxtInfoLastPlayed.Text = game.LastPlayed?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";

            // 10. Fecha de Lanzamiento:
            string releaseDateStr = "--";
            if (!string.IsNullOrEmpty(game.ReleaseDate))
            {
                if (DateTime.TryParse(game.ReleaseDate, out DateTime releaseDateVal))
                {
                    releaseDateStr = releaseDateVal.ToString("dd/MM/yyyy");
                }
                else
                {
                    releaseDateStr = game.ReleaseDate;
                }
            }
            else if (game.Year > 0)
            {
                releaseDateStr = $"01/01/{game.Year}";
            }
            TxtInfoReleaseDate.Text = releaseDateStr;

            // 11. Tipo de Lanzamiento:
            TxtInfoReleaseType.Text = string.IsNullOrEmpty(game.ReleaseType) ? "Released" : game.ReleaseType;

            // 12. Cantidad Máx. de Jugadores:
            TxtInfoMaxPlayers.Text = game.MaxPlayers.HasValue ? game.MaxPlayers.Value.ToString() : "--";

            // Mostrar/Ocultar botones de URL si existen
            BtnWikiUrl.IsVisible = !string.IsNullOrEmpty(game.WikipediaURL);
            BtnVideoUrl.IsVisible = !string.IsNullOrEmpty(game.VideoURL);

            // Descripción
            TxtInfoDescription.Text = string.IsNullOrEmpty(game.Description) ? "Sin descripción disponible." : game.Description;

            // --- Preparar Formulario de Edición (Overlay) ---
            TxtName.Text = game.Name;
            NumYear.Value = game.Year;
            TxtGenre.Text = game.Genre;
            TxtDeveloper.Text = game.Developer;
            TxtPublisher.Text = game.Publisher;
            TxtDescription.Text = game.Description;
            TxtLanguages.Text = game.Languages;

            _currentRoms.Clear();
            if (!string.IsNullOrEmpty(game.RomPath)) _currentRoms.Add(game.RomPath);
            if (!string.IsNullOrEmpty(game.AdditionalRoms))
            {
                foreach(var r in game.AdditionalRoms.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    _currentRoms.Add(r);
                }
            }
            LstRoms.ItemsSource = _currentRoms;

            TxtOverrideEmulator.Text = game.OverrideEmulatorPath;
            TxtOverrideArgs.Text = game.OverrideLaunchArguments;
            ChkIsFavorite.IsChecked = game.IsFavorite;

            var regionItem = CmbRegion.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == game.Region);
            if (regionItem != null) CmbRegion.SelectedItem = regionItem;
            else CmbRegion.SelectedIndex = 0;

            // Sincronizar CmbEditArtType
            string targetType = !string.IsNullOrEmpty(game.SelectedArtType) ? game.SelectedArtType : _settings.PreferredArtType;
            var artTypeItem = CmbEditArtType.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == targetType);
            if (artTypeItem != null) CmbEditArtType.SelectedItem = artTypeItem;
            else CmbEditArtType.SelectedIndex = 0;

            var coverObj = _gameService.GetGameCover(game.Id);
            _selectedGame.CoverType = coverObj?.ImageType ?? "Box - Front";

            // Sincronizar el selector de tipo de arte con la preferencia del usuario o del juego específico
            if (CmbArtType != null)
            {
                bool typeFound = false;
                foreach (var rawItem in CmbArtType.Items)
                {
                    if (rawItem is ComboBoxItem item && item.Content?.ToString() == targetType)
                    {
                        CmbArtType.SelectedItem = item;
                        typeFound = true;
                        break;
                    }
                }
                if (!typeFound && CmbArtType.Items.Count > 0) CmbArtType.SelectedIndex = 3; // Box por defecto
            }

            // CARGAR LA IMAGEN CORRESPONDIENTE AL TIPO SELECCIONADO
            LoadArtTypeImage(targetType);

            // --- CARGAR LOGO PARA DETALLES ---
            bool hasLogo = false;
            try
            {
                BrdDetailLogo.IsVisible = false; // Ocultar por defecto

                // Intentar cargar "Clear Logo" o "Logos"
                byte[]? logoData = _gameService.GetGameExtraImage(game.Id, "Logos") ?? 
                                  _gameService.GetGameExtraImage(game.Id, "Clear Logo");

                if (logoData != null && logoData.Length > 0)
                {
                    using (var ms = new MemoryStream(logoData))
                    {
                        ImgDetailLogo.Source = new Bitmap(ms);
                        BrdDetailLogo.IsVisible = true;
                        hasLogo = true;
                    }
                }
            }
            catch { BrdDetailLogo.IsVisible = false; }

            // Si hay logo, ocultamos el botón de 'Ver todas las imágenes' para que no solape con el logo
            BtnViewAllImages.IsVisible = !hasLogo;
            // El fondo del banner debe seguir siendo visible debajo del logo
            ImgDetailBackground.IsVisible = true;

            // Cargar screenshots y galería
            LoadGameGallery(game.Id);

            // Cargar juegos relacionados
            LoadRelatedGames(game);

            // Rellenar menú desplegable de ROMs
            PopulateRomsFlyout(game);

            // Visibilidad de Paneles
            if (PnlNoGameSelected != null) PnlNoGameSelected.IsVisible = false;
            PnlGameDetails.IsVisible = true;
            
            _isSelectingGame = false; // Permitir cambios de nuevo
        }
    }

    private void UpdateDynamicBackground(Game game)
    {
        int gameId = game.Id;
        System.Threading.Tasks.Task.Run(() => {
            try
            {
                // Intentar Fanart primero, luego captura desde la DB
                string[] types = { "Fanart - Background", "Screenshot - Gameplay", "Background" };
                byte[]? bgData = null;

                foreach (var type in types)
                {
                    bgData = _gameService.GetGameExtraImage(gameId, type);
                    if (bgData != null && bgData.Length > 0) break;
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    // Verificar que seguimos en el mismo juego para evitar parpadeos
                    if (_selectedGame != null && _selectedGame.Id == gameId)
                    {
                        if (bgData != null && bgData.Length > 0)
                        {
                            try
                            {
                                using (var ms = new MemoryStream(bgData))
                                {
                                    var bitmap = new Bitmap(ms);
                                    ImgBackground.Source = bitmap;
                                    ImgBackground.Opacity = 0.3;
                                    ImgDetailBackground.Source = bitmap;
                                }
                            }
                            catch { 
                                ImgBackground.Source = null;
                                ImgBackground.Opacity = 0;
                                ImgDetailBackground.Source = null;
                            }
                        }
                        else
                        {
                            ImgBackground.Source = null;
                            ImgBackground.Opacity = 0;
                            ImgDetailBackground.Source = null;
                        }
                    }
                });
            }
            catch {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    ImgBackground.Source = null;
                    ImgBackground.Opacity = 0;
                    ImgDetailBackground.Source = null;
                });
            }
        });
    }
    private void BtnAddGame_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero una plataforma en el menú superior antes de intentar añadir un juego.");
            return;
        }

        GestorJuegos.Utils.SoundHelper.PlaySelect();
        _selectedGame = new Game { PlatformId = _selectedPlatform.Id, Year = DateTime.Now.Year, DateAdded = DateTime.Now };
        TxtEditGameTitle.Text = "Añadir Nuevo Juego";
        
        TxtName.Text = string.Empty;
        NumYear.Value = _selectedGame.Year;
        TxtGenre.Text = string.Empty;
        TxtDeveloper.Text = string.Empty;
        TxtPublisher.Text = string.Empty;
        TxtDescription.Text = string.Empty;
        TxtLanguages.Text = string.Empty;
        TxtVersion.Text = string.Empty;
        CmbPlayStatus.SelectedIndex = 0;
        SldRating.Value = 0;
        TxtPlayCount.Text = "0";
        TxtDateAdded.Text = DateTime.Now.ToString("dd/MM/yyyy");

        _currentRoms.Clear();
        LstRoms.ItemsSource = _currentRoms;
        TxtOverrideEmulator.Text = string.Empty;
        TxtOverrideArgs.Text = string.Empty;
        ChkIsFavorite.IsChecked = false;
        CmbRegion.SelectedIndex = 0;
        _currentCover = null;
        ImgEditCover.Source = null;

        OverlayEditGame.IsVisible = true;
    }

    private void BtnEditGame_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        GestorJuegos.Utils.SoundHelper.PlaySelect();
        TxtEditGameTitle.Text = "Editar Juego";

        // Cargar datos actuales
        TxtName.Text = _selectedGame.Name;
        NumYear.Value = _selectedGame.Year;
        TxtGenre.Text = _selectedGame.Genre;
        TxtDeveloper.Text = _selectedGame.Developer;
        TxtPublisher.Text = _selectedGame.Publisher;
        TxtDescription.Text = _selectedGame.Description;
        TxtLanguages.Text = _selectedGame.Languages;
        TxtVersion.Text = _selectedGame.Version;
        
        // Estado
        foreach (var rawItem in CmbPlayStatus.Items)
        {
            if (rawItem is ComboBoxItem item && item.Content?.ToString() == _selectedGame.PlayStatus)
            {
                CmbPlayStatus.SelectedItem = item;
                break;
            }
        }
        if (CmbPlayStatus.SelectedItem == null) CmbPlayStatus.SelectedIndex = 0;

        SldRating.Value = _selectedGame.Rating;
        TxtPlayCount.Text = _selectedGame.PlayCount.ToString();
        TxtDateAdded.Text = _selectedGame.DateAdded.ToString("dd/MM/yyyy");

        _currentRoms.Clear();
        if (!string.IsNullOrEmpty(_selectedGame.RomPath)) _currentRoms.Add(_selectedGame.RomPath);
        if (!string.IsNullOrEmpty(_selectedGame.AdditionalRoms))
        {
            foreach (var r in _selectedGame.AdditionalRoms.Split('|')) _currentRoms.Add(r);
        }
        LstRoms.ItemsSource = _currentRoms;

        TxtOverrideEmulator.Text = _selectedGame.OverrideEmulatorPath;
        TxtOverrideArgs.Text = _selectedGame.OverrideLaunchArguments;
        ChkIsFavorite.IsChecked = _selectedGame.IsFavorite;

        // Región
        foreach (var rawItem in CmbRegion.Items)
        {
            if (rawItem is ComboBoxItem item && item.Content?.ToString() == _selectedGame.Region)
            {
                CmbRegion.SelectedItem = item;
                break;
            }
        }
        if (CmbRegion.SelectedItem == null) CmbRegion.SelectedIndex = 0;

        _currentCover = _selectedGame.Cover;
        UpdateCoverImage();

        OverlayEditGame.IsVisible = true;
    }

    private void BtnCancelEditGame_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayBack();
        OverlayEditGame.IsVisible = false;
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;

        GestorJuegos.Utils.SoundHelper.PlaySelect();
        _selectedGame.Name = TxtName.Text ?? string.Empty;
        _selectedGame.Year = (int)(NumYear.Value ?? DateTime.Now.Year);
        _selectedGame.Genre = TxtGenre.Text ?? string.Empty;
        _selectedGame.Developer = TxtDeveloper.Text ?? string.Empty;
        _selectedGame.Publisher = TxtPublisher.Text ?? string.Empty;
        _selectedGame.Description = TxtDescription.Text ?? string.Empty;
        _selectedGame.Languages = TxtLanguages.Text ?? string.Empty;
        _selectedGame.Version = TxtVersion.Text ?? string.Empty;
        _selectedGame.PlayStatus = (CmbPlayStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";
        _selectedGame.Rating = (int)SldRating.Value;
        
        if (_currentRoms.Count > 0)
        {
            _selectedGame.RomPath = _currentRoms[0];
            if (_currentRoms.Count > 1)
                _selectedGame.AdditionalRoms = string.Join("|", _currentRoms.Skip(1));
            else
                _selectedGame.AdditionalRoms = string.Empty;
        }
        else
        {
            _selectedGame.RomPath = string.Empty;
            _selectedGame.AdditionalRoms = string.Empty;
        }
        _selectedGame.OverrideEmulatorPath = TxtOverrideEmulator.Text ?? string.Empty;
        _selectedGame.OverrideLaunchArguments = TxtOverrideArgs.Text ?? string.Empty;
        _selectedGame.IsFavorite = ChkIsFavorite.IsChecked ?? false;
        _selectedGame.Region = (CmbRegion.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "🇺🇸 US";
        
        // Guardar preferencia de arte desde el overlay
        string editArtType = (CmbEditArtType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Box 3D";
        _selectedGame.SelectedArtType = editArtType;
        _selectedGame.CoverType = GetExternalFolderName(editArtType);

        _selectedGame.Cover = _currentCover;

        if (_selectedGame.Id == 0)
        {
            _gameService.AddGame(_selectedGame);
        }
        else
        {
            _gameService.UpdateGame(_selectedGame);
        }

        OverlayEditGame.IsVisible = false;
        LoadGames();
        
        // Refrescar selección en panel derecho tras guardar
        var updatedGame = _currentPlatformGames.FirstOrDefault(g => g.Id == _selectedGame.Id);
        if (updatedGame != null)
        {
            if (LstGames.IsVisible) LstGames.SelectedItem = updatedGame;
            else LstGamesGrid.SelectedItem = updatedGame;
            
            LstGames_SelectionChanged(null, new SelectionChangedEventArgs(null, new List<object>(), new List<object>()));
        }
    }

    private void BtnSyncExternalLib_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;
        
        GestorJuegos.Utils.SoundHelper.PlaySelect();
        
        // Buscar en el XML de Biblioteca Externa
        string lbPath = _settings.ExternalLibraryPath;
        if (!Directory.Exists(lbPath))
        {
            ShowMessage("Configura primero la ruta de la Biblioteca Externa en Ajustes.");
            return;
        }

        string xmlPath = Path.Combine(lbPath, "Data", "Platforms", $"{_selectedPlatform.Name}.xml");
        if (!File.Exists(xmlPath))
        {
            ShowMessage($"No se encontró el XML de metadatos para la plataforma: {_selectedPlatform.Name}");
            return;
        }

        try
        {
            var xdoc = System.Xml.Linq.XDocument.Load(xmlPath);
            var gameElement = xdoc.Descendants("Game")
                .FirstOrDefault(x => x.Element("Title")?.Value?.Equals(_selectedGame.Name, StringComparison.OrdinalIgnoreCase) == true);

            if (gameElement != null)
            {
                // Extraer metadatos
                TxtGenre.Text = gameElement.Element("Genre")?.Value ?? TxtGenre.Text;
                TxtDeveloper.Text = gameElement.Element("Developer")?.Value ?? TxtDeveloper.Text;
                TxtPublisher.Text = gameElement.Element("Publisher")?.Value ?? TxtPublisher.Text;
                TxtDescription.Text = gameElement.Element("Notes")?.Value ?? TxtDescription.Text;
                TxtVersion.Text = gameElement.Element("Version")?.Value ?? TxtVersion.Text;
                
                string? releaseYear = gameElement.Element("ReleaseDate")?.Value;
                if (!string.IsNullOrEmpty(releaseYear) && DateTime.TryParse(releaseYear, out var dt))
                {
                    NumYear.Value = dt.Year;
                }

                string? ratingStr = gameElement.Element("StarRating")?.Value;
                if (float.TryParse(ratingStr, out var rating))
                {
                    SldRating.Value = (int)(rating * 20); // De 0-5 a 0-100
                }

                ShowMessage("Metadatos sincronizados desde Biblioteca Externa correctamente.");
            }
            else
            {
                ShowMessage("No se encontró información exacta para este título en Biblioteca Externa.");
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error al leer el XML: {ex.Message}");
        }
    }

    private void BtnDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && _selectedGame.Id != 0)
        {
            GestorJuegos.Utils.SoundHelper.PlaySelect();
            
            // Replicar el diálogo de confirmación exacto de Biblioteca Externa
            string platformName = _selectedGame.Platform != null ? _selectedGame.Platform.Name : "Desconocida";
            TxtDeleteConfirmMessage.Text = $"¿Estás seguro que deseas borrar permanentemente {platformName} {_selectedGame.Name}?";
            
            // Por defecto, desmarcar el checkbox de borrar multimedia asociada
            ChkDeleteAssociatedMedia.IsChecked = false;
            
            // Mostrar la ventana modal estilo Biblioteca Externa
            OverlayDeleteConfirm.IsVisible = true;
        }
    }

    private void BtnCloseDeleteConfirm_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayBack();
        OverlayDeleteConfirm.IsVisible = false;
    }

    private void BtnDeleteConfirmNo_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayBack();
        OverlayDeleteConfirm.IsVisible = false;
    }

    private void BtnDeleteConfirmYes_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && _selectedGame.Id != 0)
        {
            GestorJuegos.Utils.SoundHelper.PlaySelect();
            
            bool deleteMedia = ChkDeleteAssociatedMedia.IsChecked ?? false;
            
            // Si el checkbox "Delete associated media" está marcado, borramos también las imágenes de CoversDb
            if (deleteMedia)
            {
                try
                {
                    using (var coversContext = new GestorJuegos.Data.CoversDbContext())
                    {
                        var extraImages = coversContext.Images.Where(i => i.GameId == _selectedGame.Id).ToList();
                        if (extraImages.Any()) coversContext.Images.RemoveRange(extraImages);
                        coversContext.SaveChanges();
                    }
                }
                catch { }
            }

            // Ejecutar el borrado principal del juego (que borra el juego y la portada principal)
            _gameService.DeleteGame(_selectedGame.Id);
            
            // Cerrar modal y recargar
            OverlayDeleteConfirm.IsVisible = false;
            LoadGames();
            PnlGameDetails.IsVisible = false;
        }
    }

    private void MenuOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && !string.IsNullOrEmpty(_selectedGame.RomPath))
        {
            string? dir = Path.GetDirectoryName(_selectedGame.RomPath);
            if (Directory.Exists(dir))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
                }
                catch { }
            }
        }
    }

    private void MenuDelete_Click(object? sender, RoutedEventArgs e)
    {
        BtnDelete_Click(sender, e);
    }

    private void MenuPlayStatus_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        if (sender is MenuItem menuItem && menuItem.Tag is string status)
        {
            GestorJuegos.Utils.SoundHelper.PlaySelect();
            _selectedGame.PlayStatus = status;
            _gameService.UpdateGame(_selectedGame);
            
            // Refrescar UI del bloque de información
            TxtInfoProgress.Text = status;
            TxtInfoProgress.Foreground = status switch
            {
                "Completado" => Avalonia.Media.Brushes.LightGreen,
                "Jugando" => Avalonia.Media.Brushes.LightSkyBlue,
                _ => Avalonia.Media.Brushes.Orange
            };
            
            // Actualizar Tooltip del botón flotante de progreso
            ToolTip.SetTip(BtnProgressQuick, status);
            
            // Recargar listas para sincronizar
            LoadGames();
        }
    }

    private async void BtnSelectCover_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Carátula",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count >= 1)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            _currentCover = memoryStream.ToArray();
            UpdateCoverImage();
        }
    }

    private void BtnClearCover_Click(object? sender, RoutedEventArgs e)
    {
        _currentCover = null;
        UpdateCoverImage();
    }

    private void UpdateCoverImage()
    {
        if (_currentCover != null && _currentCover.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(_currentCover);
                var bitmap = new Bitmap(ms);
                ImgCover.Source = bitmap;
                ImgEditCover.Source = bitmap;
            }
            catch
            {
                ImgCover.Source = null;
                ImgEditCover.Source = null;
            }
        }
        else
        {
            ImgCover.Source = null;
            ImgEditCover.Source = null;
        }
    }

    private async void BtnSelectEmulator_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Emulador o Ejecutable",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            TxtEmulatorPath.Text = files[0].Path.LocalPath;
        }
    }

    private async void BtnAddRom_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Añadir Archivo de Juego / ROM",
            AllowMultiple = true
        });

        foreach (var file in files)
        {
            _currentRoms.Add(file.Path.LocalPath);
        }
    }

    private void BtnRemoveRom_Click(object? sender, RoutedEventArgs e)
    {
        if (LstRoms.SelectedItem is string selectedPath)
        {
            _currentRoms.Remove(selectedPath);
        }
    }

    private async void BtnSelectOverrideEmulator_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Ejecutable del Emulador (Override)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Ejecutables") { Patterns = new[] { "*.exe", "*.bat", "*.cmd" } } }
        });

        if (files.Count > 0)
        {
            TxtOverrideEmulator.Text = files[0].TryGetLocalPath() ?? files[0].Name;
            // Si los argumentos estaban vacíos, ponemos el default
            if (string.IsNullOrWhiteSpace(TxtOverrideArgs.Text))
            {
                TxtOverrideArgs.Text = "\"{0}\"";
            }
        }
    }

    private void MenuHelpExternalLib_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "📚 IMPORTAR BIBLIOTECA EXTERNA\n\n" +
            "Esta función permite integrar una colección ya existente de otros gestores de juegos (como Biblioteca Externa, Big Box u otros sistemas basados en XML).\n\n" +
            "¿CÓMO FUNCIONA?:\n" +
            "1. Selecciona la carpeta raíz de tu biblioteca externa en la configuración.\n" +
            "2. El sistema detectará automáticamente los metadatos y las rutas de las imágenes en las carpetas estándar (Data, Images).\n" +
            "3. Se importarán juegos, plataformas, categorías y valoraciones de la comunidad.\n\n" +
            "VENTAJAS:\n" +
            "- Reutiliza todo tu arte multimedia sin duplicar archivos.\n" +
            "- Mantiene la compatibilidad con tus rutas de juegos actuales.\n" +
            "- Permite una transición fluida hacia este gestor sin perder tu progreso.";

        ShowMessage(helpText);
    }

    private void MenuHelpImportFolder_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "📁 IMPORTAR DESDE CARPETA (ESCÁNER LOCAL)\n\n" +
            "Ideal para añadir juegos nuevos o colecciones que tienes organizadas simplemente en carpetas locales.\n\n" +
            "PASOS A SEGUIR:\n" +
            "1. Selecciona la carpeta que contiene las ROMs o ejecutables de una consola.\n" +
            "2. El sistema creará entradas individuales por cada archivo compatible detectado.\n" +
            "3. Tras la importación, puedes usar el botón 'Base Maestra' o 'Vimm' para descargar automáticamente carátulas y descripciones.\n\n" +
            "CONSEJO DE ORO:\n" +
            "Para un reconocimiento del 100%, intenta que el nombre del archivo coincida con el título oficial del juego.";

        ShowMessage(helpText);
    }

    private void MenuHelpEmulator_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "🎮 CONFIGURACIÓN DE EMULADORES\n\n" +
            "Para que los juegos funcionen, debes asignar un ejecutable a cada plataforma:\n\n" +
            "1. Ve a 'Gestionar Plataformas' (icono ⚙️).\n" +
            "2. Selecciona la consola deseada.\n" +
            "3. En 'Ruta del Emulador', busca el archivo .exe de tu emulador.\n" +
            "4. Añade argumentos de línea de comandos si son necesarios (ej: -f -L cores\\snes9x_libretro.dll).\n\n" +
            "NOTA: Puedes cambiar el emulador para un juego individual editando su ficha y yendo a la pestaña 'Lanzamiento'.";

        ShowMessage(helpText);
    }

    private void MenuHelpMultiDisk_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "💿 SOPORTE PARA JUEGOS MULTI-DISCO\n\n" +
            "Si un juego tiene varios discos (CD1, CD2, etc.), puedes agruparlos en una sola ficha:\n\n" +
            "1. Edita el juego y ve a la pestaña 'Archivos'.\n" +
            "2. Pulsa el botón '+' para añadir Disco 2, Disco 3, etc.\n\n" +
            "3. CÓMO JUGAR:\n" +
            "   Al pulsar el botón 'JUGAR', si hay varios discos, aparecerá un selector desplegable (flecha ▼) para elegir qué disco iniciar.";

        ShowMessage(helpText);
    }

    private void MenuHelpDatabase_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "🗄️ BASES DE DATOS Y RESPALDOS\n\n" +
            "• ARQUITECTURA:\n" +
            "  Tus datos se separan para mayor velocidad:\n" +
            "  - GestorJuegos.db: Información de textos.\n" +
            "  - GestorCovers.db: Imágenes y miniaturas.\n\n" +
            "• CONSEJOS:\n" +
            "  - 'Sincronizar Rutas': Úsalo si mueves tu biblioteca externa a otra unidad de disco.\n" +
            "  - 'Base Maestra': Consulta nuestra DB de miles de juegos para rellenar información faltante automáticamente.\n\n" +
            "• RESPALDOS:\n" +
            "  Usa la opción de exportar periódicamente para no perder tus avances y favoritos.";

        ShowMessage(helpText);
    }

    private void MenuAbout_Click(object? sender, RoutedEventArgs e)
    {
        string aboutText = "🎮 GESTOR DE JUEGOS v1.0.9.5\n\n" +
            "Un organizador integral para colecciones de juegos retro, optimizado para grandes bibliotecas y uso con mando.\n\n" +
            "👨‍💻 Autor: Scorpio\n" +
            "📂 Repositorio: https://github.com/scorpio21/Gestordejuegos\n\n" +
            "🔥 NOVEDADES v1.0.9.5:\n" +
            "• Arquitectura de Base de Datos Dual (Datos + Multimedia).\n" +
            "• Sistema de Miniaturas con SkiaSharp.\n" +
            "• Drag & Drop recursivo de carpetas.\n" +
            "• Estadísticas visuales en el Dashboard.\n" +
            "• Filtros temporales y ordenación avanzada.\n\n" +
            "🙏 AGRADECIMIENTOS:\n" +
            "IGDB, TheGamesDB, GameTDB, PalSnesCovers y Vimm's Lair.";

        ShowMessage(aboutText);
    }

    private void BtnLaunchGame_Click(object? sender, RoutedEventArgs e)
    {
        var logLines = new System.Collections.Generic.List<string>();
        logLines.Add($"--- INICIANDO LANZAMIENTO: {DateTime.Now} ---");
        
        try
        {
            if (_selectedGame == null)
            {
                logLines.Add("Error: _selectedGame es null");
                ShowMessage("Por favor, selecciona un juego primero.");
                File.WriteAllLines("launcher_log.txt", logLines);
                return;
            }

            logLines.Add($"Juego: {_selectedGame.Name} (ID: {_selectedGame.Id})");
            logLines.Add($"RomPath: '{_selectedGame.RomPath}'");

            if (string.IsNullOrEmpty(_selectedGame.RomPath))
            {
                logLines.Add("Error: RomPath vacio.");
                ShowMessage("Por favor, asegúrate de haber configurado y guardado la ruta del juego/ROM.");
                File.WriteAllLines("launcher_log.txt", logLines);
                return;
            }

            if (!File.Exists(_selectedGame.RomPath))
            {
                logLines.Add("Error: RomPath no existe en disco.");
                ShowMessage("El archivo del juego no existe en la ruta especificada.");
                File.WriteAllLines("launcher_log.txt", logLines);
                return;
            }

            var platform = _selectedPlatform;
            if (platform == null)
            {
                logLines.Add("Error: _selectedPlatform es null.");
                File.WriteAllLines("launcher_log.txt", logLines);
                return;
            }

            logLines.Add($"Plataforma: {platform.Name} (ID: {platform.Id})");
            
            // Refetch platform directly from DB to ensure we have the absolute latest data
            using (var context = new GestorJuegos.Data.AppDbContext())
            {
                var dbPlatform = context.Platforms.FirstOrDefault(p => p.Id == platform.Id);
                if (dbPlatform != null)
                {
                    logLines.Add($"EmulatorPath DB: '{dbPlatform.EmulatorPath}'");
                    logLines.Add($"LaunchArgs DB: '{dbPlatform.LaunchArguments}'");
                    platform = dbPlatform; // Use fresh data
                }
                else
                {
                    logLines.Add("Error: No se encontró la plataforma en la base de datos.");
                }
            }

            ProcessStartInfo psi = new ProcessStartInfo();

            string finalEmulatorPath = platform.EmulatorPath;
            string finalLaunchArgs = platform.LaunchArguments;

            if (!string.IsNullOrEmpty(_selectedGame.OverrideEmulatorPath))
            {
                finalEmulatorPath = _selectedGame.OverrideEmulatorPath;
                finalLaunchArgs = _selectedGame.OverrideLaunchArguments;
                logLines.Add($"Usando OVERRIDE de juego. EmulatorPath: '{finalEmulatorPath}'");
            }

            if (string.IsNullOrEmpty(finalEmulatorPath))
            {
                logLines.Add("Aviso: EmulatorPath vacío. Usando UseShellExecute = true con RomPath.");
                string targetRom = LstRoms.SelectedItem as string ?? _selectedGame.RomPath;
                psi.FileName = targetRom;
                psi.UseShellExecute = true;
            }
            else
            {
                logLines.Add("EmulatorPath configurado. Verificando...");
                if (!File.Exists(finalEmulatorPath))
                {
                    logLines.Add("Error: finalEmulatorPath no existe en disco.");
                    ShowMessage("La ruta del emulador especificada no existe.");
                    File.WriteAllLines("launcher_log.txt", logLines);
                    return;
                }

                logLines.Add("EmulatorPath OK.");
                psi.FileName = finalEmulatorPath;
                psi.WorkingDirectory = System.IO.Path.GetDirectoryName(finalEmulatorPath) ?? string.Empty;
                
                string targetRom = LstRoms.SelectedItem as string ?? _selectedGame.RomPath;
                string args = string.IsNullOrEmpty(finalLaunchArgs) ? "\"{0}\"" : finalLaunchArgs;
                logLines.Add($"Args base: {args}");
                psi.Arguments = args.Replace("{0}", targetRom);
                logLines.Add($"Args reemplazados: {psi.Arguments}");
                psi.UseShellExecute = false;
            }

            logLines.Add($"-> Iniciando: FileName='{psi.FileName}', Arguments='{psi.Arguments}'");
            GestorJuegos.Utils.SoundHelper.PlayLaunch();
            Process.Start(psi);
            logLines.Add("Proceso iniciado con éxito.");
            
            File.WriteAllLines("launcher_log.txt", logLines);
        }
        catch (Exception ex)
        {
            logLines.Add($"EXCEPCIÓN: {ex.Message}");
            logLines.Add(ex.StackTrace ?? "");
            ShowMessage($"Error al lanzar el juego: {ex.Message}");
            File.WriteAllLines("launcher_log.txt", logLines);
        }
    }

    private async void MenuImportFolders_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleccionar Carpeta Raíz de Colección",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var rootPath = folders[0].Path.LocalPath;
            try
            {
                OverlayProgress.IsVisible = true;
                ProgBarImport.Value = 0;
                TxtProgressTitle.Text = "Procesando Importación...";
                TxtProgressDetail.Text = "Analizando estructura de carpetas...";
                
                _cts = new System.Threading.CancellationTokenSource();

                await System.Threading.Tasks.Task.Run(async () =>
                {
                    var platformDirs = new System.Collections.Generic.List<string>();
                    int gameCount = 0;
                    bool cancelled = false;

                    // CARGAR LISTA NEGRA DESDE JSON
                    var blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { 
                        "Games", "Roms", "CHDs", "Samples", "Artwork", "Bios", "System",
                        "Preproduction", "Add-Ons", "Educational", "Applications", "Demos", 
                        "Video", "Miscellaneous", "Manuals", "Media", "Images", "Covers",
                        "Spain", "España", "Europe", "USA", "Japan", "World", "Asia", "Korea", "Japón", "Europa"
                    };

                    try
                    {
                        string blacklistPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blacklist.json");
                        if (File.Exists(blacklistPath))
                        {
                            var json = File.ReadAllText(blacklistPath);
                            var loadedList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                            if (loadedList != null)
                            {
                                foreach (var item in loadedList) blacklist.Add(item);
                            }
                        }
                        else
                        {
                            // Crear archivo por defecto si no existe (con codificación legible para humanos)
                            var options = new System.Text.Json.JsonSerializerOptions 
                            { 
                                WriteIndented = true,
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };
                            var json = System.Text.Json.JsonSerializer.Serialize(blacklist.ToList(), options);
                            File.WriteAllText(blacklistPath, json);
                        }
                    }
                    catch { }

                    var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { 
                        ".zip", ".7z", ".rar", ".iso", ".bin", ".cue", ".n64", ".v64", ".z64", 
                        ".sfc", ".smc", ".nes", ".gb", ".gbc", ".gba", ".nds", ".3ds", ".cia", 
                        ".pbp", ".cso", ".rvz", ".wbfs", ".gcm", ".gdi", ".chd", ".m3u", ".txt" 
                    };

                    // BUSCADOR RECURSIVO PROFUNDO DE PLATAFORMAS
                    Action<string> findPlatformsRecursive = null!;
                    findPlatformsRecursive = (path) =>
                    {
                        if (_cts.IsCancellationRequested) return;

                        string cleanPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string folderName = Path.GetFileName(cleanPath);
                        if (string.IsNullOrEmpty(folderName)) return;

                        if (blacklist.Contains(folderName))
                        {
                            // Si es una carpeta de la lista negra, seguimos buscando DENTRO 
                            // (por si acaso hay algo útil), pero no la marcamos como plataforma.
                            foreach (var sd in Directory.GetDirectories(cleanPath)) findPlatformsRecursive(sd);
                            return;
                        }

                        // Ignorar nombres que son puramente regiones o contienen patrones de región comunes
                        if (folderName.Contains("(Europe)", StringComparison.OrdinalIgnoreCase) || 
                            folderName.Contains("(USA)", StringComparison.OrdinalIgnoreCase) ||
                            folderName.Contains("(Japan)", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var sd in Directory.GetDirectories(cleanPath)) findPlatformsRecursive(sd);
                            return;
                        }

                        var sDirs = Directory.GetDirectories(cleanPath);
                        
                        // Heurística de juego: Archivos en la carpeta actual
                        var gamesAtThisLevel = Directory.EnumerateFiles(cleanPath).Where(f => {
                            string ext = Path.GetExtension(f).ToLower();
                            return ext != ".txt" && extensions.Contains(ext);
                        }).Take(11).ToList();
                        
                        bool hasGamesAtThisLevel = gamesAtThisLevel.Count > 0;
                        bool hasHighDensity = gamesAtThisLevel.Count > 10;

                        // NUEVA HEURÍSTICA: ¿Alguna subcarpeta es "Games" o "Roms" y contiene archivos de juego?
                        bool contentSubfolderHasGames = sDirs.Any(sd => {
                            string sn = Path.GetFileName(sd);
                            if (sn.Equals("Games", StringComparison.OrdinalIgnoreCase) || sn.Equals("Roms", StringComparison.OrdinalIgnoreCase))
                            {
                                try {
                                    return Directory.EnumerateFiles(sd).Any(f => {
                                        string ext = Path.GetExtension(f).ToLower();
                                        return ext != ".txt" && extensions.Contains(ext);
                                    });
                                } catch { return false; }
                            }
                            return false;
                        });

                        // Heurística de regiones (carpetas hijo que indican que ESTA es la plataforma)
                        bool hasRegionSubdirs = sDirs.Any(sd => {
                            string n = Path.GetFileName(sd).ToLower();
                            return n == "spain" || n == "españa" || n == "europe" || n == "usa" || n == "japan" || 
                                   n == "world" || n == "asia" || n.Contains("(europe)") || n.Contains("(usa)");
                        });

                        // Heurística de nombre estándar o explícito
                        bool looksLikePlatform = folderName.Contains(" - ") || 
                                                 folderName.Equals("MAME", StringComparison.OrdinalIgnoreCase) ||
                                                 folderName.Contains("Arcade", StringComparison.OrdinalIgnoreCase) ||
                                                 folderName.Contains("System", StringComparison.OrdinalIgnoreCase) ||
                                                 folderName.Contains("Nintendo", StringComparison.OrdinalIgnoreCase) ||
                                                 folderName.Contains("Sega", StringComparison.OrdinalIgnoreCase) ||
                                                 folderName.Contains("PlayStation", StringComparison.OrdinalIgnoreCase);

                        // REFINAMIENTO: Si el nombre contiene una marca (como Sega o Nintendo) pero NO TIENE JUEGOS DIRECTOS
                        // y TIENE subcarpetas, es probable que sea una categoría (ej: RomRoot\Sega) y no la plataforma.
                        bool isCategoryOnly = (folderName.Equals("Sega", StringComparison.OrdinalIgnoreCase) || 
                                              folderName.Equals("Nintendo", StringComparison.OrdinalIgnoreCase) ||
                                              folderName.Equals("Atari", StringComparison.OrdinalIgnoreCase) ||
                                              folderName.Equals("Capcom", StringComparison.OrdinalIgnoreCase) ||
                                              folderName.Equals("SNK", StringComparison.OrdinalIgnoreCase)) 
                                              && !hasGamesAtThisLevel && !contentSubfolderHasGames && sDirs.Length > 0;

                        // NUEVA HEURÍSTICA: Detectar si es un "Juego en Carpeta Propia" (típico de MAME o PC)
                        // Si solo hay un archivo de juego y su nombre coincide con la carpeta, NO es una plataforma.
                        bool isSingleGameFolder = false;
                        if (hasGamesAtThisLevel && gamesAtThisLevel.Count == 1)
                        {
                            string singleGameName = Path.GetFileNameWithoutExtension(gamesAtThisLevel[0]);
                            if (singleGameName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                            {
                                isSingleGameFolder = true;
                            }
                        }

                        if ((hasGamesAtThisLevel || hasRegionSubdirs || looksLikePlatform || hasHighDensity || contentSubfolderHasGames) 
                             && !isCategoryOnly && !isSingleGameFolder)
                        {
                            platformDirs.Add(cleanPath);
                        }
                        else
                        {
                            foreach (var sd in sDirs) findPlatformsRecursive(sd);
                        }
                    };

                    // Escanear recursivamente a partir del rootPath seleccionado.
                    // Esto permite tanto seleccionar una carpeta con varias plataformas como una plataforma directamente.
                    findPlatformsRecursive(rootPath);

                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        var allFinalDirs = platformDirs.Distinct().ToList();
                        foreach (var pDir in allFinalDirs)
                        {
                            if (_cts.IsCancellationRequested) { cancelled = true; break; }
                            
                            string pName = Path.GetFileName(pDir);
                            if (string.IsNullOrEmpty(pName)) continue;
                            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                                TxtProgressDetail.Text = $"Importando: {pName}...";
                            });

                            Platform? platform = context.Platforms.FirstOrDefault(p => p.Name == pName);
                            if (platform == null)
                            {
                                platform = new Platform { Name = pName, Category = DetectCategory(pName) };
                                context.Platforms.Add(platform);
                                context.SaveChanges();
                            }

                            // Escaneo RECURSIVO MANUAL (Stack-based) para encontrar las ROMs en regiones
                            var gameFiles = new System.Collections.Generic.List<string>();
                            var dirStack = new System.Collections.Generic.Stack<string>();
                            dirStack.Push(pDir);

                            while (dirStack.Count > 0)
                            {
                                if (_cts.IsCancellationRequested) break;
                                string currentDir = dirStack.Pop();
                                try
                                {
                                    foreach (var f in Directory.GetFiles(currentDir))
                                    {
                                        if (extensions.Contains(Path.GetExtension(f))) gameFiles.Add(f);
                                    }
                                    foreach (var d in Directory.GetDirectories(currentDir)) dirStack.Push(d);
                                }
                                catch { }
                            }

                            if (gameFiles.Count > 0)
                            {
                                var existingPaths = new HashSet<string>(context.Games
                                    .Where(g => g.PlatformId == platform.Id && !string.IsNullOrEmpty(g.RomPath))
                                    .Select(g => g.RomPath), StringComparer.OrdinalIgnoreCase);

                                var existingGameKeys = new HashSet<string>(context.Games
                                    .Where(g => g.PlatformId == platform.Id)
                                    .AsEnumerable() // Forzar evaluación local para evitar problemas de traducción con proyecciones complejas
                                    .Select(g => $"{g.Name}|{g.Region}|{g.Languages}"), StringComparer.OrdinalIgnoreCase);

                                var newGames = new List<Game>();
                                var gamesToUpdate = new List<Game>();
                                var drossPatterns = LoadDrossPatterns();

                                for (int i = 0; i < gameFiles.Count; i++)
                                {
                                    if (_cts.IsCancellationRequested) { cancelled = true; break; }
                                    string filePath = gameFiles[i];
                                    if (existingPaths.Contains(filePath)) continue;

                                    string fileName = Path.GetFileName(filePath);
                                    if (fileName.Equals("lista.txt", StringComparison.OrdinalIgnoreCase)) continue;

                                    if (ImportService.IsDross(fileName, drossPatterns)) continue;

                                    if (i % 20 == 0)
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                                            ProgBarImport.Value = (i * 100) / gameFiles.Count;
                                            TxtProgressDetail.Text = $"[{pName}] {i}/{gameFiles.Count}: {fileName}";
                                        });
                                    }

                                    await System.Threading.Tasks.Task.Delay(1);
                                    var game = ImportService.ParseGameLine(fileName, platform.Id);
                                    game.RomPath = filePath;
                                    game.DateAdded = DateTime.Now;
                                    
                                    string uniqueKey = $"{game.Name}|{game.Region}|{game.Languages}";
                                    if (!existingGameKeys.Contains(uniqueKey))
                                    {
                                        newGames.Add(game);
                                        existingGameKeys.Add(uniqueKey);
                                        gameCount++;
                                    }
                                    else
                                    {
                                        // Búsqueda más segura de juegos existentes
                                        var existingGame = context.Games
                                            .AsEnumerable()
                                            .FirstOrDefault(g => g.PlatformId == platform.Id && 
                                                            g.Name.Equals(game.Name, StringComparison.OrdinalIgnoreCase) && 
                                                            g.Region == game.Region && 
                                                            g.Languages == game.Languages);
                                                            
                                        if (existingGame != null && string.IsNullOrEmpty(existingGame.RomPath))
                                        {
                                            existingGame.RomPath = filePath;
                                            gamesToUpdate.Add(existingGame);
                                        }
                                    }

                                    if (newGames.Count >= 500)
                                    {
                                        _gameService.AddGamesBatch(newGames);
                                        newGames.Clear();
                                    }
                                    if (gamesToUpdate.Count >= 500)
                                    {
                                        _gameService.UpdateGamesBatch(gamesToUpdate);
                                        gamesToUpdate.Clear();
                                    }
                                }

                                if (newGames.Any()) _gameService.AddGamesBatch(newGames);
                                if (gamesToUpdate.Any()) _gameService.UpdateGamesBatch(gamesToUpdate);
                            }
                            platform.LastScanDate = DateTime.Now;
                            context.Platforms.Update(platform);
                            context.SaveChanges();
                        }
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        OverlayProgress.IsVisible = false;
                        LoadPlatforms();
                        LoadDashboard();
                        string status = cancelled ? "Importación CANCELADA" : "¡Importación finalizada!";
                        ShowMessage($"{status}\n\nSe han detectado y procesado las plataformas reales correctamente.\nTotal de juegos nuevos añadidos: {gameCount}");
                    });
                });
            }
            catch (Exception ex)
            {
                OverlayProgress.IsVisible = false;
                
                // Registro detallado de errores
                try
                {
                    var logLines = new List<string>
                    {
                        $"--- ERROR DE IMPORTACIÓN [{DateTime.Now}] ---",
                        $"Mensaje: {ex.Message}",
                        $"StackTrace: {ex.StackTrace}",
                        $"InnerException: {ex.InnerException?.Message}",
                        "-------------------------------------------",
                        ""
                    };
                    File.AppendAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import_error_log.txt"), logLines);
                }
                catch { }

                ShowMessage($"Error durante la importación: {ex.Message}\n\nRevisa 'import_error_log.txt' para más detalles.");
            }
        }
    }

    private async void MenuScanLocalCovers_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero una plataforma para asociar las carátulas.");
            return;
        }

        GestorJuegos.Utils.SoundHelper.PlaySelect();

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleccionar Carpeta de Carátulas",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var coverPath = folders[0].Path.LocalPath;
            try
            {
                OverlayProgress.IsVisible = true;
                ProgBarImport.Value = 0;
                TxtProgressTitle.Text = "Buscando Carátulas...";
                TxtProgressDetail.Text = "Escaneando archivos de imagen...";
                
                _cts = new System.Threading.CancellationTokenSource();

                await System.Threading.Tasks.Task.Run(() =>
                {
                    var extensions = new[] { ".png", ".jpg", ".jpeg" };
                    var coverFiles = new List<(string Path, string Type)>();
                    
                    // Búsqueda recursiva segura (evita UnauthorizedAccessException)
                    // --- OPTIMIZACIÓN DE RUTA PARA BIBLIOTECA EXTERNA ---
                    // Si el usuario eligió la raíz de 'Images', intentamos ir directamente a la carpeta de la plataforma
                    string platformSpecificPath = Path.Combine(coverPath, _selectedPlatform.Name);
                    string effectiveScanPath = Directory.Exists(platformSpecificPath) ? platformSpecificPath : coverPath;

                    var stack = new Stack<string>();
                    stack.Push(effectiveScanPath);

                    while (stack.Count > 0)
                    {
                        string currentPath = stack.Pop();
                        try
                        {
                            // Determinar el tipo basándonos en la subcarpeta relativa
                            string relativePath = Path.GetRelativePath(effectiveScanPath, currentPath);
                            string detectedType = "Box"; // Default

                            if (relativePath != ".")
                            {
                                string firstSubfolder = relativePath.Split(Path.DirectorySeparatorChar)[0];
                                detectedType = firstSubfolder;
                            }

                            // Mapeo inteligente de nombres comunes a tipos de DB
                            if (detectedType.Contains("3D", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_3D";
                            else if (detectedType.Contains("Logo", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Clear", StringComparison.OrdinalIgnoreCase)) detectedType = "Logos";
                            else if (detectedType.Contains("Snap", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Screen", StringComparison.OrdinalIgnoreCase)) detectedType = "Snap";
                            else if (detectedType.Contains("Cart", StringComparison.OrdinalIgnoreCase)) detectedType = "Cart_Front";
                            else if (detectedType.Contains("Disc", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Support", StringComparison.OrdinalIgnoreCase)) detectedType = "Disc";
                            else if (detectedType.Contains("Fanart", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Back", StringComparison.OrdinalIgnoreCase)) detectedType = "Fanart - Background";
                            else if (detectedType.Contains("Full", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_Full";
                            else if (detectedType.Contains("Front", StringComparison.OrdinalIgnoreCase)) detectedType = "Box";

                            foreach (var f in Directory.GetFiles(currentPath))
                            {
                                if (extensions.Contains(Path.GetExtension(f).ToLower()))
                                    coverFiles.Add((f, detectedType));
                            }

                            foreach (var d in Directory.GetDirectories(currentPath))
                                stack.Push(d);
                        }
                        catch { }
                    }

                    int matchCount = 0;
                    int imagesCount = 0;
                    bool cancelled = false;
                    
                    var logLines = new List<string>();
                    logLines.Add($"--- INICIO ESCANEO RECURSIVO: {DateTime.Now} ---");
                    logLines.Add($"Plataforma: {_selectedPlatform.Name}");
                    logLines.Add($"Carpeta origen: {coverPath}");
                    logLines.Add($"Archivos encontrados en carpetas: {coverFiles.Count}");
                    logLines.Add("----------------------------------------------");

                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        var games = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).ToList();
                        var gamesToUpdate = new List<Game>();

                        for (int i = 0; i < games.Count; i++)
                        {
                            if (_cts.IsCancellationRequested) { cancelled = true; break; }
                            
                            var game = games[i];
                            
                            int currentI = i;
                            int totalI = games.Count;
                            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                                ProgBarImport.Value = (currentI * 100) / totalI;
                                TxtProgressDetail.Text = $"Procesando: {game.Name} ({currentI}/{totalI})";
                            });

                            // Normalizar nombre del juego para búsqueda (más permisivo)
                            string cleanGameName = Regex.Replace(game.Name, @"[^a-zA-Z0-9]", "").ToLower();

                            var matches = coverFiles.Where(f => {
                                string fileName = Path.GetFileNameWithoutExtension(f.Path);
                                string cleanFileName = Regex.Replace(fileName, @"[^a-zA-Z0-9]", "").ToLower();
                                return cleanFileName == cleanGameName || cleanFileName.Contains(cleanGameName) || cleanGameName.Contains(cleanFileName);
                            }).ToList();

                            if (matches.Any())
                            {
                                try 
                                {
                                    logLines.Add($"[JUEGO] {game.Name}: Encontradas {matches.Count} imágenes.");
                                    
                                    // 1. Decidir carátula principal (prioridad a Box_3D o Box)
                                    var mainMatch = matches.Any(m => m.Type == "Box_3D") 
                                                   ? matches.First(m => m.Type == "Box_3D") 
                                                   : (matches.Any(m => m.Type == "Box") 
                                                      ? matches.First(m => m.Type == "Box") 
                                                      : matches.First());

                                    // Solo actualizar carátula principal si no tiene una o si la nueva es de mejor tipo (3D)
                                    if (game.Cover == null || game.Cover.Length == 0 || mainMatch.Type == "Box_3D")
                                    {
                                        game.Cover = File.ReadAllBytes(mainMatch.Path);
                                        game.CoverType = mainMatch.Type;
                                        logLines.Add($"   -> Carátula principal: {mainMatch.Type} ({Path.GetFileName(mainMatch.Path)})");
                                    }

                                    // 2. Añadir el resto como ExtraImages
                                    if (game.ExtraImages == null) game.ExtraImages = new List<GameImage>();

                                    foreach (var m in matches)
                                    {
                                        // No duplicar la principal en extras si ya es la misma
                                        if (m.Path == mainMatch.Path) continue;

                                        // Evitar duplicados del mismo tipo en extras
                                        if (!game.ExtraImages.Any(ei => ei.ImageType == m.Type))
                                        {
                                            game.ExtraImages.Add(new GameImage 
                                            { 
                                                ImageType = m.Type, 
                                                ImageData = File.ReadAllBytes(m.Path) 
                                            });
                                            logLines.Add($"   -> Extra: {m.Type} ({Path.GetFileName(m.Path)})");
                                            imagesCount++;
                                        }
                                    }

                                    gamesToUpdate.Add(game);
                                    matchCount++;
                                    
                                    if (gamesToUpdate.Count >= 25) 
                                    {
                                        _gameService.UpdateGamesBatch(gamesToUpdate);
                                        gamesToUpdate.Clear();
                                    }
                                } catch (Exception ex) {
                                    logLines.Add($"   [ERROR] Error procesando {game.Name}: {ex.Message}");
                                }
                            }
                        }
                        
                        if (gamesToUpdate.Any())
                        {
                            _gameService.UpdateGamesBatch(gamesToUpdate);
                        }
                    }

                    logLines.Add("----------------------------------------------");
                    logLines.Add($"RESUMEN: {matchCount} juegos actualizados, {imagesCount} imágenes extra añadidas.");
                    logLines.Add($"--- FIN ESCANEO: {DateTime.Now} ---");
                    
                    try { File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import_covers_log.txt"), logLines); } catch { }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        OverlayProgress.IsVisible = false;
                        LoadGames();
                        string status = cancelled ? "Escaneo CANCELADO" : "Escaneo de carátulas finalizado";
                        ShowMessage($"{status}\n\nSe han actualizado {matchCount} juegos con un total de {imagesCount} imágenes adicionales.\n\nRevisa 'import_covers_log.txt' para ver el detalle.");
                    });
                });
            }
            catch (Exception ex)
            {
                OverlayProgress.IsVisible = false;
                ShowMessage($"Error durante el escaneo de carátulas: {ex.Message}");
            }
        }
    }

    private async void MenuMassiveScanCovers_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlaySelect();

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleccionar Carpeta Raíz de Imágenes (ej: E:\\LaunchBox\\Images)",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var rootPath = folders[0].Path.LocalPath;
            try
            {
                OverlayProgress.IsVisible = true;
                ProgBarImport.Value = 0;
                TxtProgressTitle.Text = "Escaneo Masivo de Arte...";
                TxtProgressDetail.Text = "Detectando plataformas y escaneando archivos...";
                
                _cts = new System.Threading.CancellationTokenSource();

                await System.Threading.Tasks.Task.Run(() =>
                {
                    var extensions = new[] { ".png", ".jpg", ".jpeg" };
                    int totalGamesUpdated = 0;
                    int totalImagesAdded = 0;
                    bool cancelled = false;
                    
                    var logLines = new List<string>();
                    logLines.Add($"--- INICIO ESCANEO MASIVO: {DateTime.Now} ---");
                    logLines.Add($"Carpeta raíz: {rootPath}");
                    logLines.Add("----------------------------------------------");

                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        var allPlatforms = context.Platforms.Include(p => p.AlternateNames).ToList();
                        int totalPlats = allPlatforms.Count;

                        for (int p = 0; p < totalPlats; p++)
                        {
                            if (_cts.IsCancellationRequested) { cancelled = true; break; }
                            var plat = allPlatforms[p];

                            // Búsqueda robusta de carpeta de plataforma (Nombre principal o alternativos)
                            string platformPath = "";
                            var possibleFolderNames = new List<string> { plat.Name };
                            if (plat.AlternateNames != null) possibleFolderNames.AddRange(plat.AlternateNames.Select(a => a.AlternateName));

                            foreach (var folderName in possibleFolderNames)
                            {
                                string checkPath = Path.Combine(rootPath, folderName);
                                if (Directory.Exists(checkPath))
                                {
                                    platformPath = checkPath;
                                    break;
                                }
                            }

                            if (string.IsNullOrEmpty(platformPath)) continue;

                            int currentPlatIndex = p;
                            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                                ProgBarImport.Value = (currentPlatIndex * 100) / totalPlats;
                                TxtProgressDetail.Text = $"Plataforma ({currentPlatIndex + 1}/{totalPlats}): {plat.Name}";
                            });

                            // 1. Recolectar e Indexar archivos para esta plataforma (MUCHO MÁS RÁPIDO)
                            var imageMap = new Dictionary<string, List<(string Path, string Type)>>(StringComparer.OrdinalIgnoreCase);
                            var stack = new Stack<string>();
                            stack.Push(platformPath);

                            while (stack.Count > 0)
                            {
                                string currentPath = stack.Pop();
                                try
                                {
                                    string relativePath = Path.GetRelativePath(platformPath, currentPath);
                                    string detectedType = "Box"; 
                                    if (relativePath != ".") detectedType = relativePath.Split(Path.DirectorySeparatorChar)[0];

                                    // Mapeo de tipos
                                    if (detectedType.Contains("3D", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_3D";
                                    else if (detectedType.Contains("Logo", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Clear", StringComparison.OrdinalIgnoreCase)) detectedType = "Logos";
                                    else if (detectedType.Contains("Snap", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Screen", StringComparison.OrdinalIgnoreCase)) detectedType = "Snap";
                                    else if (detectedType.Contains("Cart", StringComparison.OrdinalIgnoreCase)) detectedType = "Cart_Front";
                                    else if (detectedType.Contains("Disc", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Support", StringComparison.OrdinalIgnoreCase)) detectedType = "Disc";
                                    else if (detectedType.Contains("Fanart", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Back", StringComparison.OrdinalIgnoreCase)) detectedType = "Fanart - Background";
                                    else if (detectedType.Contains("Full", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_Full";
                                    else if (detectedType.Contains("Front", StringComparison.OrdinalIgnoreCase)) detectedType = "Box";

                                    foreach (var f in Directory.GetFiles(currentPath))
                                    {
                                        if (extensions.Contains(Path.GetExtension(f).ToLower()))
                                        {
                                            string fileName = Path.GetFileNameWithoutExtension(f);
                                            // Limpiar sufijos de LaunchBox (-01, -02...)
                                            if (fileName.Length > 3 && fileName[fileName.Length-3] == '-') fileName = fileName.Substring(0, fileName.Length - 3);
                                            
                                            string cleanKey = Regex.Replace(fileName, @"[^a-zA-Z0-9]", "").ToLower();
                                            if (string.IsNullOrEmpty(cleanKey)) continue;

                                            if (!imageMap.ContainsKey(cleanKey)) imageMap[cleanKey] = new List<(string, string)>();
                                            imageMap[cleanKey].Add((f, detectedType));
                                        }
                                    }
                                    foreach (var d in Directory.GetDirectories(currentPath)) stack.Push(d);
                                } catch { }
                            }

                            // OPTIMIZACIÓN: Crear lista ordenada de llaves para búsqueda ultra-rápida de prefijos
                            var sortedKeys = imageMap.Keys.OrderBy(k => k).ToList();

                            // 2. Procesar juegos usando el mapa indexado con verificación de caché
                            var games = context.Games.Where(g => g.PlatformId == plat.Id).ToList();
                            
                            // Obtener qué imágenes YA existen para estos juegos (para no repetir escaneo innecesario)
                            ILookup<int, string> existingImagesMap;
                            using (var coversContext = new GestorJuegos.Data.CoversDbContext())
                            {
                                var gameIds = games.Select(g => g.Id).ToList();
                                existingImagesMap = coversContext.Images
                                    .Where(img => gameIds.Contains(img.GameId))
                                    .Select(img => new { img.GameId, img.ImageType })
                                    .ToLookup(x => x.GameId, x => x.ImageType);
                            }

                            var gamesToUpdateBatch = new List<Game>();

                            for (int i = 0; i < games.Count; i++)
                            {
                                if (_cts.IsCancellationRequested) { cancelled = true; break; }
                                var game = games[i];
                                
                                // Actualización de UI controlada
                                if (i % 10 == 0 || i == games.Count - 1)
                                {
                                    string currentGameName = game.Name;
                                    int currentIdx = i;
                                    int totalG = games.Count;
                                    int currentPlatIdx = p;

                                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                                        TxtProgressDetail.Text = $"[{plat.Name}] {currentIdx}/{totalG}: {currentGameName}";
                                        ProgBarImport.Value = ((double)currentPlatIdx / totalPlats * 100) + ((double)currentIdx / totalG * (100.0 / totalPlats));
                                    });
                                }

                                string cleanGameName = Regex.Replace(game.Name, @"[^a-zA-Z0-9]", "").ToLower();
                                if (string.IsNullOrEmpty(cleanGameName)) continue;

                                // BÚSQUEDA INTELIGENTE BIDIRECCIONAL (Fix #18 mejorado)
                                List<(string Path, string Type)> matches = new List<(string, string)>();
                                
                                // 1. Intentar coincidencia exacta primero
                                if (imageMap.TryGetValue(cleanGameName, out var exactMatches))
                                {
                                    matches.AddRange(exactMatches);
                                }
                                
                                // 2. Búsqueda Binaria de prefijo y sufijo (Bidireccional)
                                int index = sortedKeys.BinarySearch(cleanGameName, StringComparer.OrdinalIgnoreCase);
                                if (index < 0) index = ~index;

                                // Escanear hacia adelante (archivos que EMPIEZAN por el nombre del juego)
                                for (int j = index; j < sortedKeys.Count; j++)
                                {
                                    string key = sortedKeys[j];
                                    if (key.StartsWith(cleanGameName, StringComparison.OrdinalIgnoreCase))
                                        matches.AddRange(imageMap[key]);
                                    else break;
                                }

                                // Escanear hacia atrás (nombres de juego que EMPIEZAN por el nombre del archivo)
                                // Útil si el juego es "Super Mario World (USA)" y el archivo es "Super Mario World"
                                for (int j = index - 1; j >= 0; j--)
                                {
                                    string key = sortedKeys[j];
                                    if (cleanGameName.StartsWith(key, StringComparison.OrdinalIgnoreCase) && key.Length > 3)
                                        matches.AddRange(imageMap[key]);
                                    else if (!key.StartsWith(cleanGameName.Substring(0, Math.Min(3, cleanGameName.Length))))
                                        break; // Detener si ya no comparten ni las primeras 3 letras
                                }

                                if (matches.Any())
                                {
                                    try
                                    {
                                        // Eliminar duplicados de rutas en los matches
                                        matches = matches.GroupBy(m => m.Path).Select(g => g.First()).ToList();

                                        var mainMatch = matches.OrderByDescending(m => m.Type == "Box_3D").ThenByDescending(m => m.Type == "Box").First();

                                        bool needsUpdate = false;
                                        // Solo procesar carátula principal si falta o si podemos mejorarla a 3D
                                        if (game.Cover == null || game.Cover.Length == 0 || (mainMatch.Type == "Box_3D" && game.CoverType != "Box_3D"))
                                        {
                                            game.Cover = File.ReadAllBytes(mainMatch.Path);
                                            game.CoverType = mainMatch.Type;
                                            needsUpdate = true;
                                        }

                                        // Imágenes extra: Solo añadir las que NO existan ya
                                        var currentExtras = existingImagesMap[game.Id].ToHashSet();
                                        foreach (var m in matches)
                                        {
                                            if (m.Path == mainMatch.Path) continue;
                                            
                                            if (!currentExtras.Contains(m.Type))
                                            {
                                                game.ExtraImages.Add(new GameImage { GameId = game.Id, ImageType = m.Type, ImageData = File.ReadAllBytes(m.Path) });
                                                totalImagesAdded++;
                                                currentExtras.Add(m.Type);
                                                needsUpdate = true;
                                            }
                                        }
                                        
                                        if (needsUpdate)
                                        {
                                            gamesToUpdateBatch.Add(game);
                                            totalGamesUpdated++;
                                            
                                            if (gamesToUpdateBatch.Count >= 50)
                                            {
                                                _gameService.UpdateGamesBatch(gamesToUpdateBatch);
                                                gamesToUpdateBatch.Clear();
                                            }
                                        }
                                    } catch { }
                                }
                            }

                            // Guardar resto de la plataforma
                            if (gamesToUpdateBatch.Any())
                            {
                                _gameService.UpdateGamesBatch(gamesToUpdateBatch);
                                gamesToUpdateBatch.Clear();
                            }
                        }
                    }

                    logLines.Add("----------------------------------------------");
                    logLines.Add($"RESUMEN FINAL: {totalGamesUpdated} juegos actualizados, {totalImagesAdded} imágenes extra añadidas.");
                    logLines.Add($"--- FIN ESCANEO MASIVO: {DateTime.Now} ---");
                    
                    try { File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "massive_scan_log.txt"), logLines); } catch { }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        OverlayProgress.IsVisible = false;
                        LoadGames();
                        string status = cancelled ? "Escaneo Masivo CANCELADO" : "Escaneo Masivo finalizado";
                        ShowMessage($"{status}\n\nSe han actualizado {totalGamesUpdated} juegos en múltiples plataformas.\nTotal de imágenes adicionales: {totalImagesAdded}.\n\nRevisa 'massive_scan_log.txt' para más detalles.");
                    });
                });
            }
            catch (Exception ex)
            {
                OverlayProgress.IsVisible = false;
                ShowMessage($"Error durante el escaneo masivo: {ex.Message}");
            }
        }
    }

    private void MenuCleanupOrphans_Click(object? sender, RoutedEventArgs e)
    {
        var orphaned = _gameService.GetOrphanedGames();
        if (orphaned.Count == 0)
        {
            ShowMessage("No se han encontrado juegos huérfanos. Todas las rutas de ROM son válidas.");
            return;
        }

        TxtConfirmTitle.Text = "Limpiar Juegos Huérfanos";
        TxtConfirmContent.Text = $"Se han encontrado {orphaned.Count} juegos cuya ruta de ROM ya no existe en el disco.\n\n" +
                               "¿Deseas eliminar estos registros de la base de datos? Esta acción no afectará a tus archivos físicos.";
        
        _onConfirmAction = () => {
            _gameService.DeleteGames(orphaned.Select(g => g.Id).ToList());
            LoadGames();
            LoadDashboard();
            ShowMessage($"Se han eliminado {orphaned.Count} registros huérfanos con éxito.");
        };
        OverlayConfirm.IsVisible = true;
    }

    private void BtnCheckDuplicates_Click(object? sender, RoutedEventArgs e)
    {
        PnlDashboard.IsVisible = false;
        PnlGlobalSearch.IsVisible = true;
        // PnlHeaderToggles.IsVisible = false; (Eliminado)
        PnlPagination.IsVisible = false;
        PnlGameDetails.IsVisible = false;

        TxtSearchStatus.Text = "Buscando duplicados en toda la colección...";

        using var context = new GestorJuegos.Data.AppDbContext();
        
        // Agrupar por nombre y región para encontrar repetidos
        var duplicateGroups = context.Games
            .GroupBy(g => new { g.Name, g.Region })
            .Where(group => group.Count() > 1)
            .Select(group => new { group.Key.Name, group.Key.Region })
            .ToList();

        var duplicateGames = new List<Game>();
        foreach (var group in duplicateGroups)
        {
            var gamesInGroup = context.Games
                .Include(g => g.Platform)
                .Where(g => g.Name == group.Name && g.Region == group.Region)
                .ToList();
            duplicateGames.AddRange(gamesInGroup);
        }

        LstGlobalSearchResults.ItemsSource = duplicateGames.OrderBy(g => g.Name).ToList();
        TxtSearchStatus.Text = duplicateGames.Count > 0 
            ? $"Se han encontrado {duplicateGames.Count} juegos duplicados" 
            : "No se han encontrado juegos duplicados en tu colección.";
    }

    private void MenuManageDross_Click(object? sender, RoutedEventArgs e)
    {
        string drossPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dross_filter.json");
        if (!File.Exists(drossPath))
        {
            var defaultDross = new List<string> { "(Demo)", "(Proto)", "(Sample)", "(Beta)", "[b]", "[t]", "[h]" };
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(drossPath, System.Text.Json.JsonSerializer.Serialize(defaultDross, options));
        }

        try
        {
            Process.Start(new ProcessStartInfo(drossPath) { UseShellExecute = true });
            ShowMessage("Se ha abierto el archivo 'dross_filter.json'.\n\nEdita la lista de palabras clave que deseas ignorar durante la importación y guarda el archivo.");
        }
        catch
        {
            ShowMessage("No se pudo abrir el archivo de filtros automáticamente. Puedes encontrarlo y editarlo en: " + drossPath);
        }
    }

    // --- CONFIGURACIÓN ---
    private async void MenuSettings_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        
        var optionsWin = new OpcionesWindow(_settings);
        var result = await optionsWin.ShowDialog<bool>(this);
        if (result)
        {
            SoundHelper.IsEnabled = _settings.EnableSoundEffects;
            ApplyTheme();
            ApplySearchFilter(); // Refrescar el listado con el nuevo tipo de arte preferido
            SaveSettings();
            ShowMessage("Configuración guardada correctamente.");
        }
    }

    private void MenuBatchScrapeVimm_Click(object? sender, RoutedEventArgs e)
    {
        // Abrir el selector de sistema de Vimm
        var platforms = VimmVaultService.GetSupportedPlatforms();
        CmbVimmSystem.ItemsSource = platforms;
        
        // Intentar pre-seleccionar si hay plataforma seleccionada
        if (_selectedPlatform != null)
        {
            var bestMatch = platforms.FirstOrDefault(p => p.Key.Contains(_selectedPlatform.Name, StringComparison.OrdinalIgnoreCase) || 
                                                        _selectedPlatform.Name.Contains(p.Key, StringComparison.OrdinalIgnoreCase));
            if (bestMatch.Key != null) CmbVimmSystem.SelectedItem = bestMatch;
            else CmbVimmSystem.SelectedIndex = 0;
        }
        else CmbVimmSystem.SelectedIndex = 0;

        OverlayVimmSystem.IsVisible = true;
    }

    private void BtnCancelVimm_Click(object? sender, RoutedEventArgs e)
    {
        OverlayVimmSystem.IsVisible = false;
    }

    private void BtnCancelExport_Click(object? sender, RoutedEventArgs e)
    {
        OverlayExportOptions.IsVisible = false;
    }

    private void BtnAcceptConfirm_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlaySelect();
        OverlayConfirm.IsVisible = false;
        _onConfirmAction?.Invoke();
        _onConfirmAction = null;
    }

    private void BtnOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && !string.IsNullOrEmpty(_selectedGame.RomPath))
        {
            try
            {
                string? dir = Path.GetDirectoryName(_selectedGame.RomPath);
                if (Directory.Exists(dir)) 
                    Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
                else
                    ShowMessage("La carpeta del juego no existe o no se puede encontrar.");
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al abrir la carpeta: {ex.Message}");
            }
        }
    }

    private void BtnToggleFavorite_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;

        _selectedGame.IsFavorite = !_selectedGame.IsFavorite;
        _gameService.UpdateGameMetadata(_selectedGame);
        
        SoundHelper.PlaySelect();
        UpdateFavoriteUI();
        
        // Refrescar UI
        LoadPlatforms(); // Actualizar contador de favoritos
        
        // Si estamos en la vista de favoritos, recargar la lista
        var sidebarNode = TvSidebar.SelectedItem as SidebarNode;
        if (sidebarNode != null && sidebarNode.Tag?.ToString() == "FAVORITES")
        {
            ApplySearchFilter();
        }
        else
        {
            // Solo refrescar la visualización del item actual
            if (LstGames.IsVisible) LstGames.ItemsSource = null; LstGames.ItemsSource = _currentPlatformGames;
            if (LstGamesGrid.IsVisible) LstGamesGrid.ItemsSource = null; LstGamesGrid.ItemsSource = _currentPlatformGames;
        }
    }

    private void UpdateFavoriteUI()
    {
        if (_selectedGame == null || TxtFavoriteIcon == null) return;
        TxtFavoriteIcon.Text = _selectedGame.IsFavorite ? "♥" : "♡";
        TxtFavoriteIcon.Foreground = _selectedGame.IsFavorite ? Avalonia.Media.Brushes.Red : Avalonia.Media.Brushes.White;
    }

    private void PopulateRomsFlyout(Game game)
    {
        MenuFlyout? flyoutRoms = null;
        if (BtnLaunchGame != null && BtnLaunchGame.Content is Grid grid && grid.Children.Count >= 3)
        {
            if (grid.Children[2] is Border border)
            {
                flyoutRoms = Avalonia.Controls.Primitives.FlyoutBase.GetAttachedFlyout(border) as MenuFlyout;
            }
        }

        if (flyoutRoms == null) return;
        flyoutRoms.Items.Clear();

        var romList = new List<string>();
        if (!string.IsNullOrEmpty(game.RomPath)) romList.Add(game.RomPath);
        if (!string.IsNullOrEmpty(game.AdditionalRoms))
        {
            foreach (var r in game.AdditionalRoms.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                romList.Add(r);
            }
        }

        if (romList.Count <= 1)
        {
            var item = new MenuItem { Header = "Iniciar ROM Principal" };
            item.Click += (s, e) => LaunchSpecificRom(game.RomPath);
            flyoutRoms.Items.Add(item);
        }
        else
        {
            for (int i = 0; i < romList.Count; i++)
            {
                string romPath = romList[i];
                string name = $"Disco {i + 1} ({Path.GetFileName(romPath)})";
                var item = new MenuItem { Header = name };
                item.Click += (s, e) => LaunchSpecificRom(romPath);
                flyoutRoms.Items.Add(item);
            }
        }
    }

    private void LaunchSpecificRom(string romPath)
    {
        if (_selectedGame == null) return;
        
        // Crear una copia temporal del juego con el RomPath específico
        var tempGame = new Game
        {
            Id = _selectedGame.Id,
            Name = _selectedGame.Name,
            RomPath = romPath,
            AdditionalRoms = _selectedGame.AdditionalRoms,
            OverrideEmulatorPath = _selectedGame.OverrideEmulatorPath,
            OverrideLaunchArguments = _selectedGame.OverrideLaunchArguments,
            PlatformId = _selectedGame.PlatformId,
            Platform = _selectedGame.Platform
        };

        var oldSelected = _selectedGame;
        _selectedGame = tempGame;
        
        BtnLaunchGame_Click(null, new RoutedEventArgs());
        
        _selectedGame = oldSelected;
    }

    private void LoadGameGallery(int gameId)
    {
        System.Threading.Tasks.Task.Run(() => {
            try
            {
                using var context = new GestorJuegos.Data.CoversDbContext();

                // Cargar siempre la portada frontal plana (Box Front) para el visor 3D interactivo
                var boxFront = context.Covers
                    .Where(c => c.Id == gameId)
                    .Select(c => c.ImageData ?? c.ThumbnailData)
                    .FirstOrDefault();

                // 2. CARGAR GALERÍA DE IMÁGENES (Capturas, Fanart, etc.)
                var galleryImages = context.Images
                    .Where(i => i.GameId == gameId &&
                           (i.ImageType == "Screenshot - Gameplay" ||
                            i.ImageType == "Snap" ||
                            i.ImageType == "Fanart - Background" ||
                            i.ImageType == "Fanart" ||
                            i.ImageType == "Screenshot"))
                    .ToList();

                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    // Verificar que seguimos en el mismo juego para evitar cargar datos viejos
                    if (_selectedGame != null && _selectedGame.Id == gameId)
                    {
                        if (boxFront != null && boxFront.Length > 0)
                        {
                            ShowGameplayPreview(boxFront);
                        }
                        else
                        {
                            ImgGameBox3D.Cover = null;
                            if (Txt3DHint != null) Txt3DHint.IsVisible = false;
                        }

                        if (galleryImages.Count > 0)
                        {
                            LstScreenshots.ItemsSource = galleryImages;
                            LstScreenshots.IsVisible = true;
                        }
                        else
                        {
                            LstScreenshots.ItemsSource = null;
                            LstScreenshots.IsVisible = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogDebug($"Error al cargar galería: {ex.Message}");
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    ImgGameBox3D.Cover = null;
                    if (Txt3DHint != null) Txt3DHint.IsVisible = false;
                    LstScreenshots.ItemsSource = null;
                    LstScreenshots.IsVisible = false;
                });
            }
        });
    }

    private void ShowGameplayPreview(byte[]? data)
    {
        if (data != null && data.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(data);
                ImgGameBox3D.Cover = new Bitmap(ms);
                if (Txt3DHint != null) Txt3DHint.IsVisible = true;
            }
            catch
            {
                ImgGameBox3D.Cover = null;
                if (Txt3DHint != null) Txt3DHint.IsVisible = false;
            }
        }
        else
        {
            ImgGameBox3D.Cover = null;
            if (Txt3DHint != null) Txt3DHint.IsVisible = false;
        }
    }

    private void LstScreenshots_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstScreenshots.SelectedItem is GameImage selectedImage)
        {
            ShowGameplayPreview(selectedImage.ImageData);
        }
    }

    private void ImgGameplayPreview_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // El control GameBox3D ahora maneja su propia interactividad
        if (_selectedGame == null) return;
        
        // Si hay una imagen seleccionada en el carrusel, usar esa; si no, la imagen cargada
        if (LstScreenshots.SelectedItem is GameImage selectedImg && selectedImg.ImageData != null)
        {
            OpenFullImageViewer(selectedImg.ImageData);
        }
        else
        {
            // Intentar con la portada o fanart
            byte[]? fullCover = _gameService.GetGameFullCover(_selectedGame.Id);
            if (fullCover != null) OpenFullImageViewer(fullCover);
        }
    }

    private void BtnViewFullArt_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        byte[]? fullCover = _gameService.GetGameFullCover(_selectedGame.Id);
        if (fullCover != null)
        {
            OpenFullImageViewer(fullCover);
        }
    }

    private void BtnViewAllImages_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        
        using var context = new GestorJuegos.Data.CoversDbContext();
        var firstScreenshot = context.Images
            .Where(i => i.GameId == _selectedGame.Id && (i.ImageType == "Screenshot - Gameplay" || i.ImageType == "Snap" || i.ImageType == "Fanart - Background"))
            .Select(i => i.ImageData)
            .FirstOrDefault();
            
        if (firstScreenshot != null)
        {
            OpenFullImageViewer(firstScreenshot);
        }
        else
        {
            byte[]? fullCover = _gameService.GetGameFullCover(_selectedGame.Id);
            if (fullCover != null) OpenFullImageViewer(fullCover);
        }
    }

    private void OpenFullImageViewer(byte[] imageData)
    {
        try
        {
            using var ms = new MemoryStream(imageData);
            ImgFullViewer.Source = new Bitmap(ms);
            OverlayImageViewer.IsVisible = true;
            SoundHelper.PlaySelect();
        }
        catch { }
    }

    private void BtnCloseImageViewer_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlayBack();
        OverlayImageViewer.IsVisible = false;
        ImgFullViewer.Source = null;
    }

    private void OverlayImageViewer_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SoundHelper.PlayBack();
        OverlayImageViewer.IsVisible = false;
        ImgFullViewer.Source = null;
    }

    private void BtnWiki_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        SoundHelper.PlaySelect();
        string url = $"https://es.wikipedia.org/wiki/Special:Search?search={Uri.EscapeDataString(_selectedGame.Name)}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void BtnYoutube_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        SoundHelper.PlaySelect();
        string platformName = _selectedPlatform?.Name ?? "";
        string url = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(_selectedGame.Name)}+{Uri.EscapeDataString(platformName)}+gameplay+trailer";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void BtnConfigQuick_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        SoundHelper.PlaySelect();
        // Abrir el editor de juegos directamente en la pestaña de emulador
        BtnEditGame_Click(sender, e);
    }

    private void LoadRelatedGames(Game game)
    {
        try
        {
            // Obtener juegos de la misma plataforma excluyendo el actual
            var related = _currentPlatformGames
                .Where(g => g.Id != game.Id)
                .Take(8)
                .ToList();

            foreach (var r in related)
            {
                string gameArtType = !string.IsNullOrEmpty(r.SelectedArtType) ? r.SelectedArtType : _settings.PreferredArtType;
                string artFolder = GetExternalFolderName(gameArtType);
                r.Cover = _gameService.GetGameThumbnail(r.Id, artFolder);
            }

            LstRelatedGames.ItemsSource = related;
            TxtNoRelatedGames.IsVisible = !related.Any();
        }
        catch
        {
            LstRelatedGames.ItemsSource = null;
            TxtNoRelatedGames.IsVisible = true;
        }
    }

    private void LstRelatedGames_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstRelatedGames.SelectedItem is Game relatedGame)
        {
            LstRelatedGames.SelectedItem = null;
            SoundHelper.PlaySelect();

            // Buscar en la lista de juegos y seleccionarlo
            var match = _currentPlatformGames.FirstOrDefault(g => g.Id == relatedGame.Id);
            if (match != null)
            {
                if (LstGames.IsVisible) LstGames.SelectedItem = match;
                else if (LstGamesGrid.IsVisible) LstGamesGrid.SelectedItem = match;
            }
        }
    }

    private void BtnWikiUrl_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && !string.IsNullOrEmpty(_selectedGame.WikipediaURL))
        {
            SoundHelper.PlaySelect();
            Process.Start(new ProcessStartInfo(_selectedGame.WikipediaURL) { UseShellExecute = true });
        }
    }

    private void BtnVideoUrl_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && !string.IsNullOrEmpty(_selectedGame.VideoURL))
        {
            SoundHelper.PlaySelect();
            Process.Start(new ProcessStartInfo(_selectedGame.VideoURL) { UseShellExecute = true });
        }
    }

    private void BtnCancelConfirm_Click(object? sender, RoutedEventArgs e)
    {
        GestorJuegos.Utils.SoundHelper.PlayBack();
        OverlayConfirm.IsVisible = false;
        _onConfirmAction = null;
    }

    // --- NUEVO SISTEMA DE MENÚ HORIZONTAL Y ORDENACIÓN ---

    private void UpdateMenuCheckmarks()
    {
        try
        {
            // 1. Actualizar checkmarks de ordenación
            if (SortByName != null) SortByName.Icon = _currentSortField == "Name" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByYear != null) SortByYear.Icon = _currentSortField == "Year" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByDateAdded != null) SortByDateAdded.Icon = _currentSortField == "DateAdded" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByDeveloper != null) SortByDeveloper.Icon = _currentSortField == "Developer" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByIsFavorite != null) SortByIsFavorite.Icon = _currentSortField == "IsFavorite" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByGenre != null) SortByGenre.Icon = _currentSortField == "Genre" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByLastPlayed != null) SortByLastPlayed.Icon = _currentSortField == "LastPlayed" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByExternalDbId != null) SortByExternalDbId.Icon = _currentSortField == "ExternalDbId" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByMaxPlayers != null) SortByMaxPlayers.Icon = _currentSortField == "MaxPlayers" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByPlayCount != null) SortByPlayCount.Icon = _currentSortField == "PlayCount" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByPlayTime != null) SortByPlayTime.Icon = _currentSortField == "PlayTime" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByRating != null) SortByRating.Icon = _currentSortField == "Rating" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByRegion != null) SortByRegion.Icon = _currentSortField == "Region" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByReleaseDate != null) SortByReleaseDate.Icon = _currentSortField == "ReleaseDate" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByPlayStatus != null) SortByPlayStatus.Icon = _currentSortField == "PlayStatus" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SortByVersion != null) SortByVersion.Icon = _currentSortField == "Version" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;

            // Submenú de Ordenar Por (duplicado bajo "Ver" para redundancia cómoda)
            if (SubSortByName != null) SubSortByName.Icon = _currentSortField == "Name" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByYear != null) SubSortByYear.Icon = _currentSortField == "Year" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByDateAdded != null) SubSortByDateAdded.Icon = _currentSortField == "DateAdded" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByDeveloper != null) SubSortByDeveloper.Icon = _currentSortField == "Developer" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByIsFavorite != null) SubSortByIsFavorite.Icon = _currentSortField == "IsFavorite" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByGenre != null) SubSortByGenre.Icon = _currentSortField == "Genre" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByLastPlayed != null) SubSortByLastPlayed.Icon = _currentSortField == "LastPlayed" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByExternalDbId != null) SubSortByExternalDbId.Icon = _currentSortField == "ExternalDbId" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByMaxPlayers != null) SubSortByMaxPlayers.Icon = _currentSortField == "MaxPlayers" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByPlayCount != null) SubSortByPlayCount.Icon = _currentSortField == "PlayCount" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByPlayTime != null) SubSortByPlayTime.Icon = _currentSortField == "PlayTime" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByRating != null) SubSortByRating.Icon = _currentSortField == "Rating" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByRegion != null) SubSortByRegion.Icon = _currentSortField == "Region" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByReleaseDate != null) SubSortByReleaseDate.Icon = _currentSortField == "ReleaseDate" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByPlayStatus != null) SubSortByPlayStatus.Icon = _currentSortField == "PlayStatus" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubSortByVersion != null) SubSortByVersion.Icon = _currentSortField == "Version" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;

            // Dirección
            if (ChkSortAscending != null) ChkSortAscending.Text = _isSortAscending ? "✓" : "";

            // 2. Actualizar checkmarks de grupo de imagen
            string prefArt = _settings.PreferredArtType;
            if (ArtTypeBackground != null) ArtTypeBackground.Icon = prefArt == "Background" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeBox != null) ArtTypeBox.Icon = prefArt == "Box" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeBox3D != null) ArtTypeBox3D.Icon = prefArt == "Box 3D" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeCartFront != null) ArtTypeCartFront.Icon = prefArt == "Cart - Front" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeCart3D != null) ArtTypeCart3D.Icon = prefArt == "Cart - 3D" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeClearLogo != null) ArtTypeClearLogo.Icon = prefArt == "Clear Logo" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeMarquee != null) ArtTypeMarquee.Icon = prefArt == "Marquee" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (ArtTypeSnap != null) ArtTypeSnap.Icon = prefArt == "Snap" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;

            // Submenú de Grupo de Imagen (bajo "Ver")
            if (SubArtTypeBackground != null) SubArtTypeBackground.Icon = prefArt == "Background" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeBox != null) SubArtTypeBox.Icon = prefArt == "Box" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeBox3D != null) SubArtTypeBox3D.Icon = prefArt == "Box 3D" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeCartFront != null) SubArtTypeCartFront.Icon = prefArt == "Cart - Front" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeCart3D != null) SubArtTypeCart3D.Icon = prefArt == "Cart - 3D" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeClearLogo != null) SubArtTypeClearLogo.Icon = prefArt == "Clear Logo" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeMarquee != null) SubArtTypeMarquee.Icon = prefArt == "Marquee" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;
            if (SubArtTypeSnap != null) SubArtTypeSnap.Icon = prefArt == "Snap" ? new TextBlock { Text = "✓", Foreground = Avalonia.Media.Brushes.LightGreen, FontWeight = Avalonia.Media.FontWeight.Bold } : null;

            // 3. Vistas e interfaces
            if (ChkViewGrid != null) ChkViewGrid.Text = LstGamesGrid != null && LstGamesGrid.IsVisible ? "✓" : "";
            if (ChkViewList != null) ChkViewList.Text = LstGames != null && LstGames.IsVisible ? "✓" : "";
            if (ChkShowFilters != null) ChkShowFilters.Text = PnlFilters != null && PnlFilters.IsVisible ? "✓" : "";

            // 4. Insignias
            if (ChkBadgeFavorite != null) ChkBadgeFavorite.Text = _showFavoriteBadge ? "✓" : "";
            if (ChkBadgeRegion != null) ChkBadgeRegion.Text = _showRegionBadge ? "✓" : "";
            if (ChkBadgePlayStatus != null) ChkBadgePlayStatus.Text = _showStatusBadge ? "✓" : "";
        }
        catch { }
    }

    private void SetSortField(string field)
    {
        SoundHelper.PlaySelect();
        _currentSortField = field;
        _currentPage = 1;
        UpdateMenuCheckmarks();
        ApplySearchFilter();
    }

    private void MenuSortTitle_Click(object? sender, RoutedEventArgs e) => SetSortField("Name");
    private void MenuSortYear_Click(object? sender, RoutedEventArgs e) => SetSortField("Year");
    private void MenuSortDateAdded_Click(object? sender, RoutedEventArgs e) => SetSortField("DateAdded");
    private void MenuSortDeveloper_Click(object? sender, RoutedEventArgs e) => SetSortField("Developer");
    private void MenuSortFavorite_Click(object? sender, RoutedEventArgs e) => SetSortField("IsFavorite");
    private void MenuSortGenre_Click(object? sender, RoutedEventArgs e) => SetSortField("Genre");
    private void MenuSortLastPlayed_Click(object? sender, RoutedEventArgs e) => SetSortField("LastPlayed");
    private void MenuSortExternalDbId_Click(object? sender, RoutedEventArgs e) => SetSortField("ExternalDbId");
    private void MenuSortMaxPlayers_Click(object? sender, RoutedEventArgs e) => SetSortField("MaxPlayers");
    private void MenuSortPlayCount_Click(object? sender, RoutedEventArgs e) => SetSortField("PlayCount");
    private void MenuSortPlayTime_Click(object? sender, RoutedEventArgs e) => SetSortField("PlayTime");
    private void MenuSortRating_Click(object? sender, RoutedEventArgs e) => SetSortField("Rating");
    private void MenuSortRegion_Click(object? sender, RoutedEventArgs e) => SetSortField("Region");
    private void MenuSortReleaseDate_Click(object? sender, RoutedEventArgs e) => SetSortField("ReleaseDate");
    private void MenuSortPlayStatus_Click(object? sender, RoutedEventArgs e) => SetSortField("PlayStatus");
    private void MenuSortVersion_Click(object? sender, RoutedEventArgs e) => SetSortField("Version");

    private void MenuSortAscending_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        _isSortAscending = !_isSortAscending;
        _currentPage = 1;
        UpdateMenuCheckmarks();
        ApplySearchFilter();
    }

    private void SetArtType(string type)
    {
        SoundHelper.PlaySelect();
        _settings.PreferredArtType = type;
        SaveSettings();
        _currentPage = 1;
        UpdateMenuCheckmarks();
        ApplySearchFilter();
    }

    private void MenuArtTypeBackground_Click(object? sender, RoutedEventArgs e) => SetArtType("Background");
    private void MenuArtTypeBox_Click(object? sender, RoutedEventArgs e) => SetArtType("Box");
    private void MenuArtTypeBox3D_Click(object? sender, RoutedEventArgs e) => SetArtType("Box 3D");
    private void MenuArtTypeCartFront_Click(object? sender, RoutedEventArgs e) => SetArtType("Cart - Front");
    private void MenuArtTypeCart3D_Click(object? sender, RoutedEventArgs e) => SetArtType("Cart - 3D");
    private void MenuArtTypeClearLogo_Click(object? sender, RoutedEventArgs e) => SetArtType("Clear Logo");
    private void MenuArtTypeMarquee_Click(object? sender, RoutedEventArgs e) => SetArtType("Marquee");
    private void MenuArtTypeSnap_Click(object? sender, RoutedEventArgs e) => SetArtType("Snap");

    private void MenuViewGrid_Click(object? sender, RoutedEventArgs e)
    {
        BtnViewGrid_Click(null, new RoutedEventArgs());
        UpdateMenuCheckmarks();
    }

    private void MenuViewList_Click(object? sender, RoutedEventArgs e)
    {
        BtnViewList_Click(null, new RoutedEventArgs());
        UpdateMenuCheckmarks();
    }

    private void MenuToggleFilters_Click(object? sender, RoutedEventArgs e)
    {
        BtnToggleFilters_Click(null, new RoutedEventArgs());
        UpdateMenuCheckmarks();
    }

    private void MenuShowStats_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        ShowFullStats();
    }

    private void MenuManagePlatforms_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        BtnManagePlatforms_Click(null, new RoutedEventArgs());
    }

    private void MenuBadgeFavorite_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        _showFavoriteBadge = !_showFavoriteBadge;
        UpdateMenuCheckmarks();
        ApplySearchFilter();
    }

    private void MenuBadgeRegion_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        _showRegionBadge = !_showRegionBadge;
        UpdateMenuCheckmarks();
        ApplySearchFilter();
    }

    private void MenuBadgePlayStatus_Click(object? sender, RoutedEventArgs e)
    {
        SoundHelper.PlaySelect();
        _showStatusBadge = !_showStatusBadge;
        UpdateMenuCheckmarks();
        ApplySearchFilter();
    }

    private void UpdateWheels()
    {
        UpdateWheelCurvature();
        UpdateHorizontalWheelCurvature();
    }

    private void UpdateWheelCurvature()
    {
        if (LstGamesWheelVertical == null || !LstGamesWheelVertical.IsVisible) return;

        double centerOfList = LstGamesWheelVertical.Bounds.Height / 2;
        if (centerOfList <= 0) return;

        var itemsSource = LstGamesWheelVertical.ItemsSource as System.Collections.IList;
        if (itemsSource == null) return;

        foreach (var item in itemsSource)
        {
            var container = LstGamesWheelVertical.ContainerFromItem(item) as ListBoxItem;
            if (container == null) continue;

            var position = container.TranslatePoint(new Point(0, 0), LstGamesWheelVertical);
            if (position.HasValue)
            {
                double itemCenterY = position.Value.Y + (container.Bounds.Height / 2);
                double distanceFromCenter = itemCenterY - centerOfList;
                double maxDistance = centerOfList;

                // Parábola más suave para evitar que se salgan de la pantalla
                double ratio = Math.Clamp(distanceFromCenter / maxDistance, -1.2, 1.2);
                double shiftX = -80 * (ratio * ratio); // Reducido de -120 a -80
                
                // Rotación sutil para simular la inclinación de la rueda
                double rotation = ratio * 15; 

                bool isSelected = container.IsSelected;
                double scale = isSelected ? 1.2 : (1.0 - Math.Abs(ratio) * 0.2); // Escala más moderada
                double opacity = 1.0 - Math.Abs(ratio) * 0.4;

                container.ZIndex = isSelected ? 100 : (int)(50 - Math.Abs(distanceFromCenter) / 5);
                container.Opacity = opacity;

                var group = new TransformGroup();
                group.Children.Add(new RotateTransform(rotation));
                group.Children.Add(new ScaleTransform(scale, scale));
                group.Children.Add(new TranslateTransform(shiftX + (isSelected ? 40 : 0), 0)); // Añadido margen derecho al seleccionado
                container.RenderTransform = group;
            }
        }
    }

    private void UpdateHorizontalWheelCurvature()
    {
        if (LstGamesWheelHorizontal == null || !LstGamesWheelHorizontal.IsVisible) return;

        double centerOfList = LstGamesWheelHorizontal.Bounds.Width / 2;
        if (centerOfList <= 0) return;

        var itemsSource = LstGamesWheelHorizontal.ItemsSource as System.Collections.IList;
        if (itemsSource == null) return;

        foreach (var item in itemsSource)
        {
            var container = LstGamesWheelHorizontal.ContainerFromItem(item) as ListBoxItem;
            if (container == null) continue;

            var position = container.TranslatePoint(new Point(0, 0), LstGamesWheelHorizontal);
            if (position.HasValue)
            {
                double itemCenterX = position.Value.X + (container.Bounds.Width / 2);
                double distanceFromCenter = itemCenterX - centerOfList;
                double maxDistance = centerOfList;

                // Efecto CoverFlow/Rueda Horizontal
                double ratio = Math.Clamp(distanceFromCenter / maxDistance, -1.0, 1.0);
                double shiftY = 60 * (ratio * ratio); 
                double rotationY = ratio * 45; // Inclinación lateral estilo CoverFlow

                bool isSelected = container.IsSelected;
                double scale = isSelected ? 1.2 : 0.8;
                
                container.ZIndex = isSelected ? 100 : (int)(50 - Math.Abs(distanceFromCenter) / 10);

                var group = new TransformGroup();
                // Simulación de rotación 3D usando Skew y ScaleX
                group.Children.Add(new ScaleTransform(scale * (1.0 - Math.Abs(ratio) * 0.2), scale));
                group.Children.Add(new SkewTransform(0, ratio * 10)); 
                group.Children.Add(new TranslateTransform(0, shiftY));
                container.RenderTransform = group;
            }
        }
    }
}
