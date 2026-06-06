using Avalonia.Controls;
using Avalonia.Input;
using GestorJuegos.Utils;

namespace GestorJuegos;

public partial class GameGridItemView : UserControl
{
    public GameGridItemView()
    {
        InitializeComponent();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        SoundHelper.PlayNavigation();
    }
}