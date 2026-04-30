using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace MicroPanelAvalonia.Views
{
    public partial class ConfirmDialog : UserControl
    {
        public event EventHandler? Cancelled;
        public event EventHandler? Confirmed;

        public ConfirmDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetContent(string title, string message)
        {
            var titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            var messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");

            if (titleTextBlock != null)
                titleTextBlock.Text = title;

            if (messageTextBlock != null)
                messageTextBlock.Text = message;
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void OnConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Confirmed?.Invoke(this, EventArgs.Empty);
        }
    }
}
