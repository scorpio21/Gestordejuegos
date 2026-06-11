using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestorJuegos.Models;
using GestorJuegos.Data;
using GestorJuegos.Utils;

namespace GestorJuegos.Services
{
    public class LauncherResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Logs { get; set; } = "";
    }

    public class LauncherService
    {
        private readonly GameService _gameService;
        private readonly AppSettings _settings;

        public event EventHandler<Game>? GameStarted;
        public event EventHandler<(Game game, TimeSpan elapsed)>? GameExited;

        public LauncherService(GameService gameService, AppSettings settings)
        {
            _gameService = gameService;
            _settings = settings;
        }

        /// <summary>
        /// Lanza un juego usando su plataforma o una ruta específica.
        /// </summary>
        public async Task<LauncherResult> LaunchGameAsync(Game game, Platform? platform = null, string? specificRomPath = null)
        {
            var result = new LauncherResult();
            var logLines = new System.Collections.Generic.List<string>();
            logLines.Add($"--- INICIANDO LANZAMIENTO: {DateTime.Now} ---");

            try
            {
                if (game == null)
                {
                    result.Message = "El juego es nulo.";
                    return result;
                }

                platform ??= game.Platform;
                if (platform == null)
                {
                    result.Message = "La plataforma del juego no está cargada.";
                    return result;
                }

                string romPath = specificRomPath ?? game.RomPath;
                logLines.Add($"Juego: {game.Name} (ID: {game.Id})");
                logLines.Add($"RomPath: '{romPath}'");

                if (string.IsNullOrEmpty(romPath))
                {
                    result.Message = "La ruta del juego está vacía.";
                    return result;
                }

                if (!File.Exists(romPath))
                {
                    result.Message = "El archivo del juego no existe en la ruta especificada.";
                    return result;
                }

                logLines.Add($"Plataforma: {platform.Name} (ID: {platform.Id})");

                ProcessStartInfo psi = new ProcessStartInfo();
                string finalEmulatorPath = platform.EmulatorPath;
                string finalLaunchArgs = platform.LaunchArguments;

                // Aplicar overrides de juego si existen
                if (!string.IsNullOrEmpty(game.OverrideEmulatorPath))
                {
                    finalEmulatorPath = game.OverrideEmulatorPath;
                    finalLaunchArgs = game.OverrideLaunchArguments;
                    logLines.Add($"Usando OVERRIDE de juego. EmulatorPath: '{finalEmulatorPath}'");
                }

                if (string.IsNullOrEmpty(finalEmulatorPath))
                {
                    logLines.Add("Aviso: EmulatorPath vacío. Usando UseShellExecute = true con RomPath.");
                    psi.FileName = romPath;
                    psi.UseShellExecute = true;
                }
                else
                {
                    if (!File.Exists(finalEmulatorPath))
                    {
                        result.Message = "La ruta del emulador especificada no existe.";
                        return result;
                    }

                    psi.FileName = finalEmulatorPath;
                    psi.WorkingDirectory = Path.GetDirectoryName(finalEmulatorPath) ?? string.Empty;
                    
                    string args = string.IsNullOrEmpty(finalLaunchArgs) ? "\"{0}\"" : finalLaunchArgs;
                    psi.Arguments = args.Replace("{0}", romPath);
                    psi.UseShellExecute = false;
                }

                logLines.Add($"-> Iniciando: FileName='{psi.FileName}', Arguments='{psi.Arguments}'");
                
                SoundHelper.PlayLaunch();
                
                var process = Process.Start(psi);
                if (process == null)
                {
                    result.Message = "No se pudo iniciar el proceso.";
                    return result;
                }

                logLines.Add("Proceso iniciado con éxito.");
                result.Success = true;

                // Actualizar metadatos básicos de ejecución
                game.PlayCount++;
                game.LastPlayed = DateTime.Now;
                _gameService.UpdateGame(game);

                GameStarted?.Invoke(this, game);

                // Monitorear en segundo plano para el tiempo de juego
                _ = MonitorProcessAsync(process, game);

                result.Logs = string.Join(Environment.NewLine, logLines);
                return result;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                logLines.Add($"EXCEPCIÓN: {ex.Message}");
                logLines.Add(ex.StackTrace ?? "");
                result.Logs = string.Join(Environment.NewLine, logLines);
                return result;
            }
        }

        /// <summary>
        /// Abre una URL en el navegador por defecto.
        /// </summary>
        public void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al abrir URL: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre una carpeta en el explorador de archivos.
        /// </summary>
        public void OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string? dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                if (dir != null && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al abrir carpeta: {ex.Message}");
            }
        }

        private async Task MonitorProcessAsync(Process process, Game game)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Esperar a que el proceso termine (asíncronamente)
                await process.WaitForExitAsync();
                
                var endTime = DateTime.Now;
                var elapsed = endTime - startTime;
                int secondsPlayed = (int)elapsed.TotalSeconds;

                if (secondsPlayed > 5) // Solo contar si duró más de 5 segundos (evitar errores de carga)
                {
                    game.PlayTime += secondsPlayed;
                    
                    // Auto-actualizar estado si supera el umbral
                    if (_settings.ProgAutoPlaytimeMin > 0 && 
                        secondsPlayed >= (_settings.ProgAutoPlaytimeMin * 60) &&
                        game.PlayStatus == "No Iniciado")
                    {
                        game.PlayStatus = _settings.ProgAutoPlaytimeVal;
                    }

                    _gameService.UpdateGame(game);
                    GameExited?.Invoke(this, (game, elapsed));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error monitoreando proceso: {ex.Message}");
            }
        }
    }
}
