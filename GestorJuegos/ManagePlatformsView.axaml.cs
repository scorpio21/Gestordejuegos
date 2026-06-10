using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Utils;

namespace GestorJuegos
{
    public partial class ManagePlatformsView : UserControl
    {
        private GameService? _gameService;
        public event EventHandler? PlatformsChanged;
        public event EventHandler? RequestClose;
        public event Action<string>? RequestMessage;

        public ManagePlatformsView()
        {
            InitializeComponent();
            SetupEvents();
        }

        private void SetupEvents()
        {
            BtnClose.Click += (s, e) => RequestClose?.Invoke(this, EventArgs.Empty);
            LstPlatforms.SelectionChanged += LstPlatforms_SelectionChanged;
            BtnSelectEmulator.Click += BtnSelectEmulator_Click;
            BtnSave.Click += BtnSave_Click;
            BtnDelete.Click += BtnDelete_Click;
        }

        public void Initialize(GameService gameService)
        {
            _gameService = gameService;
            ReloadPlatforms();
            PnlEdit.IsVisible = false;
        }

        public void SelectPlatform(int platformId)
        {
            if (LstPlatforms.ItemsSource is IEnumerable<Platform> platforms)
            {
                var matched = platforms.FirstOrDefault(p => p.Id == platformId);
                if (matched != null)
                {
                    LstPlatforms.SelectedItem = matched;
                    PnlEdit.IsVisible = true;
                }
            }
        }

        private void ReloadPlatforms()
        {
            if (_gameService == null) return;
            LstPlatforms.ItemsSource = _gameService.GetPlatforms();
        }

        private void LstPlatforms_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LstPlatforms.SelectedItem is Platform platform)
            {
                TxtName.Text = platform.Name;
                TxtEmulatorPath.Text = platform.EmulatorPath;
                TxtLaunchArgs.Text = platform.LaunchArguments;
                
                var categoryItem = CmbCategory.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content?.ToString() == platform.Category);
                if (categoryItem != null) CmbCategory.SelectedItem = categoryItem;
                else CmbCategory.SelectedIndex = 0;

                PnlEdit.IsVisible = true;
            }
            else
            {
                PnlEdit.IsVisible = false;
            }
        }

        private async void BtnSelectEmulator_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar Emulador o Ejecutable",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                TxtEmulatorPath.Text = files[0].Path.LocalPath;
            }
        }

        private void BtnSave_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_gameService != null && LstPlatforms.SelectedItem is Platform platform)
                {
                    var newName = TxtName.Text?.Trim();
                    if (!string.IsNullOrEmpty(newName))
                    {
                        platform.Name = newName;
                        platform.EmulatorPath = TxtEmulatorPath.Text?.Trim() ?? "";
                        platform.LaunchArguments = TxtLaunchArgs.Text?.Trim() ?? "";
                        platform.Category = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Consoles";
                        
                        _gameService.UpdatePlatform(platform);
                        ReloadPlatforms();
                        PlatformsChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                RequestMessage?.Invoke($"Error al actualizar la plataforma: {ex.Message}");
            }
        }

        private void BtnDelete_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_gameService != null && LstPlatforms.SelectedItem is Platform platform)
                {
                    _gameService.DeletePlatform(platform.Id);
                    ReloadPlatforms();
                    PnlEdit.IsVisible = false;
                    PlatformsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                RequestMessage?.Invoke($"Error al eliminar la plataforma: {ex.Message}");
            }
        }
    }
}
