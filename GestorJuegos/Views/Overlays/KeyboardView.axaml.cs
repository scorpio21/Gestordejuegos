using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace GestorJuegos.Views.Overlays;

public partial class KeyboardView : UserControl
{
    public event EventHandler<string>? TextSubmitted;
    public event EventHandler? RequestClose;

    private readonly string[] _keys = {
        "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
        "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
        "A", "S", "D", "F", "G", "H", "J", "K", "L", "Ñ",
        "Z", "X", "C", "V", "B", "N", "M", ",", ".", "⌫"
    };

    public KeyboardView()
    {
        InitializeComponent();
        SetupKeyboard();
        BtnClose.Click += (s, e) => RequestClose?.Invoke(this, EventArgs.Empty);
        BtnClear.Click += (s, e) => TxtInput.Text = "";
        BtnSpace.Click += (s, e) => TxtInput.Text += " ";
        BtnDone.Click += (s, e) => {
            TextSubmitted?.Invoke(this, TxtInput.Text ?? "");
            this.IsVisible = false;
        };
    }

    private void SetupKeyboard()
    {
        GridKeys.Children.Clear();
        foreach (var key in _keys)
        {
            var btn = new Button
            {
                Content = key,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = (IBrush)Application.Current!.FindResource("BorderBrush")!,
                BorderThickness = new Thickness(1),
                FontSize = 18,
                CornerRadius = new CornerRadius(5)
            };

            btn.Click += OnKeyClick;
            GridKeys.Children.Add(btn);
        }
    }

    private void OnKeyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string key)
        {
            if (key == "⌫")
            {
                if (!string.IsNullOrEmpty(TxtInput.Text))
                    TxtInput.Text = TxtInput.Text.Substring(0, TxtInput.Text.Length - 1);
            }
            else
            {
                TxtInput.Text += key;
            }
        }
    }

    public void Show(string initialText = "")
    {
        TxtInput.Text = initialText;
        this.IsVisible = true;
        TxtInput.Focus();
    }
}
