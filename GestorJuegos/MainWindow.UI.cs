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
    }
}
