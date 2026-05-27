using System;
using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class Emulator
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string DefaultArguments { get; set; } = "\"{0}\"";
        public string Website { get; set; } = string.Empty;
        public string? Version { get; set; }
        
        public List<EmulatorPlatform> SupportedPlatforms { get; set; } = new();
    }
}
