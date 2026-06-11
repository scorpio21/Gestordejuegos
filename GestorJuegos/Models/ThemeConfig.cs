using System.Collections.Generic;

namespace GestorJuegos.Models;

public class ThemeConfig
{
    public Dictionary<string, string> Colors { get; set; } = new();
    public Dictionary<string, string> Fonts { get; set; } = new();
    public string? BackgroundImage { get; set; }
    public string? OverlayImage { get; set; }
    public Dictionary<string, string> Metrics { get; set; } = new();
    public string? PreferredView { get; set; }
}
