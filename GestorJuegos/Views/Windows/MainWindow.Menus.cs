using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Data;
using GestorJuegos.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GestorJuegos.Views.Windows
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;
        private void OnTopBarViewToggle(object? sender, string name)
        {
            bool grid = name == "BtnViewGrid";
            bool list = name == "BtnViewList";
            bool wheelV = name == "BtnViewWheelVertical";
            bool wheelH = name == "BtnViewWheelHorizontal";

            TopBar.SetViewMode(grid, list, wheelV, wheelH);
            Library.SetViewMode(grid, list, wheelV, wheelH);
            UpdateMenuCheckmarks();
        }

        private void OnTopBarSortAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            string field = name.Replace("SortBy", "").Replace("SubSortBy", "");
            _currentSortField = field;
            Library.SortField = field;
            Library.ApplyFilters();
            UpdateMenuCheckmarks();
        }

        private void OnTopBarArtTypeAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            string artType = name.Replace("ArtType", "").Replace("SubArtType", "");
            _settings.PreferredArtType = artType;
            Library.ApplyFilters();
            UpdateMenuCheckmarks();
        }

        private void OnTopBarBadgeAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            if (name.Contains("Favorite")) _showFavoriteBadge = !_showFavoriteBadge;
            else if (name.Contains("Region")) _showRegionBadge = !_showRegionBadge;
            else if (name.Contains("PlayStatus")) _showStatusBadge = !_showStatusBadge;
            
            // Nota: Aquí se podrían aplicar badges a nivel de vista si se implementa en Library.
            Library.ApplyFilters();
            UpdateMenuCheckmarks();
        }

        private void OnTopBarHelpAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            switch (name)
            {
                case "MenuHelpExternalLib": ShowHelpExternalLib(); break;
                case "MenuHelpImportFolder": ShowHelpImportFolder(); break;
                case "MenuHelpEmulator": ShowHelpEmulator(); break;
                case "MenuHelpMultiDisk": ShowHelpMultiDisk(); break;
                case "MenuHelpDatabase": ShowHelpDatabase(); break;
                case "MenuAbout": ShowAbout(); break;
                default: ShowMessage($"Ayuda sobre {name}"); break;
            }
        }

        private async void OnTopBarMenuAction(object? sender, string action)
        {
            switch (action)
            {
                case "MenuExportDB":
                    OverlayExportOptions.IsVisible = true;
                    break;
                case "MenuImportDB":
                    await ImportDatabaseAsync();
                    break;
                case "MenuImportFolders":
                    await ImportFromFolder();
                    break;
                case "MenuImportExternalLib":
                    await ImportExternalLibraryAsync();
                    break;
                case "MenuImportDat":
                    await ImportFromDatAsync();
                    break;
                case "MenuSyncExternalLib":
                    await SyncExternalLibraryAsync();
                    break;
                case "MenuSyncMasterDb":
                    await SyncWithMasterDbAsync();
                    break;
                case "MenuScanLocalCovers":
                    await ScanLocalCoversAsync();
                    break;
                case "MenuMassiveScanCovers":
                    await ScanMassiveCoversAsync();
                    break;
                case "MenuCleanupOrphans":
                    CleanupOrphans();
                    break;
                case "MenuManageDross":
                    ManageDross();
                    break;
                case "MenuShowStats":
                    ShowFullStats();
                    break;
                case "MenuManagePlatforms":
                    OverlayManagePlatforms.Initialize(_gameService);
                    OverlayManagePlatforms.IsVisible = true;
                    break;
                case "MenuSettings":
                    var options = new OpcionesWindow(_settings);
                    if (await options.ShowDialog<bool>(this))
                    {
                        SaveSettings();
                        ApplyTheme();
                        Sidebar.Initialize(_gameService, _settings);
                        Library.Initialize(_gameService, _settings);
                        Library.ApplyFilters();
                    }
                    break;
            }
        }

        private void UpdateMenuCheckmarks()
        {
            TopBar.UpdateCheckmarks(_currentSortField, _isSortAscending, _settings, _showFavoriteBadge, _showRegionBadge, _showStatusBadge, Library.IsGridView, Sidebar.IsVisible);
        }

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

                    // Recargar UI
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

        private async Task ScanLocalCoversAsync()
        {
            if (_selectedPlatform == null)
            {
                ShowMessage("Por favor, selecciona primero una plataforma para asociar las carátulas.");
                return;
            }

            SoundHelper.PlaySelect();
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta de Carátulas",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string coverPath = folders[0].Path.LocalPath;
                _cts = new CancellationTokenSource();
                OverlayProgress.Update($"Escaneando carátulas: {_selectedPlatform.Name}", 0, "Iniciando escaneo de imágenes...");

                var progress = new Progress<ScanProgress>(p => {
                    OverlayProgress.Update($"Escaneando carátulas: {_selectedPlatform.Name}", p.Percentage, p.Detail);
                });

                try
                {
                    await Task.Run(() => _scannerService.ScanCoversAsync(_selectedPlatform, coverPath, progress, _cts.Token));
                    OverlayProgress.Hide();
                    LoadGames();
                    ShowMessage("¡Escaneo de carátulas finalizado!");
                }
                catch (Exception ex)
                {
                    OverlayProgress.Hide();
                    ShowMessage($"Error durante el escaneo: {ex.Message}");
                }
            }
        }

        private async Task ScanMassiveCoversAsync()
        {
            SoundHelper.PlaySelect();
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta Raíz de Media (Emumovies/Biblioteca Externa)",
                AllowMultiple = false
            });

            if (folders.Count >= 1)
            {
                string rootPath = folders[0].Path.LocalPath;
                _cts = new CancellationTokenSource();
                OverlayProgress.Update("Escaneo Masivo de Arte...", 0, "Iniciando análisis masivo de directorios...");

                var progress = new Progress<ScanProgress>(p => {
                    OverlayProgress.Update("Escaneo Masivo de Arte...", p.Percentage, p.Detail);
                });

                try
                {
                    await Task.Run(() => _scannerService.ScanMassiveCoversAsync(rootPath, progress, _cts.Token));
                    OverlayProgress.Hide();
                    LoadGames();
                    ShowMessage("¡Escaneo masivo finalizado!");
                }
                catch (Exception ex)
                {
                    OverlayProgress.Hide();
                    ShowMessage($"Error durante el escaneo: {ex.Message}");
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
