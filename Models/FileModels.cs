using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MicroPanelAvalonia.Models
{
    /// <summary>
    /// 文件或目录项
    /// </summary>
    public class FileItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("mtime")]
        public string Mtime { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "file"; // "file" or "directory"

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        // UI 辅助属性
        public bool IsRightClicked { get; set; } = false;
        public bool IsEditing { get; set; } = false;
        public string? HandlerMode { get; set; }
    }

    /// <summary>
    /// 目录列表响应
    /// </summary>
    public class DirectoryListResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public DirectoryData? Data { get; set; }
    }

    /// <summary>
    /// 目录数据
    /// </summary>
    public class DirectoryData
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("children")]
        public List<FileItem> Children { get; set; } = new();
    }

    /// <summary>
    /// 文件内容响应
    /// </summary>
    public class FileContentResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    /// <summary>
    /// 文件大小响应
    /// </summary>
    public class FileSizeResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    /// <summary>
    /// 通用操作响应
    /// </summary>
    public class FileOperationResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    /// <summary>
    /// 右键菜单项
    /// </summary>
    public class ContextMenuItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
    }
}
