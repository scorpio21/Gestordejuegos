using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GestorJuegos.Services;
using System;
using System.Timers;

namespace GestorJuegos.Views.Overlays;

public partial class ToastView : UserControl
{
    private Timer? _timer;

    public ToastView()
    {
        InitializeComponent();
    }

    public void Show(string message, string title, NotificationType type)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TxtTitle.Text = title;
            TxtMessage.Text = message;

            switch (type)
            {
                case NotificationType.Success:
                    IconText.Text = "✅";
                    ToastBorder.BorderBrush = Brushes.LimeGreen;
                    break;
                case NotificationType.Error:
                    IconText.Text = "❌";
                    ToastBorder.BorderBrush = Brushes.Red;
                    break;
                case NotificationType.Warning:
                    IconText.Text = "⚠️";
                    ToastBorder.BorderBrush = Brushes.Orange;
                    break;
                default:
                    IconText.Text = "ℹ️";
                    ToastBorder.BorderBrush = Brushes.DodgerBlue;
                    break;
            }

            ToastBorder.Opacity = 1;
            ToastBorder.Margin = new Thickness(0, 0, 20, 20); // Ajustar según contenedor

            _timer?.Stop();
            _timer = new Timer(4000);
            _timer.Elapsed += (s, e) => Hide();
            _timer.AutoReset = false;
            _timer.Start();
        });
    }

    private void Hide()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ToastBorder.Opacity = 0;
            ToastBorder.Margin = new Thickness(0, 0, 20, -100);
        });
    }
}
