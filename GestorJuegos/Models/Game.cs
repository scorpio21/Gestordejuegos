using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GestorJuegos.Models
{
    public class Game : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private string _romPath = string.Empty;
        private int _platformId;
        private string _description = string.Empty;
        private int _year;
        private string _developer = string.Empty;
        private string _publisher = string.Empty;
        private string _genre = string.Empty;
        private string _region = "World";
        private int _rating;
        private int _playCount;
        private int _playTime;
        private DateTime _dateAdded;
        private DateTime? _lastPlayed;
        private string _playStatus = "No Jugado";
        private bool _isFavorite;
        private string? _externalDbId;

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string RomPath { get => _romPath; set { _romPath = value; OnPropertyChanged(); } }
        public int PlatformId { get => _platformId; set { _platformId = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public int Year { get => _year; set { _year = value; OnPropertyChanged(); } }
        public string Developer { get => _developer; set { _developer = value; OnPropertyChanged(); } }
        public string Publisher { get => _publisher; set { _publisher = value; OnPropertyChanged(); } }
        public string Genre { get => _genre; set { _genre = value; OnPropertyChanged(); } }
        public string Region { get => _region; set { _region = value; OnPropertyChanged(); } }
        public int Rating { get => _rating; set { _rating = value; OnPropertyChanged(); } }
        public int PlayCount { get => _playCount; set { _playCount = value; OnPropertyChanged(); } }
        public int PlayTime { get => _playTime; set { _playTime = value; OnPropertyChanged(); } }
        public DateTime DateAdded { get => _dateAdded; set { _dateAdded = value; OnPropertyChanged(); } }
        public DateTime? LastPlayed { get => _lastPlayed; set { _lastPlayed = value; OnPropertyChanged(); } }
        public string PlayStatus { get => _playStatus; set { _playStatus = value; OnPropertyChanged(); } }
        public bool IsFavorite { get => _isFavorite; set { _isFavorite = value; OnPropertyChanged(); } }
        public string? ExternalDbId { get => _externalDbId; set { _externalDbId = value; OnPropertyChanged(); } }

        // --- Nuevos campos alineados con la Biblioteca Externa ---
        public string? ReleaseDate { get; set; }
        public string? ReleaseType { get; set; }
        public int? MaxPlayers { get; set; }
        public bool Cooperative { get; set; }
        public string? VideoURL { get; set; }
        public string? WikipediaURL { get; set; }
        public string? ESRB { get; set; }
        public string? CommunityRating { get; set; }
        public int CommunityRatingCount { get; set; }

        // --- Campos de compatibilidad (Ahora mapeados para persistencia) ---
        public string SelectedArtType { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;
        public string AdditionalRoms { get; set; } = string.Empty;
        public string OverrideEmulatorPath { get; set; } = string.Empty;
        public string OverrideLaunchArguments { get; set; } = string.Empty;

        [NotMapped] public byte[]? Cover { get; set; }
        [NotMapped] public string CoverType { get; set; } = "Box - Front";
        [NotMapped] public List<GameImage> ExtraImages { get; set; } = new();
        public string ShortName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Platform? Platform { get; set; }
        [NotMapped] public string GridSubtext => $"{Year} | {Developer}";
        
        // Relaciones
        public List<GameAlternateTitle> AlternateTitles { get; set; } = new();
        public List<GameImage> Images { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
