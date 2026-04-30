using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MicroPanelAvalonia.Models;
using System;

namespace MicroPanelAvalonia.Views
{
    public partial class EditUserDialog : UserControl
    {
        private ServerUser? _editingUser;
        private bool _isEditMode;

        public event EventHandler? Cancelled;
        public event EventHandler<(string username, string password)>? Confirmed;

        public EditUserDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetEditMode(ServerUser user)
        {
            _isEditMode = true;
            _editingUser = user;

            var titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");

            if (titleTextBlock != null)
                titleTextBlock.Text = "编辑用户";

            if (usernameTextBox != null)
            {
                usernameTextBox.Text = user.Username;
                usernameTextBox.IsEnabled = false; // 编辑时不能修改用户名
            }
        }

        public void SetAddMode()
        {
            _isEditMode = false;
            _editingUser = null;

            var titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");

            if (titleTextBlock != null)
                titleTextBlock.Text = "添加用户";

            if (usernameTextBox != null)
            {
                usernameTextBox.Text = string.Empty;
                usernameTextBox.IsEnabled = true;
            }

            var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            if (passwordTextBox != null)
                passwordTextBox.Text = string.Empty;
        }

        public void ShowError(string message)
        {
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");
            var errorTextBlock = this.FindControl<TextBlock>("ErrorTextBlock");

            if (errorPanel != null && errorTextBlock != null)
            {
                errorTextBlock.Text = message;
                errorPanel.IsVisible = true;
            }
        }

        public void Reset()
        {
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");
            if (errorPanel != null)
                errorPanel.IsVisible = false;

            if (!_isEditMode)
            {
                var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
                var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");

                if (usernameTextBox != null)
                    usernameTextBox.Text = string.Empty;

                if (passwordTextBox != null)
                    passwordTextBox.Text = string.Empty;
            }
            else
            {
                var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
                if (passwordTextBox != null)
                    passwordTextBox.Text = string.Empty;
            }
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void OnConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
            var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");

            var username = usernameTextBox?.Text?.Trim() ?? string.Empty;
            var password = passwordTextBox?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("请输入账号");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入密码");
                return;
            }

            Confirmed?.Invoke(this, (username, password));
        }
    }
}
