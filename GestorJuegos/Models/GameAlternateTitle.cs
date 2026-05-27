using System;

namespace GestorJuegos.Models
{
    public class GameAlternateTitle
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
        
        public string AlternateName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
