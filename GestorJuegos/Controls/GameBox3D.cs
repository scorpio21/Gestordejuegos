using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace GestorJuegos.Controls
{
    public class GameBox3D : TemplatedControl
    {
        public static readonly StyledProperty<Bitmap?> CoverProperty =
            AvaloniaProperty.Register<GameBox3D, Bitmap?>(nameof(Cover));

        public static readonly StyledProperty<double> RotationYProperty =
            AvaloniaProperty.Register<GameBox3D, double>(nameof(RotationY), -20.0);

        public Bitmap? Cover
        {
            get => GetValue(CoverProperty);
            set => SetValue(CoverProperty, value);
        }

        public double RotationY
        {
            get => GetValue(RotationYProperty);
            set => SetValue(RotationYProperty, value);
        }

        private bool _isDragging = false;
        private Point _lastMousePosition;
        private const double RotationSensitivity = 0.5;

        private Border? _spine;
        private Border? _front;
        private TextBlock? _placeholder;

        public GameBox3D()
        {
            Template = new FuncControlTemplate<GameBox3D>((parent, nameScope) =>
            {
                var root = new Panel
                {
                    Width = 300,
                    Height = 400,
                    ClipToBounds = false
                };

                _spine = new Border
                {
                    Width = 40,
                    Height = 360,
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop(Color.Parse("#1a1a1a"), 0),
                            new GradientStop(Color.Parse("#333333"), 1)
                        }
                    },
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                _front = new Border
                {
                    Width = 240,
                    Height = 360,
                    Background = new SolidColorBrush(Color.Parse("#2d2f35")),
                    CornerRadius = new CornerRadius(2),
                    BoxShadow = new BoxShadows(new BoxShadow { Blur = 20, Color = Color.Parse("#80000000"), OffsetY = 10 }),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var frontImage = new Image
                {
                    Stretch = Stretch.UniformToFill
                };
                frontImage.Bind(Image.SourceProperty, parent.GetObservable(CoverProperty));

                _placeholder = new TextBlock
                {
                    Text = "🎮",
                    FontSize = 50,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Opacity = 0.2
                };

                var frontPanel = new Panel();
                frontPanel.Children.Add(_placeholder);
                frontPanel.Children.Add(frontImage);
                _front.Child = frontPanel;

                root.Children.Add(_spine);
                root.Children.Add(_front);

                UpdateVisuals();

                return root;
            });
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CoverProperty || change.Property == RotationYProperty)
            {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (_front == null || _spine == null || _placeholder == null) return;

            _placeholder.IsVisible = Cover == null;

            double angle = RotationY;
            double rad = angle * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            // Frontal
            var frontTransform = new TransformGroup();
            frontTransform.Children.Add(new ScaleTransform(Math.Max(0.1, cos), 1.0));
            frontTransform.Children.Add(new SkewTransform(0, sin * 10));
            frontTransform.Children.Add(new TranslateTransform(150 - (120 * cos), 0));
            _front.RenderTransform = frontTransform;

            // Spine
            _spine.IsVisible = angle > -45;
            var spineTransform = new TransformGroup();
            double spineScale = Math.Max(0.1, Math.Abs(sin));
            spineTransform.Children.Add(new ScaleTransform(spineScale, 0.95));
            spineTransform.Children.Add(new SkewTransform(0, -cos * 15));
            spineTransform.Children.Add(new TranslateTransform(150 - (120 * cos) - (40 * spineScale), 0));
            _spine.RenderTransform = spineTransform;
            
            _front.Opacity = 0.5 + (0.5 * cos);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsRightButtonPressed)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(this);
                e.Pointer.Capture(this);
                e.Handled = true;
            }
            base.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDragging)
            {
                var currentPosition = e.GetPosition(this);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                RotationY = Math.Clamp(RotationY + (deltaX * RotationSensitivity), -85.0, 85.0);
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
            base.OnPointerMoved(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
            base.OnPointerReleased(e);
        }
    }
}
