using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using System;

namespace MicroPanelAvalonia.Views.Controls
{
    public partial class CodeEditor : UserControl
    {
        public static readonly StyledProperty<string> CodeProperty =
            AvaloniaProperty.Register<CodeEditor, string>(nameof(Code), defaultValue: "");

        public static readonly StyledProperty<string> LanguageProperty =
            AvaloniaProperty.Register<CodeEditor, string>(nameof(Language), defaultValue: "javascript");

        public string Code
        {
            get => GetValue(CodeProperty);
            set => SetValue(CodeProperty, value);
        }

        public string Language
        {
            get => GetValue(LanguageProperty);
            set => SetValue(LanguageProperty, value);
        }

        public event EventHandler<string>? CodeChanged;
        public event EventHandler? SaveRequested;

        public CodeEditor()
        {
            InitializeComponent();
            
            var textBox = this.FindControl<TextBox>("CodeTextBox");
            if (textBox != null)
            {
                textBox.TextChanged += OnTextChanged;
            }

            // 监听主题变化
            if (Application.Current != null)
            {
                Application.Current.ActualThemeVariantChanged += OnThemeChanged;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CodeProperty)
            {
                var textBox = this.FindControl<TextBox>("CodeTextBox");
                if (textBox != null && textBox.Text != Code)
                {
                    textBox.Text = Code;
                }
            }
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                Code = textBox.Text ?? "";
                CodeChanged?.Invoke(this, Code);
            }
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            UpdateTheme();
        }

        private void UpdateTheme()
        {
            var textBox = this.FindControl<TextBox>("CodeTextBox");
            
            if (textBox == null) return;

            bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            
            // 根据主题设置颜色
            if (isDark)
            {
                textBox.Foreground = Brushes.White;
            }
            else
            {
                textBox.Foreground = Brushes.Black;
            }
        }

        /// <summary>
        /// 插入代码模板
        /// </summary>
        private void OnInsertTemplateClick(object? sender, RoutedEventArgs e)
        {
            var template = GetCodeTemplate();
            var textBox = this.FindControl<TextBox>("CodeTextBox");
            
            if (textBox != null)
            {
                // 在光标位置插入模板
                var caretIndex = textBox.CaretIndex;
                var text = textBox.Text ?? "";
                var newText = text.Insert(caretIndex, template);
                textBox.Text = newText;
                textBox.CaretIndex = caretIndex + template.Length;
            }
        }

        /// <summary>
        /// 保存按钮点击
        /// </summary>
        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 获取代码模板
        /// </summary>
        private string GetCodeTemplate()
        {
            var language = Language.ToLower();
            
            if (language == "markdown" || language == "md")
            {
                return @"# Markdown 标题

这是正文内容。

- 列表项1
- 列表项2

**粗体文本**

[链接文本](http://example.com)
";
            }
            
            // JavaScript / 默认模板
            return @"/**
 * 编写后请记得先点击右上角保存代码，然后点击该页面顶部保存按钮即可保存此插件，保存后立即生效！
 * 这是自定义代码段，拥有JavaScript的正式环境，你可以像正常写yunzai的js插件一样编写执行逻辑
 * 该作用域提供了以下变量，包含了yunzai的基本API，你可以在全局直接使用
 * @params e 消息事件变量，包含了消息事件的全部属性和方法
 * @params Bot 全局Bot
 * @params puppeteer 渲染器，提供了如puppeteer.screenshot等方法
 * @params segment 消息段制作
 * @params loader yunzai的插件加载实例
 * @params logger yunzai日志打印
 */

/** 
 * 如果你使用npm方式安装小微插件，那么需要使用模块化导入yunzai的方法，例如const {Config} = await import('yunzai')
 * 如果你使用git安装小微插件，当前代码的执行路径为./plugins/micro-plugin/dist/apps/message.js
 * 换而言之，如果你需要相对路径导入模块，
 * 则需要使用 const puppeteer = await import('../../../../lib/puppeteer/puppeteer.js'),当然该方式不被推荐
 * 注意你不能在这里使用import xxx from 'xxx'语法，
 * 因为所有代码将在函数作用域执行，你可以使用以下的动态导入依赖示例
 */

const moment = (await import('moment')).default
const now = moment().format('YYYY/MM/DD HH:mm:ss')
logger.info('现在是：',now)
e.reply(now)
";
        }

        /// <summary>
        /// 获取当前代码
        /// </summary>
        public string GetCode()
        {
            return Code;
        }

        /// <summary>
        /// 设置代码
        /// </summary>
        public void SetCode(string code)
        {
            Code = code;
            var textBox = this.FindControl<TextBox>("CodeTextBox");
            if (textBox != null)
            {
                textBox.Text = code;
            }
        }
    }
}
