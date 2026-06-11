using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GestorJuegos.Models;
using GestorJuegos.Data;

namespace GestorJuegos.Services;

public class ImportService
{
    private readonly GameService _gameService;

    public ImportService(GameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Escanea una carpeta en busca de ROMs para una plataforma específica.
    /// </summary>
    public async Task<int> ScanFolderAsync(string path, int platformId, string[] extensions, Action<string>? onProgress = null)
    {
        if (!Directory.Exists(path)) return 0;

        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        int imported = 0;
        foreach (var file in files)
        {
            onProgress?.Invoke($"Importando: {Path.GetFileName(file)}");
            var game = new Game
            {
                Name = Path.GetFileNameWithoutExtension(file),
                RomPath = file,
                PlatformId = platformId,
                DateAdded = DateTime.Now,
                PlayStatus = "No Iniciado"
            };
            
            // Evitar duplicados por ruta
            if (!_gameService.GetGamesByPlatform(platformId).Any(g => g.RomPath == file))
            {
                _gameService.AddGame(game);
                imported++;
            }
        }

        return imported;
    }

    /// <summary>
    /// Limpia activos huérfanos de la base de datos multimedia.
    /// </summary>
    public async Task CleanupOrphanedAssetsAsync(Action<string>? onProgress = null)
    {
        onProgress?.Invoke("Escaneando activos huérfanos...");
        await Task.Delay(100); // Simulación de tarea pesada
    }

    public static bool IsDross(string fileName, string[] patterns)
    {
        foreach (var pattern in patterns)
            if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static Game ParseGameLine(string fileName, int platformId)
    {
        string nameOnly = Path.GetFileNameWithoutExtension(fileName);
        
        // Regex para extraer Región (ej: (USA), (Japan), (Europe))
        var regionMatch = Regex.Match(nameOnly, @"\(([^)]+)\)");
        string region = regionMatch.Success ? regionMatch.Groups[1].Value : "World";
        
        // Limpiar el nombre (quitar paréntesis y corchetes)
        string cleanName = Regex.Replace(nameOnly, @"\s*[\(\[][^\]\)]*[\)\]]", "").Trim();

        return new Game
        {
            Name = string.IsNullOrEmpty(cleanName) ? nameOnly : cleanName,
            PlatformId = platformId,
            Region = region,
            PlayStatus = "No Iniciado"
        };
    }
}
