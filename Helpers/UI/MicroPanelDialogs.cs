using Avalonia.Controls;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace MicroPanel.Helpers.UI;

/// <summary>
/// MicroPanel 对话框
/// </summary>
public static class MicroPanelDialogs
{
    private static ListBoxItem CreateUserListBoxItem(ServerUser user)
    {
        return new ListBoxItem()
        {
            Content = user.Username,
            DataContext = user
        };
    }

    private static ListBox CreateUserListBox(IList<ServerUser> users)
    {
        var items = new List<ListBoxItem>();
        foreach (var user in users)
        {
            items.Add(CreateUserListBoxItem(user));
        }

        return new ListBox()
        {
            ItemsSource = items,
            MinHeight = 150,
            MaxHeight = 250
        };
    }

    #region 添加服务器对话框

    /// <summary>
    /// 显示添加服务器对话框
    /// </summary>
    /// <param name="server">如果提供了服务器对象，则为编辑模式</param>
    public static async Task<(bool success, string? serverAddress, string? username, string? password, string? serverName)> ShowAddServerDialog(ServerInfo? server = null, TopLevel? root = null)
    {
        var serverNameTextBox = new TextBox() 
        { 
            Watermark = "可选 - 服务器显示名称（留空则显示地址）" 
        };
        var serverAddressTextBox = new TextBox() 
        { 
            Watermark = "例如: 192.168.1.1:8080" 
        };
        var usernameTextBox = new TextBox() 
        { 
            Watermark = "请输入用户名" 
        };
        var passwordTextBox = new TextBox() 
        { 
            Watermark = "请输入密码", 
            PasswordChar = '●' 
        };
        
        // 如果是编辑模式，填充现有值
        if (server != null)
        {
            serverNameTextBox.Text = server.ServerName;
            serverAddressTextBox.Text = server.ServerAddress;
        }

        // 根据是否为编辑模式创建不同的内容
        StackPanel stackPanel;
        
        if (server != null)
        {
            // 编辑模式：只显示名称和地址
            stackPanel = new StackPanel()
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock() { Text = "服务器名称（可选）" },
                    serverNameTextBox,
                    new TextBlock() { Text = "服务器地址" },
                    serverAddressTextBox
                }
            };
        }
        else
        {
            // 添加模式：显示名称、地址、用户名、密码
            stackPanel = new StackPanel()
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock() { Text = "服务器名称（可选）" },
                    serverNameTextBox,
                    new TextBlock() { Text = "服务器地址" },
                    serverAddressTextBox,
                    new TextBlock() { Text = "用户名" },
                    usernameTextBox,
                    new TextBlock() { Text = "密码" },
                    passwordTextBox
                }
            };
        }

        var dialog = new ContentDialog()
        {
            Title = server == null ? "添加服务器" : "编辑服务器",
            Content = stackPanel,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync(root);
        if (result == ContentDialogResult.Primary)
        {
            var serverAddress = serverAddressTextBox.Text?.Trim();
            var serverName = serverNameTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(serverAddress))
            {
                if (server != null)
                {
                    // 编辑模式：不需要用户名和密码
                    return (true, serverAddress, null, null, string.IsNullOrWhiteSpace(serverName) ? null : serverName);
                }
                else
                {
                    // 添加模式：需要用户名和密码
                    var username = usernameTextBox.Text?.Trim();
                    var password = passwordTextBox.Text;

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        return (true, serverAddress, username, password, string.IsNullOrWhiteSpace(serverName) ? null : serverName);
                    }
                }
            }
        }

        return (false, null, null, null, null);
    }

    #endregion

    #region 用户选择对话框

    /// <summary>
    /// 显示用户选择对话框
    /// </summary>
    public static async Task<(bool success, ServerInfo? server, ServerUser? user)> ShowUserSelectDialog(ServerInfo server, TopLevel? root = null)
    {
        var userListBox = CreateUserListBox(server.Users);

        var stackPanel = new StackPanel()
        {
            Spacing = 12,
            Children =
            {
                new TextBlock() { Text = $"服务器: {server.ServerAddress}" },
                new TextBlock() { Text = "选择用户登录" },
                userListBox
            }
        };

        var dialog = new ContentDialog()
        {
            Title = "选择用户登录",
            Content = stackPanel,
            PrimaryButtonText = "登录",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync(root);
        if (result == ContentDialogResult.Primary)
        {
            if (userListBox.SelectedItem is ListBoxItem item && item.DataContext is ServerUser selectedUser)
            {
                return (true, server, selectedUser);
            }
        }

        return (false, null, null);
    }

    #endregion

    #region 用户管理对话框

    /// <summary>
    /// 用户管理对话框结果
    /// </summary>
    public enum UserManagementAction
    {
        Cancel,
        EditUser,
        DeleteUser,
        AddUser
    }

    /// <summary>
    /// 显示用户管理对话框
    /// </summary>
    public static async Task<(UserManagementAction action, ServerUser? targetUser)> ShowUserManagementDialog(ServerInfo server, TopLevel? root = null)
    {
        var userListBox = CreateUserListBox(server.Users);

        var editButton = new Button() { Content = "编辑用户" };
        var deleteButton = new Button() { Content = "删除用户" };
        var addButton = new Button() { Content = "添加用户" };

        var buttonStack = new StackPanel()
        {
            Spacing = 8,
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Children = { editButton, deleteButton, addButton }
        };

        var stackPanel = new StackPanel()
        {
            Spacing = 16,
            Children =
            {
                new TextBlock() { Text = $"服务器: {server.ServerAddress}" },
                userListBox,
                buttonStack
            }
        };

        UserManagementAction action = UserManagementAction.Cancel;
        ServerUser? targetUser = null;
        var tcs = new TaskCompletionSource<(UserManagementAction, ServerUser?)>();

        var dialog = new ContentDialog()
        {
            Title = "用户管理",
            Content = stackPanel,
            CloseButtonText = "关闭",
            DefaultButton = ContentDialogButton.Primary
        };

        editButton.Click += (s, e) =>
        {
            if (userListBox.SelectedItem is ListBoxItem item && item.DataContext is ServerUser u)
            {
                targetUser = u;
                action = UserManagementAction.EditUser;
                dialog.Hide();
                tcs.TrySetResult((action, targetUser));
            }
        };

        deleteButton.Click += (s, e) =>
        {
            if (userListBox.SelectedItem is ListBoxItem item && item.DataContext is ServerUser u)
            {
                targetUser = u;
                action = UserManagementAction.DeleteUser;
                dialog.Hide();
                tcs.TrySetResult((action, targetUser));
            }
        };

        addButton.Click += (s, e) =>
        {
            action = UserManagementAction.AddUser;
            targetUser = null;
            dialog.Hide();
            tcs.TrySetResult((action, targetUser));
        };

        var dialogResult = await dialog.ShowAsync(root);
        if (tcs.Task.IsCompleted)
        {
            return await tcs.Task;
        }

        return (UserManagementAction.Cancel, null);
    }

    #endregion

    #region 编辑用户对话框

    /// <summary>
    /// 显示编辑用户对话框
    /// </summary>
    public static async Task<(bool success, string? username, string? password)> ShowEditUserDialog(ServerUser? user = null, TopLevel? root = null)
    {
        var usernameTextBox = new TextBox() { IsEnabled = user == null };
        var passwordTextBox = new TextBox() { Watermark = user == null ? "请输入密码" : "留空表示不修改", PasswordChar = '●' };

        if (user != null)
        {
            usernameTextBox.Text = user.Username;
        }
        else
        {
            usernameTextBox.Watermark = "请输入用户名";
        }

        var stackPanel = new StackPanel()
        {
            Spacing = 12,
            Children =
            {
                new TextBlock() { Text = "用户名" },
                usernameTextBox,
                new TextBlock() { Text = "密码" },
                passwordTextBox
            }
        };

        var dialog = new ContentDialog()
        {
            Title = user == null ? "添加用户" : "编辑用户",
            Content = stackPanel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync(root);
        if (result == ContentDialogResult.Primary)
        {
            var username = usernameTextBox.Text?.Trim();
            var password = passwordTextBox.Text;

            if (!string.IsNullOrWhiteSpace(username))
            {
                return (true, username, password);
            }
        }

        return (false, null, null);
    }

    #endregion

    #region 确认对话框

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public static async Task<bool> ShowConfirmDialog(string title, string content, TopLevel? root = null)
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync(root);
        return result == ContentDialogResult.Primary;
    }

    #endregion
}