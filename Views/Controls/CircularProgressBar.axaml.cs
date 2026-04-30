using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace MicroPanelAvalonia.Views.Controls
{
    public partial class CircularProgressBar : UserControl
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<CircularProgressBar, double>(nameof(Value), 0);

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<CircularProgressBar, double>(nameof(Maximum), 100);

        public static readonly StyledProperty<double> DiameterProperty =
            AvaloniaProperty.Register<CircularProgressBar, double>(nameof(Diameter), 160);

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<CircularProgressBar, double>(nameof(StrokeThickness), 12);

        public static readonly StyledProperty<IBrush> ForegroundBrushProperty =
            AvaloniaProperty.Register<CircularProgressBar, IBrush>(nameof(ForegroundBrush), new SolidColorBrush(Colors.DodgerBlue));

        public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
            AvaloniaProperty.Register<CircularProgressBar, IBrush>(nameof(BackgroundBrush), new SolidColorBrush(Colors.Gray));

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Diameter
        {
            get => GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public IBrush ForegroundBrush
        {
            get => GetValue(ForegroundBrushProperty);
            set => SetValue(ForegroundBrushProperty, value);
        }

        public IBrush BackgroundBrush
        {
            get => GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        private Path? _progressPath;
        private Ellipse? _backgroundEllipse;
        private Grid? _mainGrid;

        public CircularProgressBar()
        {
            InitializeComponent();
            
            ValueProperty.Changed.AddClassHandler<CircularProgressBar>((s, e) => s.UpdateProgress());
            MaximumProperty.Changed.AddClassHandler<CircularProgressBar>((s, e) => s.UpdateProgress());
            DiameterProperty.Changed.AddClassHandler<CircularProgressBar>((s, e) => s.UpdateCircularLayout());
            StrokeThicknessProperty.Changed.AddClassHandler<CircularProgressBar>((s, e) => s.UpdateCircularLayout());
            ForegroundBrushProperty.Changed.AddClassHandler<CircularProgressBar>((s, e) => s.UpdateCircularLayout());
            BackgroundBrushProperty.Changed.AddClassHandler<CircularProgressBar>((s, e) => s.UpdateCircularLayout());
        }

        private void UpdateCircularLayout()
        {
            if (_mainGrid == null || _backgroundEllipse == null || _progressPath == null) return;

            _mainGrid.Width = Diameter;
            _mainGrid.Height = Diameter;

            _backgroundEllipse.Width = Diameter;
            _backgroundEllipse.Height = Diameter;
            _backgroundEllipse.Stroke = BackgroundBrush;
            _backgroundEllipse.StrokeThickness = StrokeThickness;

            _progressPath.Stroke = ForegroundBrush;
            _progressPath.StrokeThickness = StrokeThickness;

            UpdateProgress();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (change.Property == IsVisibleProperty)
            {
                UpdateCircularLayout();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _progressPath = this.FindControl<Path>("ProgressPath");
            _backgroundEllipse = this.FindControl<Ellipse>("BackgroundEllipse");
            _mainGrid = this.FindControl<Grid>("MainGrid");
            UpdateCircularLayout();
        }

        private void UpdateProgress()
        {
            if (_progressPath == null) return;

            var radius = (Diameter - StrokeThickness) / 2;
            var centerX = Diameter / 2;
            var centerY = Diameter / 2;

            // 计算进度角度（从顶部开始，顺时针）
            var percentage = Math.Min(Value / Maximum, 1);
            var angle = percentage * 360 - 90; // -90 度从顶部开始

            // 转换为弧度
            var startAngle = -90 * Math.PI / 180;
            var endAngle = angle * Math.PI / 180;

            // 计算起点和终点
            var startX = centerX + radius * Math.Cos(startAngle);
            var startY = centerY + radius * Math.Sin(startAngle);
            var endX = centerX + radius * Math.Cos(endAngle);
            var endY = centerY + radius * Math.Sin(endAngle);

            // 创建弧形路径
            var largeArcFlag = percentage > 0.5 ? 1 : 0;
            
            var pathData = $"M {startX},{startY} A {radius},{radius} 0 {largeArcFlag} 1 {endX},{endY}";
            
            _progressPath.Data = PathGeometry.Parse(pathData);
        }
    }
}
