using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using System;
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
    private readonly IgdbService _igdbService;
    private readonly TheGamesDbService _theGamesDbService;
    private readonly GameTdbService _gameTdbService;
    private readonly PalSnesCoversService _palSnesCoversService;
    private string _currentScraperSource = "IGDB";
    private System.Collections.Generic.List<Game> _currentPlatformGames = new System.Collections.Generic.List<Game>();
    private int _currentPage = 1;
    private const int PageSize = 100;
    private System.Collections.ObjectModel.ObservableCollection<string> _currentRoms = new();

    public MainWindow()
    {
        InitializeComponent();
        
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
        string clientId = "";
        string clientSecret = "";
        string theGamesDbKey = "";
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("IGDB", out var igdbConfig))
                {
                    clientId = igdbConfig.GetProperty("ClientId").GetString() ?? "";
                    clientSecret = igdbConfig.GetProperty("ClientSecret").GetString() ?? "";
                }
                if (doc.RootElement.TryGetProperty("TheGamesDB", out var tgdbConfig))
                {
                    theGamesDbKey = tgdbConfig.GetProperty("ApiKey").GetString() ?? "";
                }
            }
            else
            {
                var defaultConfig = new
                {
                    IGDB = new { ClientId = "", ClientSecret = "" },
                    TheGamesDB = new { ApiKey = "" }
                };
                File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(defaultConfig, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch { }

        _igdbService = new IgdbService(clientId, clientSecret);
        _theGamesDbService = new TheGamesDbService(theGamesDbKey);
        _gameTdbService = new GameTdbService();
        _palSnesCoversService = new PalSnesCoversService();
        LoadPlatforms();
        LoadDashboard();

        AddHandler(DragDrop.DropEvent, Window_Drop);

        BtnAddGame.Click += BtnAddGame_Click;
        BtnSave.Click += BtnSave_Click;
        BtnDelete.Click += BtnDelete_Click;
        BtnSelectCover.Click += BtnSelectCover_Click;
        BtnClearCover.Click += BtnClearCover_Click;
        
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
        MenuBatchScrapeIgdb.Click += (s, e) => RunBatchScrape("IGDB");
        MenuBatchScrapeTgdb.Click += (s, e) => RunBatchScrape("TheGamesDB");
        MenuBatchScrapeGameTdb.Click += (s, e) => RunBatchScrape("GameTDB");
        MenuBatchScrapePalSnes.Click += (s, e) => RunBatchScrape("PalSnesCovers");

        BtnCloseMessage.Click += BtnCloseMessage_Click;
        
        BtnSearchIgdb.Click += BtnSearchIgdb_Click;
        BtnCancelIgdb.Click += BtnCancelIgdb_Click;
        BtnSelectIgdb.Click += BtnSelectIgdb_Click;

        BtnAddRom.Click += BtnAddRom_Click;
        BtnRemoveRom.Click += BtnRemoveRom_Click;
        BtnSelectOverrideEmulator.Click += BtnSelectOverrideEmulator_Click;
        MenuHelpEmulator.Click += MenuHelpEmulator_Click;
        MenuHelpMultiDisk.Click += MenuHelpMultiDisk_Click;
        BtnLaunchGame.Click += BtnLaunchGame_Click;

        BtnToggleFilters.Click += BtnToggleFilters_Click;
        BtnApplyFilters.Click += BtnApplyFilters_Click;
        BtnClearFilters.Click += BtnClearFilters_Click;
        
        BtnQuickFavorite.Click += BtnQuickFavorite_Click;
        BtnToggleGamepad.Click += BtnToggleGamepad_Click;

        InitVirtualKeyboard();
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
            BtnQuickFavorite_Click(null, null);
            return;
        }
        if (buttons.HasFlag(Vortice.XInput.GamepadButtons.Y))
        {
            BtnToggleFilters_Click(null, null);
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
                BtnSelectIgdb_Click(null, null);
                return;
            }
            if (_gamepadInHeader)
            {
                _kbdX = 0; _kbdY = 0;
                TxtKeyboardInput.Text = TxtSearchGame.Text;
                UpdateKeyboardHighlight();
                OverlayKeyboard.IsVisible = true;
                return;
            }

            if (PnlGameDetails.IsVisible && _selectedGame != null && BtnLaunchGame.IsVisible)
            {
                BtnLaunchGame_Click(null, null);
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
            if (BtnPrevPage.IsVisible) BtnPrevPage_Click(null, null);
        }
        else if (buttons.HasFlag(Vortice.XInput.GamepadButtons.RightShoulder))
        {
            if (BtnNextPage.IsVisible) BtnNextPage_Click(null, null);
        }

        Avalonia.Controls.ListBox? activeList = LstGames.IsVisible ? LstGames : (LstGamesGrid.IsVisible ? LstGamesGrid : null);
        
        if (_gamepadInHeader)
        {
            if (buttons.HasFlag(Vortice.XInput.GamepadButtons.DPadDown))
            {
                _gamepadInHeader = false;
                TxtSearchGame.Background = Avalonia.Media.Brush.Parse("#1e293b"); // Normal
                if (activeList != null && activeList.ItemCount > 0)
                {
                    activeList.SelectedIndex = 0;
                    var item = activeList.Items.Cast<object>().ElementAtOrDefault(0);
                    if (item != null) activeList.ScrollIntoView(item);
                }
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
                TxtSearchGame.Background = Avalonia.Media.Brush.Parse("#475569"); // Highlight
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
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona una plataforma antes de soltar un archivo de lista.");
            return;
        }

        var filesData = e.DataTransfer.TryGetFiles();
        if (filesData == null) return;
        
        var files = filesData.Select(f => f.TryGetLocalPath() ?? f.Name).ToList();

        if (files == null || files.Count == 0) return;

        var txtFile = files.FirstOrDefault(f => f != null && f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        if (txtFile != null)
        {
            try
            {
                ShowMessage("Procesando archivo...");
                using var stream = File.OpenRead(txtFile);
                using var reader = new StreamReader(stream);
                string content = await reader.ReadToEndAsync();
                
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var newGames = new System.Collections.Generic.List<Game>();
                
                var existingNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var context = new GestorJuegos.Data.AppDbContext())
                {
                    var platformGames = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).Select(g => new { g.Name, g.Region }).ToList();
                    foreach(var g in platformGames) 
                        if(g.Name != null) existingNames.Add($"{g.Name}|{g.Region}");
                }
                
                int skippedCount = 0;
                
                foreach(var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("Plataforma:", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    string baseName = Path.GetFileNameWithoutExtension(line);
                    
                    string region = "🌎 World";
                    if (baseName.Contains("(Europe") || baseName.Contains("(EU")) region = "🇪🇺 EU";
                    else if (baseName.Contains("(USA") || baseName.Contains("(US")) region = "🇺🇸 US";
                    else if (baseName.Contains("(Japan") || baseName.Contains("(JP")) region = "🇯🇵 JP";
                    else if (baseName.Contains("(Spain", StringComparison.OrdinalIgnoreCase) || baseName.Contains("(España", StringComparison.OrdinalIgnoreCase) || baseName.Contains("(Es)", StringComparison.OrdinalIgnoreCase) || baseName.Contains("(Es-Es)", StringComparison.OrdinalIgnoreCase) || baseName.Contains("(Es - Es)", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";

                    // Extract languages e.g. (En,Fr,De)
                    string langs = "";
                    var langMatch = Regex.Match(baseName, @"\(([A-Za-z]{2}(?:,[A-Za-z]{2})*)\)");
                    if (langMatch.Success)
                    {
                        langs = langMatch.Groups[1].Value;
                    }

                    // Clean name
                    string cleanName = Regex.Replace(baseName, @"\([^)]*\)|\[[^\]]*\]", "").Trim();
                    if (cleanName.Contains("•")) cleanName = cleanName.Split('•')[0].Trim();

                    if (!string.IsNullOrEmpty(cleanName))
                    {
                        string uniqueKey = $"{cleanName}|{region}";
                        if (existingNames.Contains(uniqueKey))
                        {
                            skippedCount++;
                        }
                        else
                        {
                            existingNames.Add(uniqueKey); // Evitar duplicados dentro del mismo txt
                            newGames.Add(new Game
                            {
                                PlatformId = _selectedPlatform.Id,
                                Name = cleanName,
                                Region = region,
                                Languages = langs,
                                Year = DateTime.Now.Year
                            });
                        }
                    }
                }

                if (newGames.Count > 0)
                {
                    using (var context = new GestorJuegos.Data.AppDbContext())
                    {
                        context.Games.AddRange(newGames);
                        context.SaveChanges();
                    }
                    LoadGames();
                    string msg = $"¡Importación completada! Se añadieron {newGames.Count} juegos.";
                    if (skippedCount > 0) msg += $" Se omitieron {skippedCount} que ya existían.";
                    ShowMessage(msg);
                }
                else if (skippedCount > 0)
                {
                    ShowMessage($"No se añadieron juegos. Se omitieron {skippedCount} que ya existían en la plataforma.");
                }
                else
                {
                    ShowMessage("No se encontraron juegos válidos en la lista.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al leer el archivo: {ex.Message}");
            }
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

        // Distribución
        var platformStats = context.Platforms
            .Select(p => new { Key = p.Name, Value = p.Games.Count })
            .OrderByDescending(p => p.Value)
            .ToList();
        DashPlatformStats.ItemsSource = platformStats;

        // Recientes (últimos 10 agregados)
        var recentGames = context.Games
            .Include(g => g.Platform)
            .OrderByDescending(g => g.Id)
            .Take(10)
            .ToList();
        DashRecentGames.ItemsSource = recentGames;
    }

    private async void MenuExportDB_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar Base de Datos",
            SuggestedFileName = "GestorJuegos_Backup.db",
            FileTypeChoices = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
        });

        if (file != null)
        {
            try
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");
                if (File.Exists(dbPath))
                {
                    using var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var destinationStream = await file.OpenWriteAsync();
                    await sourceStream.CopyToAsync(destinationStream);
                    ShowMessage("Base de datos exportada con éxito.");
                }
                else
                {
                    ShowMessage("No se encontró la base de datos local para exportar.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al exportar: {ex.Message}");
            }
        }
    }

    private async void MenuImportDB_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar Base de Datos",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
        });

        if (files.Count > 0)
        {
            try
            {
                var file = files[0];
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");

                using var sourceStream = await file.OpenReadAsync();
                using var destinationStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await sourceStream.CopyToAsync(destinationStream);

                // Clear UI and reload
                _selectedPlatform = null;
                TxtSelectedPlatform.Text = "Seleccione una plataforma";
                LstGames.ItemsSource = null;
                LstGamesGrid.ItemsSource = null;
                PnlGameDetails.IsVisible = false;
                LoadPlatforms();

                ShowMessage("Base de datos importada con éxito.");
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al importar: {ex.Message}. Asegúrese de no tener otras aplicaciones bloqueando el archivo.");
            }
        }
    }

    private async void RunBatchScrape(string source)
    {
        if (_selectedPlatform == null)
        {
            ShowMessage("Por favor, selecciona primero la plataforma a la que quieres descargar carátulas.");
            return;
        }

        var gamesWithoutCover = _gameService.GetGamesByPlatform(_selectedPlatform.Id)
                                            .Where(g => g.Cover == null || g.Cover.Length == 0)
                                            .ToList();

        if (gamesWithoutCover.Count == 0)
        {
            ShowMessage("Todos los juegos de esta plataforma ya tienen carátula.");
            return;
        }

        ShowMessage($"Iniciando descarga para {gamesWithoutCover.Count} juegos...");
        BtnCloseMessage.IsEnabled = false; // Deshabilitar para no cerrar mientras carga
        
        await System.Threading.Tasks.Task.Run(async () =>
        {
            int successCount = 0;
            for (int i = 0; i < gamesWithoutCover.Count; i++)
            {
                var game = gamesWithoutCover[i];
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    TxtMessageContent.Text = $"Buscando carátula para '{game.Name}' ({i + 1}/{gamesWithoutCover.Count})...";
                });

                try
                {
                    var results = new System.Collections.Generic.List<GestorJuegos.Services.IgdbSearchResult>();
                    if (source == "IGDB") results = await _igdbService.SearchGamesAsync(game.Name);
                    else if (source == "TheGamesDB") results = await _theGamesDbService.SearchGamesAsync(game.Name);
                    else if (source == "GameTDB" && _selectedPlatform != null) results = await _gameTdbService.SearchGamesAsync(game.Name, _selectedPlatform.Name);
                    else if (source == "PalSnesCovers") results = await _palSnesCoversService.SearchGamesAsync(game.Name);

                    GestorJuegos.Services.IgdbSearchResult? match = null;
                    if (results.Count > 0)
                    {
                        if (_selectedPlatform != null)
                        {
                            match = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.CoverUrl) && r.Platforms.Any(p => p.Contains(_selectedPlatform.Name, StringComparison.OrdinalIgnoreCase) || _selectedPlatform.Name.Contains(p, StringComparison.OrdinalIgnoreCase)));
                        }
                        if (match == null) match = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.CoverUrl));
                    }
                    
                    if (match != null)
                    {
                        byte[]? coverData = null;
                        if (source == "IGDB") coverData = await _igdbService.DownloadCoverAsync(match.CoverUrl);
                        else if (source == "TheGamesDB") coverData = await _theGamesDbService.DownloadCoverAsync(match.CoverUrl);
                        else if (source == "GameTDB") coverData = await _gameTdbService.DownloadCoverAsync(match.CoverUrl);
                        else if (source == "PalSnesCovers") coverData = await _palSnesCoversService.DownloadCoverAsync(match.CoverUrl);
                        
                        if (coverData != null && coverData.Length > 0)
                        {
                            game.Cover = coverData;
                            
                            // Comprobar y actualizar metadatos si faltan o tienen valores por defecto
                            if (match.Year.HasValue && (game.Year == DateTime.Now.Year || game.Year == 0))
                            {
                                game.Year = match.Year.Value;
                            }
                            
                            if (!string.IsNullOrEmpty(match.Genre) && string.IsNullOrEmpty(game.Genre))
                            {
                                game.Genre = match.Genre;
                            }
                            
                            // Guardar en la DB usando un nuevo contexto por ser asíncrono
                            using (var context = new GestorJuegos.Data.AppDbContext())
                            {
                                context.Games.Update(game);
                                context.SaveChanges();
                            }
                            
                            successCount++;
                        }
                    }
                    
                    // Pequeña pausa para no saturar la API (Rate limit es 4 peticiones por segundo en Twitch/IGDB)
                    await System.Threading.Tasks.Task.Delay(300); 
                }
                catch
                {
                    // Ignorar errores individuales para no detener el lote
                }
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                BtnCloseMessage.IsEnabled = true;
                LoadGames(); // Actualizar la vista
                if (_selectedGame != null && _selectedGame.Id != 0)
                {
                    // Refrescar carátula si el juego estaba seleccionado
                    var updatedGame = gamesWithoutCover.FirstOrDefault(g => g.Id == _selectedGame.Id);
                    if (updatedGame != null)
                    {
                        _currentCover = updatedGame.Cover;
                        UpdateCoverImage();
                    }
                }
                ShowMessage($"¡Proceso completado! Se descargaron {successCount} carátulas de {gamesWithoutCover.Count} juegos faltantes.");
            });
        });
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

                    foreach (var gameNode in games)
                    {
                        string name = gameNode.Attribute("name")?.Value ?? "";
                        if (string.IsNullOrEmpty(name)) continue;

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
                        context.Games.AddRange(newGames);
                        context.SaveChanges();
                    }
                    
                    count = newGames.Count;

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

        string selectedSource = "IGDB";
        if (CmbScraperSource.SelectedItem is ComboBoxItem item && item.Content != null)
        {
            selectedSource = item.Content.ToString() ?? "IGDB";
        }
        _currentScraperSource = selectedSource;

        OverlayIgdbSearch.IsVisible = true;
        TxtIgdbStatus.Text = $"Buscando '{query}' en {selectedSource}...";
        LstIgdbResults.ItemsSource = null;

        try
        {
            var results = new System.Collections.Generic.List<GestorJuegos.Services.IgdbSearchResult>();
            
            if (selectedSource == "IGDB") results = await _igdbService.SearchGamesAsync(query);
            else if (selectedSource == "TheGamesDB") results = await _theGamesDbService.SearchGamesAsync(query);
            else if (selectedSource == "GameTDB") 
            {
                if (_selectedPlatform == null)
                {
                    TxtIgdbStatus.Text = "GameTDB requiere seleccionar plataforma principal (Menú).";
                    return;
                }
                results = await _gameTdbService.SearchGamesAsync(query, _selectedPlatform.Name);
            }
            else if (selectedSource == "PalSnesCovers") results = await _palSnesCoversService.SearchGamesAsync(query);

            if (_selectedPlatform != null && selectedSource == "IGDB")
            {
                results = results.OrderByDescending(r => r.Platforms.Any(p => p.Contains(_selectedPlatform.Name, StringComparison.OrdinalIgnoreCase) || _selectedPlatform.Name.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
            }
            LstIgdbResults.ItemsSource = results;
            if (results.Count == 0)
            {
                TxtIgdbStatus.Text = "No se encontraron resultados.";
            }
            else
            {
                TxtIgdbStatus.Text = $"Resultados de Búsqueda ({selectedSource})";
            }
        }
        catch (Exception ex)
        {
            TxtIgdbStatus.Text = $"Error al buscar en {selectedSource}.";
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
            if (result.Year.HasValue)
                NumYear.Value = result.Year.Value;
            if (!string.IsNullOrEmpty(result.Genre))
                TxtGenre.Text = result.Genre;

            OverlayIgdbSearch.IsVisible = false;

            if (!string.IsNullOrEmpty(result.CoverUrl))
            {
                try
                {
                    ShowMessage("Descargando carátula...");
                    if (_currentScraperSource == "IGDB") _currentCover = await _igdbService.DownloadCoverAsync(result.CoverUrl);
                    else if (_currentScraperSource == "TheGamesDB") _currentCover = await _theGamesDbService.DownloadCoverAsync(result.CoverUrl);
                    else if (_currentScraperSource == "GameTDB") _currentCover = await _gameTdbService.DownloadCoverAsync(result.CoverUrl);
                    else if (_currentScraperSource == "PalSnesCovers") _currentCover = await _palSnesCoversService.DownloadCoverAsync(result.CoverUrl);
                    UpdateCoverImage();
                    OverlayMessage.IsVisible = false; // Ocultar mensaje al terminar
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

        var filteredList = filtered.ToList();

        int totalItems = filteredList.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (totalPages == 0) totalPages = 1;
        if (_currentPage > totalPages) _currentPage = totalPages;

        var paginated = filteredList.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();

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

            _currentCover = game.Cover;
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
        string helpText = "CONFIGURACIÓN DE EMULADORES:\n\n" +
            "1. Por Plataforma (Recomendado):\n" +
            "   Ve a '⚙️ Gestionar' (arriba a la derecha), selecciona una plataforma e indica su Emulador y Argumentos. " +
            "Todos los juegos de esa plataforma lo usarán por defecto.\n\n" +
            "2. Por Juego (Perfil Avanzado):\n" +
            "   Si un juego requiere un emulador distinto (ej. Snes9x en vez de RetroArch), edita el juego " +
            "y rellena la sección '⚙️ Perfil de Emulador' para sobrescribir la configuración global.\n\n" +
            "Argumentos: Usa {0} para indicar dónde va la ruta del juego.\n" +
            "Ejemplo RetroArch: -L cores/snes9x_libretro.dll \"{0}\"\n" +
            "Ejemplo Normal: \"{0}\"";
            
        ShowMessage(helpText);
    }

    private void MenuHelpMultiDisk_Click(object? sender, RoutedEventArgs e)
    {
        string helpText = "CÓMO FUNCIONA LA OPCIÓN MULTI-DISCO:\n\n" +
            "Si tienes un juego que viene en varios archivos (ej. Final Fantasy VII - Disco 1, Disco 2, etc.), " +
            "ahora puedes tenerlos agrupados en una sola entrada de tu colección.\n\n" +
            "1. En los detalles del juego, busca la lista 'Rutas de ROM (Multi-Disco)'.\n" +
            "2. Pulsa '+ Añadir Disco' para insertar todos los archivos que componen el juego.\n" +
            "3. Pulsa Guardar.\n\n" +
            "¿Cómo elegir el disco a jugar?\n" +
            "- Al momento de jugar, simplemente HAZ CLIC en el disco que desees dentro de esa lista " +
            "para seleccionarlo, y luego pulsa el gran botón verde '▶ JUGAR'.\n" +
            "- Si no seleccionas ninguno explícitamente, por defecto se cargará el primero de la lista.";
            
        ShowMessage(helpText);
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
}