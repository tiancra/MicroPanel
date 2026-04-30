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
    public partial class ProtocolConfigPage : UserControl
    {
        private readonly ApiService _apiService;
        private ProtocolConfig _protocolConfig = new();

        public ProtocolConfigPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
            Loaded += async (s, e) => await LoadProtocolConfigAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 加载协议配置
        /// </summary>
        private async Task LoadProtocolConfigAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            var response = await _apiService.GetProtocolConfigAsync(session.Token);
            if (response?.IsSuccess == true && response.Data != null)
            {
                _protocolConfig = response.Data;
                RenderConfigForm();
            }
        }

        /// <summary>
        /// 渲染配置表单
        /// </summary>
        private void RenderConfigForm()
        {
            var stdinPanel = this.FindControl<StackPanel>("StdinPanel");
            var onebotv11Panel = this.FindControl<StackPanel>("Onebotv11Panel");

            if (stdinPanel != null && _protocolConfig.Stdin != null)
            {
                stdinPanel.Children.Clear();
                foreach (var kvp in _protocolConfig.Stdin)
                {
                    var row = CreateConfigRow(kvp.Key, kvp.Value);
                    stdinPanel.Children.Add(row);
                }
            }

            if (onebotv11Panel != null && _protocolConfig.Onebotv11 != null)
            {
                onebotv11Panel.Children.Clear();
                foreach (var kvp in _protocolConfig.Onebotv11)
                {
                    var row = CreateConfigRow(kvp.Key, kvp.Value);
                    onebotv11Panel.Children.Add(row);
                }
            }
        }

        /// <summary>
        /// 创建配置行
        /// </summary>
        private Grid CreateConfigRow(string key, ProtocolConfigItem item)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("200,*")
            };

            // 标签
            var label = new TextBlock
            {
                Text = key,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // 输入控件
            var inputControl = CreateInputControl(item);
            Grid.SetColumn(inputControl, 1);
            grid.Children.Add(inputControl);

            return grid;
        }

        /// <summary>
        /// 创建输入控件
        /// </summary>
        private Control CreateInputControl(ProtocolConfigItem item)
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
        private Control CreateControlFromJsonElement(JsonElement element, ProtocolConfigItem item)
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
        private TextBox CreateTextInput(ProtocolConfigItem item, string? initialValue = null)
        {
            var text = initialValue ?? item.Value?.ToString() ?? "";

            var textBox = new TextBox
            {
                Text = text,
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
        private NumericUpDown CreateNumberInput(ProtocolConfigItem item, decimal? initialValue = null)
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
        private ToggleSwitch CreateSwitchInput(ProtocolConfigItem item, bool? initialValue = null)
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
        /// 创建数组输入
        /// </summary>
        private Control CreateArrayInput(ProtocolConfigItem item, JsonElement element)
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
        /// 保存按钮点击
        /// </summary>
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            var session = SessionService.Instance;
            if (session.CurrentServer == null || string.IsNullOrEmpty(session.Token)) return;

            _apiService.SetBaseUrl(session.CurrentServer.ServerAddress);

            var response = await _apiService.SetProtocolConfigAsync(session.Token, _protocolConfig);
            if (response?.IsSuccess == true)
            {
                // 显示保存成功 Toast
                ToastService.Instance.ShowSuccess("协议配置保存成功");
                // 保存成功，刷新列表
                await LoadProtocolConfigAsync();
            }
        }
    }
}
