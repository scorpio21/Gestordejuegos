namespace GestorJuegos.Models
{
    public class GameImage
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string ImageType { get; set; } = string.Empty; // "Box - Front", "Box - 3D", "Clear Logo", etc.
        public byte[]? ImageData { get; set; }
    }
}
