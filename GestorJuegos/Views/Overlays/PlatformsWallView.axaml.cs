using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using GestorJuegos.Models;

namespace GestorJuegos.Views.Overlays;

public partial class PlatformsWallView : UserControl
{
    public event EventHandler? RequestClose;
    public event EventHandler<Platform>? PlatformSelected;

    public PlatformsWallView()
    {
        InitializeComponent();
        BtnClose.Click += (s, e) => RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public void Initialize(IEnumerable<Platform> platforms)
    {
        LstPlatforms.ItemsSource = platforms;
        this.IsVisible = true;
    }

    private void OnPlatformClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Platform platform)
        {
            PlatformSelected?.Invoke(this, platform);
            this.IsVisible = false;
        }
    }
}
