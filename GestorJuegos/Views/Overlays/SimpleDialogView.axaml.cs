using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace GestorJuegos.Views.Overlays;

public partial class SimpleDialogView : UserControl
{
    public event EventHandler? Accepted;
    public event EventHandler? Cancelled;

    public SimpleDialogView()
    {
        InitializeComponent();
        BtnAccept.Click += (s, e) => { this.IsVisible = false; Accepted?.Invoke(this, EventArgs.Empty); };
        BtnCancel.Click += (s, e) => { this.IsVisible = false; Cancelled?.Invoke(this, EventArgs.Empty); };
    }

    public void ShowMessage(string content, string? title = null)
    {
        TxtTitle.Text = title;
        TxtTitle.IsVisible = !string.IsNullOrEmpty(title);
        TxtContent.Text = content;
        BtnCancel.IsVisible = false;
        BtnAccept.Content = "ACEPTAR";
        BtnAccept.Background = Avalonia.Media.Brush.Parse("#10b981");
        this.IsVisible = true;
    }

    public void ShowConfirm(string content, string title = "Confirmar Acción")
    {
        TxtTitle.Text = title;
        TxtTitle.IsVisible = true;
        TxtContent.Text = content;
        BtnCancel.IsVisible = true;
        BtnAccept.Content = "ACEPTAR";
        BtnAccept.Background = Avalonia.Media.Brush.Parse("#ef4444"); // Rojo para confirmaciones (borrado, etc)
        this.IsVisible = true;
    }
}
