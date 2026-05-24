using System;
using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class SidebarNode
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public object? Tag { get; set; } // Puede ser "ALL", "FAVORITES", ("PLATFORM", name), ("CATEGORY", name), etc.
        public byte[]? IconBytes { get; set; } // Icono binario de la base de datos
        public bool HasIconBytes => IconBytes != null && IconBytes.Length > 0;
        public string ResortIcon { get; set; } = "🎮"; // Emoji de respaldo
        public List<SidebarNode> Children { get; set; } = new();
        public bool IsExpanded { get; set; } = true; // Por defecto expandido como en LaunchBox

        // Propiedades de formato visual
        public bool ShowCount => Tag != null; // Oculta contador si es un nodo sin filtro
        public Avalonia.Media.FontWeight FontWeight => (Tag is string s && (s == "ALL" || s == "FAVORITES")) || (Tag is ValueTuple<string, string> t && t.Item1 == "CATEGORY") || (Tag == null)
            ? Avalonia.Media.FontWeight.Bold 
            : Avalonia.Media.FontWeight.Normal;
    }
}
