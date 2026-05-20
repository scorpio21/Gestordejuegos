using System.Collections.Generic;

namespace GestorJuegos.Services
{
    public class IgdbSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string? Genre { get; set; }
        public string? CoverUrl { get; set; }
        public List<string> Platforms { get; set; } = new();
    }
}
