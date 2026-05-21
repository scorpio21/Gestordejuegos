namespace GestorJuegos.Models
{
    public class SidebarItem
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Icon { get; set; } = string.Empty;
        public object? Tag { get; set; }
        public bool IsHeader { get; set; }
    }
}
