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
        
        // Menú de estado de juego
        var playStatusItems = BtnProgressQuick.Flyout as MenuFlyout;
        if (playStatusItems != null)
        {
            foreach (var item in playStatusItems.Items.OfType<MenuItem>())
            {
                item.Click += (s, e) => RequestPlayStatusChange?.Invoke(this, item.Tag?.ToString() ?? "");
            }
        }

        // Más opciones
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
        
        // Fecha de lanzamiento
        string releaseDateStr = "--";
        if (!string.IsNullOrEmpty(game.ReleaseDate))
        {
            if (DateTime.TryParse(game.ReleaseDate, out DateTime releaseDateVal))
                releaseDateStr = releaseDateVal.ToString("dd/MM/yyyy");
            else
                releaseDateStr = game.ReleaseDate;
        }
        else if (game.Year > 0)
        {
            releaseDateStr = game.Year.ToString();
        }
        TxtBaseReleaseDate.Text = releaseDateStr;
        
        TxtBaseDeveloper.Text = game.Developer ?? "--";
        TxtBasePublisher.Text = game.Publisher ?? "--";
        TxtBasePlaytime.Text = $"{game.PlayCount} partidas ({game.PlayStatus})";
        
        TxtInfoRatingText.Text = game.Rating.ToString("F1");
        TxtInfoRatingStars.Text = GetStars(game.Rating);
        
        TxtInfoEsrb.Text = game.ESRB ?? "--";
        TxtInfoGenre.Text = game.Genre ?? "--";
        
        // Modo de Juego
        string playMode = "Un Jugador";
        if (game.Cooperative) playMode = "Cooperativo";
        else if (game.MaxPlayers.HasValue && game.MaxPlayers.Value > 1) playMode = $"Multijugador ({game.MaxPlayers.Value} jugadores)";
        TxtInfoPlayMode.Text = playMode;
        
        TxtInfoProgress.Text = game.PlayStatus;
        TxtInfoRegion.Text = game.Region ?? "--";
        TxtInfoStatus.Text = !string.IsNullOrEmpty(game.RomPath) ? "ROM importado" : "No instalado";
        TxtInfoPortable.Text = "No"; 
        TxtInfoFile.Text = System.IO.Path.GetFileName(game.RomPath);
        TxtInfoLastPlayed.Text = game.LastPlayed?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
        TxtInfoReleaseDate.Text = TxtBaseReleaseDate.Text;
        TxtInfoReleaseType.Text = game.ReleaseType ?? "--";
        TxtInfoMaxPlayers.Text = game.MaxPlayers.HasValue ? game.MaxPlayers.ToString() : "--";
        
        TxtInfoDescription.Text = game.Description ?? "Sin descripción disponible.";
        
        // Playtime commitment
        TxtPlaytimeMain.Text = game.PlaytimeMain ?? "--";
        TxtPlaytimeExtra.Text = game.PlaytimeExtra ?? "--";
        TxtPlaytimeCompletionist.Text = game.PlaytimeCompletionist ?? "--";

        UpdateFavoriteIcon(game.IsFavorite);
        LoadGameAchievements(game);
        
        // Cargar capturas si existen
        var images = gameService.GetGameExtraImages(game.Id);
        LstScreenshots.ItemsSource = images;
        
        // Cargar juegos relacionados (misma plataforma)
        var related = gameService.GetGamesByPlatform(game.PlatformId).Where(g => g.Id != game.Id).Take(10).ToList();
        LstRelatedGames.ItemsSource = related;
        TxtNoRelatedGames.IsVisible = !related.Any();
    }

    private void LoadGameAchievements(Game game)
    {
        StackAchievementIcons.Children.Clear();

        if (game.Achievements == null || game.Achievements.Count == 0)
        {
            game.Achievements = new List<Achievement>
            {
                new Achievement { Title = "Héroe de Agrabah", Description = "Escapa de los guardias en el nivel 1", IsUnlocked = true },
                new Achievement { Title = "Cueva de las Maravillas", Description = "Entra en la cueva sin ser detectado", IsUnlocked = true },
                new Achievement { Title = "El Genio", Description = "Frota la lámpara por primera vez", IsUnlocked = true },
                new Achievement { Title = "Príncipe Ali", Description = "Entra en el palacio disfrazado", IsUnlocked = false },
                new Achievement { Title = "Jafar el Terrible", Description = "Derrota a Jafar en su forma de serpiente", IsUnlocked = false }
            };
        }

        int unlocked = game.Achievements.Count(a => a.IsUnlocked);
        int total = game.Achievements.Count;

        TxtAchievementsTitle.Text = $"RETRO ACHIEVEMENTS ({unlocked}/{total})";
        ProgAchievements.Value = total > 0 ? (unlocked * 100) / total : 0;

        foreach (var achievement in game.Achievements.Take(4))
        {
            var border = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Background = Avalonia.Media.Brush.Parse("#1e1e22")
            };

            var img = new Image 
            { 
                Stretch = Avalonia.Media.Stretch.UniformToFill,
                Opacity = achievement.IsUnlocked ? 1.0 : 0.3
            };
            
            border.Child = img;
            StackAchievementIcons.Children.Add(border);
        }

        if (total > 4)
        {
            var moreBorder = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(6),
                BorderBrush = Avalonia.Media.Brush.Parse("#475569"),
                BorderThickness = new Thickness(1),
                Background = Avalonia.Media.Brush.Parse("#161618")
            };
            moreBorder.Child = new TextBlock 
            { 
                Text = $"+ {total - 4}", 
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontSize = 11,
                Foreground = Avalonia.Media.Brush.Parse("#94a3b8"),
                FontWeight = Avalonia.Media.FontWeight.Bold
            };
            StackAchievementIcons.Children.Add(moreBorder);
        }
    }

    private string GetStars(double rating)
    {
        int stars = (int)Math.Round(rating / 2.0);
        return new string('★', stars) + new string('☆', 5 - stars);
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
        BrdDetailLogo.IsVisible = bitmap != null;
    }
}