namespace GestorJuegos.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Region { get; set; } = "🇺🇸 US"; // Default to US or none
        public string Languages { get; set; } = string.Empty;
        public byte[]? Cover { get; set; }
        public string RomPath { get; set; } = string.Empty;
        public string OverrideEmulatorPath { get; set; } = string.Empty;
        public string OverrideLaunchArguments { get; set; } = string.Empty;
        public int PlatformId { get; set; }
        public Platform Platform { get; set; } = null!;
    }
}
