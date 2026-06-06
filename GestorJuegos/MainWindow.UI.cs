using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using GestorJuegos.Models;

namespace GestorJuegos
{
    public partial class MainWindow
    {
        // --- LÓGICA DE RUEDAS Y EFECTOS VISUALES ---

        private void UpdateWheels()
        {
            UpdateWheelCurvature();
            UpdateHorizontalWheelCurvature();
        }

        private void UpdateWheelCurvature()
        {
            if (LstGamesWheelVertical == null || !LstGamesWheelVertical.IsVisible) return;

            double centerOfList = LstGamesWheelVertical.Bounds.Height / 2;
            if (centerOfList <= 0) return;

            var itemsSource = LstGamesWheelVertical.ItemsSource as System.Collections.IList;
            if (itemsSource == null) return;

            foreach (var item in itemsSource)
            {
                var container = LstGamesWheelVertical.ContainerFromItem(item) as ListBoxItem;
                if (container == null) continue;

                var position = container.TranslatePoint(new Point(0, 0), LstGamesWheelVertical);
                if (position.HasValue)
                {
                    double itemCenterY = position.Value.Y + (container.Bounds.Height / 2);
                    double distanceFromCenter = itemCenterY - centerOfList;
                    double maxDistance = centerOfList;

                    double ratio = Math.Clamp(distanceFromCenter / maxDistance, -1.2, 1.2);
                    double shiftX = -80 * (ratio * ratio); 
                    double rotation = ratio * 15; 

                    bool isSelected = container.IsSelected;
                    double scale = isSelected ? 1.2 : (1.0 - Math.Abs(ratio) * 0.2); 
                    double opacity = 1.0 - Math.Abs(ratio) * 0.4;

                    container.ZIndex = isSelected ? 100 : (int)(50 - Math.Abs(distanceFromCenter) / 5);
                    container.Opacity = opacity;

                    var group = new TransformGroup();
                    group.Children.Add(new RotateTransform(rotation));
                    group.Children.Add(new ScaleTransform(scale, scale));
                    group.Children.Add(new TranslateTransform(shiftX + (isSelected ? 40 : 0), 0));
                    container.RenderTransform = group;
                }
            }
        }

        private void UpdateHorizontalWheelCurvature()
        {
            if (LstGamesWheelHorizontal == null || !LstGamesWheelHorizontal.IsVisible) return;

            double centerOfList = LstGamesWheelHorizontal.Bounds.Width / 2;
            if (centerOfList <= 0) return;

            var itemsSource = LstGamesWheelHorizontal.ItemsSource as System.Collections.IList;
            if (itemsSource == null) return;

            foreach (var item in itemsSource)
            {
                var container = LstGamesWheelHorizontal.ContainerFromItem(item) as ListBoxItem;
                if (container == null) continue;

                var position = container.TranslatePoint(new Point(0, 0), LstGamesWheelHorizontal);
                if (position.HasValue)
                {
                    double itemCenterX = position.Value.X + (container.Bounds.Width / 2);
                    double distanceFromCenter = itemCenterX - centerOfList;
                    double maxDistance = centerOfList;

                    double ratio = Math.Clamp(distanceFromCenter / maxDistance, -1.0, 1.0);
                    double shiftY = 60 * (ratio * ratio); 
                    
                    bool isSelected = container.IsSelected;
                    double scale = isSelected ? 1.2 : 0.8;
                    
                    container.ZIndex = isSelected ? 100 : (int)(50 - Math.Abs(distanceFromCenter) / 10);

                    var group = new TransformGroup();
                    group.Children.Add(new ScaleTransform(scale * (1.0 - Math.Abs(ratio) * 0.2), scale));
                    group.Children.Add(new SkewTransform(0, ratio * 10)); 
                    group.Children.Add(new TranslateTransform(0, shiftY));
                    container.RenderTransform = group;
                }
            }
        }

        // --- LÓGICA DE RETRO ACHIEVEMENTS (UI) ---

        private void BtnExpandAchievements_Click(object? sender, RoutedEventArgs e)
        {
            OverlayAchievements.IsVisible = true;
        }

        private void BtnCloseAchievements_Click(object? sender, RoutedEventArgs e)
        {
            OverlayAchievements.IsVisible = false;
        }

        private void LoadGameAchievements(Game game)
        {
            if (PanelAchievementsSmall == null) return;

            StackAchievementIcons.Children.Clear();

            if (game.Achievements == null || game.Achievements.Count == 0)
            {
                game.Achievements = new List<Achievement>
                {
                    new Achievement { Title = "Héroe de Agrabah", Description = "Escapa de los guardias en el nivel 1", IsUnlocked = true },
                    new Achievement { Title = "Cueva de las Maravillas", Description = "Entra en la cueva sin ser detectado", IsUnlocked = true },
                    new Achievement { Title = "El Genio", Description = "Frota la lámpara por primera vez", IsUnlocked = true },
                    new Achievement { Title = "Príncipe Ali", Description = "Entra en el palacio disfrazado", IsUnlocked = false },
                    new Achievement { Title = "Jafar el Terrible", Description = "Derrota a Jafar en su forma de serpiente", IsUnlocked = false }
                };
            }

            int unlocked = game.Achievements.Count(a => a.IsUnlocked);
            int total = game.Achievements.Count;

            TxtAchievementsTitle.Text = $"RETRO ACHIEVEMENTS ({unlocked}/{total})";
            ProgAchievements.Value = total > 0 ? (unlocked * 100) / total : 0;

            foreach (var achievement in game.Achievements.Take(4))
            {
                var border = new Border
                {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(6),
                    ClipToBounds = true,
                    Background = Avalonia.Media.Brush.Parse("#1e1e22")
                };

                var img = new Image 
                { 
                    Stretch = Avalonia.Media.Stretch.UniformToFill,
                    Opacity = achievement.IsUnlocked ? 1.0 : 0.3
                };
                
                border.Child = img;
                StackAchievementIcons.Children.Add(border);
            }

            if (total > 4)
            {
                var moreBorder = new Border
                {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(6),
                    BorderBrush = Avalonia.Media.Brush.Parse("#475569"),
                    BorderThickness = new Thickness(1),
                    Background = Avalonia.Media.Brush.Parse("#161618")
                };
                moreBorder.Child = new TextBlock 
                { 
                    Text = $"+ {total - 4}", 
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    FontSize = 11,
                    Foreground = Avalonia.Media.Brush.Parse("#94a3b8"),
                    FontWeight = Avalonia.Media.FontWeight.Bold
                };
                StackAchievementIcons.Children.Add(moreBorder);
            }

            ItemsAchievementsList.ItemsSource = game.Achievements;
        }
    }
}
