using Microsoft.EntityFrameworkCore;
using GestorJuegos.Models;
using System.IO;
using System;

namespace GestorJuegos.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<Platform> Platforms { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Platform>().HasData(
                new Platform { Id = 1, Name = "Atari 2600" },
                new Platform { Id = 2, Name = "Atari 5200" },
                new Platform { Id = 3, Name = "Sega Genesis" },
                new Platform { Id = 4, Name = "Super Nintendo" }
            );
        }
    }
}
