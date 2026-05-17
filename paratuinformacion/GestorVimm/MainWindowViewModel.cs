using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GestorVimm.Models;
using GestorVimm.Services;

namespace GestorVimm;

public class MainWindowViewModel : ViewModelBase
{
    private readonly VimmVaultService _vimm = new();

    private VaultPlatform? _selectedPlatform;
    private string _txtFilePath = string.Empty;
    private string _outputFolder = string.Empty;
    private string _logText = string.Empty;
    private bool _isBusy;
    private Bitmap? _previewCover;
    private RomEntry? _selectedGame;
    private string _previewCaption = "Selecciona un juego de la lista";
    private int _progressCurrent;
    private int _progressTotal;
    private CancellationTokenSource? _downloadCts;

    public Func<Task<string?>>? PickTxtFileAsync { get; set; }
    public Func<Task<string?>>? PickOutputFolderAsync { get; set; }

    public IReadOnlyList<VaultPlatform> Platforms => VaultPlatforms.All;

    public VaultPlatform? SelectedPlatform
    {
        get => _selectedPlatform;
        set
        {
            if (SetField(ref _selectedPlatform, value))
                RefreshCommands();
        }
    }

    public string TxtFilePath
    {
        get => _txtFilePath;
        set
        {
            if (SetField(ref _txtFilePath, value))
                RefreshCommands();
        }
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set
        {
            if (SetField(ref _outputFolder, value))
                RefreshCommands();
        }
    }

    public string LogText
    {
        get => _logText;
        set => SetField(ref _logText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value))
                RefreshCommands();
        }
    }

    public bool CanStart => !IsBusy
        && SelectedPlatform is not null
        && File.Exists(TxtFilePath)
        && !string.IsNullOrWhiteSpace(OutputFolder)
        && ParsedGames.Count > 0;

    public string StatusHint
    {
        get
        {
            if (IsBusy) return "Descargando… (pulsa Cancelar para detener)";
            if (SelectedPlatform is null) return "Selecciona una plataforma.";
            if (!File.Exists(TxtFilePath)) return "El fichero .txt no existe o la ruta no es válida.";
            if (ParsedGames.Count == 0) return "Pulsa «Analizar» para cargar el listado.";
            if (string.IsNullOrWhiteSpace(OutputFolder)) return "Indica la carpeta de salida.";
            return "Listo para descargar.";
        }
    }

    public int ProgressCurrent
    {
        get => _progressCurrent;
        set
        {
            if (SetField(ref _progressCurrent, value))
                NotifyProgressChanged();
        }
    }

    public int ProgressTotal
    {
        get => _progressTotal;
        set
        {
            if (SetField(ref _progressTotal, value))
                NotifyProgressChanged();
        }
    }

    public int ProgressMaximum => Math.Max(ProgressTotal, 1);

    public string ProgressText =>
        ProgressTotal > 0 ? $"{ProgressCurrent} / {ProgressTotal}" : string.Empty;

    public bool ShowProgress => ProgressTotal > 0;

    public Bitmap? PreviewCover
    {
        get => _previewCover;
        set => SetField(ref _previewCover, value);
    }

    public RomEntry? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetField(ref _selectedGame, value))
                LoadPreviewForSelected();
        }
    }

    public string PreviewCaption
    {
        get => _previewCaption;
        set => SetField(ref _previewCaption, value);
    }

    public ObservableCollection<RomEntry> ParsedGames { get; } = new();

    public ICommand PickTxtCommand { get; }
    public ICommand PickFolderCommand { get; }
    public ICommand ParseTxtCommand { get; }
    public ICommand DownloadCoversCommand { get; }
    public ICommand PrevCoverCommand { get; }
    public ICommand NextCoverCommand { get; }
    public ICommand CancelDownloadCommand { get; }

    public MainWindowViewModel()
    {
        PickTxtCommand = new RelayCommand(async _ => await PickTxtAsync());
        PickFolderCommand = new RelayCommand(async _ => await PickFolderAsync());
        ParseTxtCommand = new RelayCommand(_ => ParseTxt(), _ => File.Exists(TxtFilePath));
        DownloadCoversCommand = new RelayCommand(async _ => await DownloadCoversAsync(), _ => CanStart);
        PrevCoverCommand = new RelayCommand(_ => NavigatePreview(-1), _ => CanNavigatePreview(-1));
        NextCoverCommand = new RelayCommand(_ => NavigatePreview(1), _ => CanNavigatePreview(1));
        CancelDownloadCommand = new RelayCommand(_ => CancelDownload(), _ => IsBusy);

        SelectedPlatform = Platforms.FirstOrDefault(p => p.SystemCode == "3DS");
        RefreshCommands();
    }

    public void LoadTxtFile(string path)
    {
        if (!File.Exists(path))
        {
            AppendLog($"No se encontró el fichero: {path}\n");
            return;
        }

        TxtFilePath = path;
        ParseTxt();
    }

    private async Task PickTxtAsync()
    {
        if (PickTxtFileAsync is null) return;
        var path = await PickTxtFileAsync();
        if (path is not null)
            LoadTxtFile(path);
    }

    private void CancelDownload() => _downloadCts?.Cancel();

    private async Task PickFolderAsync()
    {
        if (PickOutputFolderAsync is null) return;
        var path = await PickOutputFolderAsync();
        if (path is not null)
        {
            OutputFolder = path;
            LinkExistingCovers();
            LoadPreviewForSelected();
        }
    }

    private void ParseTxt()
    {
        ParsedGames.Clear();
        LogText = string.Empty;

        if (!File.Exists(TxtFilePath))
        {
            RefreshCommands();
            return;
        }

        foreach (var entry in RomFilenameParser.ParseFile(TxtFilePath))
            ParsedGames.Add(entry);

        LinkExistingCovers();
        SelectedGame = ParsedGames.FirstOrDefault();
        AppendLog($"Leídos {ParsedGames.Count} juegos del listado.\n");
        RefreshCommands();
    }

    private async Task DownloadCoversAsync()
    {
        if (SelectedPlatform is null || !CanStart)
            return;

        Directory.CreateDirectory(OutputFolder);

        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
        _downloadCts = new CancellationTokenSource();
        var ct = _downloadCts.Token;

        IsBusy = true;
        ProgressCurrent = 0;
        ProgressTotal = ParsedGames.Count;
        var ok = 0;
        var skipped = 0;
        var fail = 0;

        try
        {
            foreach (var entry in ParsedGames)
            {
                ct.ThrowIfCancellationRequested();

                ProgressCurrent++;
                AppendLog($"[{ProgressCurrent}/{ProgressTotal}] {entry.Title}... ");

                var outPath = GetCoverOutputPath(entry);
                if (File.Exists(outPath))
                {
                    entry.CoverFilePath = outPath;
                    entry.IsExistingCover = true;
                    AppendLog("ya existe en carpeta.\n");
                    skipped++;
                    continue;
                }

                try
                {
                    var id = await _vimm.FindGameIdAsync(SelectedPlatform.SystemCode, entry, ct);
                    if (id is null)
                    {
                        AppendLog("no encontrado en Vimm.\n");
                        fail++;
                        continue;
                    }

                    var bytes = await _vimm.DownloadBoxArtAsync(id.Value, ct);
                    if (bytes is null or { Length: 0 })
                    {
                        AppendLog($"sin carátula (id {id}).\n");
                        fail++;
                        continue;
                    }

                    await File.WriteAllBytesAsync(outPath, bytes, ct);

                    entry.CoverFilePath = outPath;
                    entry.IsExistingCover = false;
                    SelectedGame = entry;
                    await SetPreviewAsync(bytes);

                    AppendLog($"OK → {Path.GetFileName(outPath)} (id {id})\n");
                    ok++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AppendLog($"error: {ex.Message}\n");
                    fail++;
                }

                await Task.Delay(400, ct);
            }

            AppendLog($"\nFinalizado: {ok} descargadas, {skipped} ya existían, {fail} fallidas.\n");
        }
        catch (OperationCanceledException)
        {
            AppendLog($"\nDescarga cancelada ({ok} descargadas, {skipped} ya existían, {fail} fallidas).\n");
        }
        finally
        {
            _downloadCts?.Dispose();
            _downloadCts = null;
            IsBusy = false;
            RefreshCommands();
        }
    }

    private void NotifyProgressChanged()
    {
        OnPropertyChanged(nameof(ProgressMaximum));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ShowProgress));
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StatusHint));
        (DownloadCoversCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ParseTxtCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PrevCoverCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (NextCoverCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CancelDownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void LinkExistingCovers()
    {
        if (string.IsNullOrWhiteSpace(OutputFolder) || !Directory.Exists(OutputFolder))
            return;

        foreach (var entry in ParsedGames)
        {
            var path = GetCoverOutputPath(entry);
            if (File.Exists(path))
            {
                entry.CoverFilePath = path;
                entry.IsExistingCover = true;
            }
        }
    }

    private string GetCoverOutputPath(RomEntry entry) =>
        Path.Combine(OutputFolder, VimmVaultService.SanitizeFileName(entry.Title) + ".png");

    private void LoadPreviewForSelected()
    {
        if (SelectedGame is null)
        {
            PreviewCover = null;
            PreviewCaption = "Selecciona un juego de la lista";
            RefreshCommands();
            return;
        }

        if (SelectedGame.HasCover)
        {
            try
            {
                PreviewCover = new Bitmap(SelectedGame.CoverFilePath!);
                var index = ParsedGames.IndexOf(SelectedGame) + 1;
                PreviewCaption = $"{SelectedGame.Title} ({index}/{ParsedGames.Count})";
            }
            catch
            {
                PreviewCover = null;
                PreviewCaption = $"{SelectedGame.Title} — no se pudo cargar la imagen";
            }
        }
        else
        {
            PreviewCover = null;
            var index = ParsedGames.IndexOf(SelectedGame) + 1;
            PreviewCaption = $"{SelectedGame.Title} ({index}/{ParsedGames.Count}) — sin carátula";
        }

        RefreshCommands();
    }

    private bool CanNavigatePreview(int delta)
    {
        if (ParsedGames.Count == 0) return false;
        var start = SelectedGame is null ? -1 : ParsedGames.IndexOf(SelectedGame);
        for (var i = start + delta; i >= 0 && i < ParsedGames.Count; i += delta)
        {
            if (ParsedGames[i].HasCover)
                return true;
        }
        return false;
    }

    private void NavigatePreview(int delta)
    {
        if (ParsedGames.Count == 0) return;

        var start = SelectedGame is null
            ? (delta > 0 ? -1 : ParsedGames.Count)
            : ParsedGames.IndexOf(SelectedGame);

        for (var i = start + delta; i >= 0 && i < ParsedGames.Count; i += delta)
        {
            if (!ParsedGames[i].HasCover)
                continue;
            SelectedGame = ParsedGames[i];
            return;
        }
    }

    private async Task SetPreviewAsync(byte[] bytes)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var ms = new MemoryStream(bytes);
            PreviewCover = new Bitmap(ms);
            if (SelectedGame is not null)
            {
                var index = ParsedGames.IndexOf(SelectedGame) + 1;
                PreviewCaption = $"{SelectedGame.Title} ({index}/{ParsedGames.Count})";
            }
        });
    }

    private void AppendLog(string text) =>
        LogText += text;
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
