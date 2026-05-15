using System.Collections.Generic;
using System.Linq;
using GestorJuegos.Data;
using GestorJuegos.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorJuegos.Services
{
    public class GameService
    {
        private static bool _schemaUpdated = false;

        public GameService()
        {
            if (!_schemaUpdated)
            {
                using var context = new AppDbContext();
                context.Database.EnsureCreated();
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN RomPath TEXT NOT NULL DEFAULT ''"); } catch (System.Exception ex) { System.Console.WriteLine("Migración RomPath: " + ex.Message); }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN EmulatorPath TEXT NOT NULL DEFAULT ''"); } catch (System.Exception ex) { System.Console.WriteLine("Migración EmulatorPath: " + ex.Message); }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Platforms ADD COLUMN LaunchArguments TEXT NOT NULL DEFAULT '\"{{0}}\"'"); } catch (System.Exception ex) { System.Console.WriteLine("Migración LaunchArgs: " + ex.Message); }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideEmulatorPath TEXT NOT NULL DEFAULT ''"); } catch (System.Exception ex) { System.Console.WriteLine("Migración OverrideEmulatorPath: " + ex.Message); }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Games ADD COLUMN OverrideLaunchArguments TEXT NOT NULL DEFAULT ''"); } catch (System.Exception ex) { System.Console.WriteLine("Migración OverrideLaunchArgs: " + ex.Message); }
                
                // Limpiar juegos huérfanos (por si se eliminó una plataforma en el pasado sin borrar sus juegos)
                try { context.Database.ExecuteSqlRaw("DELETE FROM Games WHERE PlatformId NOT IN (SELECT Id FROM Platforms)"); } catch { }
                
                _schemaUpdated = true;
            }
        }

        public List<Platform> GetPlatforms()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            return context.Platforms.ToList();
        }

        public void AddPlatform(Platform platform)
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
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
                // Eliminar explícitamente los juegos asociados a esta plataforma
                var games = context.Games.Where(g => g.PlatformId == platformId).ToList();
                if (games.Count > 0)
                {
                    context.Games.RemoveRange(games);
                }
                
                context.Platforms.Remove(platform);
                context.SaveChanges();
            }
        }

        public List<Game> GetGamesByPlatform(int platformId)
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            return context.Games
                .Include(g => g.Platform)
                .Where(g => g.PlatformId == platformId)
                .ToList();
        }

        public void AddGame(Game game)
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            context.Games.Add(game);
            context.SaveChanges();
        }

        public void UpdateGame(Game game)
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            context.Games.Update(game);
            context.SaveChanges();
        }

        public void DeleteGame(int gameId)
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            var game = context.Games.Find(gameId);
            if (game != null)
            {
                context.Games.Remove(game);
                context.SaveChanges();
            }
        }

        public int GetTotalGamesCount()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            return context.Games.Count();
        }

        public Dictionary<string, int> GetGamesCountByPlatform()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            return context.Platforms
                .Select(p => new { p.Name, Count = p.Games.Count })
                .ToDictionary(x => x.Name, x => x.Count);
        }
    }
}
