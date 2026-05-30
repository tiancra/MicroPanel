using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;

namespace MicroPanel.Helpers.UI;

/// <summary>
/// 一些常用的 TaskDialog
/// </summary>
public static class CommonTaskDialogs
{
    /// <summary>
    /// 显示基本提示框
    /// </summary>
    /// <param name="header">对话框头</param>
    /// <param name="content">要显示的内容</param>
    /// <param name="xamlRoot">XAML 根元素</param>
    public static async Task<object?> ShowDialog(string header, string content, Visual? xamlRoot = null)
    {
        var dialog = new TaskDialog()
        {
            Content = content,
            Header = header,
            Buttons =
            {
                new TaskDialogButton("确定", true)
                {
                    IsDefault = true,
                }
            },
            XamlRoot = xamlRoot ?? GetRootWindow()
        };
        
        return await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="header">对话框头</param>
    /// <param name="content">要显示的内容</param>
    /// <param name="xamlRoot">XAML 根元素</param>
    /// <param name="confirmText">确认按钮文字</param>
    /// <param name="cancelText">取消按钮文字</param>
    /// <returns>是否确认</returns>
    public static async Task<bool> ShowConfirmDialog(string header, string content, Visual? xamlRoot = null, string confirmText = "确定", string cancelText = "取消")
    {
        var dialog = new TaskDialog()
        {
            Content = content,
            Header = header,
            Buttons =
            {
                new TaskDialogButton(confirmText, true)
                {
                    IsDefault = true,
                },
                new TaskDialogButton(cancelText, false)
            },
            XamlRoot = xamlRoot ?? GetRootWindow()
        };
        
        var result = await dialog.ShowAsync();
        return result is bool b && b;
    }

    /// <summary>
    /// 获取根窗口
    /// </summary>
    private static Visual? GetRootWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}