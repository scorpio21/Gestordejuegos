using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GestorJuegos;

public partial class MainWindow : Window
{
    private readonly GameService _gameService;
    private Platform? _selectedPlatform;
    private Game? _selectedGame;
    private byte[]? _currentCover;
    private readonly IgdbService _igdbService;
    private System.Collections.Generic.List<Game> _currentPlatformGames = new System.Collections.Generic.List<Game>();

    public MainWindow()
    {
        InitializeComponent();
        
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            Title = $"Gestor de Juegos v{version.Major}.{version.Minor}.{version.Build}";
        }

        _gameService = new GameService();
        string clientId = "";
        string clientSecret = "";
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var igdbConfig = doc.RootElement.GetProperty("IGDB");
                clientId = igdbConfig.GetProperty("ClientId").GetString() ?? "";
                clientSecret = igdbConfig.GetProperty("ClientSecret").GetString() ?? "";
            }
        }
        catch { }

        _igdbService = new IgdbService(clientId, clientSecret);
        LoadPlatforms();

        BtnAddGame.Click += BtnAddGame_Click;
        BtnSave.Click += BtnSave_Click;
        BtnDelete.Click += BtnDelete_Click;
        BtnSelectCover.Click += BtnSelectCover_Click;
        BtnClearCover.Click += BtnClearCover_Click;
        
        LstGames.SelectionChanged += LstGames_SelectionChanged;
        LstGamesGrid.SelectionChanged += LstGames_SelectionChanged;
        
        BtnViewList.Click += BtnViewList_Click;
        BtnViewGrid.Click += BtnViewGrid_Click;

        BtnAddPlatform.Click += BtnAddPlatform_Click;
        BtnCancelPlatform.Click += BtnCancelPlatform_Click;
        BtnSavePlatform.Click += BtnSavePlatform_Click;
        
        BtnManagePlatforms.Click += BtnManagePlatforms_Click;
        BtnCloseManagePlatforms.Click += BtnCloseManagePlatforms_Click;
        BtnSaveEditPlatform.Click += BtnSaveEditPlatform_Click;
        BtnDeletePlatform.Click += BtnDeletePlatform_Click;
        LstManagePlatforms.SelectionChanged += LstManagePlatforms_SelectionChanged;
        BtnSelectEmulator.Click += BtnSelectEmulator_Click;

        BtnStatistics.Click += BtnStatistics_Click;
        BtnCloseStatistics.Click += BtnCloseStatistics_Click;

        MenuExportDB.Click += MenuExportDB_Click;
        MenuImportDB.Click += MenuImportDB_Click;
        MenuImportDat.Click += MenuImportDat_Click;
        MenuBatchScrape.Click += MenuBatchScrape_Click;

        BtnCloseMessage.Click += BtnCloseMessage_Click;
        
        BtnSearchIgdb.Click += BtnSearchIgdb_Click;
        BtnCancelIgdb.Click += BtnCancelIgdb_Click;
        BtnSelectIgdb.Click += BtnSelectIgdb_Click;

        BtnSelectRom.Click += BtnSelectRom_Click;
        BtnLaunchGame.Click += BtnLaunchGame_Click;
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
        BtnStatistics.IsEnabled = hasPlatforms;
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

    private void BtnStatistics_Click(object? sender, RoutedEventArgs e)
    {
        var totalGames = _gameService.GetTotalGamesCount();
        TxtTotalGames.Text = totalGames.ToString();

        var stats = _gameService.GetGamesCountByPlatform();
        LstPlatformStats.ItemsSource = stats;

        OverlayStatistics.IsVisible = true;
    }

    private void BtnCloseStatistics_Click(object? sender, RoutedEventArgs e)
    {
        OverlayStatistics.IsVisible = false;
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

    private async void MenuBatchScrape_Click(object? sender, RoutedEventArgs e)
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
                    var results = await _igdbService.SearchGamesAsync(game.Name);
                    var match = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.CoverUrl));
                    
                    if (match != null)
                    {
                        var coverData = await _igdbService.DownloadCoverAsync(match.CoverUrl);
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
                    
                    foreach (var gameNode in games)
                    {
                        string name = gameNode.Attribute("name")?.Value ?? "";
                        if (string.IsNullOrEmpty(name)) continue;

                        string region = "🌎 World";
                        if (name.Contains("(JP") || name.Contains("(Japan")) region = "🇯🇵 JP";
                        else if (name.Contains("(US") || name.Contains("(USA")) region = "🇺🇸 US";
                        else if (name.Contains("(EU") || name.Contains("(Europe")) region = "🇪🇺 EU";

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

                        newGames.Add(new Game
                        {
                            PlatformId = _selectedPlatform.Id,
                            Name = cleanName,
                            Region = region,
                            Year = DateTime.Now.Year
                        });
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
                        ShowMessage($"¡Importación completada! Se añadieron {count} juegos.");
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

        OverlayIgdbSearch.IsVisible = true;
        TxtIgdbStatus.Text = $"Buscando '{query}'...";
        LstIgdbResults.ItemsSource = null;

        try
        {
            var results = await _igdbService.SearchGamesAsync(query);
            LstIgdbResults.ItemsSource = results;
            if (results.Count == 0)
            {
                TxtIgdbStatus.Text = "No se encontraron resultados.";
            }
            else
            {
                TxtIgdbStatus.Text = "Resultados de Búsqueda (IGDB)";
            }
        }
        catch (Exception ex)
        {
            TxtIgdbStatus.Text = "Error al buscar en IGDB.";
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
                    _currentCover = await _igdbService.DownloadCoverAsync(result.CoverUrl);
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

        _currentPlatformGames = _gameService.GetGamesByPlatform(_selectedPlatform.Id);
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        if (_currentPlatformGames == null) return;
        
        var query = TxtSearchGame?.Text?.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrEmpty(query) 
            ? _currentPlatformGames 
            : _currentPlatformGames.Where(g => g.Name.ToLower().Contains(query) || (g.Genre != null && g.Genre.ToLower().Contains(query))).ToList();

        LstGames.ItemsSource = filtered;
        LstGamesGrid.ItemsSource = filtered;
        
        if (_selectedPlatform != null)
        {
            TxtSelectedPlatform.Text = $"{_selectedPlatform.Name} ({filtered.Count} juegos)";
        }
    }

    private void TxtSearchGame_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplySearchFilter();
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
            TxtRomPath.Text = game.RomPath;
            
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
        TxtRomPath.Text = string.Empty;
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
        _selectedGame.RomPath = TxtRomPath.Text ?? string.Empty;
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

    private async void BtnSelectRom_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Archivo de Juego / ROM",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            TxtRomPath.Text = files[0].Path.LocalPath;
        }
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

            if (string.IsNullOrEmpty(platform.EmulatorPath))
            {
                logLines.Add("Aviso: EmulatorPath vacío. Usando UseShellExecute = true con RomPath.");
                psi.FileName = _selectedGame.RomPath;
                psi.UseShellExecute = true;
            }
            else
            {
                logLines.Add("EmulatorPath configurado. Verificando...");
                if (!File.Exists(platform.EmulatorPath))
                {
                    logLines.Add("Error: EmulatorPath no existe en disco.");
                    ShowMessage("La ruta del emulador especificada en la plataforma no existe.");
                    File.WriteAllLines("launcher_log.txt", logLines);
                    return;
                }

                logLines.Add("EmulatorPath OK.");
                psi.FileName = platform.EmulatorPath;
                psi.WorkingDirectory = System.IO.Path.GetDirectoryName(platform.EmulatorPath) ?? string.Empty;
                
                string args = string.IsNullOrEmpty(platform.LaunchArguments) ? "\"{0}\"" : platform.LaunchArguments;
                logLines.Add($"Args base: {args}");
                psi.Arguments = args.Replace("{0}", _selectedGame.RomPath);
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