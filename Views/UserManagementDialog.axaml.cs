using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MicroPanelAvalonia.Models;
using System;
using System.Collections.Generic;

namespace MicroPanelAvalonia.Views
{
    public partial class UserManagementDialog : UserControl
    {
        private ServerInfo? _serverInfo;

        public event EventHandler? CloseRequested;
        public event EventHandler<ServerUser>? EditUserRequested;
        public event EventHandler<ServerUser>? DeleteUserRequested;
        public event EventHandler? AddUserRequested;

        public UserManagementDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetServer(ServerInfo server)
        {
            _serverInfo = server;

            var serverTextBlock = this.FindControl<TextBlock>("ServerTextBlock");
            if (serverTextBlock != null)
            {
                serverTextBlock.Text = server.ServerAddress;
            }

            RefreshUsersList();
        }

        public void RefreshUsersList()
        {
            var usersListBox = this.FindControl<ListBox>("UsersListBox");
            if (usersListBox != null && _serverInfo != null)
            {
                usersListBox.ItemsSource = null;
                usersListBox.ItemsSource = _serverInfo.Users;
            }
        }

        private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditUserClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServerUser user)
            {
                EditUserRequested?.Invoke(this, user);
            }
        }

        private void OnDeleteUserClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServerUser user)
            {
                DeleteUserRequested?.Invoke(this, user);
            }
        }

        private void OnAddUserClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            AddUserRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 选中指定用户并触发编辑
        /// </summary>
        public void SelectUser(ServerUser user)
        {
            var usersListBox = this.FindControl<ListBox>("UsersListBox");
            if (usersListBox != null)
            {
                usersListBox.SelectedItem = user;
                EditUserRequested?.Invoke(this, user);
            }
        }
    }
}
