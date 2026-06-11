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
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using GestorJuegos.Utils;
using GestorJuegos.Data;

namespace GestorJuegos.Views.Windows;
public partial class MainWindow : Window
{
    private readonly GameService _gameService;
    private readonly ScannerService _scannerService;
    private readonly LauncherService _launcherService;
    private readonly ImportService _importService;
    private readonly NotificationService _notificationService;
    
    private Platform? _selectedPlatform;
    private string? _selectedCategory;
    private Game? _selectedGame;
    private List<Game> _currentPlatformGames = new List<Game>();
    
    private AppSettings _settings = new AppSettings();
    private Action? _dialogAcceptedAction;

    // Variables de estado de UI (RESTAURADAS)
    private string _currentSortField = "Name";
    private bool _isSortAscending = true;
    private bool _showFavoriteBadge = true;
    private bool _showRegionBadge = true;
    private bool _showStatusBadge = true;

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        
        _gameService = new GameService();
        _scannerService = new ScannerService(_gameService);
        _launcherService = new LauncherService(_gameService, _settings);
        _importService = new ImportService(_gameService);
        _notificationService = new NotificationService();

        // Escuchar notificaciones globales
        _notificationService.NotificationReceived += (s, e) => {
            OverlayToast.Show(e.Message, e.Title, e.Type);
        };

        // Inicializar componentes modulares
        Sidebar.Initialize(_gameService, _settings);
        Library.Initialize(_gameService, _settings);

        SetupEvents();
        
        LoadDashboard();
        UpdateMenuCheckmarks();

        // Forzar Vista Galería al inicio
        OnTopBarViewToggle(null, "BtnViewGrid");
    }

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

    private void SetupEvents()
    {
        // 1. Eventos de Sidebar
        Sidebar.SelectionChanged += Sidebar_SelectionChanged;
        Sidebar.AddPlatformRequested += (s, e) => { OverlayManagePlatforms.Initialize(_gameService); OverlayManagePlatforms.IsVisible = true; };
        Sidebar.PlatformEditRequested += (s, p) => OpenEditPlatform(p);
        Sidebar.PlatformDeleteRequested += (s, p) => { _selectedPlatform = p; DeletePlatformWithConfirm(p); };

        // 2. Eventos de Galería (Library)
        Library.GameSelected += Library_GameSelected;
        Library.PlatformsWallRequested += (s, e) => LoadPlatformsWall();

        // 3. Eventos de Barra Superior (TopBar)
        TopBar.SearchTextChanged += (s, search) => Library.SetSearchText(search);
        TopBar.SearchBox.GotFocus += (s, e) => {
            if (_settings.EnableVirtualKeyboard && TopBar.IsGamepadModeEnabled) {
                OverlayKeyboard.Show(TopBar.GetSearchText());
            }
        };
        TopBar.QuickFavoriteClicked += (s, e) => Library.ApplyFilters();
        TopBar.ToggleThemeRequested += (s, e) => ApplyTheme();
        TopBar.AddGameRequested += (s, e) => BtnAddGame_Click(null, new RoutedEventArgs());
        TopBar.ShowStatsRequested += (s, e) => ShowFullStats();
        TopBar.ManagePlatformsRequested += (s, e) => { OverlayManagePlatforms.Initialize(_gameService); OverlayManagePlatforms.IsVisible = true; };
        TopBar.ViewToggleClicked += OnTopBarViewToggle;
        TopBar.SortActionRequested += OnTopBarSortAction;
        TopBar.ArtTypeActionRequested += OnTopBarArtTypeAction;
        TopBar.BadgeActionRequested += OnTopBarBadgeAction;
        TopBar.HelpActionRequested += OnTopBarHelpAction;
        TopBar.MenuActionRequested += OnTopBarMenuAction;
        TopBar.SortAscendingToggled += (s, e) => { _isSortAscending = !_isSortAscending; Library.ApplyFilters(); UpdateMenuCheckmarks(); };

        // 4. Eventos de Dashboard
        Dashboard.RequestEditPlatform += (s, p) => OpenEditPlatform(p);
        Dashboard.RequestClose += (s, e) => { Dashboard.IsVisible = false; };

        // 5. Eventos de Detalles (GameDetails)
        SetupGameDetailsEvents();

        // 6. Otros Overlays
        OverlayEditGame.RequestClose += (s, e) => OverlayEditGame.IsVisible = false;
        OverlayEditGame.GameSaved += (s, e) => {
            LoadGames();
            if (_selectedGame != null)
            {
                GameDetails.UpdateDetails(_selectedGame, _gameService);
            }
        };
        OverlayEditGame.RequestMessage += (msg) => ShowMessage(msg);
        OverlayAchievements.RequestClose += (s, e) => OverlayAchievements.IsVisible = false;
        OverlayManagePlatforms.RequestClose += (s, e) => OverlayManagePlatforms.IsVisible = false;
        OverlayManagePlatforms.PlatformsChanged += (s, e) => Sidebar.LoadPlatforms();
        OverlayManagePlatforms.RequestMessage += (msg) => ShowMessage(msg);

        OverlayPlatformsWall.RequestClose += (s, e) => OverlayPlatformsWall.IsVisible = false;
        OverlayPlatformsWall.PlatformSelected += (s, p) => {
            _selectedPlatform = p;
            Sidebar.SelectPlatform(p.Name);
            OverlayPlatformsWall.IsVisible = false;
        };

        OverlayProgress.CancelRequested += (s, e) => _cts?.Cancel();
        OverlayDialog.Accepted += (s, e) => {
            _dialogAcceptedAction?.Invoke();
            _dialogAcceptedAction = null;
        };
        OverlayDialog.Cancelled += (s, e) => {
            _dialogAcceptedAction = null;
        };

        OverlayKeyboard.RequestClose += (s, e) => OverlayKeyboard.IsVisible = false;
        OverlayKeyboard.TextSubmitted += (s, text) => {
            TopBar.SetSearchText(text);
            Library.SetSearchText(text);
            OverlayKeyboard.IsVisible = false;
        };

        OverlayExportOptions.RequestClose += (s, e) => OverlayExportOptions.IsVisible = false;
        OverlayExportOptions.RequestExport += async (s, args) => {
            OverlayExportOptions.IsVisible = false;
            await ExportDatabaseAsync(args.exportGames, args.exportCovers);
        };

        OverlayDeleteConfirm.Confirmed += (s, deleteMedia) => { if (_selectedGame != null) { _gameService.DeleteGame(_selectedGame.Id); LoadGames(); GameDetails.IsVisible = false; } };
        
        _launcherService.GameExited += (s, e) => {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (_selectedGame?.Id == e.game.Id) GameDetails.UpdateDetails(e.game, _gameService);
                Library.ApplyFilters();
            });
        };
    }

    private void SetupGameDetailsEvents()
    {
        GameDetails.RequestLaunch += (s, g) => BtnLaunchGame_Click(null, new RoutedEventArgs());
        GameDetails.RequestEdit += (s, g) => BtnEditGame_Click(null, new RoutedEventArgs());
        GameDetails.RequestToggleFavorite += (s, g) => BtnToggleFavorite_Click(null, new RoutedEventArgs());
        GameDetails.RequestOpenFolder += (s, path) => _launcherService.OpenUrl(Path.GetDirectoryName(path) ?? "");
        GameDetails.RequestShowFullArt += (s, e) => {
            if (_selectedGame != null) {
                byte[]? fullCover = _gameService.GetGameExtraImage(_selectedGame.Id, "Box") ?? _gameService.GetGameExtraImage(_selectedGame.Id, "Fanart - Background");
                if (fullCover != null) OverlayImageViewer.ShowImage(fullCover);
            }
        };
        GameDetails.RequestExpandAchievements += (s, e) => {
            if (_selectedGame != null) {
                OverlayAchievements.Initialize(_selectedGame.Achievements);
                OverlayAchievements.IsVisible = true;
            }
        };
        GameDetails.RequestRatingChange += (s, rating) => {
            if (_selectedGame != null) {
                _gameService.UpdateGameMetadata(_selectedGame);
                Library.ApplyFilters();
            }
        };
        GameDetails.RequestPlayStatusChange += (s, status) => UpdateGameStatus(status);
        GameDetails.RequestWikiSearch += (s, name) => _launcherService.OpenUrl($"https://es.wikipedia.org/wiki/Special:Search?search={Uri.EscapeDataString(name)}");
    }

    private void Sidebar_SelectionChanged(object? sender, SidebarNode item)
    {
        SoundHelper.PlaySelect();
        LoadPlatformOrCategoryDetails(item);

        Task.Run(() => {
            using var context = new AppDbContext();
            var query = context.Games.Include(g => g.Platform).AsQueryable();

            _selectedCategory = null;
            if (item.Tag is string simpleTag)
            {
                if (simpleTag == "FAVORITES") query = query.Where(g => g.IsFavorite);
            }
            else if (item.Tag is ValueTuple<string, string> filter)
            {
                if (filter.Item1 == "PLATFORM") query = query.Where(g => g.Platform != null && g.Platform.Name == filter.Item2);
                else if (filter.Item1 == "CATEGORY") {
                    _selectedCategory = filter.Item2;
                    var names = context.Platforms.Where(p => p.Category == filter.Item2).Select(p => p.Name).ToList();
                    query = query.Where(g => g.Platform != null && names.Contains(g.Platform.Name));
                }
            }

            var games = query.ToList();
            var platformToSet = (item.Tag is ValueTuple<string, string> f && f.Item1 == "PLATFORM") 
                ? context.Platforms.FirstOrDefault(p => p.Name == f.Item2) : games.FirstOrDefault()?.Platform;

            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                _selectedPlatform = platformToSet;
                _currentPlatformGames = games;
                if (games.Count == 0) LoadDashboard();
                else Library.UpdateGames(games, platformToSet);
            });
        });
    }

    private void Library_GameSelected(object? sender, Game game)
    {
        Dashboard.IsVisible = false;
        GameDetails.IsVisible = true;
        _selectedGame = game;
        BackgroundControl.UpdateBackground(game, _gameService);
        GameDetails.UpdateDetails(game, _gameService);
        LoadGameLogo(game);
    }

    private void LoadGameLogo(Game game)
    {
        byte[]? logoData = _gameService.GetGameExtraImage(game.Id, "Logos") ?? _gameService.GetGameExtraImage(game.Id, "Clear Logo");
        if (logoData != null) {
            try { GameDetails.SetLogo(new Bitmap(new MemoryStream(logoData))); } catch { GameDetails.SetLogo(null); }
        } else GameDetails.SetLogo(null);
    }

    private void LoadGames()
    {
        if (_selectedPlatform == null) return;
        _currentPlatformGames = _gameService.GetGamesByPlatform(_selectedPlatform.Id);
        Library.UpdateGames(_currentPlatformGames, _selectedPlatform);
    }

    private void LoadDashboard()
    {
        GameDetails.IsVisible = false;
        Dashboard.IsVisible = true;
        Dashboard.ShowNoGameSelected();
        Sidebar.LoadPlatforms();
        
        using var context = new AppDbContext();
        try {
            int allCount = context.Games.Count();
            TopBar.SetGameStatusInfo($"Total de juegos: {allCount}");
        } catch { }
    }

    private void OpenEditPlatform(Platform platform)
    {
        OverlayManagePlatforms.Initialize(_gameService);
        OverlayManagePlatforms.IsVisible = true;
        OverlayManagePlatforms.SelectPlatform(platform.Id);
    }

    private void DeletePlatformWithConfirm(Platform platform)
    {
        _dialogAcceptedAction = () => {
            _gameService.DeletePlatform(platform.Id);
            Sidebar.LoadPlatforms();
            LoadDashboard();
        };
        OverlayDialog.ShowConfirm($"¿Eliminar '{platform.Name}' y sus juegos?", "Eliminar Plataforma");
    }

    private async void BtnLaunchGame_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        var result = await _launcherService.LaunchGameAsync(_selectedGame, _selectedPlatform);
        if (!result.Success) ShowMessage(result.Message);
    }

    private void BtnToggleFavorite_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        _selectedGame.IsFavorite = !_selectedGame.IsFavorite;
        _gameService.UpdateGameMetadata(_selectedGame);
        SoundHelper.PlaySelect();
        GameDetails.UpdateFavoriteIcon(_selectedGame.IsFavorite);
        Sidebar.LoadPlatforms();
        Library.ApplyFilters();
    }

    private void BtnAddGame_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null) { ShowMessage("Selecciona una plataforma primero."); return; }
        _selectedGame = new Game { PlatformId = _selectedPlatform.Id, DateAdded = DateTime.Now };
        OverlayEditGame.Initialize(_selectedGame, _selectedPlatform!, _gameService, _settings);
        OverlayEditGame.IsVisible = true;
    }

    private void BtnEditGame_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;
        OverlayEditGame.Initialize(_selectedGame, _selectedPlatform!, _gameService, _settings);
        OverlayEditGame.IsVisible = true;
    }

    private void BtnDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null) return;
        OverlayDeleteConfirm.Show($"¿Borrar {_selectedGame.Name}?");
    }

    private void ApplyTheme()
    {
        var config = ThemeHelper.LoadThemeConfig(_settings.Theme);
        ThemeHelper.ApplyTheme(_settings.Theme, config);

        string themeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", _settings.Theme);
        
        // 1. Cargar Logo (probar raíz e Images/)
        string logoPath = Path.Combine(themeDir, "Logo.png");
        if (!File.Exists(logoPath)) logoPath = Path.Combine(themeDir, "Images", "Logo.png");

        if (File.Exists(logoPath)) {
            try { Sidebar.SetAppLogo(new Bitmap(logoPath)); } catch { Sidebar.SetAppLogo(null); }
        } else Sidebar.SetAppLogo(null);

        // 2. Cargar Fondo y Overlay del Tema
        Bitmap? bgBmp = null;
        Bitmap? overBmp = null;
        if (config != null) {
            if (!string.IsNullOrEmpty(config.BackgroundImage)) {
                string bgPath = Path.Combine(themeDir, config.BackgroundImage);
                // Si la ruta del JSON ya incluye "Images/", Path.Combine lo manejará.
                if (File.Exists(bgPath)) try { bgBmp = new Bitmap(bgPath); } catch { }
            }
            if (!string.IsNullOrEmpty(config.OverlayImage)) {
                string overPath = Path.Combine(themeDir, config.OverlayImage);
                if (File.Exists(overPath)) try { overBmp = new Bitmap(overPath); } catch { }
            }
            
            // 3. Aplicar Vista Preferida
            if (!string.IsNullOrEmpty(config.PreferredView)) {
                OnTopBarViewToggle(null, $"BtnView{config.PreferredView}");
            }
        }
        BackgroundControl.SetThemeBackground(bgBmp, overBmp);
    }

    private void LoadPlatformsWall() { 
        OverlayPlatformsWall.Initialize(_gameService.GetPlatforms());
    }
    private void ShowFullStats() { OverlayFullStats.IsVisible = true; }
    private void UpdateGameStatus(string status) { if (_selectedGame != null) { _selectedGame.PlayStatus = status; _gameService.UpdateGame(_selectedGame); LoadGames(); } }
    private void ShowMessage(string message) { OverlayDialog.ShowMessage(message); }

    private void LoadPlatformOrCategoryDetails(SidebarNode item)
    {
        GameDetails.IsVisible = false; Dashboard.IsVisible = true;
        if (item.Tag is ValueTuple<string, string> filter) {
            if (filter.Item1 == "PLATFORM") {
                var platform = _gameService.GetPlatforms().FirstOrDefault(p => p.Name == filter.Item2);
                if (platform != null) Dashboard.UpdatePlatform(platform);
            } else if (filter.Item1 == "CATEGORY") Dashboard.UpdateCategory(filter.Item2, _gameService);
        } else if (item.Tag is string simpleTag && simpleTag == "FAVORITES") Dashboard.UpdateCategory("Favoritos", _gameService);
    }
}
