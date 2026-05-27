using System;

namespace GestorJuegos.Models
{
    public class PlatformAlternateName
    {
        public int Id { get; set; }
        public int PlatformId { get; set; }
        public Platform? Platform { get; set; }
        
        public string AlternateName { get; set; } = string.Empty;
    }
}
