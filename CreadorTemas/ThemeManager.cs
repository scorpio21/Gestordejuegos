using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CreadorTemas
{
    public class ThemeConfigJson
    {
        public Dictionary<string, string> Colors { get; set; } = new();
        public Dictionary<string, string> Fonts { get; set; } = new();
        public string BackgroundImage { get; set; } = "";
        public string OverlayImage { get; set; } = "";
        public Dictionary<string, string> Metrics { get; set; } = new();
        public string PreferredView { get; set; } = "Grid";
    }

    public static class ThemeManager
    {
        public static async Task<ThemeConfigJson?> LoadThemeAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                string jsonPath = Path.Combine(folderPath, "theme.json");
                if (!File.Exists(jsonPath)) return null;

                try
                {
                    string jsonText = File.ReadAllText(jsonPath);
                    return JsonSerializer.Deserialize<ThemeConfigJson>(jsonText);
                }
                catch
                {
                    return null;
                }
            });
        }

        public static async Task SaveThemeAsync(
            string themesPath,
            string themeFolder,
            ThemeConfigJson themeData,
            string? sourceMainFont,
            string? sourceHeaderFont,
            string? sourceBgImage,
            string? sourceOverlayImage,
            string? sourceLogoImage)
        {
            await Task.Run(() =>
            {
                string newThemeFolder = Path.Combine(themesPath, themeFolder);
                Directory.CreateDirectory(newThemeFolder);

                string imagesFolder = Path.Combine(newThemeFolder, "Images");
                Directory.CreateDirectory(imagesFolder);

                // Copiar fuente principal
                if (!string.IsNullOrEmpty(sourceMainFont) && File.Exists(sourceMainFont))
                {
                    string destFont = Path.Combine(newThemeFolder, Path.GetFileName(sourceMainFont));
                    if (sourceMainFont != destFont)
                        File.Copy(sourceMainFont, destFont, true);
                }

                // Copiar fuente de cabecera
                if (!string.IsNullOrEmpty(sourceHeaderFont) && File.Exists(sourceHeaderFont))
                {
                    string destFont = Path.Combine(newThemeFolder, Path.GetFileName(sourceHeaderFont));
                    if (sourceHeaderFont != destFont)
                        File.Copy(sourceHeaderFont, destFont, true);
                }

                // Copiar imagen de fondo
                if (!string.IsNullOrEmpty(sourceBgImage) && File.Exists(sourceBgImage))
                {
                    string destImg = Path.Combine(imagesFolder, Path.GetFileName(sourceBgImage));
                    if (sourceBgImage != destImg)
                        File.Copy(sourceBgImage, destImg, true);
                }

                // Copiar imagen de superposición
                if (!string.IsNullOrEmpty(sourceOverlayImage) && File.Exists(sourceOverlayImage))
                {
                    string destImg = Path.Combine(imagesFolder, Path.GetFileName(sourceOverlayImage));
                    if (sourceOverlayImage != destImg)
                        File.Copy(sourceOverlayImage, destImg, true);
                }

                // Copiar imagen del logotipo de la aplicación (guardándolo como Logo.png)
                if (!string.IsNullOrEmpty(sourceLogoImage) && File.Exists(sourceLogoImage))
                {
                    string destLogo = Path.Combine(imagesFolder, "Logo.png");
                    if (sourceLogoImage != destLogo)
                        File.Copy(sourceLogoImage, destLogo, true);
                }

                // Escribir theme.json
                string jsonOutput = JsonSerializer.Serialize(themeData, new JsonSerializerOptions { WriteIndented = true });
                string themeJsonPath = Path.Combine(newThemeFolder, "theme.json");
                File.WriteAllText(themeJsonPath, jsonOutput);
            });
        }
    }
}
