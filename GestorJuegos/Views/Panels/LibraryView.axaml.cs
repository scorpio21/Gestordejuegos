using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GestorJuegos.Models;
using GestorJuegos.Services;
using GestorJuegos.Utils;
using Microsoft.EntityFrameworkCore;

namespace GestorJuegos.Views.Panels
{
    public partial class LibraryView : UserControl
    {
        private GameService? _gameService;
        private AppSettings? _settings;
        
        private List<Game> _allGames = new List<Game>();
        private Platform? _selectedPlatform;
        private int _currentPage = 1;
        private const int PageSize = 100;
        
        private bool _isSortAscending = true;
        private string _currentSearch = "";
        
        public string SortField { get; set; } = "Name";
        public bool IsGridView => LstGamesGrid.IsVisible;
        public bool IsSortAscending
        {
            get => _isSortAscending;
            set => _isSortAscending = value;
        }
        
        private bool _isSyncingSelection = false;

        public event EventHandler<Game>? GameSelected;
        public event EventHandler? PlatformsWallRequested;
        public event EventHandler<Game>? EditGameRequested;
        public event EventHandler<Game>? DeleteGameRequested;

        public LibraryView()
        {
            InitializeComponent();
            SetupLocalEvents();
        }

        public void Initialize(GameService gameService, AppSettings settings)
        {
            _gameService = gameService;
            _settings = settings;
        }

        private void SetupLocalEvents()
        {
            BtnNextPage.Click += (s, e) => { _currentPage++; ApplyFilters(); ScrollToTop(); };
            BtnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; ApplyFilters(); ScrollToTop(); } };
            BtnToggleFilters.Click += (s, e) => PnlFilters.IsVisible = !PnlFilters.IsVisible;
            BtnApplyFilters.Click += (s, e) => { _currentPage = 1; ApplyFilters(); };
            BtnClearFilters.Click += (s, e) => { 
                CmbFilterRegion.SelectedIndex = 0; 
                NumFilterYear.Value = 0; 
                _currentPage = 1; 
                ApplyFilters(); 
            };
            BtnShowPlatformsWall.Click += (s, e) => PlatformsWallRequested?.Invoke(this, EventArgs.Empty);

            LstGames.SelectionChanged += LstGames_SelectionChanged;
            LstGamesGrid.SelectionChanged += LstGames_SelectionChanged;
            LstGamesWheelVertical.SelectionChanged += LstGames_SelectionChanged;
            LstGamesWheelHorizontal.SelectionChanged += LstGames_SelectionChanged;

            // Eventos de Rueda
            LstGamesWheelVertical.EffectiveViewportChanged += (s, e) => UpdateWheelCurvature();
            LstGamesWheelVertical.LayoutUpdated += (s, e) => UpdateWheelCurvature();
            LstGamesWheelHorizontal.EffectiveViewportChanged += (s, e) => UpdateHorizontalWheelCurvature();
            LstGamesWheelHorizontal.LayoutUpdated += (s, e) => UpdateHorizontalWheelCurvature();
        }

        public void UpdateGames(List<Game> games, Platform? platform = null)
        {
            _allGames = games;
            _selectedPlatform = platform;
            _currentPage = 1;
            PnlDashboard.IsVisible = games.Count == 0;
            ApplyFilters();
        }

        public void SetSearchText(string text)
        {
            _currentSearch = text;
            _currentPage = 1;
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            if (_allGames == null) return;

            var filtered = _allGames.AsEnumerable();

            // 1. Búsqueda
            if (!string.IsNullOrEmpty(_currentSearch))
            {
                string s = _currentSearch.ToLower();
                filtered = filtered.Where(g => g.Name.ToLower().Contains(s) || (g.Genre != null && g.Genre.ToLower().Contains(s)));
            }

            // 2. Región
            if (CmbFilterRegion.SelectedIndex > 0)
            {
                string reg = (CmbFilterRegion.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(g => g.Region != null && g.Region.Contains(reg));
            }

            // 3. Año
            if (NumFilterYear.Value > 0)
            {
                filtered = filtered.Where(g => g.Year == (int)NumFilterYear.Value);
            }

            // 4. Ordenación
            Func<Game, object> keySelector = SortField switch
            {
                "Year" => g => g.Year,
                "DateAdded" => g => g.DateAdded,
                "Developer" => g => g.Developer ?? "",
                "IsFavorite" => g => g.IsFavorite ? 0 : 1,
                "Genre" => g => g.Genre ?? "",
                "LastPlayed" => g => g.LastPlayed ?? DateTime.MinValue,
                "ExternalDbId" => g => g.ExternalDbId ?? "",
                "MaxPlayers" => g => g.MaxPlayers ?? 0,
                "PlayCount" => g => g.PlayCount,
                "PlayTime" => g => g.PlayTime,
                "Rating" => g => g.Rating,
                "Region" => g => g.Region ?? "",
                "ReleaseDate" => g => g.ReleaseDate ?? "",
                "PlayStatus" => g => g.PlayStatus ?? "",
                "Version" => g => g.Version ?? "",
                _ => g => g.Name
            };

            filtered = _isSortAscending ? filtered.OrderBy(keySelector) : filtered.OrderByDescending(keySelector);

            var filteredList = filtered.ToList();
            int totalItems = filteredList.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            if (totalPages == 0) totalPages = 1;
            if (_currentPage > totalPages) _currentPage = totalPages;

            var paginated = filteredList.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();

            UpdateItemsSource(paginated);
            
            TxtSelectedPlatform.Text = _selectedPlatform != null ? $"{_selectedPlatform.Name} ({totalItems} juegos)" : $"Colección ({totalItems} juegos)";
            TxtPageInfo.Text = $"Página {_currentPage} de {totalPages}";
            PnlPagination.IsVisible = totalPages > 1;
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void UpdateItemsSource(List<Game> games)
        {
            // Cargar miniaturas
            if (_gameService != null && _settings != null)
            {
                foreach (var game in games)
                {
                    string artType = !string.IsNullOrEmpty(game.SelectedArtType) ? game.SelectedArtType : _settings.PreferredArtType;
                    game.Cover = _gameService.GetGameThumbnail(game.Id, FileHelpers.GetExternalFolderName(artType));
                }
            }

            LstGames.ItemsSource = null; LstGames.ItemsSource = games;
            LstGamesGrid.ItemsSource = null; LstGamesGrid.ItemsSource = games;
            LstGamesWheelVertical.ItemsSource = null; LstGamesWheelVertical.ItemsSource = games;
            LstGamesWheelHorizontal.ItemsSource = null; LstGamesWheelHorizontal.ItemsSource = games;

            Dispatcher.UIThread.Post(UpdateWheels, DispatcherPriority.Background);
        }

        private void LstGames_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;
            if (sender is ListBox lb && lb.SelectedItem is Game game)
            {
                _isSyncingSelection = true;
                if (LstGames != lb) LstGames.SelectedItem = game;
                if (LstGamesGrid != lb) LstGamesGrid.SelectedItem = game;
                if (LstGamesWheelVertical != lb) LstGamesWheelVertical.SelectedItem = game;
                if (LstGamesWheelHorizontal != lb) LstGamesWheelHorizontal.SelectedItem = game;
                _isSyncingSelection = false;

                SoundHelper.PlayNavigation();
                UpdateWheels();
                GameSelected?.Invoke(this, game);
            }
        }

        public void SetViewMode(bool isGrid, bool isList, bool isWheelV, bool isWheelH)
        {
            LstGamesGrid.IsVisible = isGrid;
            LstGames.IsVisible = isList;
            LstGamesWheelVertical.IsVisible = isWheelV;
            LstGamesWheelHorizontal.IsVisible = isWheelH;
            UpdateWheels();
        }

        private void UpdateWheels()
        {
            UpdateWheelCurvature();
            UpdateHorizontalWheelCurvature();
        }

        private void UpdateWheelCurvature()
        {
            if (!LstGamesWheelVertical.IsVisible) return;
            double center = LstGamesWheelVertical.Bounds.Height / 2;
            if (center <= 0) return;

            var items = LstGamesWheelVertical.ItemsSource as System.Collections.IList;
            if (items == null) return;

            foreach (var item in items)
            {
                if (LstGamesWheelVertical.ContainerFromItem(item) is ListBoxItem container)
                {
                    var pos = container.TranslatePoint(new Point(0, 0), LstGamesWheelVertical);
                    if (pos.HasValue)
                    {
                        double itemCenter = pos.Value.Y + (container.Bounds.Height / 2);
                        double dist = itemCenter - center;
                        double ratio = Math.Clamp(dist / center, -1.2, 1.2);
                        double shiftX = -80 * (ratio * ratio);
                        double scale = container.IsSelected ? 1.2 : (1.0 - Math.Abs(ratio) * 0.2);
                        
                        var group = new TransformGroup();
                        group.Children.Add(new RotateTransform(ratio * 15));
                        group.Children.Add(new ScaleTransform(scale, scale));
                        group.Children.Add(new TranslateTransform(shiftX + (container.IsSelected ? 40 : 0), 0));
                        container.RenderTransform = group;
                        container.Opacity = 1.0 - Math.Abs(ratio) * 0.4;
                        container.ZIndex = container.IsSelected ? 100 : (int)(50 - Math.Abs(dist) / 5);
                    }
                }
            }
        }

        private void UpdateHorizontalWheelCurvature()
        {
            if (!LstGamesWheelHorizontal.IsVisible) return;
            double center = LstGamesWheelHorizontal.Bounds.Width / 2;
            if (center <= 0) return;

            var items = LstGamesWheelHorizontal.ItemsSource as System.Collections.IList;
            if (items == null) return;

            foreach (var item in items)
            {
                if (LstGamesWheelHorizontal.ContainerFromItem(item) is ListBoxItem container)
                {
                    var pos = container.TranslatePoint(new Point(0, 0), LstGamesWheelHorizontal);
                    if (pos.HasValue)
                    {
                        double itemCenter = pos.Value.X + (container.Bounds.Width / 2);
                        double dist = itemCenter - center;
                        double ratio = Math.Clamp(dist / center, -1.0, 1.0);
                        double shiftY = 60 * (ratio * ratio);
                        double scale = container.IsSelected ? 1.2 : 0.8;

                        var group = new TransformGroup();
                        group.Children.Add(new ScaleTransform(scale * (1.0 - Math.Abs(ratio) * 0.2), scale));
                        group.Children.Add(new SkewTransform(0, ratio * 10));
                        group.Children.Add(new TranslateTransform(0, shiftY));
                        container.RenderTransform = group;
                        container.ZIndex = container.IsSelected ? 100 : (int)(50 - Math.Abs(dist) / 10);
                    }
                }
            }
        }

        private void ScrollToTop()
        {
            var first = LstGames.Items.Cast<object>().FirstOrDefault();
            if (first == null) return;
            if (LstGames.IsVisible) LstGames.ScrollIntoView(first);
            if (LstGamesGrid.IsVisible) LstGamesGrid.ScrollIntoView(first);
        }

        private void MenuContextEdit_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is Game game)
            {
                SelectGameInActiveList(game);
                EditGameRequested?.Invoke(this, game);
            }
        }

        private void MenuContextDelete_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is Game game)
            {
                SelectGameInActiveList(game);
                DeleteGameRequested?.Invoke(this, game);
            }
        }

        private void SelectGameInActiveList(Game game)
        {
            if (LstGames.IsVisible) LstGames.SelectedItem = game;
            else if (LstGamesGrid.IsVisible) LstGamesGrid.SelectedItem = game;
            else if (LstGamesWheelVertical.IsVisible) LstGamesWheelVertical.SelectedItem = game;
            else if (LstGamesWheelHorizontal.IsVisible) LstGamesWheelHorizontal.SelectedItem = game;
            else if (PnlGlobalSearch.IsVisible) LstGlobalSearchResults.SelectedItem = game;
        }
    }
}