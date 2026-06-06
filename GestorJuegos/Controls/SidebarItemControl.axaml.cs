using Avalonia.Controls;
using Avalonia.Input;
using GestorJuegos.Utils;

namespace GestorJuegos.Controls;

public partial class SidebarItemControl : UserControl
{
    public SidebarItemControl()
    {
        InitializeComponent();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        SoundHelper.PlayNavigation();
    }
}