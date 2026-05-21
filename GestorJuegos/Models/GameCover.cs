using System;

namespace GestorJuegos.Models
{
    public class GameCover
    {
        public int Id { get; set; } // Coincidirá con el Id del Game
        public string ImageType { get; set; } = "Box - Front"; // Tipo de arte (ej: "Box - Front", "Cart - Front")
        public byte[]? ImageData { get; set; }
        public byte[]? ThumbnailData { get; set; }
    }
}
