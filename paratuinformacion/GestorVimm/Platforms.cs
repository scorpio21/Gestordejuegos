namespace GestorVimm;

public sealed record VaultPlatform(string Name, string SystemCode);

public static class VaultPlatforms
{
    public static IReadOnlyList<VaultPlatform> All { get; } =
    [
        new("Atari 2600", "Atari2600"),
        new("Atari 5200", "Atari5200"),
        new("Nintendo (NES)", "NES"),
        new("Master System", "SMS"),
        new("Atari 7800", "Atari7800"),
        new("TurboGrafx-16", "TG16"),
        new("Genesis", "Genesis"),
        new("TurboGrafx-CD", "TGCD"),
        new("Super Nintendo", "SNES"),
        new("CD-i", "CDi"),
        new("Sega CD", "SegaCD"),
        new("Jaguar", "Jaguar"),
        new("Sega 32X", "32X"),
        new("Saturn", "Saturn"),
        new("PlayStation", "PS1"),
        new("Jaguar CD", "JaguarCD"),
        new("Nintendo 64", "N64"),
        new("Dreamcast", "Dreamcast"),
        new("PlayStation 2", "PS2"),
        new("GameCube", "GameCube"),
        new("Xbox", "Xbox"),
        new("Xbox 360", "Xbox360"),
        new("Xbox 360 (Digital)", "X360-D"),
        new("PlayStation 3", "PS3"),
        new("Wii", "Wii"),
        new("WiiWare", "WiiWare"),
        new("Game Boy", "GB"),
        new("Lynx", "Lynx"),
        new("Game Gear", "GG"),
        new("Virtual Boy", "VB"),
        new("Game Boy Color", "GBC"),
        new("Game Boy Advance", "GBA"),
        new("Nintendo DS", "DS"),
        new("PS Portable", "PSP"),
        new("Nintendo 3DS", "3DS"),
    ];
}
