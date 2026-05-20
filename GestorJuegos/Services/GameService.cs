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
                var gamesWithCoversInMainDb = context.Games.Where(g => g.Cover != null).ToList();
                if (gamesWithCoversInMainDb.Count > 0)
                {
                    foreach (var game in gamesWithCoversInMainDb)
                    {
                        if (game.Cover != null)
                        {
                            if (!coversContext.Covers.Any(c => c.Id == game.Id))
                            {
                                coversContext.Covers.Add(new GameCover
                                {
                                    Id = game.Id,
                                    ImageData = game.Cover,
                                    ThumbnailData = ImageHelper.GenerateThumbnail(game.Cover)
                                });
                            }
                            game.Cover = null;
                        }
                    }
                    coversContext.SaveChanges();
                    context.SaveChanges();
                    try { context.Database.ExecuteSqlRaw("VACUUM"); } catch { }
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
            byte[]? coverData = game.Cover;
            game.Cover = null; 

            using var context = new AppDbContext();
            context.Games.Add(game);
            context.SaveChanges();

            if (coverData != null)
            {
                using var coversContext = new CoversDbContext();
                coversContext.Covers.Add(new GameCover
                {
                    Id = game.Id,
                    ImageData = coverData,
                    ThumbnailData = ImageHelper.GenerateThumbnail(coverData)
                });
                coversContext.SaveChanges();
            }
        }

        public void UpdateGame(Game game)
        {
            byte[]? coverData = game.Cover;
            game.Cover = null;

            using var context = new AppDbContext();
            context.Games.Update(game);
            context.SaveChanges();

            using var coversContext = new CoversDbContext();
            var existingCover = coversContext.Covers.Find(game.Id);
            if (coverData != null)
            {
                if (existingCover == null)
                {
                    coversContext.Covers.Add(new GameCover
                    {
                        Id = game.Id,
                        ImageData = coverData,
                        ThumbnailData = ImageHelper.GenerateThumbnail(coverData)
                    });
                }
                else
                {
                    existingCover.ImageData = coverData;
                    existingCover.ThumbnailData = ImageHelper.GenerateThumbnail(coverData);
                    coversContext.Covers.Update(existingCover);
                }
                coversContext.SaveChanges();
            }
            else if (existingCover != null)
            {
                coversContext.Covers.Remove(existingCover);
                coversContext.SaveChanges();
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
