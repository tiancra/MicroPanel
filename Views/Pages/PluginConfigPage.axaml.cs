using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanel.Views.Pages
{
    public partial class PluginConfigPage : UserControl
    {
        private readonly ApiService _apiService;
        private List<PluginInfo> _pluginsInfo = new();
        private List<SchemaItem> _pluginConfig = new();
        private List<string> _unconfiguredPlugins = new();
        private string _currentPluginName = "";
        private string _currentSource = "guoba";

        public PluginConfigPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            Loaded += async (s, e) => await LoadPluginsInfoAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 加载插件信息列表
        /// </summary>
        private async Task LoadPluginsInfoAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            // 获取已配置的插件列表
            var response = await _apiService.GetPluginInfoAsync(session.Token, _currentSource);
            System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Response: Code={response?.Code}, IsSuccess={response?.IsSuccess}, DataCount={response?.Data?.Count}");
            
            if (response?.IsSuccess == true && response.Data != null)
            {
                _pluginsInfo = response.Data;
                System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Loaded {_pluginsInfo.Count} plugins");
                
                // 获取文件系统目录下的插件文件夹
                await LoadUnconfiguredPluginsAsync();
                
                RenderPluginsList();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Failed to load plugins: {response?.Message}");
            }
        }

        /// <summary>
        /// 加载未配置的插件（从文件系统）
        /// </summary>
        private async Task LoadUnconfiguredPluginsAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            // 获取 plugins 目录下的文件夹列表
            var fsResponse = await _apiService.GetDirectoryListAsync(session.Token, "plugins/");
            if (fsResponse?.IsSuccess != true || fsResponse.Data == null) return;

            var excludedDirs = new[] { "example", "others", "genshin", "adapter" };
            var configuredPluginNames = _pluginsInfo.Select(p => p.PluginName.ToLower()).ToHashSet();
            
            _unconfiguredPlugins = new List<string>();
            
            // 遍历 children 数组，筛选 type 为 directory 的项
            var children = fsResponse.Data.Children ?? new List<FsChildInfo>();
            foreach (var child in children)
            {
                // 只处理目录类型
                if (child.Type != "directory") continue;
                
                var dirName = child.Name;
                // 排除 example 和 others
                if (excludedDirs.Contains(dirName.ToLower())) continue;
                // 排除已配置的插件
                if (configuredPluginNames.Contains(dirName.ToLower())) continue;
                
                _unconfiguredPlugins.Add(dirName);
            }
            
            System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Found {_unconfiguredPlugins.Count} unconfigured plugins");
        }

        /// <summary>
        /// 渲染插件列表
        /// </summary>
        private void RenderPluginsList()
        {
            var listPanel = this.FindControl<StackPanel>("PluginsListPanel");
            var formPanel = this.FindControl<StackPanel>("ConfigFormPanel");
            var backButton = this.FindControl<Button>("BackButton");
            var saveButton = this.FindControl<Button>("SaveButton");

            System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] RenderPluginsList: listPanel={listPanel != null}, _pluginsInfo.Count={_pluginsInfo.Count}");

            if (listPanel == null) return;

            listPanel.IsVisible = true;
            if (formPanel != null) formPanel.IsVisible = false;
            if (backButton != null) backButton.IsVisible = false;
            if (saveButton != null) saveButton.IsVisible = false;

            listPanel.Children.Clear();

            // 渲染已配置的插件
            foreach (var plugin in _pluginsInfo)
            {
                var card = CreatePluginCard(plugin);
                listPanel.Children.Add(card);
            }

            // 渲染未配置的插件
            foreach (var pluginName in _unconfiguredPlugins)
            {
                var card = CreateUnconfiguredPluginCard(pluginName);
                listPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// 创建插件卡片
        /// </summary>
        private Border CreatePluginCard(PluginInfo plugin)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.LightGray) { Opacity = 0.1 },
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 4),
                BorderBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.3 },
                BorderThickness = new Thickness(1)
            };

            // 水平布局：标题 | 描述 | 作者 | 版本 | 配置按钮 | 删除按钮
            var mainPanel = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("200,*,150,100,Auto,Auto")
            };

            // 标题
            var title = new TextBlock
            {
                Text = plugin.Title ?? plugin.Name,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(title, 0);
            mainPanel.Children.Add(title);

            // 描述
            var description = new TextBlock
            {
                Text = plugin.Description ?? "",
                FontSize = 12,
                Opacity = 0.7,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(description, 1);
            mainPanel.Children.Add(description);

            // 作者
            var authorStr = plugin.GetAuthorString();
            var author = new TextBlock
            {
                Text = authorStr,
                FontSize = 12,
                Opacity = 0.8,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(author, 2);
            mainPanel.Children.Add(author);

            // 版本标签
            var versionPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            if (plugin.IsV2)
                versionPanel.Children.Add(CreateVersionTag("v2"));
            if (plugin.IsV3)
                versionPanel.Children.Add(CreateVersionTag("v3"));
            if (plugin.IsV4 == true)
                versionPanel.Children.Add(CreateVersionTag("v4"));
            Grid.SetColumn(versionPanel, 3);
            mainPanel.Children.Add(versionPanel);

            // 配置按钮
            var configButton = new Button
            {
                Content = "配置",
                Classes = { "Accent" },
                Margin = new Thickness(16, 0, 0, 0)
            };
            configButton.Click += async (s, e) => await LoadPluginConfigAsync(plugin.PluginName);
            Grid.SetColumn(configButton, 4);
            mainPanel.Children.Add(configButton);

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "删除",
                Margin = new Thickness(8, 0, 0, 0)
            };
            deleteButton.Click += async (s, e) => await ShowDeleteDialogAsync(plugin.PluginName);
            Grid.SetColumn(deleteButton, 5);
            mainPanel.Children.Add(deleteButton);

            card.Child = mainPanel;
            return card;
        }

        /// <summary>
        /// 创建未配置插件卡片
        /// </summary>
        private Border CreateUnconfiguredPluginCard(string pluginName)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.Gray) { Opacity = 0.05 },
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 4),
                BorderBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.2 },
                BorderThickness = new Thickness(1)
            };

            // 水平布局：文件夹名 | 不支持配置提示 | 空按钮位置 | 删除按钮
            var mainPanel = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("200,*,150,100,Auto,Auto")
            };

            // 文件夹名（标题）
            var title = new TextBlock
            {
                Text = pluginName,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Opacity = 0.6
            };
            Grid.SetColumn(title, 0);
            mainPanel.Children.Add(title);

            // 中间提示文本
            var hintText = new TextBlock
            {
                Text = "该插件不支持配置",
                FontSize = 12,
                Opacity = 0.5,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(hintText, 1);
            mainPanel.Children.Add(hintText);

            // 占位符（保持列对齐）
            var placeholder1 = new Border { Width = 0 };
            Grid.SetColumn(placeholder1, 2);
            mainPanel.Children.Add(placeholder1);

            var placeholder2 = new Border { Width = 0 };
            Grid.SetColumn(placeholder2, 3);
            mainPanel.Children.Add(placeholder2);

            // 空按钮位置（占位）
            var buttonPlaceholder = new Border
            {
                Width = 60,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(buttonPlaceholder, 4);
            mainPanel.Children.Add(buttonPlaceholder);

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "删除",
                Margin = new Thickness(8, 0, 0, 0)
            };
            deleteButton.Click += async (s, e) => await ShowDeleteDialogAsync(pluginName);
            Grid.SetColumn(deleteButton, 5);
            mainPanel.Children.Add(deleteButton);

            card.Child = mainPanel;
            return card;
        }

        /// <summary>
        /// 创建版本标签
        /// </summary>
        private Border CreateVersionTag(string text)
        {
            return new Border
            {
                Background = new SolidColorBrush(Colors.Gray) { Opacity = 0.3 },
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
        }

        /// <summary>
        /// 显示删除确认对话框
        /// </summary>
        private async Task ShowDeleteDialogAsync(string pluginName)
        {
            var dialog = new Window
            {
                Title = "确认删除",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16
            };

            var message = new TextBlock
            {
                Text = $"确定要删除插件 '{pluginName}' 吗？\n请输入 'yes' 确认：",
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(message);

            var inputBox = new TextBox
            {
                Watermark = "输入 yes 确认"
            };
            panel.Children.Add(inputBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8
            };

            var confirmButton = new Button
            {
                Content = "确定",
                Classes = { "Accent" },
                IsEnabled = false
            };

            var cancelButton = new Button
            {
                Content = "取消"
            };

            // 输入验证
            inputBox.TextChanged += (s, e) =>
            {
                confirmButton.IsEnabled = inputBox.Text?.Trim().ToLower() == "yes";
            };

            // 按钮事件
            confirmButton.Click += async (s, e) =>
            {
                dialog.Close();
                await DeletePluginAsync(pluginName);
            };

            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(confirmButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            await dialog.ShowDialog((Window)this.GetVisualRoot()!);
        }

        /// <summary>
        /// 删除插件
        /// </summary>
        private async Task DeletePluginAsync(string pluginName)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            var response = await _apiService.DeleteDirectoryAsync(session.Token, $"plugins/{pluginName}");
            if (response?.IsSuccess == true)
            {
                // 删除成功，刷新列表
                await LoadPluginsInfoAsync();
            }
            else
            {
                // 删除失败，显示错误
                System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Delete failed: {response?.Message}");
            }
        }

        /// <summary>
        /// 加载插件配置
        /// </summary>
        private async Task LoadPluginConfigAsync(string pluginName)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            _currentPluginName = pluginName;
            var response = await _apiService.GetPluginConfigAsync(session.Token, pluginName, _currentSource);
            
            if (response?.IsSuccess == true && response.Data != null)
            {
                _pluginConfig = response.Data;
                RenderConfigForm();
            }
        }

        /// <summary>
        /// 渲染配置表单
        /// </summary>
        private void RenderConfigForm()
        {
            var listPanel = this.FindControl<StackPanel>("PluginsListPanel");
            var formPanel = this.FindControl<StackPanel>("ConfigFormPanel");
            var backButton = this.FindControl<Button>("BackButton");
            var saveButton = this.FindControl<Button>("SaveButton");

            if (listPanel != null) listPanel.IsVisible = false;
            if (formPanel != null) formPanel.IsVisible = true;
            if (backButton != null) backButton.IsVisible = true;
            if (saveButton != null) saveButton.IsVisible = true;

            if (formPanel == null) return;
            formPanel.Children.Clear();

            foreach (var item in _pluginConfig)
            {
                var control = CreateConfigControl(item);
                if (control != null)
                {
                    formPanel.Children.Add(control);
                }
            }
        }

        /// <summary>
        /// 创建配置控件
        /// </summary>
        private Control? CreateConfigControl(SchemaItem item)
        {
            // 分隔线
            if (item.Component == "Divider")
            {
                return new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Colors.Gray) { Opacity = 0.3 },
                    Margin = new Thickness(0, 8, 0, 8)
                };
            }

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("200,*"),
                Margin = new Thickness(0, 8, 0, 8)
            };

            // 标签
            var labelPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            var label = new TextBlock
            {
                Text = item.Label,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            labelPanel.Children.Add(label);

            if (item.Required)
            {
                labelPanel.Children.Add(new TextBlock
                {
                    Text = " *",
                    Foreground = Brushes.Red,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });
            }

            Grid.SetColumn(labelPanel, 0);
            grid.Children.Add(labelPanel);

            // 输入控件
            Control? inputControl = item.Component switch
            {
                "Select" => CreateSelectInput(item),
                "Input" => CreateTextInput(item, false),
                "InputTextArea" => CreateTextInput(item, true),
                "InputPassword" => CreatePasswordInput(item),
                "InputNumber" => CreateNumberInput(item),
                "Switch" => CreateSwitchInput(item),
                _ => null
            };

            if (inputControl != null)
            {
                Grid.SetColumn(inputControl, 1);
                grid.Children.Add(inputControl);
            }

            // 帮助文本
            if (!string.IsNullOrEmpty(item.BottomHelpMessage))
            {
                var helpText = new TextBlock
                {
                    Text = item.BottomHelpMessage,
                    FontSize = 12,
                    Opacity = 0.6,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                Grid.SetColumn(helpText, 1);
                Grid.SetRow(helpText, 1);
                if (grid.RowDefinitions.Count == 0)
                    grid.RowDefinitions = new RowDefinitions("Auto,Auto");
                grid.Children.Add(helpText);
            }

            return grid;
        }

        /// <summary>
        /// 创建下拉选择框
        /// </summary>
        private ComboBox CreateSelectInput(SchemaItem item)
        {
            var comboBox = new ComboBox
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            if (item.ComponentProps?.Options is List<OptionItem> options)
            {
                foreach (var option in options)
                {
                    comboBox.Items.Add(option.Label);
                }

                // 设置当前值
                var currentOption = options.FirstOrDefault(o => o.Value?.ToString() == item.Value?.ToString());
                if (currentOption != null)
                {
                    comboBox.SelectedItem = currentOption.Label;
                }
            }

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is string selectedLabel &&
                    item.ComponentProps?.Options is List<OptionItem> opts)
                {
                    var selectedOption = opts.FirstOrDefault(o => o.Label == selectedLabel);
                    if (selectedOption != null)
                    {
                        item.Value = selectedOption.Value;
                    }
                }
            };

            return comboBox;
        }

        /// <summary>
        /// 创建文本输入框
        /// </summary>
        private TextBox CreateTextInput(SchemaItem item, bool isMultiline)
        {
            var textBox = new TextBox
            {
                Text = item.Value?.ToString() ?? "",
                Watermark = item.ComponentProps?.Placeholder ?? "",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            if (isMultiline)
            {
                textBox.AcceptsReturn = true;
                textBox.TextWrapping = TextWrapping.Wrap;
                textBox.MinHeight = 80;
            }

            textBox.TextChanged += (s, e) =>
            {
                item.Value = textBox.Text;
            };

            return textBox;
        }

        /// <summary>
        /// 创建密码输入框
        /// </summary>
        private TextBox CreatePasswordInput(SchemaItem item)
        {
            var textBox = new TextBox
            {
                Text = item.Value?.ToString() ?? "",
                Watermark = item.ComponentProps?.Placeholder ?? "",
                PasswordChar = '*',
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            textBox.TextChanged += (s, e) =>
            {
                item.Value = textBox.Text;
            };

            return textBox;
        }

        /// <summary>
        /// 创建数字输入框
        /// </summary>
        private NumericUpDown CreateNumberInput(SchemaItem item)
        {
            var numeric = new NumericUpDown
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Minimum = item.ComponentProps?.Min ?? decimal.MinValue,
                Maximum = item.ComponentProps?.Max ?? decimal.MaxValue
            };

            if (item.Value != null)
            {
                numeric.Value = ConvertToDecimal(item.Value);
            }

            numeric.ValueChanged += (s, e) =>
            {
                item.Value = numeric.Value;
            };

            return numeric;
        }

        /// <summary>
        /// 创建开关控件
        /// </summary>
        private ToggleSwitch CreateSwitchInput(SchemaItem item)
        {
            var toggle = new ToggleSwitch
            {
                IsChecked = ConvertToBoolean(item.Value),
                OnContent = "开启",
                OffContent = "关闭"
            };

            toggle.IsCheckedChanged += (s, e) =>
            {
                item.Value = toggle.IsChecked ?? false;
            };

            return toggle;
        }

        /// <summary>
        /// 返回按钮点击
        /// </summary>
        private void OnBackClick(object? sender, RoutedEventArgs e)
        {
            RenderPluginsList();
        }

        /// <summary>
        /// 保存按钮点击
        /// </summary>
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Saving config for plugin: {_currentPluginName}");
            System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Config items count: {_pluginConfig.Count}");

            var response = await _apiService.SetPluginConfigAsync(session.Token, _currentPluginName, _currentSource, _pluginConfig);

            System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Save response: Code={response?.Code}, Message={response?.Message}");

            if (response?.IsSuccess == true)
            {
                // 显示保存成功 Toast
                ToastService.Instance.ShowSuccess("插件配置保存成功");
                RenderPluginsList();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PluginConfigPage] Save failed: {response?.Message}");
                ToastService.Instance.ShowError($"保存失败: {response?.Message}");
            }
        }

        /// <summary>
        /// 转换为 decimal（处理 JsonElement）
        /// </summary>
        private decimal ConvertToDecimal(object? value)
        {
            if (value == null) return 0;
            if (value is decimal dec) return dec;
            if (value is double d) return (decimal)d;
            if (value is float f) return (decimal)f;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    if (jsonElement.TryGetDecimal(out var decimalValue))
                        return decimalValue;
                    if (jsonElement.TryGetInt64(out var intValue))
                        return (decimal)intValue;
                }
                if (jsonElement.ValueKind == JsonValueKind.String && decimal.TryParse(jsonElement.GetString(), out var parsed))
                    return parsed;
            }
            if (value is string str && decimal.TryParse(str, out var parsed2))
                return parsed2;
            return 0;
        }

        /// <summary>
        /// 转换为 bool（处理 JsonElement）
        /// </summary>
        private bool ConvertToBoolean(object? value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True) return true;
                if (jsonElement.ValueKind == JsonValueKind.False) return false;
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var str = jsonElement.GetString()?.ToLower();
                    return str == "true" || str == "1" || str == "yes";
                }
            }
            return false;
        }
    }
}
