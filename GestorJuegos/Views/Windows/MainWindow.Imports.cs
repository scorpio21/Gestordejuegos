using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Data;
using GestorJuegos.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GestorJuegos.Views.Windows
{
    public partial class MainWindow : Window
    {
        private async Task ImportFromFolder()
        {
            SoundHelper.PlaySelect();
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta Raíz de Colección",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string rootPath = folders[0].Path.LocalPath;

                _cts = new CancellationTokenSource();
                OverlayProgress.Update("Escaneando colección...", 0, "Iniciando escaneo recursivo...");

                var progress = new Progress<ScanProgress>(p => {
                    OverlayProgress.Update(p.Title, p.Percentage, p.Detail);
                });

                try
                {
                    await Task.Run(() => _scannerService.ScanCollectionAsync(rootPath, progress, _cts.Token));
                    
                    OverlayProgress.Hide();
                    Sidebar.LoadPlatforms();
                    LoadDashboard();
                    ShowMessage("¡Escaneo finalizado!\n\nSe han detectado las plataformas y se han importado los juegos encontrados en sus carpetas.");
                }
                catch (OperationCanceledException)
                {
                    OverlayProgress.Hide();
                    ShowMessage("La operación fue cancelada.");
                }
                catch (Exception ex)
                {
                    OverlayProgress.Hide();
                    ShowMessage($"Error durante el escaneo: {ex.Message}");
                }
            }
        }

        private async Task ImportExternalLibraryAsync()
        {
            SoundHelper.PlaySelect();
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta Raíz de Biblioteca Externa",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string rootPath = folders[0].Path.LocalPath;

                _cts = new CancellationTokenSource();
                OverlayProgress.Update("Importando Biblioteca Externa...", 0, "Leyendo estructura XML...");

                var progress = new Progress<ScanProgress>(p => {
                    OverlayProgress.Update(p.Title, p.Percentage, p.Detail);
                });

                try
                {
                    await Task.Run(() => _scannerService.ScanExternalLibraryAsync(rootPath, progress, _cts.Token));
                    
                    OverlayProgress.Hide();
                    Sidebar.LoadPlatforms();
                    LoadDashboard();
                    ShowMessage("¡Importación finalizada!\n\nSe han procesado las plataformas y juegos correctamente.");
                }
                catch (OperationCanceledException)
                {
                    OverlayProgress.Hide();
                    ShowMessage("La operación fue cancelada.");
                }
                catch (Exception ex)
                {
                    OverlayProgress.Hide();
                    ShowMessage($"Error durante la importación: {ex.Message}");
                }
            }
        }

        private async Task ImportFromDatAsync()
        {
            if (_selectedPlatform == null)
            {
                ShowMessage("Por favor, selecciona primero la plataforma a la que quieres importar los juegos en el menú lateral.");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importar archivo DAT/XML de No-Intro",
                AllowMultiple = false,
                FileTypeFilter = new[] { 
                    new FilePickerFileType("Archivos XML/DAT") { Patterns = new[] { "*.xml", "*.dat" } }
                }
            });

            if (files.Count > 0)
            {
                try
                {
                    var file = files[0];
                    ShowMessage("Importando juegos, por favor espera...");

                    await Task.Run(async () =>
                    {
                        using var stream = await file.OpenReadAsync();
                        var doc = XDocument.Load(stream);

                        int count = 0;
                        var gamesNodes = doc.Descendants("game").ToList();
                        bool isExternalLib = false;
                        bool isExternalLibMame = false;

                        if (!gamesNodes.Any())
                        {
                            gamesNodes = doc.Descendants("Game").ToList();
                            isExternalLib = gamesNodes.Any();

                            if (!isExternalLib)
                            {
                                gamesNodes = doc.Descendants("MameFile").ToList();
                                isExternalLibMame = gamesNodes.Any();
                            }
                        }

                        var newGames = new List<Game>();
                        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        
                        using (var context = new AppDbContext())
                        {
                            var platformGames = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).Select(g => new { g.Name, g.Region }).ToList();
                            foreach (var g in platformGames)
                                if (g.Name != null) existingNames.Add($"{g.Name}|{g.Region}");
                        }

                        int skippedCount = 0;
                        var drossPatterns = FileHelpers.LoadDrossPatterns();

                        foreach (var gameNode in gamesNodes)
                        {
                            string name = "";
                            string shortName = "";
                            string region = "🌎 World";
                            string genre = "";
                            string developer = "";
                            string publisher = "";
                            int year = DateTime.Now.Year;

                            if (isExternalLib)
                            {
                                name = gameNode.Element("Title")?.Value ?? "";
                                region = gameNode.Element("Region")?.Value ?? "🌎 World";
                                genre = gameNode.Element("Genre")?.Value ?? "";
                                developer = gameNode.Element("Developer")?.Value ?? "";
                                publisher = gameNode.Element("Publisher")?.Value ?? "";
                                string relDate = gameNode.Element("ReleaseDate")?.Value ?? "";
                                if (!string.IsNullOrEmpty(relDate) && DateTime.TryParse(relDate, out var dt))
                                {
                                    year = dt.Year;
                                }
                            }
                            else if (isExternalLibMame)
                            {
                                name = gameNode.Element("Name")?.Value ?? "";
                                shortName = gameNode.Element("FileName")?.Value ?? "";
                                region = gameNode.Element("Region")?.Value ?? "🌎 World";
                                genre = gameNode.Element("Genre")?.Value ?? "";
                                developer = gameNode.Element("Developer")?.Value ?? "";
                                publisher = gameNode.Element("Publisher")?.Value ?? "";
                                string yearStr = gameNode.Element("Year")?.Value ?? "";
                                if (int.TryParse(yearStr, out var y)) year = y;
                            }
                            else
                            {
                                shortName = gameNode.Attribute("name")?.Value ?? "";
                                name = gameNode.Element("description")?.Value ?? shortName;
                            }

                            if (string.IsNullOrEmpty(name)) continue;

                            if (ImportService.IsDross(name, drossPatterns.ToArray()))
                            {
                                skippedCount++;
                                continue;
                            }

                            // Normalización de Región
                            if (region == "🌎 World" || string.IsNullOrEmpty(region))
                            {
                                if (name.Contains("(JP") || name.Contains("(Japan")) region = "🇯🇵 JP";
                                else if (name.Contains("(US") || name.Contains("(USA")) region = "🇺🇸 US";
                                else if (name.Contains("(EU") || name.Contains("(Europe")) region = "🇪🇺 EU";
                                else if (name.Contains("(Spain", StringComparison.OrdinalIgnoreCase) || name.Contains("(España", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es)", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es-Es)", StringComparison.OrdinalIgnoreCase) || name.Contains("(Es - Es)", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";
                                else region = "🌎 World";
                            }
                            else
                            {
                                if (region.Contains("Japan", StringComparison.OrdinalIgnoreCase)) region = "🇯🇵 JP";
                                else if (region.Contains("United States", StringComparison.OrdinalIgnoreCase) || region.Contains("North America", StringComparison.OrdinalIgnoreCase)) region = "🇺🇸 US";
                                else if (region.Contains("Europe", StringComparison.OrdinalIgnoreCase)) region = "🇪🇺 EU";
                                else if (region.Contains("Spain", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";
                            }

                            string cleanName = name;
                            if (!isExternalLib && !isExternalLibMame)
                            {
                                int bracketIndex = name.IndexOf('(');
                                if (bracketIndex > 0)
                                {
                                    cleanName = name.Substring(0, bracketIndex).Trim();
                                }

                                if (cleanName.Contains("•"))
                                {
                                    cleanName = cleanName.Split('•')[0].Trim();
                                }
                            }

                            string uniqueKey = $"{cleanName}|{region}";
                            if (existingNames.Contains(uniqueKey))
                            {
                                skippedCount++;
                            }
                            else
                            {
                                existingNames.Add(uniqueKey);
                                newGames.Add(new Game
                                {
                                    PlatformId = _selectedPlatform.Id,
                                    Name = cleanName,
                                    ShortName = shortName,
                                    Region = region,
                                    Genre = genre,
                                    Developer = developer,
                                    Publisher = publisher,
                                    Year = year,
                                    DateAdded = DateTime.Now
                                });
                            }
                        }

                        if (newGames.Any())
                        {
                            _gameService.AddGamesBatch(newGames);
                            count = newGames.Count;
                        }

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            LoadGames();
                            string msg = $"¡Importación completada! Se añadieron {count} juegos.";
                            if (skippedCount > 0) msg += $" Se omitieron {skippedCount} que ya existían.";
                            ShowMessage(msg);
                        });
                    });
                }
                catch (Exception ex)
                {
                    ShowMessage($"Error al importar el archivo: {ex.Message}");
                }
            }
        }
    }
}
