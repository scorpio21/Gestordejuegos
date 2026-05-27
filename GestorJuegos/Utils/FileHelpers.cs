using System;
using System.Collections.Generic;
using System.IO;

namespace GestorJuegos.Utils;

public static class FileHelpers
{
    public static List<string> LoadDrossPatterns()
    {
        string drossPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dross_filter.json");
        if (File.Exists(drossPath))
        {
            try
            {
                var json = File.ReadAllText(drossPath);
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch { }
        }
        return new List<string>();
    }

    public static string GetLaunchBoxFolderName(string friendlyName)
    {
        return friendlyName switch
        {
            "Advert" => "Advert",
            "Artwork Preview" => "Artwork_Preview",
            "Background" => "Background",
            "Box" => "Box",
            "Box 3D" => "Box_3D",
            "Box Full" => "Box_Full",
            "Box - Back" => "Box_Back",
            "Box - Spine" => "Box_Spine",
            "Cart - Front" => "Cart_Front",
            "Cart - 3D" => "Cart_3D",
            "Cart - Back" => "Cart_Back",
            "Support" => "Disc",
            "Cabinet" => "Cabinet",
            "Logos" => "Logos",
            "Marquee" => "Marquee",
            "Snap" => "Snap",
            "System Logo" => "System_Logo",
            "Title" => "Title",
            "Fanart" => "Fanart - Background",
            "Clear Logo" => "Clear Logo",
            _ => "Box" // Por defecto
        };
    }

    public static string DetectCategory(string platformName)
    {
        string name = platformName.ToLower();

        // Portátiles
        if (name.Contains("game boy") || name.Contains("gameboy") || name.Contains("psp") ||
            name.Contains("nintendo ds") || name.Contains("nintendo 3ds") || name.Contains("game gear") ||
            name.Contains("lynx") || name.Contains("vita") || name.Contains("wonderswan") || name.Contains("ngp") ||
            name.Contains("pocket"))
            return "Handhelds";

        // Ordenadores
        if (name.Contains("amiga") || name.Contains("commodore") || name.Contains("msx") ||
            name.Contains("amstrad") || name.Contains("spectrum") || name.Contains("atari st") ||
            name.Contains("dos") || name.Contains("windows") || name.Contains("mac") ||
            name.Contains("pc") || name.Contains("apple") || name.Contains("sharp x68000") ||
            name.Contains("nec pc") || name.Contains("scummvm"))
            return "Computers";

        // Arcade
        if (name.Contains("arcade") || name.Contains("mame") || name.Contains("neogeo") ||
            name.Contains("cps") || name.Contains("finalburn") || name.Contains("taito") || name.Contains("sega model") ||
            name.Contains("naomi") || name.Contains("atomiswave"))
            return "Arcade";

        // Por defecto Consolas
        return "Consoles";
    }

    public static readonly string[] RomExtensions = { ".zip", ".7z", ".rar", ".iso", ".bin", ".cue", ".n64", ".z64", ".v64", ".nes", ".sfc", ".smc", ".gb", ".gbc", ".gba", ".nds", ".3ds", ".cia", ".rvz", ".wbfs", ".gcm", ".psx", ".pbp", ".chd", ".m3u" };
}
