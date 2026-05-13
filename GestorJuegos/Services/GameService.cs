using System.Collections.Generic;
using System.Linq;
using GestorJuegos.Data;
using GestorJuegos.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorJuegos.Services
{
    public class GameService
    {
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
    }
}
