using System.IO;
using System.Text.RegularExpressions;
using GestorVimm.Models;

namespace GestorVimm.Services;

public static partial class RomFilenameParser
{
    public static RomEntry? ParseLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var name = line;
        if (name.Contains('.'))
            name = name[..name.LastIndexOf('.')];

        string? region = null;
        string? languages = null;

        var langMatch = LangSuffix().Match(name);
        if (langMatch.Success)
        {
            languages = langMatch.Groups[1].Value;
            name = name[..langMatch.Index].TrimEnd();
        }

        var regionMatch = RegionSuffix().Match(name);
        if (regionMatch.Success)
        {
            region = regionMatch.Groups[1].Value;
            name = name[..regionMatch.Index].TrimEnd();
        }

        return new RomEntry
        {
            RawLine = line,
            Title = name.Trim(),
            Region = region,
            Languages = languages
        };
    }

    public static IReadOnlyList<RomEntry> ParseFile(string path) =>
        File.ReadAllLines(path)
            .Select(ParseLine)
            .Where(e => e is not null)
            .Cast<RomEntry>()
            .ToList();

    [GeneratedRegex(@"\(([^)]+)\)\s*$")]
    private static partial Regex RegionSuffix();

    [GeneratedRegex(@"\(([A-Za-z]{2}(?:,[A-Za-z]{2})*)\)\s*$")]
    private static partial Regex LangSuffix();
}
