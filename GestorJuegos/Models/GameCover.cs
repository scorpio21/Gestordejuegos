using System;

namespace GestorJuegos.Models
{
    public class GameCover
    {
        public int Id { get; set; } // Coincidirá con el Id del Game
        public byte[]? ImageData { get; set; }
        public byte[]? ThumbnailData { get; set; }
    }
}
