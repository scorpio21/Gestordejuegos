using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace GestorJuegos.Views.Overlays;

public partial class ProgressOverlayView : UserControl
{
    public event EventHandler? CancelRequested;

    public ProgressOverlayView()
    {
        InitializeComponent();
        BtnCancel.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Update(string title, double progress, string detail)
    {
        TxtTitle.Text = title;
        ProgBar.Value = progress;
        TxtDetail.Text = detail;
        this.IsVisible = true;
    }

    public void Hide()
    {
        this.IsVisible = false;
    }
}
