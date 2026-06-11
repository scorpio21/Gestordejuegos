using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GestorJuegos.Models;
using GestorJuegos.Services;
using Microsoft.EntityFrameworkCore;

namespace GestorJuegos.Views.Panels
{
    public partial class SidebarView : UserControl
    {
        private GameService? _gameService;
        private AppSettings? _settings;

        // Eventos para comunicación con MainWindow
        public event EventHandler<SidebarNode>? SelectionChanged;
        public event EventHandler<Platform>? PlatformEditRequested;
        public event EventHandler<Platform>? PlatformDeleteRequested;
        public event EventHandler? AddPlatformRequested;

        public SidebarView()
        {
            InitializeComponent();
            
            TvSidebar.SelectionChanged += TvSidebar_SelectionChanged;
            CmbSidebarView.SelectionChanged += CmbSidebarView_SelectionChanged;
            BtnAddPlatform.Click += (s, e) => AddPlatformRequested?.Invoke(this, EventArgs.Empty);
            
            // Context Menu Handlers
            MenuEditPlatform.Click += MenuEditPlatform_Click;
            MenuDeletePlatform.Click += MenuDeletePlatform_Click;
        }

        public void Initialize(GameService gameService, AppSettings settings)
        {
            _gameService = gameService;
            _settings = settings;
            LoadPlatforms();
        }

        public void SetAppLogo(Avalonia.Media.Imaging.Bitmap? bitmap)
        {
            ImgAppLogo.Source = bitmap;
            ImgAppLogo.IsVisible = bitmap != null;
        }

        public void LoadPlatforms()
        {
            if (_gameService == null || _settings == null) return;

            int viewIndex = CmbSidebarView.SelectedIndex;
            string lbPath = _settings.ExternalLibraryPath ?? "";

            Task.Run(() => {
                try
                {
                    using var context = new GestorJuegos.Data.AppDbContext();
                    int totalGames = context.Games.Count();
                    int favoritesCount = context.Games.Count(g => g.IsFavorite);

                    byte[]? allGamesIcon = null;
                    if (!string.IsNullOrEmpty(lbPath) && Directory.Exists(lbPath))
                    {
                        string allGamesPath = Path.Combine(lbPath, "Images", "Platform Icons", "All Games.png");
                        if (File.Exists(allGamesPath))
                        {
                            try { allGamesIcon = File.ReadAllBytes(allGamesPath); } catch { }
                        }
                    }

                    var sidebarNodes = new List<SidebarNode>();

                    // Nodo raíz "Todo"
                    var rootNode = new SidebarNode
                    {
                        Name = "Todo",
                        Count = totalGames,
                        Tag = "ALL",
                        IconBytes = allGamesIcon,
                        ResortIcon = "🏠",
                        IsExpanded = true
                    };
                    sidebarNodes.Add(rootNode);

                    if (viewIndex == 0) // CATEGORÍA DE PLATAFORMA
                    {
                        LoadCategoryView(context, sidebarNodes, lbPath);
                    }
                    else if (viewIndex == 1) // PLATAFORMAS (Lista plana)
                    {
                        LoadPlatformListView(context, sidebarNodes);
                    }
                    else if (viewIndex == 2) // GÉNEROS
                    {
                        LoadGenreView(sidebarNodes);
                    }
                    else if (viewIndex == 3) // REGIONES
                    {
                        LoadRegionView(sidebarNodes);
                    }
                    else if (viewIndex == 4) // BIBLIOTECA
                    {
                        sidebarNodes.Add(new SidebarNode
                        {
                            Name = "Favoritos",
                            Count = favoritesCount,
                            Tag = "FAVORITES",
                            ResortIcon = "⭐"
                        });
                    }

                    Dispatcher.UIThread.Post(() => {
                        TvSidebar.ItemsSource = sidebarNodes;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error en LoadPlatforms: {ex.Message}");
                }
            });
        }

        private void LoadCategoryView(GestorJuegos.Data.AppDbContext context, List<SidebarNode> sidebarNodes, string lbPath)
        {
            var dbCategories = context.PlatformCategories.ToList();
            var platforms = context.Platforms.ToList();
            var gamesCount = _gameService!.GetGamesCountByPlatform() ?? new Dictionary<string, int>();

            var categoryNodes = new Dictionary<string, SidebarNode>();
            foreach (var cat in dbCategories.OrderBy(c => c.Name))
            {
                string emoji = cat.Name == "Computers" ? "🖥️" : 
                               (cat.Name == "Handhelds" ? "📟" : 
                               (cat.Name == "Arcade" ? "🕹️" : "🎮"));
                categoryNodes[cat.Name] = new SidebarNode
                {
                    Name = cat.Name,
                    Tag = ("CATEGORY", cat.Name),
                    IconBytes = cat.Icon,
                    ResortIcon = emoji,
                    IsExpanded = true
                };
            }

            foreach (var platform in platforms.OrderBy(p => p.Name))
            {
                int pCount = gamesCount.ContainsKey(platform.Name) ? gamesCount[platform.Name] : 0;
                string categoryName = platform.Category;

                if (!categoryNodes.ContainsKey(categoryName))
                {
                    categoryNodes[categoryName] = new SidebarNode
                    {
                        Name = categoryName,
                        Tag = ("CATEGORY", categoryName),
                        ResortIcon = "🎮",
                        IsExpanded = true
                    };
                }

                categoryNodes[categoryName].Children.Add(new SidebarNode
                {
                    Name = platform.Name,
                    Count = pCount,
                    Tag = ("PLATFORM", platform.Name),
                    IconBytes = platform.Icon,
                    ResortIcon = "🎮",
                    IsExpanded = true
                });
            }

            foreach (var catEntry in categoryNodes.OrderBy(c => c.Key))
            {
                if (catEntry.Value.Children.Count > 0)
                {
                    catEntry.Value.Count = catEntry.Value.Children.Sum(child => child.Count);
                    sidebarNodes.Add(catEntry.Value);
                }
            }
        }

        private void LoadPlatformListView(GestorJuegos.Data.AppDbContext context, List<SidebarNode> sidebarNodes)
        {
            var platforms = context.Platforms.ToList();
            var gamesCount = _gameService!.GetGamesCountByPlatform() ?? new Dictionary<string, int>();

            foreach (var platform in platforms.OrderBy(p => p.Name))
            {
                int pCount = gamesCount.ContainsKey(platform.Name) ? gamesCount[platform.Name] : 0;
                sidebarNodes.Add(new SidebarNode
                {
                    Name = platform.Name,
                    Count = pCount,
                    Tag = ("PLATFORM", platform.Name),
                    IconBytes = platform.Icon,
                    ResortIcon = "🎮"
                });
            }
        }

        private void LoadGenreView(List<SidebarNode> sidebarNodes)
        {
            var genreStats = _gameService!.GetGenresWithCount();
            if (genreStats != null)
            {
                foreach (var g in genreStats.OrderByDescending(x => x.Value).Take(20))
                {
                    sidebarNodes.Add(new SidebarNode
                    {
                        Name = g.Key,
                        Count = g.Value,
                        Tag = ("GENRE", g.Key),
                        ResortIcon = "📁"
                    });
                }
            }
        }

        private void LoadRegionView(List<SidebarNode> sidebarNodes)
        {
            var regionStats = _gameService!.GetRegionsWithCount();
            if (regionStats != null)
            {
                foreach (var r in regionStats.OrderByDescending(x => x.Value))
                {
                    sidebarNodes.Add(new SidebarNode
                    {
                        Name = r.Key,
                        Count = r.Value,
                        Tag = ("REGION", r.Key),
                        ResortIcon = "🌎"
                    });
                }
            }
        }

        private void TvSidebar_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TvSidebar.SelectedItem is SidebarNode node)
            {
                SelectionChanged?.Invoke(this, node);
            }
        }

        private void CmbSidebarView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadPlatforms();
        }

        public void SelectPlatform(string platformName)
        {
            SelectNodeByTag(("PLATFORM", platformName));
        }

        public void SelectNodeByTag(object tag)
        {
            var items = TvSidebar.ItemsSource as IEnumerable<SidebarNode>;
            if (items == null) return;

            foreach (var node in items)
            {
                if (IsMatchingTag(node.Tag, tag))
                {
                    TvSidebar.SelectedItem = node;
                    return;
                }
                foreach (var child in node.Children)
                {
                    if (IsMatchingTag(child.Tag, tag))
                    {
                        TvSidebar.SelectedItem = child;
                        return;
                    }
                }
            }
        }

        private bool IsMatchingTag(object? tag1, object? tag2)
        {
            if (tag1 == null || tag2 == null) return false;
            if (tag1.Equals(tag2)) return true;
            
            // Manejo especial para ValueTuples de (string, string)
            if (tag1 is ValueTuple<string, string> t1 && tag2 is ValueTuple<string, string> t2)
            {
                return t1.Item1 == t2.Item1 && t1.Item2 == t2.Item2;
            }
            return false;
        }

        private void MenuEditPlatform_Click(object? sender, RoutedEventArgs e)
        {
            if (TvSidebar.SelectedItem is SidebarNode node && node.Tag is ValueTuple<string, string> t && t.Item1 == "PLATFORM")
            {
                using var context = new GestorJuegos.Data.AppDbContext();
                var platform = context.Platforms.FirstOrDefault(p => p.Name == t.Item2);
                if (platform != null) PlatformEditRequested?.Invoke(this, platform);
            }
        }

        private void MenuDeletePlatform_Click(object? sender, RoutedEventArgs e)
        {
            if (TvSidebar.SelectedItem is SidebarNode node && node.Tag is ValueTuple<string, string> t && t.Item1 == "PLATFORM")
            {
                using var context = new GestorJuegos.Data.AppDbContext();
                var platform = context.Platforms.FirstOrDefault(p => p.Name == t.Item2);
                if (platform != null) PlatformDeleteRequested?.Invoke(this, platform);
            }
        }

        public SidebarNode? SelectedItem => TvSidebar.SelectedItem as SidebarNode;
    }
}