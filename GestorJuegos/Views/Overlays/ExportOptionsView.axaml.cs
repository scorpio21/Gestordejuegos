using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace GestorJuegos.Views.Overlays;

public partial class ExportOptionsView : UserControl
{
    public event EventHandler<(bool exportGames, bool exportCovers)>? RequestExport;
    public event EventHandler? RequestClose;

    public ExportOptionsView()
    {
        InitializeComponent();
        BtnCancel.Click += (s, e) => RequestClose?.Invoke(this, EventArgs.Empty);
        BtnConfirm.Click += (s, e) => {
            RequestExport?.Invoke(this, (ChkExportGames.IsChecked == true, ChkExportCovers.IsChecked == true));
        };
    }
}
