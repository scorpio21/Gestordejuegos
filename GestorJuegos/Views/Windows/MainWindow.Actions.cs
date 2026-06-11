using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GestorJuegos.Views.Windows;

public partial class MainWindow
{
    private void SaveSettings()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
        }
    }

    private async Task ImportFromFolder()
    {
        if (_selectedPlatform == null) { ShowMessage("Por favor, selecciona una plataforma."); return; }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"Seleccionar carpeta de ROMs para {_selectedPlatform.Name}",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            string path = folders[0].Path.LocalPath;
            OverlayProgress.IsVisible = true;
            
            // Extensiones delegadas al servicio o configuración
            string[] extensions = { ".zip", ".7z", ".iso", ".cue", ".bin", ".n64", ".z64", ".gba", ".nes", ".sfc", ".smc" };

            int imported = await _importService.ScanFolderAsync(path, _selectedPlatform.Id, extensions, (prog) => {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    OverlayProgress.Update("Importando Colección", 50, prog);
                });
            });

            OverlayProgress.Hide();
            _notificationService.Success($"Se han importado {imported} juegos.");
            LoadGames();
        }
    }

    private async Task CleanupOrphans()
    {
        OverlayProgress.IsVisible = true;
        await _importService.CleanupOrphanedAssetsAsync((prog) => {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                OverlayProgress.Update("Mantenimiento", 0, prog);
            });
        });
        OverlayProgress.Hide();
        _notificationService.Info("Limpieza de huérfanos completada.");
    }
}
