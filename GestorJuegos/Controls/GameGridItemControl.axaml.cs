using Avalonia.Controls;
using Avalonia.Input;
using GestorJuegos.Utils;

namespace GestorJuegos.Controls;

public partial class GameGridItemControl : UserControl
{
    public GameGridItemControl()
    {
        InitializeComponent();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        SoundHelper.PlayNavigation();
    }
}