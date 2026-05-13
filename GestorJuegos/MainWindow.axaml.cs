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

    public MainWindow()
    {
        InitializeComponent();
        _gameService = new GameService();
        LoadPlatforms();

        BtnAddGame.Click += BtnAddGame_Click;
        BtnSave.Click += BtnSave_Click;
        BtnDelete.Click += BtnDelete_Click;
        BtnSelectCover.Click += BtnSelectCover_Click;
        BtnClearCover.Click += BtnClearCover_Click;
        LstGames.SelectionChanged += LstGames_SelectionChanged;
    }

    private void LoadPlatforms()
    {
        var platforms = _gameService.GetPlatforms();
        MenuPlataformas.Items.Clear();

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

    private void LoadGames()
    {
        if (_selectedPlatform == null) return;
        var games = _gameService.GetGamesByPlatform(_selectedPlatform.Id);
        LstGames.ItemsSource = games;
    }

    private void LstGames_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstGames.SelectedItem is Game game)
        {
            _selectedGame = game;
            TxtName.Text = game.Name;
            NumYear.Value = game.Year;
            TxtGenre.Text = game.Genre;
            
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
        if (_selectedPlatform == null) return;

        _selectedGame = new Game { PlatformId = _selectedPlatform.Id, Year = DateTime.Now.Year };
        TxtName.Text = string.Empty;
        NumYear.Value = _selectedGame.Year;
        TxtGenre.Text = string.Empty;
        CmbRegion.SelectedIndex = 0;
        _currentCover = null;
        UpdateCoverImage();

        LstGames.SelectedItem = null;
        BtnDelete.IsVisible = false;
        PnlGameDetails.IsVisible = true;
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null) return;

        _selectedGame.Name = TxtName.Text ?? string.Empty;
        _selectedGame.Year = (int)(NumYear.Value ?? DateTime.Now.Year);
        _selectedGame.Genre = TxtGenre.Text ?? string.Empty;
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