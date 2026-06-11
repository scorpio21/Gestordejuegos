using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using GestorJuegos.Models;
using System.Text.RegularExpressions;

namespace GestorJuegos.Services;

public class ExternalMetadataService
{
    private readonly string _dbPath;

    public ExternalMetadataService(string? customDbPath = null)
    {
        if (!string.IsNullOrEmpty(customDbPath) && File.Exists(customDbPath))
        {
            _dbPath = customDbPath;
        }
        else if (File.Exists(@"K:\GestorJuegos\RevisaDB\LaunchBox.Metadata.db"))
        {
            _dbPath = @"K:\GestorJuegos\RevisaDB\LaunchBox.Metadata.db";
        }
        else if (File.Exists(@"K:\GestorJuegos\RevisaDB\Metadata.db"))
        {
            _dbPath = @"K:\GestorJuegos\RevisaDB\Metadata.db";
        }
        else
        {
            _dbPath = Path.Combine(@"C:\BibliotecaExterna", "Metadata", "Metadata.db");
        }
    }

    public bool IsDatabaseAvailable => File.Exists(_dbPath);

    public GameMetadata? GetMetadata(string gameName, string platformName)
    {
        if (!IsDatabaseAvailable) return null;

        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            string extPlatform = GetExternalPlatformName(connection, platformName);
            string cleanName = CleanGameName(gameName);

            // Intentos de búsqueda secuenciales
            GameMetadata? metadata = null;

            // 1. Búsqueda por nombre exacto (CompareName)
            metadata = SearchGame(connection, cleanName, extPlatform);
            if (metadata != null) return metadata;

            // 2. Búsqueda por Títulos Alternativos
            int? dbId = GetDatabaseIdFromAltTitle(connection, cleanName);
            if (dbId.HasValue)
            {
                metadata = GetGameById(connection, dbId.Value);
                if (metadata != null) return metadata;
            }

            // 3. Búsqueda quitando sufijos comunes (Complete, Edition, etc.)
            string strippedName = StripCommonSuffixes(cleanName);
            if (strippedName != cleanName)
            {
                metadata = SearchGame(connection, strippedName, extPlatform);
                if (metadata != null) return metadata;
            }

            // 4. Búsqueda Fuzzy / Contenido
            return SearchGameFuzzy(connection, cleanName, extPlatform);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string GetExternalPlatformName(SqliteConnection conn, string platformName)
    {
        string sql = "SELECT Name FROM PlatformAlternateNames WHERE Alternate = @alt LIMIT 1";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@alt", platformName);
        var result = cmd.ExecuteScalar();
        
        if (result != null) return result.ToString() ?? platformName;

        if (platformName.Equals("MAME", StringComparison.OrdinalIgnoreCase)) return "Arcade";
        if (platformName.Equals("SNES", StringComparison.OrdinalIgnoreCase)) return "Super Nintendo Entertainment System";
        if (platformName.Equals("NES", StringComparison.OrdinalIgnoreCase)) return "Nintendo Entertainment System";
        
        return platformName;
    }

    public PlatformMetadata? GetPlatformMetadata(string platformName)
    {
        if (!IsDatabaseAvailable) return null;

        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            string extPlatform = GetExternalPlatformName(connection, platformName);

            string sql = "SELECT * FROM Platforms WHERE Name = @name LIMIT 1";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@name", extPlatform);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new PlatformMetadata
                {
                    Name = reader["Name"]?.ToString() ?? extPlatform,
                    ReleaseDate = reader["ReleaseDate"]?.ToString(),
                    Developer = reader["Developer"]?.ToString(),
                    Manufacturer = reader["Manufacturer"]?.ToString(),
                    Cpu = reader["Cpu"]?.ToString(),
                    Memory = reader["Memory"]?.ToString(),
                    Graphics = reader["Graphics"]?.ToString(),
                    Sound = reader["Sound"]?.ToString(),
                    Display = reader["Display"]?.ToString(),
                    Media = reader["Media"]?.ToString(),
                    Notes = reader["Notes"]?.ToString()
                };
            }
        }
        catch { }
        return null;
    }

    private string CleanGameName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        // Quitar etiquetas comunes entre paréntesis o corchetes
        string cleaned = Regex.Replace(name, @"\([^)]*\)|\[[^\]]*\]", "").Trim();
        // Quitar caracteres no alfanuméricos pero mantener espacios
        return Regex.Replace(cleaned, @"[^a-zA-Z0-9\s]", "").ToLower().Trim();
    }

    private string StripCommonSuffixes(string name)
    {
        string[] suffixes = { "complete", "edition", "version", "rev 1", "rev a", "the movie", "the game", "ii", "2" };
        string result = name;
        foreach (var suffix in suffixes)
        {
            if (result.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - (suffix.Length + 1)).Trim();
            }
        }
        return result;
    }

    private GameMetadata? SearchGame(SqliteConnection conn, string cleanName, string platform)
    {
        string sql = "SELECT * FROM Games WHERE (CompareName = @name OR Name = @rawName) AND Platform = @platform LIMIT 1";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", cleanName.Replace(" ", "").ToUpper());
        cmd.Parameters.AddWithValue("@rawName", cleanName);
        cmd.Parameters.AddWithValue("@platform", platform);
        
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read()) return MapReaderToMetadata(reader);
        }

        // Re-intento con espacios en CompareName (algunos juegos lo tienen así)
        cmd.Parameters["@name"].Value = cleanName.ToUpper();
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read()) return MapReaderToMetadata(reader);
        }

        return null;
    }

    private int? GetDatabaseIdFromAltTitle(SqliteConnection conn, string cleanName)
    {
        string sql = "SELECT DatabaseID FROM GameAlternateTitles WHERE (AltNameCompareValue = @name OR AlternateName = @rawName) LIMIT 1";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", cleanName.Replace(" ", "").ToUpper());
        cmd.Parameters.AddWithValue("@rawName", cleanName);
        
        var result = cmd.ExecuteScalar();
        if (result != null) return Convert.ToInt32(result);

        cmd.Parameters["@name"].Value = cleanName.ToUpper();
        result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : null;
    }

    private GameMetadata? GetGameById(SqliteConnection conn, int dbId)
    {
        string sql = "SELECT * FROM Games WHERE DatabaseID = @id LIMIT 1";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", dbId);
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read()) return MapReaderToMetadata(reader);
        return null;
    }

    private GameMetadata? SearchGameFuzzy(SqliteConnection conn, string name, string platform)
    {
        string sql = "SELECT * FROM Games WHERE Name LIKE @name AND Platform = @platform LIMIT 1";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", $"%{name}%");
        cmd.Parameters.AddWithValue("@platform", platform);
        
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read()) return MapReaderToMetadata(reader);
        }

        // Búsqueda inversa: ¿el nombre externo está dentro de nuestro nombre de archivo?
        if (name.Length > 4)
        {
            string firstWord = name.Split(' ')[0];
            cmd.CommandText = "SELECT * FROM Games WHERE Platform = @platform AND Name LIKE @firstWord";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@platform", platform);
            cmd.Parameters.AddWithValue("@firstWord", $"%{firstWord}%");

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string extName = reader["Name"]?.ToString()?.ToLower() ?? "";
                    if (!string.IsNullOrEmpty(extName) && name.Contains(extName, StringComparison.OrdinalIgnoreCase))
                    {
                        return MapReaderToMetadata(reader);
                    }
                }
            }
        }

        return null;
    }

    private GameMetadata MapReaderToMetadata(SqliteDataReader reader)
    {
        return new GameMetadata
        {
            DatabaseID = reader["DatabaseID"] != DBNull.Value ? Convert.ToInt32(reader["DatabaseID"]) : 0,
            Name = reader["Name"]?.ToString() ?? "",
            Description = reader["Overview"]?.ToString() ?? "",
            ReleaseYear = reader["ReleaseYear"] != DBNull.Value ? Convert.ToInt32(reader["ReleaseYear"]) : 0,
            ReleaseDate = reader["ReleaseDate"]?.ToString(),
            Developer = reader["Developer"]?.ToString() ?? "",
            Publisher = reader["Publisher"]?.ToString() ?? "",
            Genres = reader["Genres"]?.ToString() ?? "",
            MaxPlayers = reader["MaxPlayers"] != DBNull.Value ? Convert.ToInt32(reader["MaxPlayers"]) : 1,
            ESRB = reader["ESRB"]?.ToString(),
            VideoURL = reader["VideoURL"]?.ToString(),
            WikipediaURL = reader["WikipediaURL"]?.ToString(),
            CommunityRating = reader["CommunityRating"]?.ToString(),
            CommunityRatingCount = reader["CommunityRatingCount"] != DBNull.Value ? Convert.ToInt32(reader["CommunityRatingCount"]) : 0,
            Cooperative = reader["Cooperative"] != DBNull.Value && Convert.ToInt32(reader["Cooperative"]) == 1
        };
    }
}

public class PlatformMetadata
{
    public string Name { get; set; } = "";
    public string? ReleaseDate { get; set; }
    public string? Developer { get; set; }
    public string? Manufacturer { get; set; }
    public string? Cpu { get; set; }
    public string? Memory { get; set; }
    public string? Graphics { get; set; }
    public string? Sound { get; set; }
    public string? Display { get; set; }
    public string? Media { get; set; }
    public string? Notes { get; set; }
}

public class GameMetadata
{
    public int DatabaseID { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ReleaseYear { get; set; }
    public string? ReleaseDate { get; set; }
    public string Developer { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Genres { get; set; } = "";
    public int MaxPlayers { get; set; }
    public string? ESRB { get; set; }
    public string? VideoURL { get; set; }
    public string? WikipediaURL { get; set; }
    public string? CommunityRating { get; set; }
    public int CommunityRatingCount { get; set; }
    public bool Cooperative { get; set; }
}
