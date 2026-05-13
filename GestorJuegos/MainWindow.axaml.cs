using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using System;
using System.IO;
using System.Linq;

namespace GestorJuegos;

public partial class MainWindow : Window
{
    private readonly GameService _gameService;
    private Platform? _selectedPlatform;
    private Game? _selectedGame;
    private byte[]? _currentCover;
    private readonly IgdbService _igdbService;

    public MainWindow()
    {
        InitializeComponent();
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

        BtnStatistics.Click += BtnStatistics_Click;
        BtnCloseStatistics.Click += BtnCloseStatistics_Click;

        MenuExportDB.Click += MenuExportDB_Click;
        MenuImportDB.Click += MenuImportDB_Click;

        BtnCloseMessage.Click += BtnCloseMessage_Click;
        
        BtnSearchIgdb.Click += BtnSearchIgdb_Click;
        BtnCancelIgdb.Click += BtnCancelIgdb_Click;
        BtnSelectIgdb.Click += BtnSelectIgdb_Click;
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
        if (_selectedPlatform == null) return;
        var games = _gameService.GetGamesByPlatform(_selectedPlatform.Id);
        LstGames.ItemsSource = games;
        LstGamesGrid.ItemsSource = games;
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
}