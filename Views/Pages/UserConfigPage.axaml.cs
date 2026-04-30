using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MicroPanelAvalonia.Models;
using MicroPanelAvalonia.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views.Pages
{
    public partial class UserConfigPage : UserControl
    {
        private readonly ApiService _apiService;
        private List<Dictionary<string, UserConfigItem>> _userConfig = new();
        private readonly Dictionary<string, string> _moduleNames = new()
        {
            ["username"] = "用户名",
            ["password"] = "密码",
            ["avatar"] = "头像",
            ["desc"] = "描述",
            ["routes"] = "对该用户隐藏的选项",
            ["masterQQ"] = "主人QQ",
            ["token"] = "登录令牌",
            ["expires"] = "Token过期时间"
        };

        // 对该用户隐藏的选项映射
        private readonly Dictionary<string, string> _routeMap = new()
        {
            ["Status"] = "系统状态",
            ["Logs"] = "日志输出",
            ["Fs"] = "文件管理",
            ["Plugin"] = "插件开发",
            ["Bot"] = "Bot配置",
            ["Protocol"] = "协议配置",
            ["Plugins"] = "插件配置",
            ["Permission"] = "权限配置",
            ["About"] = "关于应用"
        };

        public UserConfigPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            Loaded += async (s, e) => await LoadUserConfigAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 加载用户配置
        /// </summary>
        private async Task LoadUserConfigAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            var response = await _apiService.GetUserConfigAsync(session.Token);
            if (response?.IsSuccess == true && response.Data != null)
            {
                _userConfig = response.Data;
                RenderConfigForm();
            }
        }

        /// <summary>
        /// 渲染配置表单
        /// </summary>
        private void RenderConfigForm()
        {
            var configPanel = this.FindControl<StackPanel>("ConfigPanel");
            if (configPanel == null) return;

            configPanel.Children.Clear();

            foreach (var userDict in _userConfig)
            {
                var userCard = CreateUserExpander(userDict);
                configPanel.Children.Add(userCard);
            }
        }

        /// <summary>
        /// 创建用户折叠卡片
        /// </summary>
        private Expander CreateUserExpander(Dictionary<string, UserConfigItem> userDict)
        {
            // 获取用户名作为标题
            var username = userDict.TryGetValue("username", out var userItem) 
                ? GetStringValue(userItem.Value) 
                : "未知用户";

            var expander = new Expander
            {
                Header = new TextBlock
                {
                    Text = username,
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold
                },
                Background = new SolidColorBrush(Colors.LightGray) { Opacity = 0.1 },
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 4),
                BorderBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.3 },
                BorderThickness = new Thickness(1),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            // 内容区域
            var contentStack = new StackPanel { Spacing = 12 };

            foreach (var kvp in userDict)
            {
                // 跳过用户名，已经在标题显示
                if (kvp.Key == "username") continue;

                var configRow = CreateConfigRow(kvp.Key, kvp.Value);
                contentStack.Children.Add(configRow);
            }

            expander.Content = contentStack;
            return expander;
        }

        /// <summary>
        /// 创建配置行
        /// </summary>
        private Grid CreateConfigRow(string key, UserConfigItem item)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("200,*,Auto")
            };

            // 标签
            var label = new TextBlock
            {
                Text = _moduleNames.TryGetValue(key, out var name) ? name : key,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // 输入控件 - routes 使用复选框组
            Control? inputControl;
            if (key == "routes")
            {
                inputControl = CreateRoutesCheckBoxes(item);
            }
            else
            {
                inputControl = CreateInputControl(item);
            }
            
            if (inputControl != null)
            {
                Grid.SetColumn(inputControl, 1);
                grid.Children.Add(inputControl);
            }

            // 开关（用于启用/禁用）- routes、password、desc、expires 不需要开关
            var noToggleKeys = new[] { "routes", "password", "desc", "expires" };
            if (!noToggleKeys.Contains(key))
            {
                var toggle = new ToggleSwitch
                {
                    OnContent = "启用",
                    OffContent = "禁用",
                    IsChecked = item.Value != null && !string.IsNullOrEmpty(GetStringValue(item.Value)),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(16, 0, 0, 0)
                };
                toggle.IsCheckedChanged += (s, e) =>
                {
                    if (toggle.IsChecked != true)
                    {
                        item.Value = "";
                        // 刷新输入控件
                        RenderConfigForm();
                    }
                };
                Grid.SetColumn(toggle, 2);
                grid.Children.Add(toggle);
            }

            return grid;
        }

        /// <summary>
        /// 创建对该用户隐藏的选项复选框组
        /// </summary>
        private Control CreateRoutesCheckBoxes(UserConfigItem item)
        {
            var panel = new WrapPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // 获取当前已选中的路由
            var selectedRoutes = GetSelectedRoutes(item.Value);

            foreach (var route in _routeMap)
            {
                var checkBox = new CheckBox
                {
                    Content = route.Value,
                    IsChecked = selectedRoutes.Contains(route.Key),
                    Margin = new Thickness(0, 4, 16, 4)
                };

                checkBox.IsCheckedChanged += (s, e) =>
                {
                    UpdateRoutesValue(item, route.Key, checkBox.IsChecked == true);
                };

                panel.Children.Add(checkBox);
            }

            return panel;
        }

        /// <summary>
        /// 获取已选中的路由列表
        /// </summary>
        private List<string> GetSelectedRoutes(object? value)
        {
            var routes = new List<string>();

            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        routes.Add(element.GetString() ?? "");
                    }
                }
            }
            else if (value is List<object> list)
            {
                foreach (var item in list)
                {
                    routes.Add(item.ToString() ?? "");
                }
            }

            return routes;
        }

        /// <summary>
        /// 更新对该用户隐藏的选项值
        /// </summary>
        private void UpdateRoutesValue(UserConfigItem item, string route, bool isChecked)
        {
            var currentRoutes = GetSelectedRoutes(item.Value);

            if (isChecked && !currentRoutes.Contains(route))
            {
                currentRoutes.Add(route);
            }
            else if (!isChecked && currentRoutes.Contains(route))
            {
                currentRoutes.Remove(route);
            }

            item.Value = currentRoutes;
        }

        /// <summary>
        /// 创建输入控件
        /// </summary>
        private Control? CreateInputControl(UserConfigItem item)
        {
            var value = item.Value;

            // 根据类型创建不同的控件
            if (value is JsonElement jsonElement)
            {
                return CreateControlFromJsonElement(jsonElement, item);
            }

            // 默认文本输入
            return CreateTextInput(item);
        }

        /// <summary>
        /// 根据 JsonElement 类型创建控件
        /// </summary>
        private Control? CreateControlFromJsonElement(JsonElement element, UserConfigItem item)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return CreateTextInput(item, element.GetString() ?? "");
                case JsonValueKind.Number:
                    // 处理整数和小数
                    if (element.TryGetDecimal(out var decimalValue))
                    {
                        return CreateNumberInput(item, decimalValue);
                    }
                    if (element.TryGetInt64(out var intValue))
                    {
                        return CreateNumberInput(item, (decimal)intValue);
                    }
                    return CreateTextInput(item, element.ToString());
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return CreateSwitchInput(item, element.GetBoolean());
                case JsonValueKind.Array:
                    return CreateArrayInput(item, element);
                default:
                    return CreateTextInput(item, element.ToString());
            }
        }

        /// <summary>
        /// 创建文本输入框
        /// </summary>
        private TextBox CreateTextInput(UserConfigItem item, string? initialValue = null)
        {
            var text = initialValue ?? GetStringValue(item.Value) ?? "";
            
            var textBox = new TextBox
            {
                Text = text,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                IsEnabled = !string.IsNullOrEmpty(text)
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
        private NumericUpDown CreateNumberInput(UserConfigItem item, decimal? initialValue = null)
        {
            var numeric = new NumericUpDown
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            if (initialValue.HasValue)
            {
                numeric.Value = initialValue.Value;
            }

            numeric.ValueChanged += (s, e) =>
            {
                item.Value = numeric.Value ?? 0;
            };

            return numeric;
        }

        /// <summary>
        /// 创建开关控件
        /// </summary>
        private ToggleSwitch CreateSwitchInput(UserConfigItem item, bool? initialValue = null)
        {
            var toggle = new ToggleSwitch
            {
                IsChecked = initialValue ?? false,
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
        /// 创建数组输入（标签形式）
        /// </summary>
        private Control CreateArrayInput(UserConfigItem item, JsonElement element)
        {
            var panel = new StackPanel { Spacing = 4 };

            // 显示当前数组内容
            var items = new List<string>();
            foreach (var arrayItem in element.EnumerateArray())
            {
                items.Add(arrayItem.ToString());
            }

            var textBox = new TextBox
            {
                Text = string.Join(", ", items),
                Watermark = "使用逗号分隔多个值",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            textBox.TextChanged += (s, e) =>
            {
                // 将逗号分隔的文本转换为数组
                var values = textBox.Text?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();
                item.Value = values;
            };

            panel.Children.Add(textBox);
            return panel;
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        private string GetStringValue(object? value)
        {
            if (value == null) return "";
            if (value is string str) return str;
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString() ?? "";
            }
            return value.ToString() ?? "";
        }

        /// <summary>
        /// 保存按钮点击
        /// </summary>
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            var response = await _apiService.SetUserConfigAsync(session.Token, _userConfig);
            if (response?.IsSuccess == true)
            {
                // 显示保存成功 Toast
                ToastService.Instance.ShowSuccess("用户配置保存成功");
                // 保存成功，刷新列表
                await LoadUserConfigAsync();
            }
        }
    }
}
