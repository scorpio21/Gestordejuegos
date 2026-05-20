using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

namespace GestorJuegos;

public partial class MainWindow : Window
{
    private readonly GameService _gameService;
    private Platform? _selectedPlatform;
    private Game? _selectedGame;
    private byte[]? _currentCover;
    private readonly VimmVaultService _vimmService;
    private readonly EmuMoviesService _emuService;
    private System.Collections.Generic.List<Game> _currentPlatformGames = new System.Collections.Generic.List<Game>();
    private int _currentPage = 1;
    private const int PageSize = 100;
    private System.Collections.ObjectModel.ObservableCollection<string> _currentRoms = new();
    private System.Threading.CancellationTokenSource? _cts;
    private Action? _onConfirmAction;

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
        }
        catch { _settings = new AppSettings(); }
    }

    private void SaveSettings()
    {
        try
        {
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

        // MIGRACIÓN DE EMERGENCIA: Asegurar que la columna LastScanDate existe antes de que EF Core intente leerla
        try
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");
            if (File.Exists(dbPath))
            {
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    // Intentamos añadir la columna directamente. Si ya existe, lanzará un error que ignoraremos.
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "ALTER TABLE Platforms ADD COLUMN LastScanDate TEXT;";
                        try { command.ExecuteNonQuery(); } catch { }
                    }
                }
            }
        }
        catch { }
        
        _gamepadTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gamepadTimer.Tick += GamepadTimer_Tick;
        _gamepadTimer.Start();
        
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            Title = $"Gestor de Juegos v{version.Major}.{version.Minor}.{version.Build}";
        }

        _gameService = new GameService();
        _vimmService = new VimmVaultService();
        _emuService = new EmuMoviesService();

        LoadPlatforms();
        LoadDashboard();

        AddHandler(DragDrop.DropEvent, Window_Drop);

        BtnAddGame.Click += BtnAddGame_Click;
        BtnSave.Click += BtnSave_Click;
        BtnDelete.Click += BtnDelete_Click;
        BtnSelectCover.Click += BtnSelectCover_Click;
        BtnClearCover.Click += BtnClearCover_Click;
        
        BtnCancelProgress.Click += (s, e) => _cts?.Cancel();

        LstGames.SelectionChanged += LstGames_SelectionChanged;
        LstGamesGrid.SelectionChanged += LstGames_SelectionChanged;
        
        BtnViewList.Click += BtnViewList_Click;
        BtnViewGrid.Click += BtnViewGrid_Click;
        BtnPrevPage.Click += BtnPrevPage_Click;
        BtnNextPage.Click += BtnNextPage_Click;

        BtnAddPlatform.Click += BtnAddPlatform_Click;
        BtnCancelPlatform.Click += BtnCancelPlatform_Click;
        BtnSavePlatform.Click += BtnSavePlatform_Click;
        
        BtnManagePlatforms.Click += BtnManagePlatforms_Click;
        BtnCloseManagePlatforms.Click += BtnCloseManagePlatforms_Click;
        BtnSaveEditPlatform.Click += BtnSaveEditPlatform_Click;
        BtnDeletePlatform.Click += BtnDeletePlatform_Click;
        LstManagePlatforms.SelectionChanged += LstManagePlatforms_SelectionChanged;
        BtnSelectEmulator.Click += BtnSelectEmulator_Click;

        BtnGoDashboard.Click += BtnGoDashboard_Click;

        MenuExportDB.Click += MenuExportDB_Click;
        MenuImportDB.Click += MenuImportDB_Click;
        MenuImportDat.Click += MenuImportDat_Click;
        MenuBatchScrapeVimm.Click += (s, e) => RunBatchScrape("Vimm's Lair");

        MenuImportFolders.Click += MenuImportFolders_Click;
        MenuImportLaunchBox.Click += MenuImportLaunchBox_Click;
        MenuScanLocalCovers.Click += MenuScanLocalCovers_Click;

        BtnCancelExport.Click += (s, e) => OverlayExportOptions.IsVisible = false;
        BtnConfirmExport.Click += BtnConfirmExport_Click;

        BtnCloseMessage.Click += BtnCloseMessage_Click;
        
        BtnSearchIgdb.Click += BtnSearchIgdb_Click;
        BtnCancelIgdb.Click += BtnCancelIgdb_Click;
        BtnSelectIgdb.Click += BtnSelectIgdb_Click;

        BtnAddRom.Click += BtnAddRom_Click;
        BtnRemoveRom.Click += BtnRemoveRom_Click;
        BtnSelectOverrideEmulator.Click += BtnSelectOverrideEmulator_Click;
        MenuHelpEmulator.Click += MenuHelpEmulator_Click;
        MenuHelpMultiDisk.Click += MenuHelpMultiDisk_Click;
        MenuHelpDatabase.Click += MenuHelpDatabase_Click;
        MenuAbout.Click += MenuAbout_Click;
        BtnLaunchGame.Click += BtnLaunchGame_Click;

        BtnToggleFilters.Click += BtnToggleFilters_Click;
        BtnApplyFilters.Click += BtnApplyFilters_Click;
        BtnClearFilters.Click += BtnClearFilters_Click;
        
        BtnQuickFavorite.Click += BtnQuickFavorite_Click;
        BtnToggleGamepad.Click += BtnToggleGamepad_Click;

        BtnCancelVimm.Click += (s, e) => OverlayVimmSystem.IsVisible = false;
        BtnConfirmVimm.Click += BtnConfirmVimm_Click;

        MenuCleanupOrphans.Click += MenuCleanupOrphans_Click;
        MenuManageDross.Click += MenuManageDross_Click;
        MenuSettings.Click += (s, e) => {
            CfgLbPath.Text = _settings.LaunchBoxPath;
            CfgArtType.SelectedIndex = CmbArtType.Items.Cast<ComboBoxItem>().ToList().FindIndex(i => i.Content?.ToString() == _settings.PreferredArtType);
            if (CfgArtType.SelectedIndex < 0) CfgArtType.SelectedIndex = 0;
            CfgAutoImportCovers.IsChecked = _settings.AutoImportCovers;
            CfgEmuUser.Text = _settings.EmuMoviesUser;
            CfgEmuPass.Text = _settings.EmuMoviesPass;
            CfgEmuApiKey.Text = _settings.EmuMoviesApiKey;
            OverlaySettings.IsVisible = true;
        };

        BtnCancelSettings.Click += (s, e) => OverlaySettings.IsVisible = false;
        BtnSaveSettings.Click += (s, e) => {
            _settings.LaunchBoxPath = CfgLbPath.Text ?? "";
            _settings.PreferredArtType = (CfgArtType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Box - Front";
            _settings.AutoImportCovers = CfgAutoImportCovers.IsChecked ?? true;
            _settings.EmuMoviesUser = CfgEmuUser.Text ?? "";
            _settings.EmuMoviesPass = CfgEmuPass.Text ?? "";
            _settings.EmuMoviesApiKey = CfgEmuApiKey.Text ?? "D4F5E6A7B8C9D0E1F2";
            SaveSettings();
            OverlaySettings.IsVisible = false;
        };

        BtnBrowseLb.Click += async (s, e) => {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Seleccionar Carpeta Raíz de LaunchBox",
                    AllowMultiple = false
                });
                if (folders.Count > 0) CfgLbPath.Text = folders[0].Path.LocalPath;
            }
        };

        BtnCancelConfirm.Click += (s, e) => OverlayConfirm.IsVisible = false;
        BtnAcceptConfirm.Click += (s, e) => {
            OverlayConfirm.IsVisible = false;
            _onConfirmAction?.Invoke();
        };

        TxtGlobalSearch.TextChanged += TxtGlobalSearch_TextChanged;
        BtnClearGlobalSearch.Click += (s, e) => TxtGlobalSearch.Text = string.Empty;
        BtnCheckDuplicates.Click += BtnCheckDuplicates_Click;
        BtnBackFromSearch.Click += (s, e) => {
            TxtGlobalSearch.Text = string.Empty;
            PnlGlobalSearch.IsVisible = false;
            PnlDashboard.IsVisible = true;
        };
        LstGlobalSearchResults.SelectionChanged += LstGlobalSearchResults_SelectionChanged;

        CmbArtType.SelectionChanged += CmbArtType_SelectionChanged;

        BtnSearchEmuMovies.Click += BtnSearchEmuMovies_Click;
        BtnCancelEmu.Click += (s, e) => OverlayEmuSearch.IsVisible = false;
        BtnSelectEmu.Click += BtnSelectEmu_Click;

        InitVirtualKeyboard();
    }

    private async void BtnSearchEmuMovies_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;
        
        if (string.IsNullOrEmpty(_settings.EmuMoviesUser) || string.IsNullOrEmpty(_settings.EmuMoviesPass))
        {
            ShowMessage("Por favor, configura tu usuario y contraseña de EmuMovies en el panel de Configuración Global.");
            return;
        }

        OverlayEmuSearch.IsVisible = true;
        TxtEmuStatus.Text = "Iniciando sesión en EmuMovies...";
        LstEmuResults.ItemsSource = null;

        try
        {
            _emuService.SetCredentials(_settings.EmuMoviesApiKey, "Skyscraper");
            bool loggedIn = await _emuService.LoginAsync(_settings.EmuMoviesUser, _settings.EmuMoviesPass);
            
            if (!loggedIn)
            {
                TxtEmuStatus.Text = $"Error: {_emuService.LastErrorMessage}";
                return;
            }

            TxtEmuStatus.Text = $"Buscando '{_selectedGame.Name}'...";
            
            // Mapear nombre de plataforma al estándar de EmuMovies si es necesario
            string system = _selectedPlatform.Name;
            
            var results = await _emuService.SearchMediaAsync(_selectedGame.Name, system, "Box_Front");
            if (results.Count == 0)
            {
                // Reintento con búsqueda más simple si falla
                results = await _emuService.SearchMediaAsync(_selectedGame.Name.Split('(')[0].Trim(), system, "Box_Front");
            }

            LstEmuResults.ItemsSource = results;
            TxtEmuStatus.Text = results.Count > 0 ? $"Resultados ({results.Count})" : "No se encontraron resultados.";
        }
        catch (Exception ex)
        {
            TxtEmuStatus.Text = "Error en la búsqueda.";
            ShowMessage($"Error EmuMovies: {ex.Message}");
        }
    }

    private async void BtnSelectEmu_Click(object? sender, RoutedEventArgs e)
    {
        if (LstEmuResults.SelectedItem is EmuMediaResult result)
        {
            OverlayEmuSearch.IsVisible = false;
            ShowMessage("Descargando desde EmuMovies...");
            
            try
            {
                var data = await _emuService.DownloadMediaAsync(result.DownloadUrl);
                if (data != null && data.Length > 0)
                {
                    _currentCover = data;
                    UpdateCoverImage();
                    OverlayMessage.IsVisible = false;
                }
                else
                {
                    ShowMessage("Error al descargar el archivo.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error de descarga: {ex.Message}");
            }
        }
    }

    private void CmbArtType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;
        
        // Si el usuario cambia el tipo de arte, intentamos ver si existe en LaunchBox localmente
        string lbPath = _settings.LaunchBoxPath;
        if (Directory.Exists(lbPath))
        {
            string artType = (CmbArtType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Box - Front";
            string imagesPlatformPath = Path.Combine(lbPath, "Images", _selectedPlatform.Name, artType);
            
            if (Directory.Exists(imagesPlatformPath))
            {
                string title = _selectedGame.Name;
                string imgPath = Path.Combine(imagesPlatformPath, $"{title}.jpg");
                if (!File.Exists(imgPath)) imgPath = Path.Combine(imagesPlatformPath, $"{title}.png");
                
                if (!File.Exists(imgPath))
                {
                    imgPath = Directory.GetFiles(imagesPlatformPath, $"{title}*.*")
                        .FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) ?? "";
                }

                if (File.Exists(imgPath))
                {
                    try
                    {
                        _currentCover = File.ReadAllBytes(imgPath);
                        UpdateCoverImage();
                    }
                    catch { }
                }
            }
        }
    }

    private void TxtGlobalSearch_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string query = TxtGlobalSearch.Text?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            if (PnlGlobalSearch.IsVisible)
            {
                PnlGlobalSearch.IsVisible = false;
                PnlDashboard.IsVisible = true;
            }
            return;
        }

        PnlDashboard.IsVisible = false;
        PnlGlobalSearch.IsVisible = true;
        PnlHeaderToggles.IsVisible = false;
        PnlPagination.IsVisible = false;
        PnlGameDetails.IsVisible = false;

        using var context = new GestorJuegos.Data.AppDbContext();
        var results = context.Games
            .Include(g => g.Platform)
            .Where(g => g.Name.ToLower().Contains(query) || (g.Genre != null && g.Genre.ToLower().Contains(query)))
            .OrderBy(g => g.Name)
            .Take(50) // Límite para rendimiento
            .ToList();

        LstGlobalSearchResults.ItemsSource = results;
        TxtSearchStatus.Text = results.Count > 0 
            ? $"Resultados para '{query}' ({results.Count})" 
            : $"No hay resultados para '{query}'";
    }

    private void LstGlobalSearchResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstGlobalSearchResults.SelectedItem is Game game)
        {
            // Al seleccionar un juego de la búsqueda global, cargamos su plataforma
            _selectedPlatform = game.Platform;
            PnlGlobalSearch.IsVisible = false;
            PnlDashboard.IsVisible = false;
            PnlHeaderToggles.IsVisible = true;
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
                        targetPlatform = new Platform { Name = platformName };
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

    private async void MenuImportLaunchBox_Click(object? sender, RoutedEventArgs e)
    {
        string lbPath = _settings.LaunchBoxPath;
        
        if (!Directory.Exists(lbPath) || !Directory.Exists(Path.Combine(lbPath, "Data", "Platforms")))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta Raíz de LaunchBox (Ej: C:\\Users\\Nombre\\LaunchBox)",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                lbPath = folders[0].Path.LocalPath;
                // Validar que es una carpeta de LaunchBox
                if (!Directory.Exists(Path.Combine(lbPath, "Data", "Platforms")))
                {
                    ShowMessage("La carpeta seleccionada no parece ser una instalación válida de LaunchBox (No se encontró 'Data\\Platforms').");
                    return;
                }
                _settings.LaunchBoxPath = lbPath;
                SaveSettings();
            }
            else return;
        }

        string platformsPath = Path.Combine(lbPath, "Data", "Platforms");
        if (!Directory.Exists(platformsPath))
        {
            ShowMessage("No se encontró la carpeta 'Data\\Platforms' en la ruta de LaunchBox seleccionada.");
            return;
        }

        try
        {
            OverlayProgress.IsVisible = true;
            ProgBarImport.Value = 0;
            TxtProgressTitle.Text = "Importando desde LaunchBox...";
            TxtProgressDetail.Text = "Escaneando archivos de plataforma...";
            
            _cts = new System.Threading.CancellationTokenSource();

            await System.Threading.Tasks.Task.Run(async () =>
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

                    Platform? platform;
                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        platform = context.Platforms.FirstOrDefault(p => p.Name == platformName);
                        if (platform == null)
                        {
                            platform = new Platform { Name = platformName };
                            context.Platforms.Add(platform);
                            context.SaveChanges();
                        }
                    }

                    try
                    {
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
                                if (_settings.AutoImportCovers)
                                {
                                    try
                                    {
                                        // Intentar buscar carátula local en LaunchBox usando la preferencia
                                        string imagesPlatformPath = Path.Combine(lbPath, "Images", platformName, _settings.PreferredArtType);
                                        if (Directory.Exists(imagesPlatformPath))
                                        {
                                            string imgPath = Path.Combine(imagesPlatformPath, $"{title}.jpg");
                                            if (!File.Exists(imgPath)) imgPath = Path.Combine(imagesPlatformPath, $"{title}.png");
                                            
                                            if (!File.Exists(imgPath))
                                            {
                                                imgPath = Directory.GetFiles(imagesPlatformPath, $"{title}*.*")
                                                    .FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) ?? "";
                                            }

                                            if (File.Exists(imgPath)) coverData = File.ReadAllBytes(imgPath);
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
                                    Cover = coverData
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
                    catch { }
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    OverlayProgress.IsVisible = false;
                    LoadPlatforms();
                    LoadDashboard();
                    string status = cancelled ? "Importación CANCELADA" : "¡Importación desde LaunchBox finalizada!";
                    ShowMessage($"{status}\n\nSe han procesado las plataformas y juegos correctamente.\nTotal de juegos nuevos añadidos: {totalGamesAdded}");
                });
            });
        }
        catch (Exception ex)
        {
            OverlayProgress.IsVisible = false;
            ShowMessage($"Error durante la importación de LaunchBox: {ex.Message}");
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
        OverlayAddPlatform.IsVisible = true;
    }

    private void BtnCancelPlatform_Click(object? sender, RoutedEventArgs e)
    {
        OverlayAddPlatform.IsVisible = false;
    }

    private void BtnSavePlatform_Click(object? sender, RoutedEventArgs e)
    {
        var platformName = TxtNewPlatformName.Text?.Trim();
        if (!string.IsNullOrEmpty(platformName))
        {
            _gameService.AddPlatform(new Platform { Name = platformName });
            LoadPlatforms();
        }
        OverlayAddPlatform.IsVisible = false;
    }

    private void BtnViewList_Click(object? sender, RoutedEventArgs e)
    {
        LstGames.IsVisible = true;
        LstGamesGrid.IsVisible = false;
        BtnViewList.Background = Avalonia.Media.Brush.Parse("#444444");
        BtnViewGrid.Background = Avalonia.Media.Brush.Parse("#222222");
    }

    private void BtnViewGrid_Click(object? sender, RoutedEventArgs e)
    {
        LstGames.IsVisible = false;
        LstGamesGrid.IsVisible = true;
        BtnViewList.Background = Avalonia.Media.Brush.Parse("#222222");
        BtnViewGrid.Background = Avalonia.Media.Brush.Parse("#444444");
    }

    private void LoadPlatforms()
    {
        var platforms = _gameService.GetPlatforms();
        MenuPlataformas.Items.Clear();

        bool hasPlatforms = platforms.Count > 0;
        
        // Disable requested UI elements when there are no platforms
        MenuPlataformas.IsEnabled = hasPlatforms;
        BtnManagePlatforms.IsEnabled = hasPlatforms;
        BtnAddGame.IsEnabled = hasPlatforms;
        BtnViewList.IsEnabled = hasPlatforms;
        BtnViewGrid.IsEnabled = hasPlatforms;
        MenuExportDB.IsEnabled = hasPlatforms;
        // Note: MenuImportDB is NOT disabled, so the user can import a backup even if the DB is empty.

        foreach (var platform in platforms)
        {
            var menuItem = new MenuItem { Header = platform.Name, Tag = platform };
            menuItem.Click += PlatformMenuItem_Click;
            MenuPlataformas.Items.Add(menuItem);
        }
    }

    private void PlatformMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Platform platform)
        {
            _selectedPlatform = platform;
            TxtSelectedPlatform.Text = $"Plataforma: {platform.Name}";
            PnlDashboard.IsVisible = false;
            PnlHeaderToggles.IsVisible = true;
            PnlPagination.IsVisible = true;
            
            // Restore proper view based on selection
            if (BtnViewList.Background != null && BtnViewList.Background.ToString() == "#ff444444")
            {
                LstGames.IsVisible = true;
                LstGamesGrid.IsVisible = false;
            }
            else
            {
                LstGames.IsVisible = false;
                LstGamesGrid.IsVisible = true;
            }

            LoadGames();
            PnlGameDetails.IsVisible = false;
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

    private void LoadDashboard()
    {
        PnlDashboard.IsVisible = true;
        PnlHeaderToggles.IsVisible = false;
        PnlPagination.IsVisible = false;
        LstGames.IsVisible = false;
        LstGamesGrid.IsVisible = false;
        PnlGameDetails.IsVisible = false;

        using var context = new GestorJuegos.Data.AppDbContext();
        context.Database.EnsureCreated();

        int totalGames = context.Games.Count();
        int totalPlatforms = context.Platforms.Count();
        
        DashTotalGames.Text = totalGames.ToString();
        DashTotalPlatforms.Text = totalPlatforms.ToString();

        // Género favorito
        var topGenre = context.Games
            .Where(g => !string.IsNullOrEmpty(g.Genre))
            .GroupBy(g => g.Genre)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Ninguno";
        
        DashTopGenre.Text = topGenre;

        // Estadísticas de Carátulas
        using var coversContext = new GestorJuegos.Data.CoversDbContext();
        int gamesWithCover = coversContext.Covers.Count();
        double coverPercent = totalGames > 0 ? (gamesWithCover * 100.0) / totalGames : 0;
        DashCoverProgress.Value = coverPercent;
        DashCoverPercent.Text = $"{coverPercent:F1}% ({gamesWithCover}/{totalGames})";

        // Estadísticas de Regiones
        var regionStats = context.Games
            .Where(g => !string.IsNullOrEmpty(g.Region))
            .GroupBy(g => g.Region)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Key = g.Key, Value = g.Count() })
            .ToList();
        DashRegionStats.ItemsSource = regionStats;

        // Distribución
        var platformStats = _gameService.GetGamesCountByPlatform()
            .Select(p => new { Key = p.Key, Value = p.Value })
            .OrderByDescending(p => p.Value)
            .ToList();
        DashPlatformStats.ItemsSource = platformStats;

        // Recientes (últimos 10 agregados)
        var recentGames = context.Games
            .Include(g => g.Platform)
            .OrderByDescending(g => g.Id)
            .Take(10)
            .ToList();
        
        // Cargar miniaturas para los recientes
        foreach(var rg in recentGames)
        {
            rg.Cover = _gameService.GetGameThumbnail(rg.Id);
        }
        DashRecentGames.ItemsSource = recentGames;
    }

    private void MenuExportDB_Click(object? sender, RoutedEventArgs e)
    {
        OverlayExportOptions.IsVisible = true;
    }

    private async void BtnConfirmExport_Click(object? sender, RoutedEventArgs e)
    {
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
                    var games = doc.Descendants("game");
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

                    foreach (var gameNode in games)
                    {
                        string name = gameNode.Attribute("name")?.Value ?? "";
                        if (string.IsNullOrEmpty(name)) continue;

                        if (ImportService.IsDross(name, drossPatterns))
                        {
                            skippedCount++;
                            continue;
                        }

                        string region = "🌎 World";
                        if (name.Contains("(JP") || name.Contains("(Japan")) region = "🇯🇵 JP";
                        else if (name.Contains("(US") || name.Contains("(USA")) region = "🇺🇸 US";
                        else if (name.Contains("(EU") || name.Contains("(Europe")) region = "🇪🇺 EU";
                        else if (name.Contains("(Spain", StringComparison.OrdinalIgnoreCase) || name.Contains("(España", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es)", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es-Es)", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es - Es)", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";

                        string cleanName = name;
                        int bracketIndex = name.IndexOf('(');
                        if (bracketIndex > 0)
                        {
                            cleanName = name.Substring(0, bracketIndex).Trim();
                        }
                        
                        if (cleanName.Contains("•"))
                        {
                            cleanName = cleanName.Split('•')[0].Trim();
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
                                Region = region,
                                Year = DateTime.Now.Year
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

    private async void BtnSearchIgdb_Click(object? sender, RoutedEventArgs e)
    {
        string query = TxtName.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            ShowMessage("Por favor, escriba el nombre del juego antes de buscar.");
            return;
        }

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
            PnlEditPlatform.IsVisible = true;
        }
    }

    private void BtnSaveEditPlatform_Click(object? sender, RoutedEventArgs e)
    {
        if (LstManagePlatforms.SelectedItem is Platform platform)
        {
            var newName = TxtEditPlatformName.Text?.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                platform.Name = newName;
                platform.EmulatorPath = TxtEmulatorPath.Text?.Trim() ?? "";
                platform.LaunchArguments = TxtLaunchArgs.Text?.Trim() ?? "";
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

    private void BtnDeletePlatform_Click(object? sender, RoutedEventArgs e)
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
        _currentPlatformGames = _gameService.GetGamesByPlatform(_selectedPlatform.Id);
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

        // Aplicar Ordenación
        if (CmbSortOrder != null)
        {
            switch (CmbSortOrder.SelectedIndex)
            {
                case 0: // Nombre (A-Z)
                    filtered = filtered.OrderBy(g => g.Name);
                    break;
                case 1: // Nombre (Z-A)
                    filtered = filtered.OrderByDescending(g => g.Name);
                    break;
                case 2: // Año (Asc)
                    filtered = filtered.OrderBy(g => g.Year);
                    break;
                case 3: // Año (Desc)
                    filtered = filtered.OrderByDescending(g => g.Year);
                    break;
                case 4: // Recién añadidos
                    filtered = filtered.OrderByDescending(g => g.DateAdded ?? DateTime.MinValue).ThenBy(g => g.Name);
                    break;
                case 5: // Antiguos
                    filtered = filtered.OrderBy(g => g.DateAdded ?? DateTime.MinValue).ThenBy(g => g.Name);
                    break;
                default:
                    filtered = filtered.OrderBy(g => g.Name);
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

        // Cargar miniaturas para la página actual
        foreach (var game in paginated)
        {
            game.Cover = _gameService.GetGameThumbnail(game.Id);
        }

        LstGames.ItemsSource = paginated;
        LstGamesGrid.ItemsSource = paginated;
        
        if (_selectedPlatform != null)
        {
            TxtSelectedPlatform.Text = $"{_selectedPlatform.Name} ({totalItems} juegos)";
        }
        
        TxtPageInfo.Text = $"Página {_currentPage} de {totalPages}";
        BtnPrevPage.IsEnabled = _currentPage > 1;
        BtnNextPage.IsEnabled = _currentPage < totalPages;
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
        _currentPage++;
        ApplySearchFilter();
        
        // Scroll to top
        var firstList = LstGames.Items.Cast<object>().FirstOrDefault();
        if (LstGames.IsVisible && firstList != null) LstGames.ScrollIntoView(firstList);
        
        var firstGrid = LstGamesGrid.Items.Cast<object>().FirstOrDefault();
        if (LstGamesGrid.IsVisible && firstGrid != null) LstGamesGrid.ScrollIntoView(firstGrid);
    }

    private void LstGames_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox?.SelectedItem is Game game)
        {
            // Sync selection
            if (sender == LstGames && LstGamesGrid.SelectedItem != game)
                LstGamesGrid.SelectedItem = game;
            else if (sender == LstGamesGrid && LstGames.SelectedItem != game)
                LstGames.SelectedItem = game;

            _selectedGame = game;
            TxtName.Text = game.Name;
            NumYear.Value = game.Year;
            TxtGenre.Text = game.Genre;
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
            
            // Set the selected region in the ComboBox
            var regionItem = CmbRegion.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == game.Region);
            if (regionItem != null)
            {
                CmbRegion.SelectedItem = regionItem;
            }
            else
            {
                CmbRegion.SelectedIndex = 0;
            }

            _currentCover = _gameService.GetGameFullCover(game.Id);
            UpdateCoverImage();

            BtnDelete.IsVisible = true;
            PnlGameDetails.IsVisible = true;
        }
    }

    private void BtnAddGame_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero una plataforma en el menú superior antes de intentar añadir un juego.");
            return;
        }

        _selectedGame = new Game { PlatformId = _selectedPlatform.Id, Year = DateTime.Now.Year };
        TxtName.Text = string.Empty;
        NumYear.Value = _selectedGame.Year;
        TxtGenre.Text = string.Empty;
        TxtLanguages.Text = string.Empty;
        _currentRoms.Clear();
        LstRoms.ItemsSource = _currentRoms;
        TxtOverrideEmulator.Text = string.Empty;
        TxtOverrideArgs.Text = string.Empty;
        ChkIsFavorite.IsChecked = false;
        CmbRegion.SelectedIndex = 0;
        _currentCover = null;
        UpdateCoverImage();

        LstGames.SelectedItem = null;
        LstGamesGrid.SelectedItem = null;
        BtnDelete.IsVisible = false;
        PnlGameDetails.IsVisible = true;
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;

        _selectedGame.Name = TxtName.Text ?? string.Empty;
        _selectedGame.Year = (int)(NumYear.Value ?? DateTime.Now.Year);
        _selectedGame.Genre = TxtGenre.Text ?? string.Empty;
        _selectedGame.Languages = TxtLanguages.Text ?? string.Empty;
        
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
        _selectedGame.Cover = _currentCover;

        if (_selectedGame.Id == 0)
        {
            _gameService.AddGame(_selectedGame);
        }
        else
        {
            _gameService.UpdateGame(_selectedGame);
        }

        LoadGames();
        PnlGameDetails.IsVisible = false;
    }

    private void BtnDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame != null && _selectedGame.Id != 0)
        {
            _gameService.DeleteGame(_selectedGame.Id);
            LoadGames();
            PnlGameDetails.IsVisible = false;
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
                ImgCover.Source = new Bitmap(ms);
            }
            catch
            {
                ImgCover.Source = null;
            }
        }
        else
        {
            ImgCover.Source = null;
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

    private void MenuHelpEmulator_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "🚀 CONFIGURACIÓN DE EMULADORES\n\n" +
            "Para que tus juegos funcionen, el Gestor necesita saber con qué programa abrirlos:\n\n" +
            "• CONFIGURACIÓN GLOBAL (Por Plataforma):\n" +
            "  Haz clic en '⚙️ Gestionar' (arriba), selecciona una consola y busca el ejecutable (.exe). " +
            "Todos los juegos de esa consola se abrirán con él por defecto.\n\n" +
            "• CONFIGURACIÓN INDIVIDUAL (Por Juego):\n" +
            "  Puedes editar un juego específico y rellenar 'Perfil de Emulador' para ignorar la configuración global.\n\n" +
            "💡 USO DE ARGUMENTOS:\n" +
            "  Usa el marcador {0} para indicar la ruta del juego.\n" +
            "  Ejemplo: -L cores\\snes9x_libretro.dll \"{0}\"";
            
        ShowMessage(helpText);
    }

    private void MenuHelpMultiDisk_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "💿 SOPORTE PARA JUEGOS MULTI-DISCO\n\n" +
            "1. CÓMO AÑADIR DISCOS:\n" +
            "   Al editar un juego, pulsa el botón '+' en 'Rutas de ROM' para añadir Disco 1, Disco 2, etc.\n\n" +
            "2. CÓMO JUGAR:\n" +
            "   Selecciona el juego, haz clic sobre el disco deseado en la lista de detalles y pulsa '▶ JUGAR'.\n\n" +
            "3. IMPORTACIÓN INTELIGENTE:\n" +
            "   Si sueltas una carpeta sobre el programa, este escaneará recursivamente y agrupará los juegos automáticamente.";
            
        ShowMessage(helpText);
    }

    private void MenuHelpDatabase_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "🗄️ BASES DE DATOS Y RESPALDOS (v1.0.9.5)\n\n" +
            "• ARQUITECTURA DUAL:\n" +
            "  Tus datos están separados en dos archivos para mayor velocidad:\n" +
            "  - GestorJuegos.db: Información de textos (rápida).\n" +
            "  - GestorCovers.db: Imágenes y miniaturas (multimedia).\n\n" +
            "• RESPALDOS:\n" +
            "  Al exportar, verás un panel para elegir qué base de datos quieres salvar. Recomendamos exportar ambas periódicamente.\n\n" +
            "• MINIATURAS (CACHE):\n" +
            "  El programa genera miniaturas automáticas. Esto permite navegar por miles de juegos sin ralentizar tu PC.";

        ShowMessage(helpText);
    }

    private void MenuAbout_Click(object? sender, RoutedEventArgs e)
    {
        string aboutText = "🎮 GESTOR DE JUEGOS v1.0.9.5\n\n" +
            "Un organizador integral para colecciones de juegos retro, optimizado para grandes bibliotecas y uso con mando.\n\n" +
            "👨‍💻 Autor: scorpio21 / Gemini CLI\n" +
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

                        if ((hasGamesAtThisLevel || hasRegionSubdirs || looksLikePlatform || hasHighDensity || contentSubfolderHasGames) && !isCategoryOnly)
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

                            var platform = context.Platforms.FirstOrDefault(p => p.Name == pName);
                            if (platform == null)
                            {
                                platform = new Platform { Name = pName };
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
                                        var existingGame = context.Games.FirstOrDefault(g => g.PlatformId == platform.Id && 
                                                            g.Name == game.Name && g.Region == game.Region && g.Languages == game.Languages);
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
                ShowMessage($"Error durante la importación: {ex.Message}");
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

                await System.Threading.Tasks.Task.Run(async () =>
                {
                    var coverFiles = Directory.GetFiles(coverPath, "*.*", SearchOption.AllDirectories)
                                             .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                                         f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                         f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                             .ToList();

                    int matchCount = 0;
                    bool cancelled = false;
                    
                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        var games = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).ToList();
                        var gamesToUpdate = new List<Game>();

                        for (int i = 0; i < games.Count; i++)
                        {
                            if (_cts.IsCancellationRequested) { cancelled = true; break; }
                            
                            var game = games[i];
                            
                            // Solo buscar si no tiene carátula en la DB secundaria
                            if (_gameService.GetGameThumbnail(game.Id) != null) continue;

                            int currentI = i;
                            int totalI = games.Count;
                            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                                ProgBarImport.Value = (currentI * 100) / totalI;
                                TxtProgressDetail.Text = $"Buscando para: {game.Name} ({currentI}/{totalI})";
                            });

                            var match = coverFiles.FirstOrDefault(f => {
                                string fileName = Path.GetFileNameWithoutExtension(f);
                                return fileName.Equals(game.Name, StringComparison.OrdinalIgnoreCase) || 
                                       fileName.Contains(game.Name, StringComparison.OrdinalIgnoreCase) ||
                                       game.Name.Contains(fileName, StringComparison.OrdinalIgnoreCase);
                            });

                            if (match != null)
                            {
                                try 
                                {
                                    game.Cover = File.ReadAllBytes(match);
                                    gamesToUpdate.Add(game);
                                    matchCount++;
                                    
                                    if (gamesToUpdate.Count >= 100)
                                    {
                                        _gameService.UpdateGamesBatch(gamesToUpdate);
                                        gamesToUpdate.Clear();
                                    }
                                } catch { }
                            }
                        }
                        
                        if (gamesToUpdate.Any())
                        {
                            _gameService.UpdateGamesBatch(gamesToUpdate);
                        }
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        OverlayProgress.IsVisible = false;
                        LoadGames();
                        string status = cancelled ? "Escaneo CANCELADO" : "Escaneo de carátulas finalizado";
                        ShowMessage($"{status}\n\nSe han asociado {matchCount} carátulas buscando recursivamente en {_selectedPlatform.Name}.");
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
        PnlHeaderToggles.IsVisible = false;
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
}
