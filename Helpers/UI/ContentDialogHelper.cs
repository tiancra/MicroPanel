using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;

namespace MicroPanel.Helpers.UI;

/// <summary>
/// ContentDialog 辅助类
/// </summary>
public static class ContentDialogHelper
{
    /// <summary>
    /// 显示确认提示框
    /// </summary>
    /// <param name="title">提示框标题</param>
    /// <param name="content">内容</param>
    /// <param name="root">视觉根</param>
    /// <param name="positiveText">确认按钮文字</param>
    /// <param name="negativeText">取消按钮文字</param>
    /// <returns>是否通过验证</returns>
    public static async Task<bool> ShowConfirmationDialog(string title, string content, TopLevel? root = null, string positiveText = "确定", string negativeText = "取消")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            Content = content,
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = positiveText,
            CloseButtonText = negativeText
        };
        var r = await dialog.ShowAsync(root);
        return r == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 显示自定义内容的对话框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容控件</param>
    /// <param name="root">视觉根</param>
    /// <param name="primaryText">主按钮文字</param>
    /// <param name="secondaryText">次按钮文字</param>
    /// <param name="closeText">关闭按钮文字</param>
    /// <returns>对话框结果</returns>
    public static async Task<ContentDialogResult> ShowDialog(string title, Control content, TopLevel? root = null, string? primaryText = "确定", string? secondaryText = null, string? closeText = "取消")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            Content = content,
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = primaryText ?? "",
            SecondaryButtonText = secondaryText ?? "",
            CloseButtonText = closeText ?? ""
        };
        return await dialog.ShowAsync(root);
    }
}