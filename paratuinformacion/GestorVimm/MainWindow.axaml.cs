using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace GestorVimm;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainWindowViewModel
        {
            PickTxtFileAsync = PickTxtFileAsync,
            PickOutputFolderAsync = PickOutputFolderAsync
        };
        DataContext = vm;

        SetupTxtDragDrop();
    }

    private void SetupTxtDragDrop()
    {
        TxtDropZone.AddHandler(DragDrop.DragOverEvent, OnTxtDragOver);
        TxtDropZone.AddHandler(DragDrop.DragLeaveEvent, OnTxtDragLeave);
        TxtDropZone.AddHandler(DragDrop.DropEvent, OnTxtDrop);
    }

    private void OnTxtDragOver(object? sender, DragEventArgs e)
    {
        if (TryGetTxtPath(e.DataTransfer, out _))
        {
            e.DragEffects = DragDropEffects.Copy;
            TxtDropZone.Classes.Add("drag-over");
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnTxtDragLeave(object? sender, DragEventArgs e)
    {
        TxtDropZone.Classes.Remove("drag-over");
        e.Handled = true;
    }

    private void OnTxtDrop(object? sender, DragEventArgs e)
    {
        TxtDropZone.Classes.Remove("drag-over");

        if (TryGetTxtPath(e.DataTransfer, out var path) && DataContext is MainWindowViewModel vm)
            vm.LoadTxtFile(path);

        e.Handled = true;
    }

    private static bool TryGetTxtPath(IDataTransfer data, out string path)
    {
        path = string.Empty;

        if (!data.Contains(DataFormat.File))
            return false;

        if (data.TryGetFiles() is not { } files)
            return false;

        var localPath = files.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(localPath))
            return false;

        if (!localPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return false;

        path = localPath;
        return true;
    }

    private async Task<string?> PickTxtFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar listado de juegos",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Texto") { Patterns = ["*.txt"] },
                new FilePickerFileType("Todos") { Patterns = ["*.*"] }
            ]
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickOutputFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Carpeta para guardar carátulas",
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
