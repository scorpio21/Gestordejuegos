using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GestorJuegos.Models;
using System;

namespace GestorJuegos.Views.Panels
{
    public partial class TopBarView : UserControl
    {
        public event EventHandler<string>? MenuActionRequested;
        public event EventHandler<string>? ViewActionRequested;
        public event EventHandler<string>? SortActionRequested;
        public event EventHandler<string>? ArtTypeActionRequested;
        public event EventHandler<string>? BadgeActionRequested;
        public event EventHandler<string>? HelpActionRequested;
        public event EventHandler<string>? SearchTextChanged;
        public event EventHandler? QuickFavoriteClicked;
        public event EventHandler? ToggleThemeRequested;
        public event EventHandler? AddGameRequested;
        public event EventHandler? ShowStatsRequested;
        public event EventHandler? ManagePlatformsRequested;
        public event EventHandler<string>? ViewToggleClicked;
        public event EventHandler? SortAscendingToggled;

        public TopBarView()
        {
            InitializeComponent();
        }

        public void UpdateCheckmarks(string currentSortField, bool isSortAscending, AppSettings settings, bool showFavoriteBadge, bool showRegionBadge, bool showStatusBadge, bool isGridView, bool showFilters)
        {
            try
            {
                // 1. Actualizar checkmarks de ordenación
                if (SortByName != null) SortByName.Icon = currentSortField == "Name" ? CloneCheck() : null;
                if (SortByYear != null) SortByYear.Icon = currentSortField == "Year" ? CloneCheck() : null;
                if (SortByDateAdded != null) SortByDateAdded.Icon = currentSortField == "DateAdded" ? CloneCheck() : null;
                if (SortByDeveloper != null) SortByDeveloper.Icon = currentSortField == "Developer" ? CloneCheck() : null;
                if (SortByIsFavorite != null) SortByIsFavorite.Icon = currentSortField == "IsFavorite" ? CloneCheck() : null;
                if (SortByGenre != null) SortByGenre.Icon = currentSortField == "Genre" ? CloneCheck() : null;
                if (SortByLastPlayed != null) SortByLastPlayed.Icon = currentSortField == "LastPlayed" ? CloneCheck() : null;
                if (SortByExternalDbId != null) SortByExternalDbId.Icon = currentSortField == "ExternalDbId" ? CloneCheck() : null;
                if (SortByMaxPlayers != null) SortByMaxPlayers.Icon = currentSortField == "MaxPlayers" ? CloneCheck() : null;
                if (SortByPlayCount != null) SortByPlayCount.Icon = currentSortField == "PlayCount" ? CloneCheck() : null;
                if (SortByPlayTime != null) SortByPlayTime.Icon = currentSortField == "PlayTime" ? CloneCheck() : null;
                if (SortByRating != null) SortByRating.Icon = currentSortField == "Rating" ? CloneCheck() : null;
                if (SortByRegion != null) SortByRegion.Icon = currentSortField == "Region" ? CloneCheck() : null;
                if (SortByReleaseDate != null) SortByReleaseDate.Icon = currentSortField == "ReleaseDate" ? CloneCheck() : null;
                if (SortByPlayStatus != null) SortByPlayStatus.Icon = currentSortField == "PlayStatus" ? CloneCheck() : null;
                if (SortByVersion != null) SortByVersion.Icon = currentSortField == "Version" ? CloneCheck() : null;

                if (SubSortByName != null) SubSortByName.Icon = currentSortField == "Name" ? CloneCheck() : null;
                if (SubSortByYear != null) SubSortByYear.Icon = currentSortField == "Year" ? CloneCheck() : null;
                if (SubSortByDateAdded != null) SubSortByDateAdded.Icon = currentSortField == "DateAdded" ? CloneCheck() : null;
                if (SubSortByDeveloper != null) SubSortByDeveloper.Icon = currentSortField == "Developer" ? CloneCheck() : null;
                if (SubSortByIsFavorite != null) SubSortByIsFavorite.Icon = currentSortField == "IsFavorite" ? CloneCheck() : null;
                if (SubSortByGenre != null) SubSortByGenre.Icon = currentSortField == "Genre" ? CloneCheck() : null;
                if (SubSortByLastPlayed != null) SubSortByLastPlayed.Icon = currentSortField == "LastPlayed" ? CloneCheck() : null;
                if (SubSortByExternalDbId != null) SubSortByExternalDbId.Icon = currentSortField == "ExternalDbId" ? CloneCheck() : null;
                if (SubSortByMaxPlayers != null) SubSortByMaxPlayers.Icon = currentSortField == "MaxPlayers" ? CloneCheck() : null;
                if (SubSortByPlayCount != null) SubSortByPlayCount.Icon = currentSortField == "PlayCount" ? CloneCheck() : null;
                if (SubSortByPlayTime != null) SubSortByPlayTime.Icon = currentSortField == "PlayTime" ? CloneCheck() : null;
                if (SubSortByRating != null) SubSortByRating.Icon = currentSortField == "Rating" ? CloneCheck() : null;
                if (SubSortByRegion != null) SubSortByRegion.Icon = currentSortField == "Region" ? CloneCheck() : null;
                if (SubSortByReleaseDate != null) SubSortByReleaseDate.Icon = currentSortField == "ReleaseDate" ? CloneCheck() : null;
                if (SubSortByPlayStatus != null) SubSortByPlayStatus.Icon = currentSortField == "PlayStatus" ? CloneCheck() : null;
                if (SubSortByVersion != null) SubSortByVersion.Icon = currentSortField == "Version" ? CloneCheck() : null;

                if (ChkSortAscending != null) ChkSortAscending.Text = isSortAscending ? "✓" : "";

                // 2. Grupo de Imagen
                string prefArt = settings.PreferredArtType;
                if (ArtTypeBackground != null) ArtTypeBackground.Icon = prefArt == "Background" ? CloneCheck() : null;
                if (ArtTypeBox != null) ArtTypeBox.Icon = prefArt == "Box" ? CloneCheck() : null;
                if (ArtTypeBox3D != null) ArtTypeBox3D.Icon = prefArt == "Box 3D" ? CloneCheck() : null;
                if (ArtTypeCartFront != null) ArtTypeCartFront.Icon = prefArt == "Cart - Front" ? CloneCheck() : null;
                if (ArtTypeCart3D != null) ArtTypeCart3D.Icon = prefArt == "Cart - 3D" ? CloneCheck() : null;
                if (ArtTypeClearLogo != null) ArtTypeClearLogo.Icon = prefArt == "Clear Logo" ? CloneCheck() : null;
                if (ArtTypeMarquee != null) ArtTypeMarquee.Icon = prefArt == "Marquee" ? CloneCheck() : null;
                if (ArtTypeSnap != null) ArtTypeSnap.Icon = prefArt == "Snap" ? CloneCheck() : null;

                if (SubArtTypeBackground != null) SubArtTypeBackground.Icon = prefArt == "Background" ? CloneCheck() : null;
                if (SubArtTypeBox != null) SubArtTypeBox.Icon = prefArt == "Box" ? CloneCheck() : null;
                if (SubArtTypeBox3D != null) SubArtTypeBox3D.Icon = prefArt == "Box 3D" ? CloneCheck() : null;
                if (SubArtTypeCartFront != null) SubArtTypeCartFront.Icon = prefArt == "Cart - Front" ? CloneCheck() : null;
                if (SubArtTypeCart3D != null) SubArtTypeCart3D.Icon = prefArt == "Cart - 3D" ? CloneCheck() : null;
                if (SubArtTypeClearLogo != null) SubArtTypeClearLogo.Icon = prefArt == "Clear Logo" ? CloneCheck() : null;
                if (SubArtTypeMarquee != null) SubArtTypeMarquee.Icon = prefArt == "Marquee" ? CloneCheck() : null;
                if (SubArtTypeSnap != null) SubArtTypeSnap.Icon = prefArt == "Snap" ? CloneCheck() : null;

                // 3. Insignias
                if (ChkBadgeFavorite != null) ChkBadgeFavorite.Text = showFavoriteBadge ? "✓" : "";
                if (ChkBadgeRegion != null) ChkBadgeRegion.Text = showRegionBadge ? "✓" : "";
                if (ChkBadgePlayStatus != null) ChkBadgePlayStatus.Text = showStatusBadge ? "✓" : "";

                // 4. Vistas
                if (ChkViewGrid != null) ChkViewGrid.Text = isGridView ? "✓" : "";
                if (ChkViewList != null) ChkViewList.Text = !isGridView ? "✓" : "";
                if (ChkShowFilters != null) ChkShowFilters.Text = showFilters ? "✓" : "";
                
                if (BtnViewGrid != null) BtnViewGrid.IsChecked = isGridView;
                if (BtnViewList != null) BtnViewList.IsChecked = !isGridView;
            }
            catch { }
        }

        private TextBlock CloneCheck() => new TextBlock { Text = "✓", Foreground = Brushes.LightGreen, FontWeight = FontWeight.Bold };

        private void OnMenuAction(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item) MenuActionRequested?.Invoke(this, item.Name ?? "");
        }

        private void OnViewAction(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item) ViewActionRequested?.Invoke(this, item.Name ?? "");
        }

        private void OnSortAction(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item) SortActionRequested?.Invoke(this, item.Name ?? "");
        }

        private void OnArtTypeAction(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item) ArtTypeActionRequested?.Invoke(this, item.Name ?? "");
        }

        private void OnBadgeAction(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item) BadgeActionRequested?.Invoke(this, item.Name ?? "");
        }

        private void OnHelpAction(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item) HelpActionRequested?.Invoke(this, item.Name ?? "");
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            SearchTextChanged?.Invoke(this, (sender as TextBox)?.Text ?? "");
        }

        private void OnQuickFavoriteClick(object? sender, RoutedEventArgs e)
        {
            QuickFavoriteClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnToggleThemeClick(object? sender, RoutedEventArgs e)
        {
            ToggleThemeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddGameClick(object? sender, RoutedEventArgs e)
        {
            AddGameRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnShowStatsClick(object? sender, RoutedEventArgs e)
        {
            ShowStatsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnManagePlatformsClick(object? sender, RoutedEventArgs e)
        {
            ManagePlatformsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnViewToggleClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Control c) ViewToggleClicked?.Invoke(this, c.Name ?? "");
        }

        private void OnSortAscendingAction(object? sender, RoutedEventArgs e)
        {
            SortAscendingToggled?.Invoke(this, EventArgs.Empty);
        }

        // Métodos de utilidad para actualizar la UI
        public void SetGameStatusInfo(string info)
        {
            TxtGameStatusInfo.Text = info;
        }

        public void SetSearchText(string text)
        {
            TxtSearchGame.Text = text;
        }

        public TextBox SearchBox => TxtSearchGame;
        public string GetSearchText() => TxtSearchGame.Text ?? "";

        public bool IsQuickFavoriteChecked
        {
            get => BtnQuickFavorite.IsChecked ?? false;
            set => BtnQuickFavorite.IsChecked = value;
        }

        public string ThemeIcon
        {
            get => BtnToggleTheme.Content?.ToString() ?? "🌙";
            set => BtnToggleTheme.Content = value;
        }

        public bool IsGridView => BtnViewGrid.IsChecked ?? false;
        public bool IsListView => BtnViewList.IsChecked ?? false;
        public bool IsWheelVerticalView => BtnViewWheelVertical.IsChecked ?? false;
        public bool IsWheelHorizontalView => BtnViewWheelHorizontal.IsChecked ?? false;

        public bool IsGamepadModeEnabled
        {
            get => BtnToggleGamepad.IsChecked ?? false;
            set => BtnToggleGamepad.IsChecked = value;
        }

        public string GamepadButtonContent
        {
            get => BtnToggleGamepad.Content?.ToString() ?? "";
            set => BtnToggleGamepad.Content = value;
        }

        public IBrush? GamepadButtonForeground
        {
            get => BtnToggleGamepad.Foreground;
            set => BtnToggleGamepad.Foreground = value;
        }

        public void SetViewMode(bool isGridView, bool isListView = false, bool isWheelVertical = false, bool isWheelHorizontal = false)
        {
            BtnViewGrid.IsChecked = isGridView;
            BtnViewList.IsChecked = isListView;
            BtnViewWheelVertical.IsChecked = isWheelVertical;
            BtnViewWheelHorizontal.IsChecked = isWheelHorizontal;
        }

        public void FocusSearch() => TxtSearchGame.Focus();
        public void FocusAddButton() => BtnAddGame.Focus();
    }
}