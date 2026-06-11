using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using GestorJuegos.Data;
using GestorJuegos.Models;
using GestorJuegos.Utils;

namespace GestorJuegos.Services
{
    public class ScanProgress
    {
        public int Percentage { get; set; }
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    public class ScannerService
    {
        private readonly GameService _gameService;

        public ScannerService(GameService gameService)
        {
            _gameService = gameService;
        }

        public async Task ScanCollectionAsync(string rootPath, IProgress<ScanProgress> progress, CancellationToken ct)
        {
            var platformDirs = new List<string>();
            int gameCount = 0;

            // CARGAR LISTA NEGRA
            var blacklist = LoadBlacklist();
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                ".zip", ".7z", ".rar", ".iso", ".bin", ".cue", ".n64", ".v64", ".z64", 
                ".sfc", ".smc", ".nes", ".gb", ".gbc", ".gba", ".nds", ".3ds", ".cia", 
                ".pbp", ".cso", ".rvz", ".wbfs", ".gcm", ".gdi", ".chd", ".m3u", ".txt" 
            };

            // BUSCADOR RECURSIVO PROFUNDO DE PLATAFORMAS
            void FindPlatformsRecursive(string path)
            {
                if (ct.IsCancellationRequested) return;

                string cleanPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string folderName = Path.GetFileName(cleanPath);
                if (string.IsNullOrEmpty(folderName)) return;

                if (blacklist.Contains(folderName))
                {
                    foreach (var sd in Directory.GetDirectories(cleanPath)) FindPlatformsRecursive(sd);
                    return;
                }

                if (folderName.Contains("(Europe)", StringComparison.OrdinalIgnoreCase) || 
                    folderName.Contains("(USA)", StringComparison.OrdinalIgnoreCase) ||
                    folderName.Contains("(Japan)", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var sd in Directory.GetDirectories(cleanPath)) FindPlatformsRecursive(sd);
                    return;
                }

                var sDirs = Directory.GetDirectories(cleanPath);
                
                var gamesAtThisLevel = Directory.EnumerateFiles(cleanPath).Where(f => {
                    string ext = Path.GetExtension(f).ToLower();
                    return ext != ".txt" && extensions.Contains(ext);
                }).Take(11).ToList();
                
                bool hasGamesAtThisLevel = gamesAtThisLevel.Count > 0;
                bool hasHighDensity = gamesAtThisLevel.Count > 10;

                bool contentSubfolderHasGames = sDirs.Any(sd => {
                    string sn = Path.GetFileName(sd);
                    if (sn.Equals("Games", StringComparison.OrdinalIgnoreCase) || sn.Equals("Roms", StringComparison.OrdinalIgnoreCase))
                    {
                        try {
                            return Directory.EnumerateFiles(sd).Any(f => {
                                string ext = Path.GetExtension(f).ToLower();
                                return ext != ".txt" && extensions.Contains(ext);
                            });
                        } catch { return false; }
                    }
                    return false;
                });

                bool hasRegionSubdirs = sDirs.Any(sd => {
                    string n = Path.GetFileName(sd).ToLower();
                    return n == "spain" || n == "españa" || n == "europe" || n == "usa" || n == "japan" || 
                           n == "world" || n == "asia" || n.Contains("(europe)") || n.Contains("(usa)");
                });

                bool looksLikePlatform = folderName.Contains(" - ") || 
                                         folderName.Equals("MAME", StringComparison.OrdinalIgnoreCase) ||
                                         folderName.Contains("Arcade", StringComparison.OrdinalIgnoreCase) ||
                                         folderName.Contains("System", StringComparison.OrdinalIgnoreCase) ||
                                         folderName.Contains("Nintendo", StringComparison.OrdinalIgnoreCase) ||
                                         folderName.Contains("Sega", StringComparison.OrdinalIgnoreCase) ||
                                         folderName.Contains("PlayStation", StringComparison.OrdinalIgnoreCase);

                bool isCategoryOnly = (folderName.Equals("Sega", StringComparison.OrdinalIgnoreCase) || 
                                      folderName.Equals("Nintendo", StringComparison.OrdinalIgnoreCase) ||
                                      folderName.Equals("Atari", StringComparison.OrdinalIgnoreCase) ||
                                      folderName.Equals("Capcom", StringComparison.OrdinalIgnoreCase) ||
                                      folderName.Equals("SNK", StringComparison.OrdinalIgnoreCase)) 
                                      && !hasGamesAtThisLevel && !contentSubfolderHasGames && sDirs.Length > 0;

                bool isSingleGameFolder = false;
                if (hasGamesAtThisLevel && gamesAtThisLevel.Count == 1)
                {
                    string singleGameName = Path.GetFileNameWithoutExtension(gamesAtThisLevel[0]);
                    if (singleGameName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    {
                        isSingleGameFolder = true;
                    }
                }

                if ((hasGamesAtThisLevel || hasRegionSubdirs || looksLikePlatform || hasHighDensity || contentSubfolderHasGames) 
                     && !isCategoryOnly && !isSingleGameFolder)
                {
                    platformDirs.Add(cleanPath);
                }
                else
                {
                    foreach (var sd in sDirs) FindPlatformsRecursive(sd);
                }
            }

            FindPlatformsRecursive(rootPath);

            using (var context = new AppDbContext())
            {
                var allFinalDirs = platformDirs.Distinct().ToList();
                foreach (var pDir in allFinalDirs)
                {
                    if (ct.IsCancellationRequested) break;
                    
                    string pName = Path.GetFileName(pDir);
                    if (string.IsNullOrEmpty(pName)) continue;
                    
                    progress.Report(new ScanProgress { Detail = $"Importando: {pName}...", Percentage = 0 });

                    Platform? platform = context.Platforms.FirstOrDefault(p => p.Name == pName);
                    if (platform == null)
                    {
                        platform = new Platform { Name = pName, Category = FileHelpers.DetectCategory(pName) };
                        context.Platforms.Add(platform);
                        context.SaveChanges();
                    }

                    var gameFiles = new List<string>();
                    var dirStack = new Stack<string>();
                    dirStack.Push(pDir);

                    while (dirStack.Count > 0)
                    {
                        if (ct.IsCancellationRequested) break;
                        string currentDir = dirStack.Pop();
                        try
                        {
                            foreach (var f in Directory.GetFiles(currentDir))
                            {
                                if (extensions.Contains(Path.GetExtension(f))) gameFiles.Add(f);
                            }
                            foreach (var d in Directory.GetDirectories(currentDir)) dirStack.Push(d);
                        }
                        catch { }
                    }

                    if (gameFiles.Count > 0)
                    {
                        var existingPaths = new HashSet<string>(context.Games
                            .Where(g => g.PlatformId == platform.Id && !string.IsNullOrEmpty(g.RomPath))
                            .Select(g => g.RomPath), StringComparer.OrdinalIgnoreCase);

                        var existingGameKeys = new HashSet<string>(context.Games
                            .Where(g => g.PlatformId == platform.Id)
                            .AsEnumerable()
                            .Select(g => $"{g.Name}|{g.Region}|{g.Languages}"), StringComparer.OrdinalIgnoreCase);

                        var newGames = new List<Game>();
                        var gamesToUpdate = new List<Game>();
                        var drossPatterns = FileHelpers.LoadDrossPatterns();

                        for (int i = 0; i < gameFiles.Count; i++)
                        {
                            if (ct.IsCancellationRequested) break;
                            string filePath = gameFiles[i];
                            if (existingPaths.Contains(filePath)) continue;

                            string fileName = Path.GetFileName(filePath);
                            if (fileName.Equals("lista.txt", StringComparison.OrdinalIgnoreCase)) continue;

                            if (ImportService.IsDross(fileName, drossPatterns.ToArray())) continue;

                            if (i % 20 == 0)
                            {
                                progress.Report(new ScanProgress { 
                                    Percentage = (i * 100) / gameFiles.Count,
                                    Detail = $"[{pName}] {i}/{gameFiles.Count}: {fileName}" 
                                });
                            }

                            await Task.Delay(1);
                            var game = ImportService.ParseGameLine(fileName, platform.Id);
                            game.RomPath = filePath;
                            game.DateAdded = DateTime.Now;
                            
                            string uniqueKey = $"{game.Name}|{game.Region}|{game.Languages}";
                            if (!existingGameKeys.Contains(uniqueKey))
                            {
                                newGames.Add(game);
                                existingGameKeys.Add(uniqueKey);
                                gameCount++;
                            }
                            else
                            {
                                var existingGame = context.Games
                                    .AsEnumerable()
                                    .FirstOrDefault(g => g.PlatformId == platform.Id && 
                                                        g.Name.Equals(game.Name, StringComparison.OrdinalIgnoreCase) && 
                                                        g.Region == game.Region && 
                                                        g.Languages == game.Languages);
                                                        
                                if (existingGame != null && string.IsNullOrEmpty(existingGame.RomPath))
                                {
                                    existingGame.RomPath = filePath;
                                    gamesToUpdate.Add(existingGame);
                                }
                            }

                            if (newGames.Count >= 500)
                            {
                                _gameService.AddGamesBatch(newGames);
                                newGames.Clear();
                            }
                            if (gamesToUpdate.Count >= 500)
                            {
                                _gameService.UpdateGamesBatch(gamesToUpdate);
                                gamesToUpdate.Clear();
                            }
                        }

                        if (newGames.Any()) _gameService.AddGamesBatch(newGames);
                        if (gamesToUpdate.Any()) _gameService.UpdateGamesBatch(gamesToUpdate);
                    }
                    platform.LastScanDate = DateTime.Now;
                    context.Platforms.Update(platform);
                    context.SaveChanges();
                }
            }
        }

        public async Task ScanExternalLibraryAsync(string lbPath, IProgress<ScanProgress> progress, CancellationToken ct)
        {
            string platformsPath = Path.Combine(lbPath, "Data", "Platforms");
            if (!Directory.Exists(platformsPath)) return;

            var xmlFiles = Directory.GetFiles(platformsPath, "*.xml");
            int totalGamesAdded = 0;

            for (int i = 0; i < xmlFiles.Length; i++)
            {
                if (ct.IsCancellationRequested) break;
                
                string xmlFile = xmlFiles[i];
                string platformName = Path.GetFileNameWithoutExtension(xmlFile);
                
                progress.Report(new ScanProgress { 
                    Percentage = (i * 100) / xmlFiles.Length,
                    Detail = $"Procesando plataforma: {platformName}..." 
                });

                try
                {
                    Platform? platform;
                    using (var context = new AppDbContext())
                    {
                        platform = context.Platforms.FirstOrDefault(p => p.Name == platformName);
                        if (platform == null)
                        {
                            platform = new Platform { Name = platformName, Category = FileHelpers.DetectCategory(platformName) };
                            context.Platforms.Add(platform);
                            context.SaveChanges();
                        }
                    }

                    var doc = XDocument.Load(xmlFile);
                    var gamesNodes = doc.Descendants("Game").ToList();
                    var gamesToImport = new List<Game>();
                    var gamesToUpdate = new List<Game>();
                    
                    using (var context = new AppDbContext())
                    {
                        // Cargar todos los juegos existentes de la plataforma en un diccionario en memoria
                        var existingGames = context.Games
                            .Where(g => g.PlatformId == platform.Id)
                            .ToList();
                        
                        var existingGamesDict = new Dictionary<string, Game>(StringComparer.OrdinalIgnoreCase);
                        foreach (var g in existingGames)
                        {
                            string key = $"{g.Name}|{g.Region}";
                            if (!existingGamesDict.ContainsKey(key))
                            {
                                existingGamesDict[key] = g;
                            }
                        }

                        foreach (var node in gamesNodes)
                        {
                            if (ct.IsCancellationRequested) break;

                            string title = node.Element("Title")?.Value ?? "";
                            if (string.IsNullOrEmpty(title)) continue;

                            string region = node.Element("Region")?.Value ?? "🌎 World";
                            if (string.IsNullOrEmpty(region)) region = "🌎 World";
                            else if (region.Contains("Japan", StringComparison.OrdinalIgnoreCase)) region = "🇯🇵 JP";
                            else if (region.Contains("United States", StringComparison.OrdinalIgnoreCase) || region.Contains("North America", StringComparison.OrdinalIgnoreCase)) region = "🇺🇸 US";
                            else if (region.Contains("Europe", StringComparison.OrdinalIgnoreCase)) region = "🇪🇺 EU";
                            else if (region.Contains("Spain", StringComparison.OrdinalIgnoreCase)) region = "🇪🇸 ES";

                            string uniqueKey = $"{title}|{region}";
                            
                            string raId = node.Element("RetroAchievementsId")?.Value ?? "";
                            string developer = node.Element("Developer")?.Value ?? "";
                            string publisher = node.Element("Publisher")?.Value ?? "";
                            string notes = node.Element("Notes")?.Value ?? "";
                            string wikiUrl = node.Element("WikipediaURL")?.Value ?? "";
                            string videoUrl = node.Element("VideoUrl")?.Value ?? "";
                            int? maxPlayers = null;
                            if (int.TryParse(node.Element("MaxPlayers")?.Value, out int mp)) maxPlayers = mp;
                            string releaseDate = node.Element("ReleaseDate")?.Value ?? "";
                            string communityRating = node.Element("CommunityStarRating")?.Value ?? "";
                            int commRatingCount = 0;
                            if (int.TryParse(node.Element("CommunityStarRatingTotalVotes")?.Value, out int crc)) commRatingCount = crc;

                            if (existingGamesDict.TryGetValue(uniqueKey, out var existingGame))
                            {
                                bool needsUpdate = false;
                                
                                // Si no tiene ID de logros, y el XML sí lo tiene, actualizamos
                                if (string.IsNullOrEmpty(existingGame.ExternalDbId) && !string.IsNullOrEmpty(raId))
                                {
                                    existingGame.ExternalDbId = raId;
                                    needsUpdate = true;
                                }

                                // Enriquecer otros campos vacíos si existen en el XML
                                if (string.IsNullOrEmpty(existingGame.Developer) && !string.IsNullOrEmpty(developer)) { existingGame.Developer = developer; needsUpdate = true; }
                                if (string.IsNullOrEmpty(existingGame.Publisher) && !string.IsNullOrEmpty(publisher)) { existingGame.Publisher = publisher; needsUpdate = true; }
                                if (string.IsNullOrEmpty(existingGame.Description) && !string.IsNullOrEmpty(notes)) { existingGame.Description = notes; needsUpdate = true; }
                                if (string.IsNullOrEmpty(existingGame.WikipediaURL) && !string.IsNullOrEmpty(wikiUrl)) { existingGame.WikipediaURL = wikiUrl; needsUpdate = true; }
                                if (string.IsNullOrEmpty(existingGame.VideoURL) && !string.IsNullOrEmpty(videoUrl)) { existingGame.VideoURL = videoUrl; needsUpdate = true; }
                                if (existingGame.MaxPlayers == null && maxPlayers != null) { existingGame.MaxPlayers = maxPlayers; needsUpdate = true; }
                                if (string.IsNullOrEmpty(existingGame.ReleaseDate) && !string.IsNullOrEmpty(releaseDate)) { existingGame.ReleaseDate = releaseDate; needsUpdate = true; }
                                if (string.IsNullOrEmpty(existingGame.CommunityRating) && !string.IsNullOrEmpty(communityRating)) { existingGame.CommunityRating = communityRating; needsUpdate = true; }
                                if (existingGame.CommunityRatingCount == 0 && commRatingCount != 0) { existingGame.CommunityRatingCount = commRatingCount; needsUpdate = true; }

                                if (needsUpdate)
                                {
                                    gamesToUpdate.Add(existingGame);
                                }
                                continue;
                            }

                            string genre = node.Element("Genre")?.Value ?? "";
                            string appPath = node.Element("ApplicationPath")?.Value ?? "";
                            
                            if (!string.IsNullOrEmpty(appPath) && !Path.IsPathRooted(appPath))
                            {
                                appPath = Path.GetFullPath(Path.Combine(lbPath, appPath));
                            }

                            int year = 0;
                            string relDate = node.Element("ReleaseDate")?.Value ?? "";
                            if (!string.IsNullOrEmpty(relDate) && DateTime.TryParse(relDate, out var dt))
                            {
                                year = dt.Year;
                            }

                            bool isFavorite = (node.Element("Favorite")?.Value ?? "").Equals("true", StringComparison.OrdinalIgnoreCase);

                            gamesToImport.Add(new Game
                            {
                                PlatformId = platform.Id,
                                Name = title,
                                Region = region,
                                Year = year,
                                Genre = genre,
                                RomPath = appPath,
                                IsFavorite = isFavorite,
                                DateAdded = DateTime.Now,
                                Developer = developer,
                                Publisher = publisher,
                                Description = notes,
                                WikipediaURL = wikiUrl,
                                VideoURL = videoUrl,
                                MaxPlayers = maxPlayers,
                                ReleaseDate = releaseDate,
                                CommunityRating = communityRating,
                                CommunityRatingCount = commRatingCount,
                                ExternalDbId = raId
                            });

                            if (gamesToImport.Count >= 500)
                            {
                                _gameService.AddGamesBatch(gamesToImport);
                                totalGamesAdded += gamesToImport.Count;
                                gamesToImport.Clear();
                            }

                            if (gamesToUpdate.Count >= 500)
                            {
                                _gameService.UpdateGamesMetadataBatch(gamesToUpdate);
                                gamesToUpdate.Clear();
                            }
                        }

                        if (gamesToImport.Any())
                        {
                            _gameService.AddGamesBatch(gamesToImport);
                            totalGamesAdded += gamesToImport.Count;
                        }

                        if (gamesToUpdate.Any())
                        {
                            _gameService.UpdateGamesMetadataBatch(gamesToUpdate);
                        }
                    }

                }
                catch { }
            }
        }

        public async Task ScanCoversAsync(Platform platform, string coverPath, IProgress<ScanProgress> progress, CancellationToken ct)
        {
            var extensions = new[] { ".png", ".jpg", ".jpeg" };
            var coverFiles = new List<(string Path, string Type)>();
            
            string platformSpecificPath = Path.Combine(coverPath, platform.Name);
            string effectiveScanPath = Directory.Exists(platformSpecificPath) ? platformSpecificPath : coverPath;

            var stack = new Stack<string>();
            stack.Push(effectiveScanPath);

            while (stack.Count > 0)
            {
                if (ct.IsCancellationRequested) return;
                string currentPath = stack.Pop();
                try
                {
                    string relativePath = Path.GetRelativePath(effectiveScanPath, currentPath);
                    string detectedType = "Box"; 

                    if (relativePath != ".")
                    {
                        string firstSubfolder = relativePath.Split(Path.DirectorySeparatorChar)[0];
                        detectedType = firstSubfolder;
                    }

                    if (detectedType.Contains("3D", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_3D";
                    else if (detectedType.Contains("Logo", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Clear", StringComparison.OrdinalIgnoreCase)) detectedType = "Logos";
                    else if (detectedType.Contains("Snap", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Screen", StringComparison.OrdinalIgnoreCase)) detectedType = "Snap";
                    else if (detectedType.Contains("Cart", StringComparison.OrdinalIgnoreCase)) detectedType = "Cart_Front";
                    else if (detectedType.Contains("Disc", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Support", StringComparison.OrdinalIgnoreCase)) detectedType = "Disc";
                    else if (detectedType.Contains("Fanart", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Back", StringComparison.OrdinalIgnoreCase)) detectedType = "Fanart - Background";
                    else if (detectedType.Contains("Full", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_Full";
                    else if (detectedType.Contains("Front", StringComparison.OrdinalIgnoreCase)) detectedType = "Box";

                    foreach (var f in Directory.GetFiles(currentPath))
                    {
                        if (extensions.Contains(Path.GetExtension(f).ToLower()))
                            coverFiles.Add((f, detectedType));
                    }
                    foreach (var d in Directory.GetDirectories(currentPath)) stack.Push(d);
                } catch { }
            }

            using (var context = new AppDbContext())
            {
                var games = context.Games.Where(g => g.PlatformId == platform.Id).ToList();
                var gamesToUpdate = new List<Game>();

                for (int i = 0; i < games.Count; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    var game = games[i];
                    
                    progress.Report(new ScanProgress { 
                        Percentage = (i * 100) / games.Count,
                        Detail = $"Procesando: {game.Name} ({i}/{games.Count})" 
                    });

                    string cleanGameName = Regex.Replace(game.Name, @"[^a-zA-Z0-9]", "").ToLower();
                    var matches = coverFiles.Where(f => {
                        string fileName = Path.GetFileNameWithoutExtension(f.Path);
                        string cleanFileName = Regex.Replace(fileName, @"[^a-zA-Z0-9]", "").ToLower();
                        return cleanFileName == cleanGameName || cleanFileName.Contains(cleanGameName) || cleanGameName.Contains(cleanFileName);
                    }).ToList();

                    if (matches.Any())
                    {
                        var mainMatch = matches.Any(m => m.Type == "Box_3D") 
                                       ? matches.First(m => m.Type == "Box_3D") 
                                       : (matches.Any(m => m.Type == "Box") ? matches.First(m => m.Type == "Box") : matches.First());

                        if (game.Cover == null || game.Cover.Length == 0 || mainMatch.Type == "Box_3D")
                        {
                            game.Cover = File.ReadAllBytes(mainMatch.Path);
                            game.CoverType = mainMatch.Type;
                        }

                        if (game.ExtraImages == null) game.ExtraImages = new List<GameImage>();
                        foreach (var m in matches)
                        {
                            if (m.Path == mainMatch.Path) continue;
                            if (!game.ExtraImages.Any(ei => ei.ImageType == m.Type))
                            {
                                game.ExtraImages.Add(new GameImage { ImageType = m.Type, ImageData = File.ReadAllBytes(m.Path) });
                            }
                        }
                        gamesToUpdate.Add(game);
                        if (gamesToUpdate.Count >= 25) { _gameService.UpdateGamesBatch(gamesToUpdate); gamesToUpdate.Clear(); }
                    }
                }
                if (gamesToUpdate.Any()) _gameService.UpdateGamesBatch(gamesToUpdate);
            }
        }

        public async Task ScanMassiveCoversAsync(string rootPath, IProgress<ScanProgress> progress, CancellationToken ct)
        {
            var extensions = new[] { ".png", ".jpg", ".jpeg" };
            using (var context = new AppDbContext())
            {
                var allPlatforms = context.Platforms.Include(p => p.AlternateNames).ToList();
                for (int p = 0; p < allPlatforms.Count; p++)
                {
                    if (ct.IsCancellationRequested) break;
                    var plat = allPlatforms[p];
                    string platformPath = "";
                    var possibleFolderNames = new List<string> { plat.Name };
                    if (plat.AlternateNames != null) possibleFolderNames.AddRange(plat.AlternateNames.Select(a => a.AlternateName));

                    foreach (var folderName in possibleFolderNames)
                    {
                        string checkPath = Path.Combine(rootPath, folderName);
                        if (Directory.Exists(checkPath)) { platformPath = checkPath; break; }
                    }

                    if (string.IsNullOrEmpty(platformPath)) continue;

                    progress.Report(new ScanProgress { 
                        Percentage = (p * 100) / allPlatforms.Count,
                        Detail = $"Plataforma ({p + 1}/{allPlatforms.Count}): {plat.Name}" 
                    });

                    var imageMap = new Dictionary<string, List<(string Path, string Type)>>(StringComparer.OrdinalIgnoreCase);
                    var stack = new Stack<string>();
                    stack.Push(platformPath);

                    while (stack.Count > 0)
                    {
                        string currentPath = stack.Pop();
                        try
                        {
                            string relativePath = Path.GetRelativePath(platformPath, currentPath);
                            string detectedType = "Box"; 
                            if (relativePath != ".") detectedType = relativePath.Split(Path.DirectorySeparatorChar)[0];

                            if (detectedType.Contains("3D", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_3D";
                            else if (detectedType.Contains("Logo", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Clear", StringComparison.OrdinalIgnoreCase)) detectedType = "Logos";
                            else if (detectedType.Contains("Snap", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Screen", StringComparison.OrdinalIgnoreCase)) detectedType = "Snap";
                            else if (detectedType.Contains("Cart", StringComparison.OrdinalIgnoreCase)) detectedType = "Cart_Front";
                            else if (detectedType.Contains("Disc", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Support", StringComparison.OrdinalIgnoreCase)) detectedType = "Disc";
                            else if (detectedType.Contains("Fanart", StringComparison.OrdinalIgnoreCase) || detectedType.Contains("Back", StringComparison.OrdinalIgnoreCase)) detectedType = "Fanart - Background";
                            else if (detectedType.Contains("Full", StringComparison.OrdinalIgnoreCase)) detectedType = "Box_Full";
                            else if (detectedType.Contains("Front", StringComparison.OrdinalIgnoreCase)) detectedType = "Box";

                            foreach (var f in Directory.GetFiles(currentPath))
                            {
                                if (extensions.Contains(Path.GetExtension(f).ToLower()))
                                {
                                    string fileName = Path.GetFileNameWithoutExtension(f);
                                    if (fileName.Length > 3 && fileName[fileName.Length-3] == '-') fileName = fileName.Substring(0, fileName.Length - 3);
                                    string cleanKey = Regex.Replace(fileName, @"[^a-zA-Z0-9]", "").ToLower();
                                    if (string.IsNullOrEmpty(cleanKey)) continue;
                                    if (!imageMap.ContainsKey(cleanKey)) imageMap[cleanKey] = new List<(string, string)>();
                                    imageMap[cleanKey].Add((f, detectedType));
                                }
                            }
                            foreach (var d in Directory.GetDirectories(currentPath)) stack.Push(d);
                        } catch { }
                    }

                    var sortedKeys = imageMap.Keys.OrderBy(k => k).ToList();
                    var games = context.Games.Where(g => g.PlatformId == plat.Id).ToList();
                    
                    ILookup<int, string> existingImagesMap;
                    using (var coversContext = new CoversDbContext())
                    {
                        var gameIds = games.Select(g => g.Id).ToList();
                        existingImagesMap = coversContext.Images.Where(img => gameIds.Contains(img.GameId)).Select(img => new { img.GameId, img.ImageType }).ToLookup(x => x.GameId, x => x.ImageType);
                    }

                    var gamesToUpdateBatch = new List<Game>();
                    for (int i = 0; i < games.Count; i++)
                    {
                        if (ct.IsCancellationRequested) break;
                        var game = games[i];
                        string cleanGameName = Regex.Replace(game.Name, @"[^a-zA-Z0-9]", "").ToLower();
                        if (string.IsNullOrEmpty(cleanGameName)) continue;

                        List<(string Path, string Type)> matches = new List<(string, string)>();
                        if (imageMap.TryGetValue(cleanGameName, out var exactMatches)) matches.AddRange(exactMatches);
                        int idx = sortedKeys.BinarySearch(cleanGameName, StringComparer.OrdinalIgnoreCase);
                        if (idx < 0) idx = ~idx;

                        for (int j = idx; j < sortedKeys.Count; j++)
                        {
                            if (sortedKeys[j].StartsWith(cleanGameName, StringComparison.OrdinalIgnoreCase)) matches.AddRange(imageMap[sortedKeys[j]]);
                            else break;
                        }
                        for (int j = idx - 1; j >= 0; j--)
                        {
                            if (cleanGameName.StartsWith(sortedKeys[j], StringComparison.OrdinalIgnoreCase) && sortedKeys[j].Length > 3) matches.AddRange(imageMap[sortedKeys[j]]);
                            else if (!sortedKeys[j].StartsWith(cleanGameName.Substring(0, Math.Min(3, cleanGameName.Length)))) break;
                        }

                        if (matches.Any())
                        {
                            matches = matches.GroupBy(m => m.Path).Select(g => g.First()).ToList();
                            var mainMatch = matches.OrderByDescending(m => m.Type == "Box_3D").ThenByDescending(m => m.Type == "Box").First();
                            bool needsUpdate = false;
                            if (game.Cover == null || game.Cover.Length == 0 || (mainMatch.Type == "Box_3D" && game.CoverType != "Box_3D"))
                            {
                                game.Cover = File.ReadAllBytes(mainMatch.Path);
                                game.CoverType = mainMatch.Type;
                                needsUpdate = true;
                            }
                            var currentExtras = existingImagesMap[game.Id].ToHashSet();
                            foreach (var m in matches)
                            {
                                if (m.Path == mainMatch.Path) continue;
                                if (!currentExtras.Contains(m.Type))
                                {
                                    game.ExtraImages.Add(new GameImage { GameId = game.Id, ImageType = m.Type, ImageData = File.ReadAllBytes(m.Path) });
                                    currentExtras.Add(m.Type);
                                    needsUpdate = true;
                                }
                            }
                            if (needsUpdate) { gamesToUpdateBatch.Add(game); if (gamesToUpdateBatch.Count >= 50) { _gameService.UpdateGamesBatch(gamesToUpdateBatch); gamesToUpdateBatch.Clear(); } }
                        }
                    }
                    if (gamesToUpdateBatch.Any()) _gameService.UpdateGamesBatch(gamesToUpdateBatch);
                }
            }
        }

        private HashSet<string> LoadBlacklist()
        {
            var blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                "Games", "Roms", "CHDs", "Samples", "Artwork", "Bios", "System",
                "Preproduction", "Add-Ons", "Educational", "Applications", "Demos", 
                "Video", "Miscellaneous", "Manuals", "Media", "Images", "Covers",
                "Spain", "España", "Europe", "USA", "Japan", "World", "Asia", "Korea", "Japón", "Europa"
            };

            try
            {
                string blacklistPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blacklist.json");
                if (File.Exists(blacklistPath))
                {
                    var json = File.ReadAllText(blacklistPath);
                    var loadedList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                    if (loadedList != null)
                    {
                        foreach (var item in loadedList) blacklist.Add(item);
                    }
                }
            }
            catch { }
            return blacklist;
        }
    }
}
