using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GestorJuegos.Views.Windows
{
    public partial class MainWindow : Window
    {
        private async Task ExportDatabaseAsync(bool exportGames, bool exportCovers)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            try
            {
                int exportsDone = 0;

                if (exportGames)
                {
                    var fileData = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Exportar Base de Datos de Juegos",
                        SuggestedFileName = "GestorJuegos_Backup.db",
                        FileTypeChoices = new[] { new FilePickerFileType("Base de datos SQLite") { Patterns = new[] { "*.db" } } }
                    });

                    if (fileData != null)
                    {
                        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");
                        if (File.Exists(dbPath))
                        {
                            using (var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var destinationStream = await fileData.OpenWriteAsync())
                            {
                                await sourceStream.CopyToAsync(destinationStream);
                            }
                            exportsDone++;
                        }
                    }
                }

                if (exportCovers)
                {
                    var fileCovers = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Exportar Base de Datos de Carátulas",
                        SuggestedFileName = "GestorCovers_Backup.db",
                        FileTypeChoices = new[] { new FilePickerFileType("Base de datos SQLite") { Patterns = new[] { "*.db" } } }
                    });

                    if (fileCovers != null)
                    {
                        string coversDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorCovers.db");
                        if (File.Exists(coversDbPath))
                        {
                            using (var sourceStream = new FileStream(coversDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var destinationStream = await fileCovers.OpenWriteAsync())
                            {
                                await sourceStream.CopyToAsync(destinationStream);
                            }
                            exportsDone++;
                        }
                    }
                }

                if (exportsDone > 0)
                {
                    ShowMessage($"Respaldo completado: Se han exportado {exportsDone} archivos con éxito.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al exportar: {ex.Message}");
            }
        }

        private async Task ImportDatabaseAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var filesData = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importar Base de Datos de Juegos (GestorJuegos.db)",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Base de datos SQLite") { Patterns = new[] { "*.db" } } }
            });

            if (filesData.Count > 0)
            {
                try
                {
                    var fileData = filesData[0];
                    string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");

                    using (var sourceStream = await fileData.OpenReadAsync())
                    using (var destinationStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }

                    var filesCovers = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Importar Base de Datos de Carátulas (GestorCovers.db)",
                        AllowMultiple = false,
                        FileTypeFilter = new[] { new FilePickerFileType("Base de datos SQLite") { Patterns = new[] { "*.db" } } }
                    });

                    if (filesCovers.Count > 0)
                    {
                        var fileCovers = filesCovers[0];
                        string coversDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorCovers.db");

                        using (var sourceStream = await fileCovers.OpenReadAsync())
                        using (var destinationStream = new FileStream(coversDbPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await sourceStream.CopyToAsync(destinationStream);
                        }
                        ShowMessage("Restauración completa: Se han importado los juegos y las carátulas.");
                    }
                    else
                    {
                        ShowMessage("Se importaron los juegos, pero no se seleccionó base de datos de carátulas.");
                    }

                    _selectedPlatform = null;
                    Sidebar.LoadPlatforms();
                    LoadDashboard();
                }
                catch (Exception ex)
                {
                    ShowMessage($"Error al importar: {ex.Message}. Asegúrese de cerrar el programa si el archivo está bloqueado.");
                }
            }
        }

        private void CleanupOrphans()
        {
            var orphaned = _gameService.GetOrphanedGames();
            if (orphaned.Count == 0)
            {
                ShowMessage("No se han encontrado juegos huérfanos. Todas las rutas de ROM son válidas.");
                return;
            }

            _dialogAcceptedAction = () => {
                _gameService.DeleteGames(orphaned.Select(g => g.Id).ToList());
                LoadGames();
                LoadDashboard();
                ShowMessage($"Se han eliminado {orphaned.Count} registros huérfanos con éxito.");
            };

            OverlayDialog.ShowConfirm(
                $"Se han encontrado {orphaned.Count} juegos cuya ruta de ROM ya no existe en el disco.\n\n" +
                "¿Deseas eliminar estos registros de la base de datos? Esta acción no afectará a tus archivos físicos.",
                "Limpiar Juegos Huérfanos"
            );
        }

        private void ManageDross()
        {
            try
            {
                string drossPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dross_filter.json");
                if (!File.Exists(drossPath))
                {
                    File.WriteAllText(drossPath, "[\"sample\", \"demo\", \"trailer\", \"bonus\", \"video\", \"preview\", \"teaser\"]");
                }
                Process.Start(new ProcessStartInfo("notepad.exe", drossPath) { UseShellExecute = true });
                ShowMessage("Se ha abierto el archivo 'dross_filter.json'.\n\nEdita la lista de palabras clave que deseas ignorar durante la importación y guarda el archivo.");
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al abrir el filtro de importación: {ex.Message}");
            }
        }
    }
}
