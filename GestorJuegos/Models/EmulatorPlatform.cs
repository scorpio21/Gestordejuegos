using System;

namespace GestorJuegos.Models
{
    public class EmulatorPlatform
    {
        public int Id { get; set; }
        public int EmulatorId { get; set; }
        public Emulator? Emulator { get; set; }
        
        public int PlatformId { get; set; }
        public Platform? Platform { get; set; }
        
        public string SpecificArguments { get; set; } = string.Empty;
    }
}
