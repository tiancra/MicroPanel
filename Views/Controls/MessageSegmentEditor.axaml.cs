using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MicroPanel.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MicroPanel.Views.Controls
{
    public partial class MessageSegmentEditor : UserControl
    {
        private MessageType _message = new();
        private int _index;
        private bool _isInitialized = false;

        public event EventHandler<int>? DeleteRequested;
        public event EventHandler<MessageType>? DataChanged;

        public MessageSegmentEditor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            InitializeEventHandlers();
            _isInitialized = true;
        }

        /// <summary>
        /// 初始化所有控件的事件处理器（只调用一次）
        /// </summary>
        private void InitializeEventHandlers()
        {
            // 文本类型
            var textBox = this.FindControl<TextBox>("TextContentTextBox");
            if (textBox != null)
            {
                textBox.TextChanged += (s, e) =>
                {
                    if (_message.Type == "text")
                    {
                        _message.Data = textBox.Text;
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 图片类型
            var imageUrlTextBox = this.FindControl<TextBox>("ImageUrlTextBox");
            if (imageUrlTextBox != null)
            {
                imageUrlTextBox.TextChanged += (s, e) =>
                {
                    if (_message.Type == "image")
                    {
                        _message.Url = imageUrlTextBox.Text;
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 音频类型
            var recordUrlTextBox = this.FindControl<TextBox>("RecordUrlTextBox");
            if (recordUrlTextBox != null)
            {
                recordUrlTextBox.TextChanged += (s, e) =>
                {
                    if (_message.Type == "record")
                    {
                        _message.Url = recordUrlTextBox.Text;
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 视频类型
            var videoUrlTextBox = this.FindControl<TextBox>("VideoUrlTextBox");
            if (videoUrlTextBox != null)
            {
                videoUrlTextBox.TextChanged += (s, e) =>
                {
                    if (_message.Type == "video")
                    {
                        _message.Url = videoUrlTextBox.Text;
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 表情类型
            var faceNumeric = this.FindControl<NumericUpDown>("FaceIdNumeric");
            if (faceNumeric != null)
            {
                faceNumeric.ValueChanged += (s, e) =>
                {
                    if (_message.Type == "face")
                    {
                        _message.Data = (int)(faceNumeric.Value ?? 0);
                        UpdateFacePreview((int)(faceNumeric.Value ?? 0));
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 戳一戳类型
            var pokeNumeric = this.FindControl<NumericUpDown>("PokeIdNumeric");
            if (pokeNumeric != null)
            {
                pokeNumeric.ValueChanged += (s, e) =>
                {
                    if (_message.Type == "poke")
                    {
                        _message.Data = (int)(pokeNumeric.Value ?? 0);
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 骰子类型
            var diceNumeric = this.FindControl<NumericUpDown>("DiceIdNumeric");
            if (diceNumeric != null)
            {
                diceNumeric.ValueChanged += (s, e) =>
                {
                    if (_message.Type == "dice")
                    {
                        _message.Data = (int)(diceNumeric.Value ?? 1);
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 猜拳类型
            var rpsNumeric = this.FindControl<NumericUpDown>("RpsIdNumeric");
            if (rpsNumeric != null)
            {
                rpsNumeric.ValueChanged += (s, e) =>
                {
                    if (_message.Type == "rps")
                    {
                        _message.Data = (int)(rpsNumeric.Value ?? 1);
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }

            // 代码类型
            var codeEditor = this.FindControl<CodeEditor>("CodeEditorControl");
            if (codeEditor != null)
            {
                codeEditor.CodeChanged += (s, code) =>
                {
                    if (_message.Type == "code")
                    {
                        _message.Data = code;
                        DataChanged?.Invoke(this, _message);
                    }
                };
            }
        }

        /// <summary>
        /// 设置消息段数据
        /// </summary>
        public void SetMessage(MessageType message, int index)
        {
            _message = message;
            _index = index;

            // 更新类型标签
            var typeTextBlock = this.FindControl<TextBlock>("TypeTextBlock");
            if (typeTextBlock != null)
            {
                typeTextBlock.Text = GetMessageTypeName(message.Type);
            }

            // 隐藏所有面板
            HideAllPanels();

            // 根据类型显示对应面板并加载数据
            switch (message.Type)
            {
                case "text":
                    ShowTextPanel();
                    break;
                case "image":
                    ShowImagePanel();
                    break;
                case "record":
                    ShowRecordPanel();
                    break;
                case "video":
                    ShowVideoPanel();
                    break;
                case "face":
                    ShowFacePanel();
                    break;
                case "poke":
                    ShowPokePanel();
                    break;
                case "dice":
                    ShowDicePanel();
                    break;
                case "rps":
                    ShowRpsPanel();
                    break;
                case "code":
                    ShowCodePanel();
                    break;
            }
        }

        /// <summary>
        /// 获取消息类型名称
        /// </summary>
        private string GetMessageTypeName(string type)
        {
            return type switch
            {
                "text" => "文本",
                "image" => "图片",
                "record" => "音频",
                "video" => "视频",
                "face" => "表情",
                "poke" => "戳一戳",
                "dice" => "骰子",
                "rps" => "猜拳",
                "markdown" => "Markdown",
                "button" => "按钮",
                "code" => "代码",
                _ => type
            };
        }

        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        private void HideAllPanels()
        {
            var panels = new[] { "TextPanel", "ImagePanel", "RecordPanel", "VideoPanel", 
                                "FacePanel", "PokePanel", "DicePanel", "RpsPanel", "CodePanel" };
            foreach (var panelName in panels)
            {
                var panel = this.FindControl<StackPanel>(panelName);
                if (panel != null) panel.IsVisible = false;
            }
        }

        #region 文本类型

        private void ShowTextPanel()
        {
            var panel = this.FindControl<StackPanel>("TextPanel");
            var textBox = this.FindControl<TextBox>("TextContentTextBox");

            if (panel != null) panel.IsVisible = true;
            if (textBox != null)
            {
                textBox.Text = _message.Data?.ToString() ?? "";
            }
        }

        #endregion

        #region 图片类型

        private void ShowImagePanel()
        {
            var panel = this.FindControl<StackPanel>("ImagePanel");
            var urlTextBox = this.FindControl<TextBox>("ImageUrlTextBox");
            var hashTextBlock = this.FindControl<TextBlock>("ImageHashTextBlock");

            if (panel != null) panel.IsVisible = true;
            if (urlTextBox != null)
            {
                urlTextBox.Text = _message.Url ?? "";
            }

            if (hashTextBlock != null)
            {
                if (!string.IsNullOrEmpty(_message.Hash))
                {
                    hashTextBlock.Text = $"Hash: {_message.Hash}";
                    hashTextBlock.IsVisible = true;
                }
                else
                {
                    hashTextBlock.IsVisible = false;
                }
            }
        }

        private void OnUploadImageClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 实现文件上传
        }

        private void OnEditImageClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 实现在线图片编辑器
        }

        #endregion

        #region 音频类型

        private void ShowRecordPanel()
        {
            var panel = this.FindControl<StackPanel>("RecordPanel");
            var urlTextBox = this.FindControl<TextBox>("RecordUrlTextBox");

            if (panel != null) panel.IsVisible = true;
            if (urlTextBox != null)
            {
                urlTextBox.Text = _message.Url ?? "";
            }
        }

        private void OnUploadRecordClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 实现文件上传
        }

        #endregion

        #region 视频类型

        private void ShowVideoPanel()
        {
            var panel = this.FindControl<StackPanel>("VideoPanel");
            var urlTextBox = this.FindControl<TextBox>("VideoUrlTextBox");

            if (panel != null) panel.IsVisible = true;
            if (urlTextBox != null)
            {
                urlTextBox.Text = _message.Url ?? "";
            }
        }

        private void OnUploadVideoClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 实现文件上传
        }

        #endregion

        #region 表情类型

        private void ShowFacePanel()
        {
            var panel = this.FindControl<StackPanel>("FacePanel");
            var numeric = this.FindControl<NumericUpDown>("FaceIdNumeric");

            if (panel != null) panel.IsVisible = true;

            if (numeric != null)
            {
                numeric.Value = _message.Data is int data ? data : 0;
            }

            UpdateFacePreview(_message.Data is int d ? d : 0);
        }

        private void UpdateFacePreview(int faceId)
        {
            var previewBorder = this.FindControl<Border>("FacePreviewBorder");

            if (previewBorder != null)
            {
                previewBorder.IsVisible = faceId > 0;
            }
        }

        private void OnSelectFaceClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 显示表情选择器
        }

        #endregion

        #region 戳一戳类型

        private void ShowPokePanel()
        {
            var panel = this.FindControl<StackPanel>("PokePanel");
            var numeric = this.FindControl<NumericUpDown>("PokeIdNumeric");

            if (panel != null) panel.IsVisible = true;
            if (numeric != null)
            {
                numeric.Value = _message.Data is int data ? data : 0;
            }
        }

        private void OnSelectPokeClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 显示戳一戳类型选择器
        }

        #endregion

        #region 骰子类型

        private void ShowDicePanel()
        {
            var panel = this.FindControl<StackPanel>("DicePanel");
            var numeric = this.FindControl<NumericUpDown>("DiceIdNumeric");

            if (panel != null) panel.IsVisible = true;
            if (numeric != null)
            {
                numeric.Value = _message.Data is int data ? data : 1;
            }
        }

        private void OnSelectDiceClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 显示骰子选择器
        }

        #endregion

        #region 猜拳类型

        private void ShowRpsPanel()
        {
            var panel = this.FindControl<StackPanel>("RpsPanel");
            var numeric = this.FindControl<NumericUpDown>("RpsIdNumeric");

            if (panel != null) panel.IsVisible = true;
            if (numeric != null)
            {
                numeric.Value = _message.Data is int data ? data : 1;
            }
        }

        private void OnSelectRpsClick(object? sender, RoutedEventArgs e)
        {
            // TODO: 显示猜拳选择器
        }

        #endregion

        #region 代码类型

        private void ShowCodePanel()
        {
            var panel = this.FindControl<StackPanel>("CodePanel");
            var editor = this.FindControl<CodeEditor>("CodeEditorControl");

            if (panel != null) panel.IsVisible = true;
            if (editor != null)
            {
                var code = _message.Data?.ToString() ?? "";
                editor.SetCode(code);
            }
        }

        #endregion

        /// <summary>
        /// 删除按钮点击
        /// </summary>
        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, _index);
        }

        /// <summary>
        /// 获取当前消息数据
        /// </summary>
        public MessageType GetMessage()
        {
            // 从控件获取最新数据
            RefreshMessageData();
            return _message;
        }

        /// <summary>
        /// 从控件刷新消息数据
        /// </summary>
        private void RefreshMessageData()
        {
            switch (_message.Type)
            {
                case "text":
                    var textBox = this.FindControl<TextBox>("TextContentTextBox");
                    if (textBox != null) _message.Data = textBox.Text;
                    break;
                case "image":
                    var imageUrlTextBox = this.FindControl<TextBox>("ImageUrlTextBox");
                    if (imageUrlTextBox != null) _message.Url = imageUrlTextBox.Text;
                    break;
                case "record":
                    var recordUrlTextBox = this.FindControl<TextBox>("RecordUrlTextBox");
                    if (recordUrlTextBox != null) _message.Url = recordUrlTextBox.Text;
                    break;
                case "video":
                    var videoUrlTextBox = this.FindControl<TextBox>("VideoUrlTextBox");
                    if (videoUrlTextBox != null) _message.Url = videoUrlTextBox.Text;
                    break;
                case "face":
                    var faceNumeric = this.FindControl<NumericUpDown>("FaceIdNumeric");
                    if (faceNumeric != null) _message.Data = (int)(faceNumeric.Value ?? 0);
                    break;
                case "poke":
                    var pokeNumeric = this.FindControl<NumericUpDown>("PokeIdNumeric");
                    if (pokeNumeric != null) _message.Data = (int)(pokeNumeric.Value ?? 0);
                    break;
                case "dice":
                    var diceNumeric = this.FindControl<NumericUpDown>("DiceIdNumeric");
                    if (diceNumeric != null) _message.Data = (int)(diceNumeric.Value ?? 1);
                    break;
                case "rps":
                    var rpsNumeric = this.FindControl<NumericUpDown>("RpsIdNumeric");
                    if (rpsNumeric != null) _message.Data = (int)(rpsNumeric.Value ?? 1);
                    break;
                case "code":
                    var codeEditor = this.FindControl<CodeEditor>("CodeEditorControl");
                    if (codeEditor != null) _message.Data = codeEditor.GetCode();
                    break;
            }
        }
    }
}
