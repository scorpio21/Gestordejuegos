using System;
using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class Platform
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Consoles"; // Valor por defecto
        public string EmulatorPath { get; set; } = string.Empty;
        public string LaunchArguments { get; set; } = "\"{0}\"";
        public DateTime? LastScanDate { get; set; }
        public List<Game> Games { get; set; } = new();

        public byte[]? Logo { get; set; }
        public byte[]? Icon { get; set; }
        public byte[]? HardwareImage { get; set; }

        public string? ReleaseDate { get; set; }
        public string? Developer { get; set; }
        public string? Manufacturer { get; set; }
        public string? Cpu { get; set; }
        public string? Memory { get; set; }
        public string? Graphics { get; set; }
        public string? Sound { get; set; }
        public string? Display { get; set; }
        public string? Media { get; set; }
        public string? Notes { get; set; }
    }
}
