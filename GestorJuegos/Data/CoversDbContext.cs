using Microsoft.EntityFrameworkCore;
using GestorJuegos.Models;
using System.IO;
using System;

namespace GestorJuegos.Data
{
    public class CoversDbContext : DbContext
    {
        public DbSet<GameCover> Covers { get; set; }
        public DbSet<GameImage> Images { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorCovers.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameCover>().HasKey(c => c.Id);
            modelBuilder.Entity<GameImage>().HasKey(i => i.Id);
            modelBuilder.Entity<GameImage>().HasIndex(i => new { i.GameId, i.ImageType });
        }
    }
}
