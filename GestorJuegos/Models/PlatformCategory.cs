using System;

namespace GestorJuegos.Models
{
    public class PlatformCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Computers", "Consoles", "Handhelds"
        public byte[]? Icon { get; set; }  // Icono pixel-art (F:\LaunchBox\Images\Platform Icons\Platform Categories)
        public byte[]? Graphic { get; set; } // Gráfico Clear Logo grande (F:\LaunchBox\Images\Platform Categories\[Name]\Clear Logo)
        public string? Notes { get; set; }
    }
}
