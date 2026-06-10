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

        public void UpdateTextures(Bitmap? front, Bitmap? back, Bitmap? spine)
        {
            Cover = front;
            // Podríamos asignar back y spine a nuevas propiedades si las añadimos al control
        }

        private bool _isDragging = false;
        private Point _lastMousePosition;
        private const double RotationSensitivity = 0.5;

        private Border? _spine;
        private Border? _front;
        private Border? _shadow;
        private TextBlock? _placeholder;
        private Panel? _root;

        public GameBox3D()
        {
            Template = new FuncControlTemplate<GameBox3D>((parent, nameScope) =>
            {
                _root = new Panel
                {
                    Width = 220,
                    Height = 300,
                    ClipToBounds = false
                };

                // CAPA 0: SOMBRA PROYECTADA
                _shadow = new Border
                {
                    Width = 160,
                    Height = 250,
                    Background = new SolidColorBrush(Color.Parse("#A0000000")),
                    CornerRadius = new CornerRadius(4),
                    Effect = new BlurEffect { Radius = 20 },
                    IsHitTestVisible = false
                };

                // CAPA 1: EL LOMO (Spine)
                _spine = new Border
                {
                    Width = 30,
                    Height = 250,
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop(Color.Parse("#111111"), 0),
                            new GradientStop(Color.Parse("#252525"), 0.5),
                            new GradientStop(Color.Parse("#1a1a1a"), 1)
                        }
                    },
                    BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                    BorderThickness = new Thickness(1, 0, 0, 0),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // CAPA 2: CARA FRONTAL
                _front = new Border
                {
                    Width = 160,
                    Height = 250,
                    Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#444444")),
                    BorderThickness = new Thickness(0.5),
                    CornerRadius = new CornerRadius(1),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var frontImage = new Image { Stretch = Stretch.UniformToFill };
                frontImage.Bind(Image.SourceProperty, parent.GetObservable(CoverProperty));

                _placeholder = new TextBlock
                {
                    Text = "🎮",
                    FontSize = 60,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Opacity = 0.15,
                    Foreground = Brushes.White
                };

                var frontPanel = new Panel();
                frontPanel.Children.Add(_placeholder);
                frontPanel.Children.Add(frontImage);
                _front.Child = frontPanel;

                _root.Children.Add(_shadow);
                _root.Children.Add(_spine);
                _root.Children.Add(_front);

                UpdateVisuals();

                return _root;
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
            if (_front == null || _spine == null || _shadow == null) return;

            if (_placeholder != null) _placeholder.IsVisible = Cover == null;

            double angle = RotationY;
            double rad = angle * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            // GEOMETRÍA DE LA CAJA REDUCIDA (Basada en frontal de 160 y lomo de 30)
            double visualFrontWidth = 160 * cos;
            double visualSpineWidth = 30 * Math.Abs(sin);

            double centerX = 110; // Centro de 220

            // 1. Transformación del Frontal
            var frontGroup = new TransformGroup();
            frontGroup.Children.Add(new ScaleTransform(Math.Max(0.01, cos), 1.0));
            frontGroup.Children.Add(new SkewTransform(0, sin * 6));
            
            double frontX = centerX - (visualFrontWidth / 2) + (visualSpineWidth / 2 * (angle < 0 ? 1 : -1));
            frontGroup.Children.Add(new TranslateTransform(frontX - (centerX - 80), 0));
            _front.RenderTransform = frontGroup;

            // 2. Transformación del Lomo
            _spine.IsVisible = angle > -85;
            var spineGroup = new TransformGroup();
            spineGroup.Children.Add(new ScaleTransform(Math.Max(0.01, Math.Abs(sin)), 0.98));
            spineGroup.Children.Add(new SkewTransform(0, -cos * 8));
            double spineX = frontX - (visualSpineWidth * (angle > 0 ? 1 : 0));
            spineGroup.Children.Add(new TranslateTransform(spineX - (centerX - 80), 0));
            _spine.RenderTransform = spineGroup;

            // 3. Sombra dinámica
            var shadowGroup = new TransformGroup();
            shadowGroup.Children.Add(new ScaleTransform(cos, 1.0));
            shadowGroup.Children.Add(new SkewTransform(sin * 0.15, 0));
            shadowGroup.Children.Add(new TranslateTransform(frontX - (centerX - 80) + 10, 10));
            _shadow.RenderTransform = shadowGroup;
            _shadow.Opacity = Math.Max(0.15, cos * 0.7);

            // 4. Sombreado de caras
            _front.Opacity = 0.7 + (0.3 * cos);
            _spine.Opacity = 0.4 + (0.6 * Math.Abs(sin));
            
            _spine.ZIndex = angle > 0 ? 1 : 2;
            _front.ZIndex = angle > 0 ? 2 : 1;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed)
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
                // Sensibilidad ajustada para que el giro se sienta natural
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
