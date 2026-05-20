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
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN RomPath TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN EmulatorPath TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN LaunchArguments TEXT NOT NULL DEFAULT '\"{0}\"'"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideEmulatorPath TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideLaunchArguments TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN AdditionalRoms TEXT NOT NULL DEFAULT ''"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN IsFavorite INTEGER NOT NULL DEFAULT 0"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN DateAdded TEXT"); } catch { }
                    try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN LastScanDate TEXT;"); } catch { }
                    try { context.Database.ExecuteSqlRaw("DELETE FROM Games WHERE PlatformId NOT IN (SELECT Id FROM Platforms)"); } catch { }
                }

                using (var coversContext = new CoversDbContext())
                {
                    coversContext.Database.EnsureCreated();
                }

                // Migrar portadas si es necesario
                MigrateCoversToNewDb();
                
                _schemaUpdated = true;
            }
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

        public byte[]? GetGameThumbnail(int gameId)
        {
            using var coversContext = new CoversDbContext();
            return coversContext.Covers.Where(c => c.Id == gameId).Select(c => c.ThumbnailData).FirstOrDefault();
        }

        public byte[]? GetGameFullCover(int gameId)
        {
            using var coversContext = new CoversDbContext();
            return coversContext.Covers.Where(c => c.Id == gameId).Select(c => c.ImageData).FirstOrDefault();
        }

        public List<Platform> GetPlatforms()
        {
            using var context = new AppDbContext();
            return context.Platforms.OrderBy(p => p.Name).ToList();
        }

        public void AddPlatform(Platform platform)
        {
            using var context = new AppDbContext();
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
                
                // Guardar copias de los datos de carátula antes de limpiarlos del objeto Game
                var coversToProcess = batch
                    .Where(g => g.Cover != null && g.Cover.Length > 0)
                    .Select(g => new { Game = g, Data = g.Cover })
                    .ToList();

                // Limpiar carátulas de los modelos de juego para que no se guarden en la DB principal
                foreach (var g in batch) g.Cover = null;

                using (var context = new AppDbContext())
                {
                    context.Games.AddRange(batch);
                    context.SaveChanges(); // Genera los IDs
                }

                if (coversToProcess.Any())
                {
                    using (var coversContext = new CoversDbContext())
                    {
                        var coversToAdd = coversToProcess.Select(cp => new GameCover
                        {
                            Id = cp.Game.Id,
                            ImageData = cp.Data!,
                            ThumbnailData = ImageHelper.GenerateThumbnail(cp.Data!)
                        }).ToList();

                        coversContext.Covers.AddRange(coversToAdd);
                        coversContext.SaveChanges();
                    }
                }
            }
        }

        public void UpdateGame(Game game)
        {
            UpdateGamesBatch(new List<Game> { game });
        }

        public void UpdateGamesBatch(List<Game> games)
        {
            if (games == null || games.Count == 0) return;

            const int batchSize = 500;
            for (int i = 0; i < games.Count; i += batchSize)
            {
                var batch = games.Skip(i).Take(batchSize).ToList();

                // Extraer carátulas para procesarlas en la DB secundaria
                var coversToUpdate = batch
                    .Where(g => g.Cover != null && g.Cover.Length > 0)
                    .Select(g => new { GameId = g.Id, Data = g.Cover })
                    .ToList();

                using (var context = new AppDbContext())
                {
                    context.Games.UpdateRange(batch);
                    context.SaveChanges();
                }

                if (coversToUpdate.Any())
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
                                    ImageData = cu.Data!,
                                    ThumbnailData = ImageHelper.GenerateThumbnail(cu.Data!)
                                });
                            }
                            else
                            {
                                existingCover.ImageData = cu.Data!;
                                existingCover.ThumbnailData = ImageHelper.GenerateThumbnail(cu.Data!);
                                coversContext.Covers.Update(existingCover);
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
                .Select(p => new { p.Name, Count = p.Games.Count })
                .ToDictionary(x => x.Name, x => x.Count);
        }
    }
}
