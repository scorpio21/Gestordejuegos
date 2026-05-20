using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GestorJuegos.Models;

namespace GestorJuegos.Services
{
    public class ImportService
    {
        public static Game ParseGameLine(string line, int platformId)
        {
            string baseName = Path.GetFileNameWithoutExtension(line);
            
            string region = "🌎 World";
            if (baseName.Contains("(Europe") || baseName.Contains("(EU")) region = "🇪🇺 EU";
            else if (baseName.Contains("(USA") || baseName.Contains("(US")) region = "🇺🇸 US";
            else if (baseName.Contains("(Japan") || baseName.Contains("(JP")) region = "🇯🇵 JP";
            else if (baseName.Contains("(Spain", StringComparison.OrdinalIgnoreCase) || 
                     baseName.Contains("(España", StringComparison.OrdinalIgnoreCase) || 
                     baseName.Contains("(Es)", StringComparison.OrdinalIgnoreCase) || 
                     baseName.Contains("(Es-Es)", StringComparison.OrdinalIgnoreCase) || 
                     baseName.Contains("(Es - Es)", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";

            // Extract languages e.g. (En,Fr,De)
            string langs = "";
            var langMatch = Regex.Match(baseName, @"\(([A-Za-z]{2}(?:,[A-Za-z]{2})*)\)");
            if (langMatch.Success)
            {
                langs = langMatch.Groups[1].Value;
            }

            // Clean name: remove (tags) and [tags]
            string cleanName = Regex.Replace(baseName, @"\([^)]*\)|\[[^\]]*\]", "").Trim();
            if (cleanName.Contains("•")) cleanName = cleanName.Split('•')[0].Trim();

            return new Game
            {
                PlatformId = platformId,
                Name = cleanName,
                Region = region,
                Languages = langs,
                Year = DateTime.Now.Year,
                DateAdded = DateTime.Now,
                RomPath = "" // Will be filled if file is found
            };
        }

        public static bool IsDross(string fileName, List<string> drossPatterns)
        {
            if (drossPatterns == null || drossPatterns.Count == 0) return false;
            foreach (var pattern in drossPatterns)
            {
                if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
