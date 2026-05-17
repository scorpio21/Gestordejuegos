using System.IO;
using GestorVimm;

namespace GestorVimm.Models;

public sealed class RomEntry : ViewModelBase
{
    public required string RawLine { get; init; }
    public required string Title { get; init; }
    public string? Region { get; init; }
    public string? Languages { get; init; }

    private string? _coverFilePath;
    private bool _isExistingCover;

    public string? CoverFilePath
    {
        get => _coverFilePath;
        set
        {
            if (SetField(ref _coverFilePath, value))
                OnPropertyChanged(nameof(HasCover));
        }
    }

    /// <summary>La carátula ya estaba en la carpeta de salida (no se volvió a descargar).</summary>
    public bool IsExistingCover
    {
        get => _isExistingCover;
        set => SetField(ref _isExistingCover, value);
    }

    public bool HasCover =>
        !string.IsNullOrEmpty(CoverFilePath) && File.Exists(CoverFilePath);
}
