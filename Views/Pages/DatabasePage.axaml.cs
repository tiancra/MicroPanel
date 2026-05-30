using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroPanel.Views.Pages
{
    public partial class DatabasePage : UserControl
    {
        private readonly ApiService _apiService;
        private List<RedisKeyNode> _treeData = new();
        private RedisKeyNode? _currentNode;

        public DatabasePage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 加载Redis数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);
            
            var sepTextBox = this.FindControl<TextBox>("SepTextBox");
            var sep = sepTextBox?.Text ?? ":";

            var response = await _apiService.GetRedisKeysAsync(session.Token, sep);
            if (response?.IsSuccess == true && response.Data != null)
            {
                _treeData = response.Data;
                var treeView = this.FindControl<TreeView>("RedisTreeView");
                if (treeView != null)
                {
                    treeView.ItemsSource = _treeData;
                }
            }
        }

        /// <summary>
        /// 刷新按钮点击
        /// </summary>
        private async void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 添加根节点按钮点击
        /// </summary>
        private async void OnAddRootClick(object? sender, RoutedEventArgs e)
        {
            _currentNode = new RedisKeyNode
            {
                Name = "",
                Path = "",
                Children = _treeData
            };
            
            var keyName = await ShowInputDialogAsync("添加键", "请输入键名：");
            if (!string.IsNullOrEmpty(keyName))
            {
                await AddKeyAsync(keyName);
            }
        }

        /// <summary>
        /// 添加子节点按钮点击
        /// </summary>
        private async void OnAddChildClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RedisKeyNode node)
            {
                _currentNode = node;
                
                var keyName = await ShowInputDialogAsync("添加键", "请输入键名：");
                if (!string.IsNullOrEmpty(keyName))
                {
                    await AddKeyAsync(keyName);
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 删除按钮点击
        /// </summary>
        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RedisKeyNode node)
            {
                var session = SessionService.Instance;
                if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

                _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

                // 判断是否有子节点，决定是单个删除还是批量删除
                bool hasChildren = node.Children != null && node.Children.Count > 0;
                string keyToDelete = hasChildren ? node.Path + "*" : node.Path;

                ApiResponse<string>? response;
                if (hasChildren)
                {
                    response = await _apiService.DelRedisKeysAsync(session.Token, keyToDelete);
                }
                else
                {
                    response = await _apiService.DelRedisKeyAsync(session.Token, keyToDelete);
                }

                if (response?.IsSuccess == true)
                {
                    await LoadDataAsync();
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 树节点选择变化
        /// </summary>
        private async void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is TreeView treeView && treeView.SelectedItem is RedisKeyNode node)
            {
                // 只有叶子节点（没有子节点）才打开编辑对话框
                if (node.Children == null || node.Children.Count == 0)
                {
                    await EditKeyValueAsync(node);
                }
            }
        }

        /// <summary>
        /// 树节点双击事件 - 展开/折叠节点
        /// </summary>
        private void OnTreeDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (sender is TreeView treeView)
            {
                // 获取双击的源元素
                var source = e.Source as Control;
                if (source != null)
                {
                    // 向上查找 TreeViewItem
                    var treeViewItem = FindParentTreeViewItem(source);
                    if (treeViewItem != null)
                    {
                        // 切换展开/折叠状态
                        treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 查找父级 TreeViewItem
        /// </summary>
        private TreeViewItem? FindParentTreeViewItem(Control? element)
        {
            while (element != null)
            {
                if (element is TreeViewItem treeViewItem)
                {
                    return treeViewItem;
                }
                element = element.Parent as Control;
            }
            return null;
        }

        /// <summary>
        /// 添加键
        /// </summary>
        private async Task AddKeyAsync(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
            {
                return;
            }

            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            // 构建完整键名
            string fullKey;
            if (_currentNode == null || string.IsNullOrEmpty(_currentNode.Path))
            {
                fullKey = keyName;
            }
            else
            {
                var sepTextBox = this.FindControl<TextBox>("SepTextBox");
                var sep = sepTextBox?.Text ?? ":";
                fullKey = $"{_currentNode.Path}{sep}{keyName}";
            }

            // 检查是否已存在
            var children = _currentNode?.Children ?? _treeData;
            if (children.Any(n => n.Name == keyName))
            {
                return;
            }

            var response = await _apiService.SetRedisKeyAsync(session.Token, fullKey, "");
            if (response?.IsSuccess == true)
            {
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// 编辑键值
        /// </summary>
        private async Task EditKeyValueAsync(RedisKeyNode node)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            // 获取键值
            var getResponse = await _apiService.GetRedisKeyAsync(session.Token, node.Path);
            if (getResponse?.IsSuccess == true)
            {
                var currentValue = getResponse.Data ?? "";
                var newValue = await ShowEditValueDialogAsync($"编辑键: {node.Name}", currentValue);
                
                if (newValue != null) // null 表示取消
                {
                    var setResponse = await _apiService.SetRedisKeyAsync(session.Token, node.Path, newValue);
                    if (setResponse?.IsSuccess == true)
                    {
                        await LoadDataAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        private async Task<string?> ShowInputDialogAsync(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var textBox = new TextBox 
            { 
                Margin = new Avalonia.Thickness(20, 10, 20, 10),
                Watermark = "请输入..."
            };

            string? result = null;

            var confirmButton = new Button 
            { 
                Content = "确认",
                Classes = { "Accent" },
                Margin = new Avalonia.Thickness(5)
            };
            confirmButton.Click += (s, e) => 
            { 
                result = textBox.Text;
                dialog.Close();
            };

            var cancelButton = new Button 
            { 
                Content = "取消",
                Margin = new Avalonia.Thickness(5)
            };
            cancelButton.Click += (s, e) => dialog.Close();

            var content = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Avalonia.Thickness(20)
            };

            content.Children.Add(new TextBlock 
            { 
                Text = message,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });
            Grid.SetRow(content.Children[0], 0);

            content.Children.Add(textBox);
            Grid.SetRow(content.Children[1], 1);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(confirmButton);

            content.Children.Add(buttonPanel);
            Grid.SetRow(content.Children[2], 2);

            dialog.Content = content;

            var tcs = new TaskCompletionSource<string?>();
            dialog.Closed += (s, e) => tcs.TrySetResult(result);
            
            if (VisualRoot is Window parentWindow)
            {
                await dialog.ShowDialog(parentWindow);
            }
            
            return await tcs.Task;
        }

        /// <summary>
        /// 显示编辑值对话框
        /// </summary>
        private async Task<string?> ShowEditValueDialogAsync(string title, string currentValue)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true
            };

            var textBox = new TextBox 
            { 
                Margin = new Avalonia.Thickness(20, 10, 20, 10),
                AcceptsReturn = true,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                FontFamily = new Avalonia.Media.FontFamily("Consolas, Monaco, monospace"),
                Text = currentValue
            };

            string? result = null;

            var confirmButton = new Button 
            { 
                Content = "保存",
                Classes = { "Accent" },
                Margin = new Avalonia.Thickness(5)
            };
            confirmButton.Click += (s, e) => 
            { 
                result = textBox.Text;
                dialog.Close();
            };

            var cancelButton = new Button 
            { 
                Content = "取消",
                Margin = new Avalonia.Thickness(5)
            };
            cancelButton.Click += (s, e) => dialog.Close();

            var content = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Avalonia.Thickness(20)
            };

            content.Children.Add(new TextBlock 
            { 
                Text = "键值内容：",
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });
            Grid.SetRow(content.Children[0], 0);

            content.Children.Add(textBox);
            Grid.SetRow(content.Children[1], 1);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(confirmButton);

            content.Children.Add(buttonPanel);
            Grid.SetRow(content.Children[2], 2);

            dialog.Content = content;

            var tcs = new TaskCompletionSource<string?>();
            dialog.Closed += (s, e) => tcs.TrySetResult(result);
            
            if (VisualRoot is Window parentWindow)
            {
                await dialog.ShowDialog(parentWindow);
            }
            
            return await tcs.Task;
        }
    }
}
