using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GestorJuegos.Models
{
    public class Game : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty; // Identificador interno (ej: puckman)
        public string? LaunchBoxDbId { get; set; } // ID vinculado a la DB de LaunchBox
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Rating { get; set; } = 0;
        public string PlayStatus { get; set; } = "Pendiente";
        public string Version { get; set; } = string.Empty;
        public int PlayCount { get; set; } = 0;
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

        [NotMapped]
        public string CoverType { get; set; } = "Box - Front";

        [NotMapped]
        public List<GameImage> ExtraImages { get; set; } = new();
        
        public string RomPath { get; set; } = string.Empty;
        public string SelectedArtType { get; set; } = string.Empty; // Nueva: Guarda el tipo elegido (ej: "Box 3D")
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
