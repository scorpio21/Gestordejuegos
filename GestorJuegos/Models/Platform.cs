using System.Collections.Generic;

namespace GestorJuegos.Models
{
    public class Platform
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Game> Games { get; set; } = new();
    }
}
