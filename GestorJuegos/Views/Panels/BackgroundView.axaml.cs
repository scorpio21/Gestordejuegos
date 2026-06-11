using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using GestorJuegos.Models;
using GestorJuegos.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GestorJuegos.Views.Panels;

public partial class BackgroundView : UserControl
{
    private int? _lastGameId;

    public BackgroundView()
    {
        InitializeComponent();
    }

    public void UpdateBackground(Game? game, GameService gameService)
    {
        if (game == null)
        {
            _lastGameId = null;
            ImgBackground.Source = null;
            return;
        }

        if (_lastGameId == game.Id) return;
        _lastGameId = game.Id;

        Task.Run(() => {
            string[] types = { "Fanart - Background", "Screenshot - Gameplay", "Background" };
            byte[]? bgData = null;
            foreach (var type in types) {
                bgData = gameService.GetGameExtraImage(game.Id, type);
                if (bgData != null && bgData.Length > 0) break;
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (_lastGameId == game.Id) {
                    if (bgData != null) {
                        try {
                            using var ms = new MemoryStream(bgData);
                            var bitmap = new Bitmap(ms);
                            ImgBackground.Source = bitmap;
                            ImgBackground.Opacity = 0.3;
                        } catch { ImgBackground.Source = null; }
                    } else ImgBackground.Source = null;
                }
            });
        });
    }

    public void SetThemeBackground(Bitmap? bitmap, Bitmap? overlay = null)
    {
        ImgThemeBackground.Source = bitmap;
        ImgThemeBackground.IsVisible = bitmap != null;
        
        ImgThemeOverlay.Source = overlay;
        ImgThemeOverlay.IsVisible = overlay != null;
    }
}
