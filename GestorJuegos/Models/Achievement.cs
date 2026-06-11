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
        public string? Type { get; set; }

        public bool IsWin => !string.IsNullOrEmpty(Type) && (Type.Equals("win_condition", StringComparison.OrdinalIgnoreCase) || Type.Contains("win"));
        public bool IsNormalUnlock => IsUnlocked && !IsWin;
        public bool IsWinUnlock => IsUnlocked && IsWin;
    }
}
