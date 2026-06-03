using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CreadorTemas
{
    public partial class MainWindow : Window
    {
        private string _accentColor = "#ff007f";
        private string _deepDarkColor = "#0b0c10";
        private string _panelColor = "#161920";
        private string _borderColor = "#00f3ff";
        private string _mainForegroundColor = "#ffffff";
        private string _secondaryTextColor = "#00f3ff";
        private string _hoverColor = "#ff007f";

        private string? _mainFontPath;
        private string? _headerFontPath;
        private string? _bgImagePath;
        private string? _overlayImagePath;
        private string? _logoImagePath;
        private double _cornerRadius = 8;
        private double _hoverGlowBlur = 12;
        private bool _isPreviewHovered = false;

        public MainWindow()
        {
            InitializeComponent();
            SetupEvents();
            DetectThemesPath();
            UpdatePreview();
        }

        private void SetupEvents()
        {
            BtnBrowseThemesPath.Click += BtnBrowseThemesPath_Click;
            BtnSelectAccent.Click += (s, e) => SelectColor(TxtAccentHex, BrdAccentPreview, c => _accentColor = c);
            BtnSelectDeepDark.Click += (s, e) => SelectColor(TxtDeepDarkHex, BrdDeepDarkPreview, c => _deepDarkColor = c);
            BtnSelectPanel.Click += (s, e) => SelectColor(TxtPanelHex, BrdPanelPreview, c => _panelColor = c);
            BtnSelectBorder.Click += (s, e) => SelectColor(TxtBorderHex, BrdBorderPreview, c => _borderColor = c);
            BtnSelectForeground.Click += (s, e) => SelectColor(TxtForegroundHex, BrdForegroundPreview, c => _mainForegroundColor = c);
            BtnSelectSecondary.Click += (s, e) => SelectColor(TxtSecondaryHex, BrdSecondaryPreview, c => _secondaryTextColor = c);
            BtnSelectHoverColor.Click += (s, e) => SelectColor(TxtHoverHex, BrdHoverPreview, c => _hoverColor = c);

            SldHoverGlow.ValueChanged += (s, e) => {
                _hoverGlowBlur = e.NewValue;
                if (TxtHoverGlowVal != null) TxtHoverGlowVal.Text = $"{(int)_hoverGlowBlur} px";
                UpdatePreview();
            };

            BtnBrowseLogoImage.Click += (s, e) => BrowseFile("Imágenes (*.jpg;*.jpeg;*.png)", new[] { "*.jpg", "*.jpeg", "*.png" }, path => {
                _logoImagePath = path;
                TxtLogoImagePath.Text = Path.GetFileName(path);
                UpdatePreviewImage(ImgPreviewLogo, path);
                UpdatePreview();
            });
            BtnClearLogoImage.Click += (s, e) => { 
                _logoImagePath = null; 
                TxtLogoImagePath.Text = ""; 
                ImgPreviewLogo.Source = null; 
                ImgPreviewLogo.IsVisible = false;
                UpdatePreview();
            };

            BrdPreviewPanelCard.PointerEntered += (s, e) => { _isPreviewHovered = true; UpdatePreview(); };
            BrdPreviewPanelCard.PointerExited += (s, e) => { _isPreviewHovered = false; UpdatePreview(); };

            BtnBrowseMainFont.Click += (s, e) => BrowseFile("Fuentes (*.ttf;*.otf)", new[] { "*.ttf", "*.otf" }, path => {
                _mainFontPath = path;
                TxtMainFontPath.Text = Path.GetFileName(path);
                UpdatePreview();
            });
            BtnClearMainFont.Click += (s, e) => { _mainFontPath = null; TxtMainFontPath.Text = ""; UpdatePreview(); };

            BtnBrowseHeaderFont.Click += (s, e) => BrowseFile("Fuentes (*.ttf;*.otf)", new[] { "*.ttf", "*.otf" }, path => {
                _headerFontPath = path;
                TxtHeaderFontPath.Text = Path.GetFileName(path);
                UpdatePreview();
            });
            BtnClearHeaderFont.Click += (s, e) => { _headerFontPath = null; TxtHeaderFontPath.Text = ""; UpdatePreview(); };

            BtnBrowseBgImage.Click += (s, e) => BrowseFile("Imágenes (*.jpg;*.jpeg;*.png)", new[] { "*.jpg", "*.jpeg", "*.png" }, path => {
                _bgImagePath = path;
                TxtBgImagePath.Text = Path.GetFileName(path);
                UpdatePreviewImage(ImgPreviewBg, path);
            });
            BtnClearBgImage.Click += (s, e) => { _bgImagePath = null; TxtBgImagePath.Text = ""; ImgPreviewBg.Source = null; ImgPreviewBg.IsVisible = false; };

            BtnBrowseOverlayImage.Click += (s, e) => BrowseFile("Imágenes (*.jpg;*.jpeg;*.png)", new[] { "*.jpg", "*.jpeg", "*.png" }, path => {
                _overlayImagePath = path;
                TxtOverlayImagePath.Text = Path.GetFileName(path);
                UpdatePreviewImage(ImgPreviewOverlay, path);
            });
            BtnClearOverlayImage.Click += (s, e) => { _overlayImagePath = null; TxtOverlayImagePath.Text = ""; ImgPreviewOverlay.Source = null; ImgPreviewOverlay.IsVisible = false; };

            SldCornerRadius.ValueChanged += (s, e) => {
                _cornerRadius = e.NewValue;
                if (TxtCornerRadiusVal != null) TxtCornerRadiusVal.Text = $"{(int)_cornerRadius} px";
                UpdatePreview();
            };
            BtnImportTheme.Click += BtnImportTheme_Click;
            BtnSaveTheme.Click += BtnSaveTheme_Click;
        }

        private void DetectThemesPath()
        {
            try
            {
                string startDir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 6; i++)
                {
                    string testPath3 = Path.Combine(startDir, "GestorJuegos", "GestorJuegos", "Themes");
                    string testPath2 = Path.Combine(startDir, "GestorJuegos", "Themes");

                    if (Directory.Exists(testPath3)) { TxtThemesPath.Text = Path.GetFullPath(testPath3); return; }
                    if (Directory.Exists(testPath2)) { TxtThemesPath.Text = Path.GetFullPath(testPath2); return; }

                    string? parent = Path.GetDirectoryName(startDir);
                    if (string.IsNullOrEmpty(parent) || parent == startDir) break;
                    startDir = parent;
                }
                string localThemes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
                if (!Directory.Exists(localThemes)) Directory.CreateDirectory(localThemes);
                TxtThemesPath.Text = localThemes;
            }
            catch { TxtThemesPath.Text = AppDomain.CurrentDomain.BaseDirectory; }
        }

        private async void BtnBrowseThemesPath_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Carpeta Themes", AllowMultiple = false });
            if (folders != null && folders.Count > 0) TxtThemesPath.Text = folders[0].Path.LocalPath;
        }

        private async void SelectColor(TextBox textBox, Border previewBorder, Action<string> setColorProp)
        {
            var dialog = new ColorPickerDialog(textBox.Text ?? "#ffffff");
            var result = await dialog.ShowDialog<string>(this);
            if (result != null)
            {
                textBox.Text = result;
                try { previewBorder.Background = Brush.Parse(result); } catch { }
                setColorProp(result);
                UpdatePreview();
            }
        }

        private async void BrowseFile(string fileTypeName, string[] patterns, Action<string> onSelected)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Seleccionar {fileTypeName}",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType(fileTypeName) { Patterns = patterns } }
            });
            if (files != null && files.Count > 0) onSelected(files[0].Path.LocalPath);
        }

        private void UpdatePreview()
        {
            try
            {
                if (BrdPreviewDeepDark != null) BrdPreviewDeepDark.Background = Brush.Parse(_deepDarkColor);
                if (BrdPreviewPanelHeader != null) BrdPreviewPanelHeader.Background = Brush.Parse(_panelColor);
                if (BrdPreviewPanelCard != null)
                {
                    BrdPreviewPanelCard.Background = Brush.Parse(_panelColor);
                    BrdPreviewPanelCard.CornerRadius = new CornerRadius(_cornerRadius);
                    
                    if (_isPreviewHovered)
                    {
                        BrdPreviewPanelCard.BorderBrush = Brush.Parse(_hoverColor);
                        BrdPreviewPanelCard.BoxShadow = new BoxShadows(new BoxShadow 
                        { 
                            Blur = _hoverGlowBlur, 
                            Spread = _hoverGlowBlur > 0 ? 2 : 0, 
                            Color = Color.Parse(_hoverColor), 
                            OffsetY = 0 
                        });
                    }
                    else
                    {
                        BrdPreviewPanelCard.BorderBrush = Brush.Parse(_borderColor);
                        BrdPreviewPanelCard.BoxShadow = new BoxShadows(new BoxShadow 
                        { 
                            Blur = 8, 
                            Spread = 0, 
                            Color = Color.Parse("#50000000"), 
                            OffsetY = 4 
                        });
                    }
                }
                if (ImgPreviewLogo != null && TxtPreviewHeaderTitle != null)
                {
                    bool hasLogo = ImgPreviewLogo.Source != null;
                    TxtPreviewHeaderTitle.IsVisible = !hasLogo;
                    ImgPreviewLogo.IsVisible = hasLogo;
                }
                if (BrdPreviewPanelFooter != null) BrdPreviewPanelFooter.Background = Brush.Parse(_panelColor);
                if (TxtPreviewHeaderTitle != null) TxtPreviewHeaderTitle.Foreground = Brush.Parse(_accentColor);
                if (TxtPreviewMainText != null) TxtPreviewMainText.Foreground = Brush.Parse(_mainForegroundColor);
                if (TxtPreviewSubText != null) TxtPreviewSubText.Foreground = Brush.Parse(_secondaryTextColor);

                ApplyFontToControl(TxtPreviewMainText, _mainFontPath);
                ApplyFontToControl(TxtPreviewSubText, _mainFontPath);
                ApplyFontToControl(TxtPreviewHeaderTitle, _headerFontPath);
            }
            catch { }
        }

        private void ApplyFontToControl(TextBlock? control, string? fontPath)
        {
            if (control == null) return;
            if (!string.IsNullOrEmpty(fontPath) && File.Exists(fontPath))
            {
                try
                {
                    // Crear el URI absoluto de forma segura usando el constructor de Uri de .NET
                    var fontUri = new Uri(fontPath);
                    var fontFamily = new FontFamily(fontUri, Path.GetFileNameWithoutExtension(fontPath));

                    // VALIDACIÓN CRÍTICA: Forzar la carga y verificar si Avalonia puede crear el GlyphTypeface
                    var typeface = new Typeface(fontFamily);
                    if (typeface.GlyphTypeface != null)
                    {
                        control.FontFamily = fontFamily;
                    }
                    else
                    {
                        control.FontFamily = FontFamily.Default;
                    }
                }
                catch 
                { 
                    control.FontFamily = FontFamily.Default; 
                }
            }
            else { control.FontFamily = FontFamily.Default; }
        }

        private void UpdatePreviewImage(Image imgControl, string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    imgControl.Source = new Bitmap(path);
                    imgControl.IsVisible = true;
                }
                else { imgControl.Source = null; imgControl.IsVisible = false; }
            }
            catch { imgControl.Source = null; imgControl.IsVisible = false; }
        }

        private async void BtnImportTheme_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Seleccionar Carpeta de Tema", AllowMultiple = false });
            if (folders == null || folders.Count == 0) return;

            string folder = folders[0].Path.LocalPath;
            if (File.Exists(Path.Combine(folder, "theme.json")))
            {
                var theme = await ThemeManager.LoadThemeAsync(folder);
                if (theme != null)
                {
                    TxtThemeName.Text = Path.GetFileName(folder);
                    LoadImportedData(theme, folder);
                    UpdatePreview();
                    await ShowMessageDialog("Tema cargado", "El tema se ha cargado correctamente en el formulario.");
                }
                else await ShowMessageDialog("Error", "No se pudo leer el archivo theme.json.");
            }
            else if (Directory.Exists(Path.Combine(folder, "Fonts")) || Directory.Exists(Path.Combine(folder, "Images")) || File.Exists(Path.Combine(folder, "ThemeSettings.xml")))
            {
                ImportLaunchBoxTheme(folder);
            }
            else await ShowMessageDialog("No válido", "La carpeta seleccionada no contiene un tema válido.");
        }

        private void LoadImportedData(ThemeConfigJson theme, string folder)
        {
            if (theme.Colors.TryGetValue("AccentBrush", out var acc)) LoadColorVal(acc, TxtAccentHex, BrdAccentPreview, c => _accentColor = c);
            if (theme.Colors.TryGetValue("DeepDarkBrush", out var dark)) LoadColorVal(dark, TxtDeepDarkHex, BrdDeepDarkPreview, c => _deepDarkColor = c);
            if (theme.Colors.TryGetValue("PanelBrush", out var pan)) LoadColorVal(pan, TxtPanelHex, BrdPanelPreview, c => _panelColor = c);
            if (theme.Colors.TryGetValue("BorderBrush", out var bor)) LoadColorVal(bor, TxtBorderHex, BrdBorderPreview, c => _borderColor = c);
            if (theme.Colors.TryGetValue("MainForeground", out var fore)) LoadColorVal(fore, TxtForegroundHex, BrdForegroundPreview, c => _mainForegroundColor = c);
            if (theme.Colors.TryGetValue("SecondaryTextBrush", out var sec)) LoadColorVal(sec, TxtSecondaryHex, BrdSecondaryPreview, c => _secondaryTextColor = c);
            
            if (theme.Colors.TryGetValue("HoverBorderBrush", out var hovColor)) 
                LoadColorVal(hovColor, TxtHoverHex, BrdHoverPreview, c => _hoverColor = c);
            else 
                LoadColorVal(_accentColor, TxtHoverHex, BrdHoverPreview, c => _hoverColor = c);

            if (theme.Metrics.TryGetValue("HoverGlowBlur", out var hgStr) && double.TryParse(hgStr, out double hg))
            {
                SldHoverGlow.Value = hg;
                _hoverGlowBlur = hg;
            }
            else
            {
                SldHoverGlow.Value = 12;
                _hoverGlowBlur = 12;
            }

            _logoImagePath = GetAssetPath("Images/Logo.png", folder);
            TxtLogoImagePath.Text = string.IsNullOrEmpty(_logoImagePath) ? "" : Path.GetFileName(_logoImagePath);
            UpdatePreviewImage(ImgPreviewLogo, _logoImagePath ?? "");

            _mainFontPath = GetAssetPath(theme.Fonts.GetValueOrDefault("MainFont"), folder);
            TxtMainFontPath.Text = string.IsNullOrEmpty(_mainFontPath) ? "" : Path.GetFileName(_mainFontPath);

            _headerFontPath = GetAssetPath(theme.Fonts.GetValueOrDefault("HeaderFont"), folder);
            TxtHeaderFontPath.Text = string.IsNullOrEmpty(_headerFontPath) ? "" : Path.GetFileName(_headerFontPath);

            _bgImagePath = GetAssetPath(theme.BackgroundImage, folder);
            TxtBgImagePath.Text = string.IsNullOrEmpty(_bgImagePath) ? "" : Path.GetFileName(_bgImagePath);
            UpdatePreviewImage(ImgPreviewBg, _bgImagePath ?? "");

            _overlayImagePath = GetAssetPath(theme.OverlayImage, folder);
            TxtOverlayImagePath.Text = string.IsNullOrEmpty(_overlayImagePath) ? "" : Path.GetFileName(_overlayImagePath);
            UpdatePreviewImage(ImgPreviewOverlay, _overlayImagePath ?? "");

            if (theme.Metrics.TryGetValue("CornerRadius", out var crStr) && double.TryParse(crStr, out double cr))
            {
                SldCornerRadius.Value = cr;
                _cornerRadius = cr;
            }

            for (int i = 0; i < CmbPreferredView.Items.Count; i++)
            {
                if (CmbPreferredView.Items[i] is ComboBoxItem item && item.Tag?.ToString() == theme.PreferredView)
                {
                    CmbPreferredView.SelectedIndex = i;
                    break;
                }
            }
        }

        private string? GetAssetPath(string? relativePath, string folder)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            string fullPath = Path.Combine(folder, relativePath);
            return File.Exists(fullPath) ? fullPath : null;
        }

        private async void ImportLaunchBoxTheme(string folder)
        {
            try
            {
                TxtThemeName.Text = Path.GetFileName(folder);
                _mainFontPath = null; _headerFontPath = null; TxtMainFontPath.Text = ""; TxtHeaderFontPath.Text = "";

                string fontsDir = Path.Combine(folder, "Fonts");
                if (Directory.Exists(fontsDir))
                {
                    var files = Directory.GetFiles(fontsDir, "*.*", SearchOption.AllDirectories);
                    int count = 0;
                    foreach (var f in files)
                    {
                        string ext = Path.GetExtension(f).ToLower();
                        if (ext == ".ttf" || ext == ".otf")
                        {
                            if (count == 0) { _mainFontPath = f; TxtMainFontPath.Text = Path.GetFileName(f); count++; }
                            else if (count == 1) { _headerFontPath = f; TxtHeaderFontPath.Text = Path.GetFileName(f); break; }
                        }
                    }
                }

                _bgImagePath = null; TxtBgImagePath.Text = "";
                _logoImagePath = null; TxtLogoImagePath.Text = "";
                ImgPreviewBg.Source = null; ImgPreviewBg.IsVisible = false;
                ImgPreviewLogo.Source = null; ImgPreviewLogo.IsVisible = false;

                string imagesDir = Path.Combine(folder, "Images");
                if (Directory.Exists(imagesDir))
                {
                    var files = Directory.GetFiles(imagesDir, "*.*", SearchOption.AllDirectories);
                    string? bestBg = null; long maxLen = 0;
                    foreach (var img in files)
                    {
                        string ext = Path.GetExtension(img).ToLower();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                        {
                            string name = Path.GetFileNameWithoutExtension(img).ToLower();
                            if (name.Contains("background") || name.Contains("bg") || name.Contains("wall") || name.Contains("fondo")) { bestBg = img; break; }
                            var info = new FileInfo(img);
                            if (info.Length > maxLen) { maxLen = info.Length; bestBg = img; }
                        }
                    }
                    if (bestBg != null) { _bgImagePath = bestBg; TxtBgImagePath.Text = Path.GetFileName(bestBg); UpdatePreviewImage(ImgPreviewBg, bestBg); }

                    // Buscar Logo.png si existe
                    string? logoPath = null;
                    foreach (var img in files)
                    {
                        if (Path.GetFileName(img).Equals("logo.png", StringComparison.OrdinalIgnoreCase))
                        {
                            logoPath = img;
                            break;
                        }
                    }
                    if (logoPath != null)
                    {
                        _logoImagePath = logoPath;
                        TxtLogoImagePath.Text = Path.GetFileName(logoPath);
                        UpdatePreviewImage(ImgPreviewLogo, logoPath);
                    }
                }

                LoadColorVal("#ff007f", TxtAccentHex, BrdAccentPreview, c => _accentColor = c);
                LoadColorVal("#0b0c10", TxtDeepDarkHex, BrdDeepDarkPreview, c => _deepDarkColor = c);
                LoadColorVal("#161920", TxtPanelHex, BrdPanelPreview, c => _panelColor = c);
                LoadColorVal("#00f3ff", TxtBorderHex, BrdBorderPreview, c => _borderColor = c);
                LoadColorVal("#ffffff", TxtForegroundHex, BrdForegroundPreview, c => _mainForegroundColor = c);
                LoadColorVal("#00f3ff", TxtSecondaryHex, BrdSecondaryPreview, c => _secondaryTextColor = c);
                LoadColorVal("#ff007f", TxtHoverHex, BrdHoverPreview, c => _hoverColor = c);
                SldHoverGlow.Value = 12;
                _hoverGlowBlur = 12;
                SldCornerRadius.Value = 8;
                _cornerRadius = 8;
                CmbPreferredView.SelectedIndex = 0;

                UpdatePreview();
                await ShowMessageDialog("Tema de LaunchBox", "Recursos (fuentes e imágenes) extraídos del tema de LaunchBox. Personaliza colores y guarda.");
            }
            catch (Exception ex) { await ShowMessageDialog("Error", "Error al importar de LaunchBox: " + ex.Message); }
        }

        private void LoadColorVal(string hex, TextBox textBox, Border previewBorder, Action<string> setColorProp)
        {
            textBox.Text = hex;
            try { previewBorder.Background = Brush.Parse(hex); } catch { }
            setColorProp(hex);
        }

        private async void BtnSaveTheme_Click(object? sender, RoutedEventArgs e)
        {
            string themesPath = TxtThemesPath.Text ?? "";
            string themeName = TxtThemeName.Text ?? "";

            if (string.IsNullOrWhiteSpace(themesPath) || !Directory.Exists(themesPath)) { await ShowMessageDialog("Error", "Carpeta Themes no válida."); return; }
            if (string.IsNullOrWhiteSpace(themeName)) { await ShowMessageDialog("Nombre Requerido", "Ingresa un nombre para el tema."); return; }

            string safeName = themeName;
            foreach (char c in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(c, '_');
            safeName = safeName.Trim();
            if (string.IsNullOrWhiteSpace(safeName)) { await ShowMessageDialog("Nombre Inválido", "Nombre con caracteres no permitidos."); return; }

            try
            {
                var themeObj = new ThemeConfigJson
                {
                    Colors = new Dictionary<string, string> {
                        { "AccentBrush", _accentColor }, { "DeepDarkBrush", _deepDarkColor }, { "PanelBrush", _panelColor },
                        { "BorderBrush", _borderColor }, { "MainForeground", _mainForegroundColor }, { "SecondaryTextBrush", _secondaryTextColor },
                        { "HoverBorderBrush", _hoverColor }
                    },
                    Fonts = new Dictionary<string, string> {
                        { "MainFont", string.IsNullOrEmpty(_mainFontPath) ? "" : Path.GetFileName(_mainFontPath) },
                        { "HeaderFont", string.IsNullOrEmpty(_headerFontPath) ? "" : Path.GetFileName(_headerFontPath) }
                    },
                    BackgroundImage = string.IsNullOrEmpty(_bgImagePath) ? "" : "Images/" + Path.GetFileName(_bgImagePath),
                    OverlayImage = string.IsNullOrEmpty(_overlayImagePath) ? "" : "Images/" + Path.GetFileName(_overlayImagePath),
                    Metrics = new Dictionary<string, string> { 
                        { "CornerRadius", ((int)_cornerRadius).ToString() },
                        { "HoverGlowBlur", ((int)_hoverGlowBlur).ToString() }
                    },
                    PreferredView = (CmbPreferredView.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Grid"
                };

                await ThemeManager.SaveThemeAsync(themesPath, safeName, themeObj, _mainFontPath, _headerFontPath, _bgImagePath, _overlayImagePath, _logoImagePath);
                await ShowMessageDialog("¡Éxito!", $"Tema '{themeName}' guardado correctamente en:\n{Path.Combine(themesPath, safeName)}");
            }
            catch (Exception ex) { await ShowMessageDialog("Error de Guardado", "No se pudo guardar el tema: " + ex.Message); }
        }

        private Task ShowMessageDialog(string title, string message)
        {
            var msgWin = new MessageWindow(title, message);
            return msgWin.ShowDialog(this);
        }

        private void BtnExit_Click(object? sender, RoutedEventArgs e) => Close();
    }
}