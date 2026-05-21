using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GestorJuegos.Models
{
    public class Game : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Region { get; set; } = "🇺🇸 US"; // Default to US or none
        public string Languages { get; set; } = string.Empty;
        
        private byte[]? _cover;
        [NotMapped]
        public byte[]? Cover 
        { 
            get => _cover; 
            set 
            { 
                _cover = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string RomPath { get; set; } = string.Empty;
        public string AdditionalRoms { get; set; } = string.Empty;
        public bool IsFavorite { get; set; } = false;
        public string OverrideEmulatorPath { get; set; } = string.Empty;
        public string OverrideLaunchArguments { get; set; } = string.Empty;
        public DateTime? DateAdded { get; set; }
        public int PlatformId { get; set; }
        public Platform Platform { get; set; } = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
