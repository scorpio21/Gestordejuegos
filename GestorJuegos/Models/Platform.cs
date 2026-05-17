using System;
using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class Platform
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmulatorPath { get; set; } = string.Empty;
        public string LaunchArguments { get; set; } = "\"{0}\"";
        public DateTime? LastScanDate { get; set; }
        public List<Game> Games { get; set; } = new();
    }
}
