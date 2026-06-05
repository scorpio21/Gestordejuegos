using System.Collections.Generic;
using System.Linq;
using GestorJuegos.Data;
using GestorJuegos.Models;
using Microsoft.EntityFrameworkCore;
using GestorJuegos.Utils;
using System;

namespace GestorJuegos.Services
{
    public class GameService
    {
        private static bool _schemaUpdated = false;

        public GameService()
        {
            if (!_schemaUpdated)
            {
                using (var context = new AppDbContext())
                {
                    context.Database.EnsureCreated();
                    // Migraciones existentes...
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN SelectedArtType TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Languages TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Developer TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Publisher TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Description TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideEmulatorPath TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN EmulatorPath TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN LaunchArguments TEXT NOT NULL DEFAULT '\"{0}\"'"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideEmulatorPath TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideLaunchArguments TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN AdditionalRoms TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN IsFavorite INTEGER NOT NULL DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN PlayTime INTEGER NOT NULL DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN PlayCount INTEGER NOT NULL DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Rating INTEGER NOT NULL DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN PlayStatus TEXT NOT NULL DEFAULT 'No Jugado'"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN LastPlayed TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN DateAdded TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN ShortName TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Version TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN ExternalDbId TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN ReleaseDate TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN ReleaseType TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN MaxPlayers INTEGER"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN Cooperative INTEGER DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN VideoURL TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN WikipediaURL TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN ESRB TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN CommunityRating TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN CommunityRatingCount INTEGER DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN LastScanDate TEXT;"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Logo BLOB"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Icon BLOB"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN HardwareImage BLOB"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN ReleaseDate TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Developer TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Manufacturer TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Cpu TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Memory TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Graphics TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Sound TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Display TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Media TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Notes TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN Emulated INTEGER NOT NULL DEFAULT 1"); } catch { }
                    try { context.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS PlatformCategories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Icon BLOB, Graphic BLOB);"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE PlatformCategories ADD COLUMN Notes TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("DELETE FROM Games WHERE PlatformId NOT IN (SELECT Id FROM Platforms)"); } catch { }
                }

                using (var coversContext = new CoversDbContext())
                {
                    coversContext.Database.EnsureCreated();
                    try { coversContext.Database.ExecuteSqlRaw("ALTER TABLE Covers ADD COLUMN ImageType TEXT NOT NULL DEFAULT 'Box - Front'"); } catch { }
                    try { coversContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Images (Id INTEGER PRIMARY KEY AUTOINCREMENT, GameId INTEGER NOT NULL, ImageType TEXT NOT NULL, ImageData BLOB);"); } catch { }
                    try { coversContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Images_GameId_ImageType ON Images (GameId, ImageType);"); } catch { }
                }

                // Migrar portadas si es necesario
                MigrateCoversToNewDb();
                
                _schemaUpdated = true;
            }
        }

        private readonly ExternalMetadataService _metadataService = new();

        public void EnrichGameWithMetadata(Game game, string platformName)
        {
            var metadata = _metadataService.GetMetadata(game.Name, platformName);
            if (metadata != null)
            {
                game.ExternalDbId = metadata.DatabaseID.ToString();
                game.Description = metadata.Description;
                game.Year = metadata.ReleaseYear;
                game.Developer = metadata.Developer;
                game.Publisher = metadata.Publisher;
                game.Genre = metadata.Genres;
                
                // Nuevos campos
                game.ReleaseDate = metadata.ReleaseDate;
                game.MaxPlayers = metadata.MaxPlayers;
                game.Cooperative = metadata.Cooperative;
                game.VideoURL = metadata.VideoURL;
                game.WikipediaURL = metadata.WikipediaURL;
                game.ESRB = metadata.ESRB;
                game.CommunityRating = metadata.CommunityRating;
                game.CommunityRatingCount = metadata.CommunityRatingCount;
            }
        }

        public void EnrichPlatformWithMetadata(Platform platform)
        {
            var metadata = _metadataService.GetPlatformMetadata(platform.Name);
            if (metadata != null)
            {
                platform.ReleaseDate = metadata.ReleaseDate;
                platform.Developer = metadata.Developer;
                platform.Manufacturer = metadata.Manufacturer;
                platform.Cpu = metadata.Cpu;
                platform.Memory = metadata.Memory;
                platform.Graphics = metadata.Graphics;
                platform.Sound = metadata.Sound;
                platform.Display = metadata.Display;
                platform.Media = metadata.Media;
                platform.Notes = metadata.Notes;
            }
        }

        public byte[]? GetGameExtraImage(int gameId, string type)
        {
            using var context = new CoversDbContext();
            
            // 1. Buscar en la tabla de imágenes adicionales
            var extra = context.Images
                .Where(i => i.GameId == gameId && i.ImageType == type)
                .Select(i => i.ImageData)
                .FirstOrDefault();

            if (extra != null) return extra;

            // 2. Buscar en la tabla de carátulas principales, verificando el tipo
            var cover = context.Covers.Find(gameId);
            if (cover != null && cover.ImageType == type)
            {
                return cover.ImageData;
            }

            return null;
        }

        public void SaveGameImage(int gameId, string type, byte[] data)
        {
            using var context = new CoversDbContext();
            var existing = context.Images.FirstOrDefault(i => i.GameId == gameId && i.ImageType == type);
            if (existing != null)
            {
                existing.ImageData = data;
                context.Images.Update(existing);
            }
            else
            {
                context.Images.Add(new GameImage { GameId = gameId, ImageType = type, ImageData = data });
            }
            context.SaveChanges();
        }

        public void SaveGameImagesBatch(List<GameImage> images)
        {
            using var context = new CoversDbContext();
            foreach (var img in images)
            {
                var existing = context.Images.FirstOrDefault(i => i.GameId == img.GameId && i.ImageType == img.ImageType);
                if (existing != null) existing.ImageData = img.ImageData;
                else context.Images.Add(img);
            }
            context.SaveChanges();
        }

        private void MigrateCoversToNewDb()
        {
            using (var context = new AppDbContext())
            using (var coversContext = new CoversDbContext())
            {
                // Usamos ADO.NET directo para saltarnos las restricciones de [NotMapped] de EF Core
                var connection = context.Database.GetDbConnection();
                bool hasOpened = false;
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                    hasOpened = true;
                }

                try
                {
                    // Verificar si la columna Cover existe antes de intentar la migración
                    bool hasCoverColumn = false;
                    using (var checkCommand = connection.CreateCommand())
                    {
                        checkCommand.CommandText = "PRAGMA table_info(Games)";
                        using (var reader = checkCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["name"].ToString() == "Cover")
                                {
                                    hasCoverColumn = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!hasCoverColumn) return;

                    using (var command = connection.CreateCommand())
                    {
                        // Seleccionamos los juegos que tienen datos en la columna Cover de la DB principal
                        command.CommandText = "SELECT Id, Cover FROM Games WHERE Cover IS NOT NULL";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                byte[]? coverData = reader[1] as byte[];

                                if (coverData != null && coverData.Length > 0)
                                {
                                    if (!coversContext.Covers.Any(c => c.Id == id))
                                    {
                                        coversContext.Covers.Add(new GameCover
                                        {
                                            Id = id,
                                            ImageData = coverData,
                                            ThumbnailData = ImageHelper.GenerateThumbnail(coverData)
                                        });
                                    }
                                }
                            }
                        }
                    }
                    
                    coversContext.SaveChanges();

                    // Limpiar la columna Cover de la DB principal y compactar
                    using (var updateCommand = connection.CreateCommand())
                    {
                        updateCommand.CommandText = "UPDATE Games SET Cover = NULL WHERE Cover IS NOT NULL";
                        updateCommand.ExecuteNonQuery();
                    }
                    
                    try { context.Database.ExecuteSqlRaw("VACUUM"); } catch { }
                }
                finally
                {
                    if (hasOpened) connection.Close();
                }
            }
        }

        public byte[]? GetGameThumbnail(int gameId, string? artType = null)
        {
            using var coversContext = new CoversDbContext();

            // Si se especifica un tipo, intentar buscarlo estrictamente
            if (!string.IsNullOrEmpty(artType))
            {
                // 1. Buscar en la tabla de imágenes adicionales
                var extraThumb = coversContext.Images
                    .Where(i => i.GameId == gameId && i.ImageType == artType)
                    .Select(i => i.ImageData)
                    .FirstOrDefault();

                if (extraThumb != null) return extraThumb;

                // 2. Buscar en carátulas principales solo si el tipo coincide
                var cover = coversContext.Covers.Find(gameId);
                if (cover != null && cover.ImageType == artType)
                {
                    return cover.ThumbnailData ?? cover.ImageData;
                }

                // Si se pidió un tipo específico y no se encontró, NO hacer fallback
                return null;
            }

            // Si NO se especifica tipo (o es nulo), devolver el thumbnail de la carátula principal (comportamiento por defecto)
            return coversContext.Covers.Where(c => c.Id == gameId).Select(c => c.ThumbnailData).FirstOrDefault();
        }

        public byte[]? GetGameFullCover(int gameId)
        {
            using var coversContext = new CoversDbContext();
            return coversContext.Covers.Where(c => c.Id == gameId).Select(c => c.ImageData).FirstOrDefault();
        }

        public GameCover? GetGameCover(int gameId)
        {
            using var coversContext = new CoversDbContext();
            return coversContext.Covers.Find(gameId);
        }

        public List<Platform> GetPlatforms()
        {
            using var context = new AppDbContext();
            return context.Platforms.OrderBy(p => p.Name).ToList();
        }

        public void AddPlatform(Platform platform)
        {
            if (string.IsNullOrWhiteSpace(platform.Name)) return;

            using var context = new AppDbContext();
            // Evitar duplicados por nombre
            if (context.Platforms.Any(p => p.Name.ToLower() == platform.Name.ToLower()))
            {
                return;
            }

            context.Platforms.Add(platform);
            context.SaveChanges();
        }

        public void UpdatePlatform(Platform platform)
        {
            using var context = new AppDbContext();
            context.Platforms.Update(platform);
            context.SaveChanges();
        }

        public void DeletePlatform(int platformId)
        {
            using var context = new AppDbContext();
            var platform = context.Platforms.Find(platformId);
            if (platform != null)
            {
                var games = context.Games.Where(g => g.PlatformId == platformId).ToList();
                var gameIds = games.Select(g => g.Id).ToList();
                
                context.Games.RemoveRange(games);
                context.Platforms.Remove(platform);
                context.SaveChanges();

                using var coversContext = new CoversDbContext();
                var covers = coversContext.Covers.Where(c => gameIds.Contains(c.Id)).ToList();
                coversContext.Covers.RemoveRange(covers);
                coversContext.SaveChanges();
            }
        }

        public List<Game> GetGamesByPlatform(int platformId)
        {
            using var context = new AppDbContext();
            return context.Games.Where(g => g.PlatformId == platformId).ToList();
        }

        public void AddGame(Game game)
        {
            AddGamesBatch(new List<Game> { game });
        }

        public void AddGamesBatch(List<Game> games)
        {
            if (games == null || games.Count == 0) return;

            const int batchSize = 500;
            for (int i = 0; i < games.Count; i += batchSize)
            {
                var batch = games.Skip(i).Take(batchSize).ToList();
                
                // Asegurar que campos obligatorios en DB tengan valor (evitar NOT NULL constraint failed)
                foreach (var g in batch)
                {
                    if (g.ShortName == null) g.ShortName = string.Empty;
                    if (g.Version == null) g.Version = string.Empty;
                    if (g.SelectedArtType == null) g.SelectedArtType = string.Empty;
                    if (g.Languages == null) g.Languages = string.Empty;
                    if (g.AdditionalRoms == null) g.AdditionalRoms = string.Empty;
                    if (g.OverrideEmulatorPath == null) g.OverrideEmulatorPath = string.Empty;
                    if (g.OverrideLaunchArguments == null) g.OverrideLaunchArguments = string.Empty;
                    if (g.Developer == null) g.Developer = string.Empty;
                    if (g.Publisher == null) g.Publisher = string.Empty;
                    if (g.Description == null) g.Description = string.Empty;
                }
                
                // 1. Guardar primero en la DB principal para generar los IDs
                using (var context = new AppDbContext())
                {
                    context.Games.AddRange(batch);
                    context.SaveChanges(); 
                }

                // 2. Ahora que tenemos IDs, guardar las carátulas e imágenes extra en GestorCovers.db
                using (var coversContext = new CoversDbContext())
                {
                    foreach (var g in batch)
                    {
                        // Portada principal
                        if (g.Cover != null && g.Cover.Length > 0)
                        {
                            coversContext.Covers.Add(new GameCover
                            {
                                Id = g.Id,
                                ImageType = g.CoverType,
                                ImageData = g.Cover,
                                ThumbnailData = ImageHelper.GenerateThumbnail(g.Cover)
                            });
                        }

                        // Imágenes extra
                        if (g.ExtraImages != null && g.ExtraImages.Any())
                        {
                            foreach (var extra in g.ExtraImages)
                            {
                                extra.GameId = g.Id;
                                coversContext.Images.Add(extra);
                            }
                        }
                    }
                    coversContext.SaveChanges();

                    // Limpiar datos de memoria para optimizar
                    foreach (var g in batch)
                    {
                        g.Cover = null;
                        if (g.ExtraImages != null) g.ExtraImages.Clear();
                    }
                }
            }
        }

        public void UpdateGame(Game game)
        {
            UpdateGamesBatch(new List<Game> { game });
        }

        public void UpdateGameMetadata(Game game)
        {
            using (var context = new AppDbContext())
            {
                context.Games.Update(game);
                context.SaveChanges();
            }
        }

        public void UpdateGamesMetadataBatch(List<Game> games)
        {
            using (var context = new AppDbContext())
            {
                context.Games.UpdateRange(games);
                context.SaveChanges();
            }
        }

        public void UpdateGamesBatch(List<Game> games)
        {
            if (games == null || games.Count == 0) return;

            const int batchSize = 500;
            for (int i = 0; i < games.Count; i += batchSize)
            {
                var batch = games.Skip(i).Take(batchSize).ToList();

                // Extraer carátulas e imágenes extra para procesarlas en la DB secundaria
                var coversToUpdate = batch
                    .Where(g => g.Cover != null && g.Cover.Length > 0)
                    .Select(g => new { GameId = g.Id, Data = g.Cover, Type = g.CoverType })
                    .ToList();

                var extraImagesToUpdate = batch
                    .Where(g => g.ExtraImages != null && g.ExtraImages.Any())
                    .Select(g => new { GameId = g.Id, Images = g.ExtraImages })
                    .ToList();

                using (var context = new AppDbContext())
                {
                    context.Games.UpdateRange(batch);
                    context.SaveChanges();
                }

                if (coversToUpdate.Any() || extraImagesToUpdate.Any())
                {
                    using (var coversContext = new CoversDbContext())
                    {
                        foreach (var cu in coversToUpdate)
                        {
                            var existingCover = coversContext.Covers.Find(cu.GameId);
                            if (existingCover == null)
                            {
                                coversContext.Covers.Add(new GameCover
                                {
                                    Id = cu.GameId,
                                    ImageType = cu.Type,
                                    ImageData = cu.Data!,
                                    ThumbnailData = ImageHelper.GenerateThumbnail(cu.Data!)
                                });
                            }
                            else
                            {
                                existingCover.ImageType = cu.Type;
                                existingCover.ImageData = cu.Data!;
                                existingCover.ThumbnailData = ImageHelper.GenerateThumbnail(cu.Data!);
                                coversContext.Covers.Update(existingCover);
                            }
                        }

                        foreach (var extra in extraImagesToUpdate)
                        {
                            // Para imágenes extra, podemos optar por reemplazar todas las de ese juego
                            // o intentar mezclarlas. Por simplicidad en la edición, si se pasan imágenes,
                            // asumimos que son el nuevo set completo para esos tipos.
                            foreach (var img in extra.Images)
                            {
                                var existing = coversContext.Images.FirstOrDefault(i => i.GameId == extra.GameId && i.ImageType == img.ImageType);
                                if (existing != null)
                                {
                                    existing.ImageData = img.ImageData;
                                    coversContext.Images.Update(existing);
                                }
                                else
                                {
                                    img.GameId = extra.GameId;
                                    coversContext.Images.Add(img);
                                }
                            }
                        }
                        coversContext.SaveChanges();
                    }
                }
            }
        }

        public void DeleteGame(int gameId)
        {
            using var context = new AppDbContext();
            var game = context.Games.Find(gameId);
            if (game != null)
            {
                context.Games.Remove(game);
                context.SaveChanges();

                using var coversContext = new CoversDbContext();
                var cover = coversContext.Covers.Find(gameId);
                if (cover != null)
                {
                    coversContext.Covers.Remove(cover);
                    coversContext.SaveChanges();
                }
            }
        }

        public void DeleteGames(List<int> gameIds)
        {
            using var context = new AppDbContext();
            var games = context.Games.Where(g => gameIds.Contains(g.Id)).ToList();
            context.Games.RemoveRange(games);
            context.SaveChanges();

            using var coversContext = new CoversDbContext();
            var covers = coversContext.Covers.Where(c => gameIds.Contains(c.Id)).ToList();
            coversContext.Covers.RemoveRange(covers);
            coversContext.SaveChanges();
        }

        public List<Game> GetOrphanedGames()
        {
            using var context = new AppDbContext();
            return context.Games.AsEnumerable().Where(g => !string.IsNullOrEmpty(g.RomPath) && !System.IO.File.Exists(g.RomPath)).ToList();
        }

        public Dictionary<string, int> GetGamesCountByPlatform()
        {
            using var context = new AppDbContext();
            return context.Platforms
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .Select(p => new { p.Name, Count = context.Games.Count(g => g.PlatformId == p.Id) })
                .AsEnumerable()
                .GroupBy(x => x.Name)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        }

        public Dictionary<string, int> GetGenresWithCount()
        {
            using var context = new AppDbContext();
            return context.Games
                .Where(g => !string.IsNullOrEmpty(g.Genre))
                .AsEnumerable()
                .SelectMany(g => g.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .GroupBy(g => g)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<string, int> GetRegionsWithCount()
        {
            using var context = new AppDbContext();
            return context.Games
                .Where(g => !string.IsNullOrEmpty(g.Region))
                .AsEnumerable() // Forzar evaluación en el cliente
                .GroupBy(g => g.Region)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
