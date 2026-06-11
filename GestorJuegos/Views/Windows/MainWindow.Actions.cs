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
}
