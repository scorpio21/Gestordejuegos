using System;

namespace GestorJuegos.Models
{
    public class Achievement
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
        public int Points { get; set; }
        public DateTime? UnlockDate { get; set; }
    }
}
