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

        // Playtime commitment
        TxtPlaytimeMain.Text = game.PlaytimeMain ?? "--";
        TxtPlaytimeCompletionist.Text = game.PlaytimeCompletionist ?? "--";

        UpdateFavoriteIcon(game.IsFavorite);
        LoadGameAchievements(game, gameService);
        
        LstScreenshots.ItemsSource = gameService.GetGameExtraImages(game.Id);
        LstRelatedGames.ItemsSource = gameService.GetGamesByPlatform(game.PlatformId).Where(g => g.Id != game.Id).Take(10).ToList();
    }

    private readonly RetroAchievementsService _raService = new();

    private async void LoadGameAchievements(Game game, GameService gameService)
    {
        StackAchievementIcons.Children.Clear();

        // 1. Mostrar estado inicial o cargado de logros si ya están en memoria
        bool hasAchievementsInMemory = game.Achievements != null && game.Achievements.Any();
        if (hasAchievementsInMemory)
        {
            DisplayAchievements(game.Achievements!);
        }

        // 2. Si hay ID de RetroAchievements, intentar cargar datos en caliente
        if (int.TryParse(game.ExternalDbId, out int raId))
        {
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null && settings.EnableRetroAchievements)
                    {
                        _raService.Initialize(settings.RetroUsername, settings.RetroApiKey);

                        // Si no están en memoria, descargar los logros
                        if (!hasAchievementsInMemory)
                        {
                            var achievements = await _raService.GetGameAchievements(raId);
                            if (achievements.Any())
                            {
                                game.Achievements = achievements;
                                DisplayAchievements(achievements);
                            }
                            else
                            {
                                TxtAchievementsTitle.Text = "RETRO ACHIEVEMENTS (0/0)";
                                ProgAchievements.Value = 0;
                            }
                        }

                        // Descargar progresión de tiempos de juego y número de jugadores
                        var progression = await _raService.GetGameProgression(raId);
                        if (progression != null)
                        {
                            game.PlaytimeMain = FormatDuration(progression.MedianTimeToBeat);
                            game.PlaytimeCompletionist = FormatDuration(progression.MedianTimeToMaster);

                            TxtPlaytimeMain.Text = game.PlaytimeMain;
                            TxtPlaytimeCompletionist.Text = game.PlaytimeCompletionist;
                            TxtPlaytimeProvider.Text = $"Provided by RetroAchievements ({progression.NumDistinctPlayers} players)";

                            // Guardar metadatos actualizados en la base de datos local
                            gameService.UpdateGameMetadata(game);
                        }
                        return;
                    }
                }
                catch { }
            }
        }

        // Si no hay vinculación válida o falla
        if (!hasAchievementsInMemory)
        {
            TxtAchievementsTitle.Text = "RETRO ACHIEVEMENTS (SIN VINCULAR)";
            ProgAchievements.Value = 0;
        }
        TxtPlaytimeMain.Text = game.PlaytimeMain ?? "--";
        TxtPlaytimeCompletionist.Text = game.PlaytimeCompletionist ?? "--";
        TxtPlaytimeProvider.Text = "Provided by RetroAchievements";
    }

    private string FormatDuration(double seconds)
    {
        if (seconds <= 0) return "--";
        var time = TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1)
        {
            int hours = (int)time.TotalHours;
            int minutes = time.Minutes;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }
        else
        {
            int minutes = time.Minutes;
            return minutes > 0 ? $"{minutes}m" : "--";
        }
    }

    private void DisplayAchievements(List<Achievement> achievements)
    {
        int unlockedCount = achievements.Count(a => a.IsUnlocked);
        int total = achievements.Count;
        TxtAchievementsTitle.Text = $"RETRO ACHIEVEMENTS ({unlockedCount}/{total})";
        ProgAchievements.Value = total > 0 ? (unlockedCount * 100) / total : 0;

        // Limpiar iconos anteriores
        StackAchievementIcons.Children.Clear();

        if (total == 0)
        {
            var txtEmpty = new TextBlock { Text = "No hay logros disponibles", Foreground = Avalonia.Media.Brush.Parse("#475569"), FontSize = 11, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            StackAchievementIcons.Children.Add(txtEmpty);
            return;
        }

        // Ordenar logros: desbloqueados primero (ordenados por fecha de desbloqueo más reciente), luego bloqueados
        var orderedAchievements = achievements
            .OrderByDescending(a => a.IsUnlocked)
            .ThenByDescending(a => a.UnlockDate ?? DateTime.MinValue)
            .ToList();

        // Mostrar hasta 4 miniaturas
        foreach (var achievement in orderedAchievements.Take(4))
        {
            var border = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(6), ClipToBounds = true, Background = Avalonia.Media.Brush.Parse("#1e1e22") };
            
            // Si el logro no está desbloqueado, aplicar opacidad
            var img = new Image { Stretch = Avalonia.Media.Stretch.UniformToFill, Opacity = achievement.IsUnlocked ? 1.0 : 0.3 };
            
            if (!string.IsNullOrEmpty(achievement.IconUrl))
            {
                Task.Run(async () => {
                    try {
                        using var client = new System.Net.Http.HttpClient();
                        var data = await client.GetByteArrayAsync(achievement.IconUrl);
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                            try { img.Source = new Avalonia.Media.Imaging.Bitmap(new System.IO.MemoryStream(data)); } catch { }
                        });
                    } catch { }
                });
            }

            // Crear el ToolTip del logro (título + descripción + estado)
            var tooltipStack = new StackPanel { Spacing = 4, MaxWidth = 220 };
            tooltipStack.Children.Add(new TextBlock { Text = achievement.Title, FontWeight = Avalonia.Media.FontWeight.Bold, Foreground = Avalonia.Media.Brush.Parse("White"), FontSize = 12 });
            tooltipStack.Children.Add(new TextBlock { Text = achievement.Description, Foreground = Avalonia.Media.Brush.Parse("#cbd5e1"), FontSize = 11, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            
            if (achievement.IsUnlocked && achievement.UnlockDate.HasValue)
            {
                tooltipStack.Children.Add(new TextBlock { Text = $"✓ Desbloqueado: {achievement.UnlockDate.Value.ToString("dd/MM/yyyy HH:mm")}", Foreground = Avalonia.Media.Brush.Parse("#10b981"), FontSize = 10 });
            }
            else
            {
                tooltipStack.Children.Add(new TextBlock { Text = "🔒 Bloqueado", Foreground = Avalonia.Media.Brush.Parse("#ef4444"), FontSize = 10 });
            }
            
            ToolTip.SetTip(border, tooltipStack);

            border.Child = img;
            StackAchievementIcons.Children.Add(border);
        }

        // Si hay más de 4 logros, añadir el badge "+ X"
        if (total > 4)
        {
            var borderText = new Border {
                Width = 36, Height = 36, CornerRadius = new CornerRadius(6),
                Background = Avalonia.Media.Brush.Parse("#1e1e22"),
                BorderBrush = Avalonia.Media.Brush.Parse("#475569"), BorderThickness = new Thickness(1),
                Padding = new Thickness(2)
            };
            var textBlock = new TextBlock {
                Text = $"+ {total - 4}", Foreground = Avalonia.Media.Brush.Parse("White"),
                FontSize = 11, FontWeight = Avalonia.Media.FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            borderText.Child = textBlock;
            StackAchievementIcons.Children.Add(borderText);
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
        ImgDetailBackground.Source = bitmap;
    }
    
    public void SetLogo(Avalonia.Media.Imaging.Bitmap? bitmap)
    {
        ImgDetailLogo.Source = bitmap;
        bool hasLogo = bitmap != null;
        BrdDetailLogo.IsVisible = hasLogo;
        BtnViewAllImages.IsVisible = !hasLogo;
    }

    private void OnWikiSearchClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_game != null)
        {
            RequestWikiSearch?.Invoke(this, _game.Name ?? "");
        }
    }
}
