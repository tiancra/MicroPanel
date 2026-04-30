using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MicroPanelAvalonia.Models;
using System;

namespace MicroPanelAvalonia.Views
{
    public partial class ServerCard : UserControl
    {
        public static readonly StyledProperty<ServerInfo?> ServerProperty =
            AvaloniaProperty.Register<ServerCard, ServerInfo?>(nameof(Server));

        public ServerInfo? Server
        {
            get => GetValue(ServerProperty);
            set => SetValue(ServerProperty, value);
        }

        public event EventHandler<ServerInfo>? CardClicked;
        public event EventHandler<ServerInfo>? UserManagementRequested;
        public event EventHandler<ServerInfo>? DeleteRequested;

        private Border? _cardBorder;

        public ServerCard()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _cardBorder = this.FindControl<Border>("CardBorder");
            if (_cardBorder != null)
            {
                _cardBorder.PointerPressed += OnBorderPointerPressed;
                _cardBorder.PointerReleased += OnBorderPointerReleased;
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ServerInfo server)
            {
                Server = server;
            }
        }

        private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // 捕获指针以确保能收到 PointerReleased 事件
            if (_cardBorder != null)
            {
                e.Pointer.Capture(_cardBorder);
            }
        }

        private void OnBorderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // 释放指针捕获
            e.Pointer.Capture(null);

            var properties = e.GetCurrentPoint(this).Properties;
            
            if (properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                // 左键释放
                if (Server != null)
                {
                    CardClicked?.Invoke(this, Server);
                }
                e.Handled = true;
            }
        }

        private void OnUserManagementClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (Server != null)
            {
                UserManagementRequested?.Invoke(this, Server);
            }
            e.Handled = true;
        }

        private void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (Server != null)
            {
                DeleteRequested?.Invoke(this, Server);
            }
            e.Handled = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            
            // 清理事件订阅
            if (_cardBorder != null)
            {
                _cardBorder.PointerPressed -= OnBorderPointerPressed;
                _cardBorder.PointerReleased -= OnBorderPointerReleased;
            }
        }
    }
}
