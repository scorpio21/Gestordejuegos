using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Data;
using GestorJuegos.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GestorJuegos.Views.Windows
{
    public partial class MainWindow : Window
    {
        private void OnTopBarViewToggle(object? sender, string name)
        {
            bool grid = name == "BtnViewGrid";
            bool list = name == "BtnViewList";
            bool wheelV = name == "BtnViewWheelVertical";
            bool wheelH = name == "BtnViewWheelHorizontal";

            TopBar.SetViewMode(grid, list, wheelV, wheelH);
            Library.SetViewMode(grid, list, wheelV, wheelH);
            UpdateMenuCheckmarks();
        }

        private void OnTopBarSortAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            string field = name.Replace("SortBy", "").Replace("SubSortBy", "");
            _currentSortField = field;
            Library.SortField = field;
            Library.ApplyFilters();
            UpdateMenuCheckmarks();
        }

        private void OnTopBarArtTypeAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            string cleanName = name.Replace("ArtType", "").Replace("SubArtType", "");
            string artType = cleanName switch
            {
                "Background" => "Background",
                "Box" => "Box",
                "Box3D" => "Box 3D",
                "CartFront" => "Cart - Front",
                "Cart3D" => "Cart - 3D",
                "ClearLogo" => "Clear Logo",
                "Marquee" => "Marquee",
                "Snap" => "Snap",
                _ => "Box"
            };
            _settings.PreferredArtType = artType;
            Library.ApplyFilters();
            UpdateMenuCheckmarks();
        }

        private void OnTopBarBadgeAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            if (name.Contains("Favorite")) _showFavoriteBadge = !_showFavoriteBadge;
            else if (name.Contains("Region")) _showRegionBadge = !_showRegionBadge;
            else if (name.Contains("PlayStatus")) _showStatusBadge = !_showStatusBadge;
            
            Library.ApplyFilters();
            UpdateMenuCheckmarks();
        }

        private void OnTopBarHelpAction(object? sender, string name)
        {
            SoundHelper.PlaySelect();
            switch (name)
            {
                case "MenuHelpExternalLib": ShowHelpExternalLib(); break;
                case "MenuHelpImportFolder": ShowHelpImportFolder(); break;
                case "MenuHelpEmulator": ShowHelpEmulator(); break;
                case "MenuHelpMultiDisk": ShowHelpMultiDisk(); break;
                case "MenuHelpDatabase": ShowHelpDatabase(); break;
                case "MenuAbout": ShowAbout(); break;
                default: ShowMessage($"Ayuda sobre {name}"); break;
            }
        }

        private async void OnTopBarMenuAction(object? sender, string action)
        {
            switch (action)
            {
                case "MenuExportDB":
                    OverlayExportOptions.IsVisible = true;
                    break;
                case "MenuImportDB":
                    await ImportDatabaseAsync();
                    break;
                case "MenuImportFolders":
                    await ImportFromFolder();
                    break;
                case "MenuImportExternalLib":
                    await ImportExternalLibraryAsync();
                    break;
                case "MenuImportDat":
                    await ImportFromDatAsync();
                    break;
                case "MenuSyncExternalLib":
                    await SyncExternalLibraryAsync();
                    break;
                case "MenuSyncMasterDb":
                    await SyncWithMasterDbAsync();
                    break;
                case "MenuScanLocalCovers":
                    await ScanLocalCoversAsync();
                    break;
                case "MenuMassiveScanCovers":
                    await ScanMassiveCoversAsync();
                    break;
                case "MenuCleanupOrphans":
                    CleanupOrphans();
                    break;
                case "MenuManageDross":
                    ManageDross();
                    break;
                case "MenuShowStats":
                    ShowFullStats();
                    break;
                case "MenuManagePlatforms":
                    OverlayManagePlatforms.Initialize(_gameService);
                    OverlayManagePlatforms.IsVisible = true;
                    break;
                case "MenuSettings":
                    var options = new OpcionesWindow(_settings);
                    if (await options.ShowDialog<bool>(this))
                    {
                        SaveSettings();
                        ApplyTheme();
                        Sidebar.Initialize(_gameService, _settings);
                        Library.Initialize(_gameService, _settings);
                        Library.ApplyFilters();
                    }
                    break;
            }
        }

        private void UpdateMenuCheckmarks()
        {
            TopBar.UpdateCheckmarks(_currentSortField, _isSortAscending, _settings, _showFavoriteBadge, _showRegionBadge, _showStatusBadge, Library.IsGridView, Sidebar.IsVisible);
        }
    }
}
