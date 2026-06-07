using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Utils;

namespace GestorJuegos;

public partial class EditGameView : UserControl
{
    private Game? _selectedGame;
    private Platform? _selectedPlatform;
    private GameService? _gameService;
    private AppSettings? _settings;
    private byte[]? _currentCover;
    private ObservableCollection<string> _currentRoms = new();

    public event EventHandler? GameSaved;
    public event EventHandler? RequestClose;
    public event Action<string>? RequestMessage;

    public EditGameView()
    {
        InitializeComponent();
        SetupInternalEvents();
    }

    private void SetupInternalEvents()
    {
        this.FindControl<Button>("BtnCancelEditGameTop")!.Click += (s, e) => OnCancel();
        this.FindControl<Button>("BtnCancelEditGame")!.Click += (s, e) => OnCancel();
        this.FindControl<Button>("BtnSave")!.Click += BtnSave_Click;
        this.FindControl<Button>("BtnSyncMasterDbLocal")!.Click += BtnSyncMasterDbLocal_Click;
        this.FindControl<Button>("BtnSyncExternalLibLocal")!.Click += BtnSyncExternalLib_Click;
        this.FindControl<Button>("BtnSearchIgdb")!.Click += BtnSearchIgdb_Click;
        this.FindControl<Button>("BtnAddRom")!.Click += BtnAddRom_Click;
        this.FindControl<Button>("BtnRemoveRom")!.Click += BtnRemoveRom_Click;
        this.FindControl<Button>("BtnSelectCover")!.Click += BtnSelectCover_Click;
        this.FindControl<Button>("BtnClearCover")!.Click += BtnClearCover_Click;
        this.FindControl<Button>("BtnSelectOverrideEmulator")!.Click += BtnSelectOverrideEmulator_Click;
    }

    public void Initialize(Game game, Platform platform, GameService gameService, AppSettings settings)
    {
        _selectedGame = game;
        _selectedPlatform = platform;
        _gameService = gameService;
        _settings = settings;

        this.FindControl<TextBlock>("TxtEditGameTitle")!.Text = game.Id == 0 ? "Añadir Nuevo Juego" : "Editar Juego";

        this.FindControl<TextBox>("TxtName")!.Text = game.Name;
        this.FindControl<NumericUpDown>("NumYear")!.Value = game.Year;
        this.FindControl<TextBox>("TxtGenre")!.Text = game.Genre;
        this.FindControl<TextBox>("TxtDeveloper")!.Text = game.Developer;
        this.FindControl<TextBox>("TxtPublisher")!.Text = game.Publisher;
        this.FindControl<TextBox>("TxtDescription")!.Text = game.Description;
        this.FindControl<TextBox>("TxtLanguages")!.Text = game.Languages;
        this.FindControl<TextBox>("TxtVersion")!.Text = game.Version;
        this.FindControl<CheckBox>("ChkIsFavorite")!.IsChecked = game.IsFavorite;

        var cmbStatus = this.FindControl<ComboBox>("CmbPlayStatus")!;
        cmbStatus.SelectedIndex = 0;
        foreach (var item in cmbStatus.Items.OfType<ComboBoxItem>())
        {
            if (item.Content?.ToString() == game.PlayStatus)
            {
                cmbStatus.SelectedItem = item;
                break;
            }
        }

        this.FindControl<Slider>("SldRating")!.Value = game.Rating;
        this.FindControl<TextBlock>("TxtPlayCount")!.Text = game.PlayCount.ToString();
        this.FindControl<TextBlock>("TxtDateAdded")!.Text = game.DateAdded.ToString("dd/MM/yyyy");

        _currentRoms.Clear();
        if (!string.IsNullOrEmpty(game.RomPath)) _currentRoms.Add(game.RomPath);
        if (!string.IsNullOrEmpty(game.AdditionalRoms))
        {
            foreach (var r in game.AdditionalRoms.Split('|')) _currentRoms.Add(r);
        }
        this.FindControl<ListBox>("LstRoms")!.ItemsSource = _currentRoms;

        this.FindControl<TextBox>("TxtOverrideEmulator")!.Text = game.OverrideEmulatorPath;
        this.FindControl<TextBox>("TxtOverrideArgs")!.Text = game.OverrideLaunchArguments;

        var cmbRegion = this.FindControl<ComboBox>("CmbRegion")!;
        cmbRegion.SelectedIndex = 0;
        foreach (var item in cmbRegion.Items.OfType<ComboBoxItem>())
        {
            if (item.Content?.ToString() == game.Region)
            {
                cmbRegion.SelectedItem = item;
                break;
            }
        }

        var cmbArt = this.FindControl<ComboBox>("CmbEditArtType")!;
        cmbArt.SelectedIndex = 0;
        foreach (var item in cmbArt.Items.OfType<ComboBoxItem>())
        {
            if (item.Content?.ToString() == game.SelectedArtType)
            {
                cmbArt.SelectedItem = item;
                break;
            }
        }

        _currentCover = game.Cover;
        UpdateCoverImage();
    }

    private void OnCancel()
    {
        SoundHelper.PlayBack();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _gameService == null) return;

        SoundHelper.PlaySelect();
        _selectedGame.Name = this.FindControl<TextBox>("TxtName")!.Text ?? string.Empty;
        _selectedGame.Year = (int)(this.FindControl<NumericUpDown>("NumYear")!.Value ?? DateTime.Now.Year);
        _selectedGame.Genre = this.FindControl<TextBox>("TxtGenre")!.Text ?? string.Empty;
        _selectedGame.Developer = this.FindControl<TextBox>("TxtDeveloper")!.Text ?? string.Empty;
        _selectedGame.Publisher = this.FindControl<TextBox>("TxtPublisher")!.Text ?? string.Empty;
        _selectedGame.Description = this.FindControl<TextBox>("TxtDescription")!.Text ?? string.Empty;
        _selectedGame.Languages = this.FindControl<TextBox>("TxtLanguages")!.Text ?? string.Empty;
        _selectedGame.Version = this.FindControl<TextBox>("TxtVersion")!.Text ?? string.Empty;
        _selectedGame.PlayStatus = (this.FindControl<ComboBox>("CmbPlayStatus")!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente";
        _selectedGame.Rating = (int)this.FindControl<Slider>("SldRating")!.Value;
        
        if (_currentRoms.Count > 0)
        {
            _selectedGame.RomPath = _currentRoms[0];
            _selectedGame.AdditionalRoms = _currentRoms.Count > 1 ? string.Join("|", _currentRoms.Skip(1)) : string.Empty;
        }
        else
        {
            _selectedGame.RomPath = string.Empty;
            _selectedGame.AdditionalRoms = string.Empty;
        }

        _selectedGame.OverrideEmulatorPath = this.FindControl<TextBox>("TxtOverrideEmulator")!.Text ?? string.Empty;
        _selectedGame.OverrideLaunchArguments = this.FindControl<TextBox>("TxtOverrideArgs")!.Text ?? string.Empty;
        _selectedGame.IsFavorite = this.FindControl<CheckBox>("ChkIsFavorite")!.IsChecked ?? false;
        _selectedGame.Region = (this.FindControl<ComboBox>("CmbRegion")!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "🇺🇸 US";
        
        string editArtType = (this.FindControl<ComboBox>("CmbEditArtType")!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Box 3D";
        _selectedGame.SelectedArtType = editArtType;
        _selectedGame.CoverType = GetExternalFolderName(editArtType);
        _selectedGame.Cover = _currentCover;

        if (_selectedGame.Id == 0) _gameService.AddGame(_selectedGame);
        else _gameService.UpdateGame(_selectedGame);

        GameSaved?.Invoke(this, EventArgs.Empty);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private string GetExternalFolderName(string uiName) => uiName switch
    {
        "Box" => "Box",
        "Box 3D" => "Box 3D",
        "Box Full" => "Box Full",
        "Cart - Front" => "Cart - Front",
        "Cart - 3D" => "Cart - 3D",
        "Support" => "Support",
        _ => "Box 3D"
    };

    private async void BtnSelectCover_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Carátula",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count >= 1)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            _currentCover = memoryStream.ToArray();
            UpdateCoverImage();
        }
    }

    private void BtnClearCover_Click(object? sender, RoutedEventArgs e)
    {
        _currentCover = null;
        UpdateCoverImage();
    }

    private void UpdateCoverImage()
    {
        var imgEditCover = this.FindControl<Image>("ImgEditCover")!;
        if (_currentCover != null && _currentCover.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(_currentCover);
                imgEditCover.Source = new Bitmap(ms);
            }
            catch { imgEditCover.Source = null; }
        }
        else imgEditCover.Source = null;
    }

    private async void BtnAddRom_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Añadir Archivo de Juego / ROM",
            AllowMultiple = true
        });

        foreach (var file in files) _currentRoms.Add(file.Path.LocalPath);
    }

    private void BtnRemoveRom_Click(object? sender, RoutedEventArgs e)
    {
        var lstRoms = this.FindControl<ListBox>("LstRoms")!;
        if (lstRoms.SelectedItem is string selectedPath) _currentRoms.Remove(selectedPath);
    }

    private async void BtnSelectOverrideEmulator_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Ejecutable del Emulador (Override)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Ejecutables") { Patterns = new[] { "*.exe", "*.bat", "*.cmd" } } }
        });

        if (files.Count > 0)
        {
            var txtEmulator = this.FindControl<TextBox>("TxtOverrideEmulator")!;
            var txtArgs = this.FindControl<TextBox>("TxtOverrideArgs")!;
            txtEmulator.Text = files[0].TryGetLocalPath() ?? files[0].Name;
            if (string.IsNullOrWhiteSpace(txtArgs.Text)) txtArgs.Text = "\"{0}\"";
        }
    }

    private void BtnSyncExternalLib_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedGame == null || _selectedPlatform == null || _settings == null) return;
        
        SoundHelper.PlaySelect();
        string lbPath = _settings.ExternalLibraryPath;
        if (!Directory.Exists(lbPath))
        {
            RequestMessage?.Invoke("Configura primero la ruta de la Biblioteca Externa en Ajustes.");
            return;
        }

        string xmlPath = Path.Combine(lbPath, "Data", "Platforms", $"{_selectedPlatform.Name}.xml");
        if (!File.Exists(xmlPath))
        {
            RequestMessage?.Invoke($"No se encontró el XML de metadatos para la plataforma: {_selectedPlatform.Name}");
            return;
        }

        try
        {
            var xdoc = System.Xml.Linq.XDocument.Load(xmlPath);
            var gameElement = xdoc.Descendants("Game")
                .FirstOrDefault(x => x.Element("Title")?.Value?.Equals(_selectedGame.Name, StringComparison.OrdinalIgnoreCase) == true);

            if (gameElement != null)
            {
                this.FindControl<TextBox>("TxtGenre")!.Text = gameElement.Element("Genre")?.Value ?? this.FindControl<TextBox>("TxtGenre")!.Text;
                this.FindControl<TextBox>("TxtDeveloper")!.Text = gameElement.Element("Developer")?.Value ?? this.FindControl<TextBox>("TxtDeveloper")!.Text;
                this.FindControl<TextBox>("TxtPublisher")!.Text = gameElement.Element("Publisher")?.Value ?? this.FindControl<TextBox>("TxtPublisher")!.Text;
                this.FindControl<TextBox>("TxtDescription")!.Text = gameElement.Element("Notes")?.Value ?? this.FindControl<TextBox>("TxtDescription")!.Text;
                this.FindControl<TextBox>("TxtVersion")!.Text = gameElement.Element("Version")?.Value ?? this.FindControl<TextBox>("TxtVersion")!.Text;
                
                if (DateTime.TryParse(gameElement.Element("ReleaseDate")?.Value, out var dt))
                    this.FindControl<NumericUpDown>("NumYear")!.Value = dt.Year;

                if (float.TryParse(gameElement.Element("StarRating")?.Value, out var rating))
                    this.FindControl<Slider>("SldRating")!.Value = (int)(rating * 20);

                RequestMessage?.Invoke("Metadatos sincronizados desde Biblioteca Externa correctamente.");
            }
            else RequestMessage?.Invoke("No se encontró información exacta para este título en Biblioteca Externa.");
        }
        catch (Exception ex) { RequestMessage?.Invoke($"Error al leer el XML: {ex.Message}"); }
    }

    private void BtnSyncMasterDbLocal_Click(object? sender, RoutedEventArgs e)
    {
        RequestMessage?.Invoke("Sincronización con Base de Datos Local no implementada en este módulo aún.");
    }

    private void BtnSearchIgdb_Click(object? sender, RoutedEventArgs e)
    {
        RequestMessage?.Invoke("La búsqueda en Vimm requiere integración con el diálogo global de búsqueda.");
    }
}
