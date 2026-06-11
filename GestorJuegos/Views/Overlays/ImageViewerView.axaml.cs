using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System.IO;

namespace GestorJuegos.Views.Overlays;

public partial class ImageViewerView : UserControl
{
    public ImageViewerView()
    {
        InitializeComponent();
        BtnClose.Click += (s, e) => this.IsVisible = false;
    }

    public void ShowImage(byte[] data)
    {
        try
        {
            using var ms = new MemoryStream(data);
            ImgFull.Source = new Bitmap(ms);
            this.IsVisible = true;
        }
        catch { }
    }
}
