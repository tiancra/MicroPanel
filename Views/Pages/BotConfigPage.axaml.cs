using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanel.Views.Pages
{
    public partial class BotConfigPage : UserControl
    {
        private readonly ApiService _apiService;
        private string _currentConfigType = "bot";
        private Dictionary<string, ConfigItem> _configData = new();

        public BotConfigPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            
            Loaded += async (s, e) => await LoadConfigAsync("bot");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private async Task LoadConfigAsync(string configName)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);
            
            var response = await _apiService.GetBotConfigAsync(session.Token, configName);
            if (response?.IsSuccess == true && response.Data != null)
            {
                _configData = response.Data;
                _currentConfigType = configName;
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

            foreach (var item in _configData)
            {
                var configItem = item.Value;
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("200,*"),
                    Margin = new Avalonia.Thickness(0, 8, 0, 8)
                };

                // 标签
                var labelText = IsLogLevel(configItem) 
                    ? "日志等级" 
                    : configItem.Desc;
                var label = new TextBlock
                {
                    Text = labelText,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                };
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                // 根据类型创建不同的输入控件
                Control inputControl = configItem.Type switch
                {
                    "string" => IsLogLevel(configItem) 
                        ? CreateLogLevelComboBox(configItem) 
                        : CreateStringInput(configItem),
                    "number" => CreateNumberInput(configItem),
                    "boolean" => CreateBooleanInput(configItem),
                    "array" => CreateArrayInput(configItem),
                    _ => new TextBox { Text = configItem.Value?.ToString() }
                };
                
                Grid.SetColumn(inputControl, 1);
                grid.Children.Add(inputControl);

                configPanel.Children.Add(grid);
            }
        }

        /// <summary>
        /// 判断是否是日志等级配置
        /// </summary>
        private bool IsLogLevel(ConfigItem item)
        {
            return item.Desc.Contains("日志等级") && 
                   item.Desc.Contains("trace") && 
                   item.Desc.Contains("debug");
        }

        /// <summary>
        /// 创建日志等级下拉框
        /// </summary>
        private ComboBox CreateLogLevelComboBox(ConfigItem item)
        {
            var comboBox = new ComboBox
            {
                Width = 150,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };

            var logLevels = new[] { "trace", "debug", "info", "warn", "fatal", "mark", "error", "off" };
            foreach (var level in logLevels)
            {
                comboBox.Items.Add(level);
            }

            // 设置当前值
            var currentValue = ConvertJsonValueToString(item.Value) ?? "info";
            comboBox.SelectedItem = currentValue;

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is string selectedLevel)
                {
                    item.Value = selectedLevel;
                }
            };

            return comboBox;
        }

        /// <summary>
        /// 创建字符串输入框
        /// </summary>
        private TextBox CreateStringInput(ConfigItem item)
        {
            var textBox = new TextBox
            {
                Text = ConvertJsonValueToString(item.Value) ?? "",
                Watermark = $"请输入{item.Desc}"
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
        private NumericUpDown CreateNumberInput(ConfigItem item)
        {
            var numeric = new NumericUpDown
            {
                Value = ConvertJsonValueToDecimal(item.Value),
                Minimum = decimal.MinValue,
                Maximum = decimal.MaxValue
            };
            
            numeric.ValueChanged += (s, e) =>
            {
                item.Value = numeric.Value;
            };
            
            return numeric;
        }

        /// <summary>
        /// 创建布尔开关
        /// </summary>
        private ToggleSwitch CreateBooleanInput(ConfigItem item)
        {
            var toggle = new ToggleSwitch
            {
                IsChecked = ConvertJsonValueToBoolean(item.Value),
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
        /// 将JsonElement转换为字符串
        /// </summary>
        private string? ConvertJsonValueToString(object? value)
        {
            if (value == null) return null;
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => jsonElement.ToString()
                };
            }
            return value.ToString();
        }

        /// <summary>
        /// 将JsonElement转换为Decimal
        /// </summary>
        private decimal ConvertJsonValueToDecimal(object? value)
        {
            if (value == null) return 0;
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    if (jsonElement.TryGetDecimal(out var dec)) return dec;
                    if (jsonElement.TryGetDouble(out var dbl)) return (decimal)dbl;
                    if (jsonElement.TryGetInt64(out var lng)) return lng;
                }
                if (jsonElement.ValueKind == JsonValueKind.String && 
                    decimal.TryParse(jsonElement.GetString(), out var parsed))
                {
                    return parsed;
                }
                return 0;
            }
            return 0;
        }

        /// <summary>
        /// 将JsonElement转换为Boolean
        /// </summary>
        private bool ConvertJsonValueToBoolean(object? value)
        {
            if (value == null) return false;
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => bool.TryParse(jsonElement.GetString(), out var b) && b,
                    _ => false
                };
            }
            return false;
        }

        /// <summary>
        /// 创建数组输入
        /// </summary>
        private Grid CreateArrayInput(ConfigItem item)
        {
            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto")
            };

            // 标签行 - 先声明以避免闭包问题
            var tagsPanel = new WrapPanel
            {
                Margin = new Avalonia.Thickness(0, 8, 0, 0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };

            // 输入行
            var inputPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };

            // 当前值输入框
            Control valueInput = item.SubType switch
            {
                "string" => new TextBox { Width = 150, Watermark = "输入值" },
                "number" => new NumericUpDown { Width = 150 },
                "boolean" => new ToggleSwitch { OnContent = "Y", OffContent = "N" },
                _ => new TextBox { Width = 150 }
            };
            valueInput.Name = "CurrentValueInput";
            inputPanel.Children.Add(valueInput);

            // 添加按钮
            var addButton = new Button
            {
                Content = "+",
                Classes = { "Accent" }
            };
            addButton.Click += (s, e) =>
            {
                object? newValue = valueInput switch
                {
                    TextBox tb => tb.Text,
                    NumericUpDown num => num.Value,
                    ToggleSwitch toggle => toggle.IsChecked,
                    _ => null
                };

                if (newValue != null)
                {
                    var list = GetOrCreateList(item);
                    if (list != null)
                    {
                        list.Add(newValue);
                        RenderArrayTags(item, tagsPanel);
                    }
                }
            };
            inputPanel.Children.Add(addButton);

            grid.Children.Add(inputPanel);
            Grid.SetRow(inputPanel, 0);

            // 初始化标签
            RenderArrayTags(item, tagsPanel);
            grid.Children.Add(tagsPanel);
            Grid.SetRow(tagsPanel, 1);

            return grid;
        }

        /// <summary>
        /// 获取或创建列表
        /// </summary>
        private List<object>? GetOrCreateList(ConfigItem item)
        {
            if (item.Value == null)
            {
                item.Value = new List<object>();
                return item.Value as List<object>;
            }

            if (item.Value is List<object> list)
            {
                return list;
            }

            // 处理 JsonElement 数组
            if (item.Value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                var newList = new List<object>();
                foreach (var element in jsonElement.EnumerateArray())
                {
                    newList.Add(ConvertJsonElementToObject(element));
                }
                item.Value = newList;
                return newList;
            }

            return null;
        }

        /// <summary>
        /// 将 JsonElement 转换为对象
        /// </summary>
        private object? ConvertJsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToList(),
                JsonValueKind.Object => element,
                _ => element.ToString()
            };
        }

        /// <summary>
        /// 渲染数组标签
        /// </summary>
        private void RenderArrayTags(ConfigItem item, WrapPanel panel)
        {
            panel.Children.Clear();
            
            var list = GetOrCreateList(item);
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                var index = i;
                
                var border = new Border
                {
                    Background = Avalonia.Media.Brushes.LightGray,
                    CornerRadius = new Avalonia.CornerRadius(4),
                    Padding = new Avalonia.Thickness(8, 4, 8, 4),
                    Margin = new Avalonia.Thickness(0, 0, 8, 8)
                };

                var stack = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 6
                };

                var text = new TextBlock
                {
                    Text = value?.ToString(),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                stack.Children.Add(text);

                var deleteBtn = new Button
                {
                    Content = "×",
                    Classes = { "Ghost" },
                    Padding = new Avalonia.Thickness(2),
                    FontSize = 12
                };
                deleteBtn.Click += (s, e) =>
                {
                    list.RemoveAt(index);
                    RenderArrayTags(item, panel);
                };
                stack.Children.Add(deleteBtn);

                border.Child = stack;
                panel.Children.Add(border);
            }
        }

        /// <summary>
        /// 配置类型改变
        /// </summary>
        private async void OnConfigTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    await LoadConfigAsync(tag);
                }
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            // 转换配置数据 - 与前端Vue保持一致，发送完整的配置对象
            var saveData = new Dictionary<string, object>();
            foreach (var item in _configData)
            {
                var configItem = item.Value;
                saveData[item.Key] = new
                {
                    desc = configItem.Desc,
                    type = configItem.Type,
                    subType = configItem.SubType,
                    value = configItem.Value,
                    cur = configItem.Cur
                };
            }

            var response = await _apiService.SetBotConfigAsync(session.Token, _currentConfigType, saveData);
            if (response?.IsSuccess == true)
            {
                // 显示保存成功 Toast
                ToastService.Instance.ShowSuccess("配置保存成功");
            }
        }
    }

}
