using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Data;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace GestorJuegos.Views.Panels;

public partial class DashboardView : UserControl
{
    public event EventHandler<Platform>? RequestEditPlatform;
    public event EventHandler? RequestClose;

    private Platform? _currentPlatform;

    public DashboardView()
    {
        InitializeComponent();
        SetupEvents();
    }

    private void SetupEvents()
    {
        BtnEditPlatformQuick.Click += (s, e) => { if (_currentPlatform != null) RequestEditPlatform?.Invoke(this, _currentPlatform); };
        BtnClosePlatformDetails.Click += (s, e) => RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public void ShowNoGameSelected()
    {
        PnlPlatformDetails.IsVisible = false;
        PnlNoGameSelected.IsVisible = true;
    }

    public void UpdatePlatform(Platform platform)
    {
        _currentPlatform = platform;
        PnlNoGameSelected.IsVisible = false;
        PnlPlatformDetails.IsVisible = true;

        // Reset visibility for platform specific rows
        RowPlatDeveloper.IsVisible = true;
        RowPlatManufacturer.IsVisible = true;
        RowPlatCpu.IsVisible = true;
        RowPlatMemory.IsVisible = true;
        RowPlatGraphics.IsVisible = true;
        RowPlatSound.IsVisible = true;
        RowPlatDisplay.IsVisible = true;
        RowPlatMedia.IsVisible = true;

        // 1. Logo y Background
        if (platform.Logo != null && platform.Logo.Length > 0)
        {
            try { ImgPlatformLogo.Source = new Bitmap(new MemoryStream(platform.Logo)); } catch { ImgPlatformLogo.Source = null; }
        }
        else ImgPlatformLogo.Source = null;

        ImgPlatformBackground.Source = null; // No hay banner en el modelo actual, se podría usar un Fanart si existiera

        // 2. Fecha de Estreno
        if (!string.IsNullOrEmpty(platform.ReleaseDate))
        {
            if (DateTime.TryParse(platform.ReleaseDate, out var dt))
                TxtPlatformReleaseDate.Text = $"Estrenado en {dt:dd/MM/yyyy}";
            else
                TxtPlatformReleaseDate.Text = $"Estrenado en {platform.ReleaseDate}";
            TxtPlatformReleaseDate.IsVisible = true;
        }
        else TxtPlatformReleaseDate.IsVisible = false;

        // 3. Foto de Consola
        if (platform.HardwareImage != null && platform.HardwareImage.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(platform.HardwareImage);
                ImgPlatformHardware.Source = new Bitmap(ms);
                ImgPlatformHardware.IsVisible = true;
                BrdPlatformHardware.IsVisible = true;
                TxtPlatformHardwarePlaceholder.IsVisible = false;
            }
            catch 
            { 
                ImgPlatformHardware.IsVisible = false;
                BrdPlatformHardware.IsVisible = true;
                TxtPlatformHardwarePlaceholder.IsVisible = true; 
            }
        }
        else
        {
            ImgPlatformHardware.IsVisible = false;
            BrdPlatformHardware.IsVisible = true;
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
        UpdateStats(platform.Id);

        // 6. Descripción
        TxtPlatDescription.Text = string.IsNullOrEmpty(platform.Notes) ? "Sin descripción histórica disponible." : platform.Notes;
    }

    public void UpdateCategory(string categoryName, GameService gameService)
    {
        _currentPlatform = null;
        PnlNoGameSelected.IsVisible = false;
        PnlPlatformDetails.IsVisible = true;

        RowPlatDeveloper.IsVisible = false;
        RowPlatManufacturer.IsVisible = false;
        RowPlatCpu.IsVisible = false;
        RowPlatMemory.IsVisible = false;
        RowPlatGraphics.IsVisible = false;
        RowPlatSound.IsVisible = false;
        RowPlatDisplay.IsVisible = false;
        RowPlatMedia.IsVisible = false;

        TxtPlatformReleaseDate.IsVisible = false;
        ImgPlatformBackground.Source = null;
        ImgPlatformHardware.IsVisible = false;
        BrdPlatformHardware.IsVisible = false;

        // Buscar categoría en la base de datos para el logo y descripción
        using var context = new AppDbContext();
        var category = context.PlatformCategories.FirstOrDefault(c => c.Name == categoryName);
        if (category != null)
        {
            if (category.Graphic != null)
                try { ImgPlatformLogo.Source = new Bitmap(new MemoryStream(category.Graphic)); } catch { ImgPlatformLogo.Source = null; }
            else ImgPlatformLogo.Source = null;

            TxtPlatDescription.Text = string.IsNullOrEmpty(category.Notes) ? $"Colección de juegos de {categoryName}" : category.Notes;
        }
        else
        {
            ImgPlatformLogo.Source = null;
            TxtPlatDescription.Text = $"Colección de juegos de {categoryName}";
        }

        // Estadísticas de la categoría
        var games = context.Games.Include(g => g.Platform).ToList();
        var filteredGames = games.Where(g => (g.Platform != null && g.Platform.Category == categoryName) || (categoryName == "Favoritos" && g.IsFavorite)).ToList();
        UpdateStatsFromList(filteredGames);
    }

    private void UpdateStats(int platformId)
    {
        using var context = new AppDbContext();
        var games = context.Games.Where(g => g.PlatformId == platformId).ToList();
        UpdateStatsFromList(games);
    }

    private void UpdateStatsFromList(System.Collections.Generic.List<Game> games)
    {
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
            var lastPlayedGame = playedGames.OrderByDescending(g => g.LastPlayed).FirstOrDefault();
            TxtPlatLastPlayedGame.Text = lastPlayedGame?.Name ?? "--";
        }
        else
        {
            TxtPlatLastPlayed.Text = "Nunca";
            TxtPlatLastPlayedGame.Text = "--";
        }

        var mostPlayedGame = games.OrderByDescending(g => g.PlayCount).FirstOrDefault();
        TxtPlatMostPlayedGame.Text = (mostPlayedGame != null && mostPlayedGame.PlayCount > 0) ? mostPlayedGame.Name : "--";
    }
}
