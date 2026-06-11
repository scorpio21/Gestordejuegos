using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace GestorJuegos.Views.Overlays;

public partial class DeleteConfirmView : UserControl
{
    public event EventHandler<bool>? Confirmed;
    public event EventHandler? Cancelled;

    public DeleteConfirmView()
    {
        InitializeComponent();
        BtnYes.Click += (s, e) => { this.IsVisible = false; Confirmed?.Invoke(this, (ChkDeleteMedia.IsChecked == true)); };
        BtnNo.Click += (s, e) => { this.IsVisible = false; Cancelled?.Invoke(this, EventArgs.Empty); };
        BtnClose.Click += (s, e) => { this.IsVisible = false; Cancelled?.Invoke(this, EventArgs.Empty); };
    }

    public void Show(string message, bool showMediaOption = true)
    {
        TxtMessage.Text = message;
        ChkDeleteMedia.IsVisible = showMediaOption;
        this.IsVisible = true;
    }
}
