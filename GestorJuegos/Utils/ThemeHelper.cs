using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using GestorJuegos.Models;

namespace GestorJuegos.Utils;

public static class ThemeHelper
{
    public static ThemeConfig? LoadThemeConfig(string themeName)
    {
        if (string.IsNullOrEmpty(themeName) || themeName == "Default") return null;

        try
        {
            string themesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", themeName);
            if (!Directory.Exists(themesDir)) return null;

            string jsonPath = Path.Combine(themesDir, "theme.json");
            if (!File.Exists(jsonPath)) return null;

            var json = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<ThemeConfig>(json);
        }
        catch { return null; }
    }

    public static void ApplyTheme(string themeName, ThemeConfig? config)
    {
        if (config == null || themeName == "Default")
        {
            ApplyDefaultTheme();
            return;
        }

        string themeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", themeName);

        // Aplicar Colores
        if (config.Colors != null)
        {
            foreach (var color in config.Colors)
            {
                UpdateResource(color.Key, color.Value);
            }
        }

        // Aplicar Fuentes Dinámicas (Clave: MainFont según Global.axaml)
        if (config.Fonts != null && config.Fonts.TryGetValue("MainFont", out var mainFontFile))
        {
            string fontPath = Path.Combine(themeDir, mainFontFile);
            if (File.Exists(fontPath))
            {
                try
                {
                    // Obtener nombre de familia (sin extensión)
                    string familyName = Path.GetFileNameWithoutExtension(mainFontFile);
                    
                    // Usar Uri absoluta para manejar espacios y caracteres especiales
                    var fileUri = new Uri(fontPath);
                    
                    // Constructor robusto para fuentes externas: FontFamily(baseUri, name)
                    // Avalonia 11 requiere que la baseUri apunte al archivo y el name sea el identificador
                    var fontFamily = new FontFamily(fileUri, familyName);
                    
                    Application.Current!.Resources["MainFont"] = fontFamily;
                }
                catch
                {
                    // Fallback silencioso para evitar crash
                    Application.Current!.Resources["MainFont"] = new FontFamily("Segoe UI");
                }
            }
            else
            {
                Application.Current!.Resources["MainFont"] = new FontFamily("Segoe UI");
            }
        }
    }

    private static void UpdateResource(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        
        try
        {
            if (key.EndsWith("Brush") || key.EndsWith("Foreground"))
            {
                if (Color.TryParse(value, out var color))
                    Application.Current!.Resources[key] = new SolidColorBrush(color);
            }
            else
            {
                Application.Current!.Resources[key] = value;
            }
        }
        catch { }
    }

    private static void ApplyDefaultTheme()
    {
        UpdateResource("AccentBrush", "#3a5180");
        UpdateResource("DeepDarkBrush", "#121316");
        UpdateResource("PanelBrush", "#1c1d22");
        UpdateResource("BorderBrush", "#2c2e35");
        UpdateResource("MainForeground", "#ffffff");
        Application.Current!.Resources["MainFont"] = new FontFamily("Segoe UI");
    }
}
