using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace GestorJuegos
{
    public partial class ColorPickerDialog : Window
    {
        private string _currentColorHex = "#ffffff";
        private bool _isUpdating = false;

        // Colores predefinidos estilo LaunchBox / Arcade
        private static readonly string[] PresetColors = new[]
        {
            "#1c1d22", "#121316", "#2a2b30", "#3f424a", "#5f626a", "#a1a1aa", "#d1d5db", "#ffffff",
            "#ff007f", "#00f3ff", "#3b82f6", "#10b981", "#ef4444", "#f59e0b", "#8b5cf6", "#ec4899",
            "#0f172a", "#1e293b", "#334155", "#3a5180", "#2c2e35", "#1b1c21", "#0b0c10", "#161920"
        };

        public ColorPickerDialog()
        {
            InitializeComponent();
            PopulatePresets();
        }

        public ColorPickerDialog(string startColorHex)
        {
            InitializeComponent();
            _currentColorHex = ValidateHex(startColorHex);
            PopulatePresets();
            
            // Cargar color actual
            try
            {
                BrdCurrentColor.Background = Brush.Parse(_currentColorHex);
            }
            catch
            {
                BrdCurrentColor.Background = Brushes.White;
            }
            
            SetColorToSlidersAndHex(_currentColorHex);
        }

        private string ValidateHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return "#ffffff";
            hex = hex.Trim();
            if (!hex.StartsWith("#")) hex = "#" + hex;
            if (hex.Length != 7 && hex.Length != 9) return "#ffffff";
            return hex;
        }

        private void PopulatePresets()
        {
            if (GridPresets == null) return;

            foreach (var hex in PresetColors)
            {
                var btn = new Button
                {
                    Classes = { "PresetBtn" },
                    Background = Brush.Parse(hex),
                    Tag = hex
                };
                btn.Click += PresetBtn_Click;
                GridPresets.Children.Add(btn);
            }
        }

        private void PresetBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                SetColorToSlidersAndHex(hex);
            }
        }

        private void SetColorToSlidersAndHex(string hex)
        {
            _isUpdating = true;
            try
            {
                var color = Color.Parse(hex);
                SldRed.Value = color.R;
                SldGreen.Value = color.G;
                SldBlue.Value = color.B;

                TxtRed.Text = color.R.ToString();
                TxtGreen.Text = color.G.ToString();
                TxtBlue.Text = color.B.ToString();

                TxtHexCode.Text = hex;
                BrdNewColor.Background = new SolidColorBrush(color);
            }
            catch { }
            finally
            {
                _isUpdating = false;
            }
        }

        private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdating) return;
            UpdateFromSliders();
        }

        private void UpdateFromSliders()
        {
            _isUpdating = true;
            try
            {
                byte r = (byte)(SldRed.Value);
                byte g = (byte)(SldGreen.Value);
                byte b = (byte)(SldBlue.Value);

                TxtRed.Text = r.ToString();
                TxtGreen.Text = g.ToString();
                TxtBlue.Text = b.ToString();

                var color = Color.FromRgb(r, g, b);
                string hex = $"#{r:x2}{g:x2}{b:x2}";

                TxtHexCode.Text = hex;
                BrdNewColor.Background = new SolidColorBrush(color);
            }
            catch { }
            finally
            {
                _isUpdating = false;
            }
        }

        private void TxtHexCode_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            
            string hex = TxtHexCode.Text ?? "";
            if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
            {
                _isUpdating = true;
                try
                {
                    var color = Color.Parse(hex);
                    SldRed.Value = color.R;
                    SldGreen.Value = color.G;
                    SldBlue.Value = color.B;

                    TxtRed.Text = color.R.ToString();
                    TxtGreen.Text = color.G.ToString();
                    TxtBlue.Text = color.B.ToString();

                    BrdNewColor.Background = new SolidColorBrush(color);
                }
                catch { }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        private void BtnAccept_Click(object? sender, RoutedEventArgs e)
        {
            string hex = TxtHexCode.Text ?? "#ffffff";
            Close(ValidateHex(hex));
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
