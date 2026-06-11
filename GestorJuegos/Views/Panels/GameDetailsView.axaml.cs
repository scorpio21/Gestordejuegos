using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GestorJuegos.Models;
using GestorJuegos.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestorJuegos.Views.Panels;

public partial class GameDetailsView : UserControl
{
    public event EventHandler<Game>? RequestLaunch;
    public event EventHandler<Game>? RequestEdit;
    public event EventHandler<Game>? RequestDelete;
    public event EventHandler<Game>? RequestToggleFavorite;
    public event EventHandler<string>? RequestOpenFolder;
    public event EventHandler? RequestShowFullArt;
    public event EventHandler? RequestViewAllImages;
    public event EventHandler? RequestExpandAchievements;
    public event EventHandler<string>? RequestPlayStatusChange;
    public event EventHandler<string>? RequestWikiSearch;
    public event EventHandler<int>? RequestRatingChange;

    private Game? _game;

    public GameDetailsView()
    {
        InitializeComponent();
        SetupEvents();
    }

    private void SetupEvents()
    {
        BtnLaunchGame.Click += (s, e) => { if (_game != null) RequestLaunch?.Invoke(this, _game); };
        BtnEditGame.Click += (s, e) => { if (_game != null) RequestEdit?.Invoke(this, _game); };
        BtnFavoriteQuick.Click += (s, e) => { if (_game != null) RequestToggleFavorite?.Invoke(this, _game); };
        BtnViewFullArt.Click += (s, e) => RequestShowFullArt?.Invoke(this, EventArgs.Empty);
        BtnViewAllImages.Click += (s, e) => RequestViewAllImages?.Invoke(this, EventArgs.Empty);
        BtnExpandAchievements.Click += (s, e) => RequestExpandAchievements?.Invoke(this, EventArgs.Empty);
        BtnWikiSearch.Click += (s, e) => RequestWikiSearch?.Invoke(this, _game?.Name ?? "");
        
        var playStatusItems = BtnProgressQuick.Flyout as MenuFlyout;
        if (playStatusItems != null)
        {
            foreach (var item in playStatusItems.Items.OfType<MenuItem>())
            {
                item.Click += (s, e) => RequestPlayStatusChange?.Invoke(this, item.Tag?.ToString() ?? "");
            }
        }

        var moreOptionsItems = BtnMoreOptions.Flyout as MenuFlyout;
        if (moreOptionsItems != null)
        {
            var openFolderItem = moreOptionsItems.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header?.ToString() == "Abrir Carpeta");
            if (openFolderItem != null) openFolderItem.Click += (s, e) => { if (_game != null) RequestOpenFolder?.Invoke(this, _game.RomPath); };

            var deleteItem = moreOptionsItems.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header?.ToString() == "Borrar");
            if (deleteItem != null) deleteItem.Click += (s, e) => { if (_game != null) RequestDelete?.Invoke(this, _game); };
        }
    }

    public void UpdateDetails(Game game, GameService gameService)
    {
        _game = game;
        TxtInfoName.Text = game.Name;
        TxtInfoPlatform.Text = game.Platform?.Name ?? "DESCONOCIDA";
        
        TxtBaseReleaseDate.Text = !string.IsNullOrEmpty(game.ReleaseDate) ? game.ReleaseDate : (game.Year > 0 ? game.Year.ToString() : "--");
        TxtBaseDeveloper.Text = game.Developer ?? "--";
        TxtBasePublisher.Text = game.Publisher ?? "--";
        TxtBasePlaytime.Text = $"{game.PlayCount} partidas ({game.PlayStatus})";
        
        TxtInfoRatingText.Text = (game.Rating / 20.0).ToString("F1");
        UpdateStarsDisplay(game.Rating);
        
        TxtInfoEsrb.Text = game.ESRB ?? "--";
        TxtInfoGenre.Text = game.Genre ?? "--";
        TxtInfoProgress.Text = game.PlayStatus;
        TxtInfoRegion.Text = game.Region ?? "--";
        TxtInfoFile.Text = System.IO.Path.GetFileName(game.RomPath);
        TxtInfoDescription.Text = game.Description ?? "Sin descripción disponible.";

        UpdateFavoriteIcon(game.IsFavorite);
        LoadGameAchievements(game);
        
        LstScreenshots.ItemsSource = gameService.GetGameExtraImages(game.Id);
        LstRelatedGames.ItemsSource = gameService.GetGamesByPlatform(game.PlatformId).Where(g => g.Id != game.Id).Take(10).ToList();
    }

    private void LoadGameAchievements(Game game)
    {
        StackAchievementIcons.Children.Clear();
        if (game.Achievements == null) return;

        int unlocked = game.Achievements.Count(a => a.IsUnlocked);
        int total = game.Achievements.Count;
        TxtAchievementsTitle.Text = $"RETRO ACHIEVEMENTS ({unlocked}/{total})";
        ProgAchievements.Value = total > 0 ? (unlocked * 100) / total : 0;

        foreach (var achievement in game.Achievements.Take(4))
        {
            var border = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(6), ClipToBounds = true, Background = Avalonia.Media.Brush.Parse("#1e1e22") };
            border.Child = new Image { Stretch = Avalonia.Media.Stretch.UniformToFill, Opacity = achievement.IsUnlocked ? 1.0 : 0.3 };
            StackAchievementIcons.Children.Add(border);
        }
    }

    private void UpdateStarsDisplay(int rating)
    {
        var stars = new List<StarItem>();
        int activeStars = (int)Math.Round(rating / 20.0);
        for (int i = 1; i <= 5; i++)
        {
            bool isActive = i <= activeStars;
            stars.Add(new StarItem 
            { 
                Value = i, 
                StarChar = isActive ? "★" : "☆", 
                Color = isActive ? "#38bdf8" : "#475569" // Azul si activa, Gris si no
            });
        }
        ItemsRatingStars.ItemsSource = stars;
    }

    private void OnStarClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StarItem star && _game != null)
        {
            int newRating = star.Value * 20;
            _game.Rating = newRating;
            TxtInfoRatingText.Text = star.Value.ToString("F1");
            UpdateStarsDisplay(newRating);
            RequestRatingChange?.Invoke(this, newRating);
        }
    }

    public void UpdateFavoriteIcon(bool isFavorite)
    {
        TxtFavoriteIcon.Text = isFavorite ? "★" : "♡";
    }

    public void SetCover(Avalonia.Media.Imaging.Bitmap? bitmap)
    {
        ImgCover.Source = bitmap;
        ImgDetailBackground.Source = bitmap;
    }
    
    public void SetLogo(Avalonia.Media.Imaging.Bitmap? bitmap)
    {
        ImgDetailLogo.Source = bitmap;
        bool hasLogo = bitmap != null;
        BrdDetailLogo.IsVisible = hasLogo;
        BtnViewAllImages.IsVisible = !hasLogo;
    }
}
