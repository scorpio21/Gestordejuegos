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
        public DbSet<PlatformCategory> PlatformCategories { get; set; }
        public DbSet<GameImage> GameImages { get; set; }
        public DbSet<GameAlternateTitle> GameAlternateTitles { get; set; }
        public DbSet<PlatformAlternateName> PlatformAlternateNames { get; set; }
        public DbSet<Emulator> Emulators { get; set; }
        public DbSet<EmulatorPlatform> EmulatorPlatforms { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorJuegos.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuraciones adicionales si son necesarias
            modelBuilder.Entity<Game>()
                .HasOne(g => g.Platform)
                .WithMany(p => p.Games)
                .HasForeignKey(g => g.PlatformId);

            modelBuilder.Entity<GameAlternateTitle>()
                .HasOne(at => at.Game)
                .WithMany(g => g.AlternateTitles)
                .HasForeignKey(at => at.GameId);

            modelBuilder.Entity<PlatformAlternateName>()
                .HasOne(pan => pan.Platform)
                .WithMany(p => p.AlternateNames)
                .HasForeignKey(pan => pan.PlatformId);

            modelBuilder.Entity<EmulatorPlatform>()
                .HasOne(ep => ep.Emulator)
                .WithMany(e => e.SupportedPlatforms)
                .HasForeignKey(ep => ep.EmulatorId);

            modelBuilder.Entity<EmulatorPlatform>()
                .HasOne(ep => ep.Platform)
                .WithMany(p => p.CompatibleEmulators)
                .HasForeignKey(ep => ep.PlatformId);
        }
    }
}
