using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Data;
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
        private async Task SyncExternalLibraryAsync()
        {
            if (_selectedPlatform == null)
            {
                ShowMessage("Por favor, selecciona primero la plataforma en el menú lateral.");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar XML de Plataforma de Biblioteca Externa (ej: arcade.xml)",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("XML Plataforma Biblioteca Externa") { Patterns = new[] { "*.xml" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];

                await Task.Run(async () =>
                {
                    try
                    {
                        using var stream = await file.OpenReadAsync();
                        var doc = XDocument.Load(stream);

                        var gameNodes = doc.Descendants("Game").ToList();
                        if (!gameNodes.Any())
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowMessage("El archivo no es un XML válido de Biblioteca Externa (no se encontraron etiquetas <Game>)."));
                            return;
                        }

                        // Mapas para búsqueda rápida
                        var titleToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        var shortNameToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var node in gameNodes)
                        {
                            string title = node.Element("Title")?.Value ?? "";
                            string path = node.Element("ApplicationPath")?.Value ?? "";

                            if (!string.IsNullOrEmpty(path))
                            {
                                if (!string.IsNullOrEmpty(title) && !titleToPath.ContainsKey(title))
                                    titleToPath.Add(title, path);

                                try
                                {
                                    string fileName = Path.GetFileNameWithoutExtension(path);
                                    if (!string.IsNullOrEmpty(fileName) && !shortNameToPath.ContainsKey(fileName))
                                        shortNameToPath.Add(fileName, path);
                                }
                                catch { }
                            }
                        }

                        using var context = new AppDbContext();
                        var gamesToUpdate = context.Games.Where(g => g.PlatformId == _selectedPlatform.Id).ToList();
                        int updatedCount = 0;

                        foreach (var game in gamesToUpdate)
                        {
                            var node = gameNodes.FirstOrDefault(n =>
                                (n.Element("Title")?.Value?.Equals(game.Name, StringComparison.OrdinalIgnoreCase) == true) ||
                                (Path.GetFileNameWithoutExtension(n.Element("ApplicationPath")?.Value ?? "")?.Equals(game.ShortName, StringComparison.OrdinalIgnoreCase) == true)
                            );

                            if (node != null)
                            {
                                string path = node.Element("ApplicationPath")?.Value ?? "";
                                if (!string.IsNullOrEmpty(path)) game.RomPath = path;

                                game.Description = node.Element("Notes")?.Value ?? game.Description;
                                game.Developer = node.Element("Developer")?.Value ?? game.Developer;
                                game.Publisher = node.Element("Publisher")?.Value ?? game.Publisher;
                                game.Genre = node.Element("Genre")?.Value ?? game.Genre;
                                game.Version = node.Element("Version")?.Value ?? game.Version;
                                game.ExternalDbId = node.Element("DatabaseID")?.Value ?? game.ExternalDbId;

                                string relDate = node.Element("ReleaseDate")?.Value ?? "";
                                if (!string.IsNullOrEmpty(relDate) && DateTime.TryParse(relDate, out var dt))
                                {
                                    game.Year = dt.Year;
                                }

                                string starRating = node.Element("StarRating")?.Value ?? "0";
                                if (float.TryParse(starRating, out var rating))
                                {
                                    game.Rating = (int)(rating * 20);
                                }

                                updatedCount++;
                            }
                        }

                        if (updatedCount > 0)
                        {
                            context.UpdateRange(gamesToUpdate);
                            context.SaveChanges();
                        }

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            ShowMessage($"Sincronización Inteligente completada.\nSe han vinculado las rutas de {updatedCount} juegos.");
                            LoadGames();
                        });
                    }
                    catch (Exception ex)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowMessage($"Error durante la sincronización: {ex.Message}"));
                    }
                });
            }
        }

        private async Task SyncWithMasterDbAsync()
        {
            if (_selectedPlatform == null && string.IsNullOrEmpty(_selectedCategory))
            {
                ShowMessage("Por favor, selecciona primero una plataforma o categoría en el menú lateral.");
                return;
            }

            var metadataService = new ExternalMetadataService();

            if (!metadataService.IsDatabaseAvailable)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Seleccionar Base de Datos Maestra (Metadata.db)",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Base de datos SQLite") { Patterns = new[] { "*.db" } } }
                });

                if (files.Count > 0)
                {
                    string path = files[0].Path.LocalPath;
                    metadataService = new ExternalMetadataService(path);
                }
                else return;
            }

            _cts = new CancellationTokenSource();
            OverlayProgress.Update("Importando Metadatos de Base Maestra", 0, "Iniciando proceso...");

            await Task.Run(() =>
            {
                try
                {
                    using var context = new AppDbContext();
                    var platformsToSync = new List<Platform>();

                    if (!string.IsNullOrEmpty(_selectedCategory))
                    {
                        platformsToSync = context.Platforms.Where(p => p.Category == _selectedCategory).ToList();
                    }
                    else if (_selectedPlatform != null)
                    {
                        platformsToSync = new List<Platform> { context.Platforms.First(p => p.Id == _selectedPlatform.Id) };
                    }

                    int updatedCount = 0;
                    int currentGameIndex = 0;

                    var allGamesToUpdate = new List<(Game game, string platformName)>();
                    foreach (var platform in platformsToSync)
                    {
                        var games = context.Games.Where(g => g.PlatformId == platform.Id).ToList();
                        foreach (var g in games) allGamesToUpdate.Add((g, platform.Name));
                    }

                    foreach (var item in allGamesToUpdate)
                    {
                        if (_cts.IsCancellationRequested) break;

                        currentGameIndex++;
                        var game = item.game;
                        var platformName = item.platformName;

                        int progressPercent = (int)((double)currentGameIndex / allGamesToUpdate.Count * 100);
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            OverlayProgress.Update(
                                "Importando Metadatos de Base Maestra",
                                progressPercent,
                                $"[{currentGameIndex}/{allGamesToUpdate.Count}] {game.Name}"
                            );
                        });

                        var oldId = game.ExternalDbId;
                        _gameService.EnrichGameWithMetadata(game, platformName);

                        if (game.ExternalDbId != oldId || !string.IsNullOrEmpty(game.Description))
                        {
                            updatedCount++;
                        }

                        if (currentGameIndex % 50 == 0)
                        {
                            context.UpdateRange(allGamesToUpdate.Take(currentGameIndex).Select(x => x.game));
                            context.SaveChanges();
                        }
                    }

                    if (!_cts.IsCancellationRequested)
                    {
                        context.UpdateRange(allGamesToUpdate.Select(x => x.game));
                        context.SaveChanges();
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        OverlayProgress.Hide();

                        if (_cts.IsCancellationRequested)
                        {
                            ShowMessage("Proceso cancelado por el usuario.");
                        }
                        else
                        {
                            string scope = !string.IsNullOrEmpty(_selectedCategory) ? $"la categoría '{_selectedCategory}'" : $"la plataforma '{_selectedPlatform?.Name}'";
                            ShowMessage($"Importación de Metadatos para {scope} completada.\nSe han enriquecido {updatedCount} juegos de {allGamesToUpdate.Count} procesados.");
                        }
                        LoadGames();
                    });
                }
                catch (Exception ex)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        OverlayProgress.Hide();
                        ShowMessage($"Error al leer la base de datos maestra: {ex.Message}");
                    });
                }
            });
        }
    }
}
