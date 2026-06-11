using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestorJuegos.Views.Windows
{
    public partial class MainWindow : Window
    {
        private async Task ScanLocalCoversAsync()
        {
            if (_selectedPlatform == null)
            {
                ShowMessage("Por favor, selecciona primero una plataforma para asociar las carátulas.");
                return;
            }

            SoundHelper.PlaySelect();
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta de Carátulas",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string coverPath = folders[0].Path.LocalPath;
                _cts = new CancellationTokenSource();
                OverlayProgress.Update($"Escaneando carátulas: {_selectedPlatform.Name}", 0, "Iniciando escaneo de imágenes...");

                var progress = new Progress<ScanProgress>(p => {
                    OverlayProgress.Update($"Escaneando carátulas: {_selectedPlatform.Name}", p.Percentage, p.Detail);
                });

                try
                {
                    await Task.Run(() => _scannerService.ScanCoversAsync(_selectedPlatform, coverPath, progress, _cts.Token));
                    OverlayProgress.Hide();
                    LoadGames();
                    ShowMessage("¡Escaneo de carátulas finalizado!");
                }
                catch (Exception ex)
                {
                    OverlayProgress.Hide();
                    ShowMessage($"Error durante el escaneo: {ex.Message}");
                }
            }
        }

        private async Task ScanMassiveCoversAsync()
        {
            SoundHelper.PlaySelect();
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Seleccionar Carpeta Raíz de Media (Emumovies/Biblioteca Externa)",
                AllowMultiple = false
            });

            if (folders.Count >= 1)
            {
                string rootPath = folders[0].Path.LocalPath;
                _cts = new CancellationTokenSource();
                OverlayProgress.Update("Escaneo Masivo de Arte...", 0, "Iniciando análisis masivo de directorios...");

                var progress = new Progress<ScanProgress>(p => {
                    OverlayProgress.Update("Escaneo Masivo de Arte...", p.Percentage, p.Detail);
                });

                try
                {
                    await Task.Run(() => _scannerService.ScanMassiveCoversAsync(rootPath, progress, _cts.Token));
                    OverlayProgress.Hide();
                    LoadGames();
                    ShowMessage("¡Escaneo masivo finalizado!");
                }
                catch (Exception ex)
                {
                    OverlayProgress.Hide();
                    ShowMessage($"Error durante el escaneo: {ex.Message}");
                }
            }
        }
    }
}
