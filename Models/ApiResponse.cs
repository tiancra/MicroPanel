using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroPanelAvalonia.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        public bool IsSuccess => Code == 200;
    }

    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public class UserInfoResponse
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("masterQQ")]
        public string? MasterQQ { get; set; }

        [JsonPropertyName("routes")]
        public List<string>? Routes { get; set; }
    }

    public class SystemStatusResponse : SystemStatusData
    {
    }

    public class LoginRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Redis键节点
    /// </summary>
    public class RedisKeyNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("children")]
        public List<RedisKeyNode>? Children { get; set; }
    }

    /// <summary>
    /// Bot配置项
    /// </summary>
    public class ConfigItem
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        [JsonPropertyName("subType")]
        public string? SubType { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("cur")]
        public object? Cur { get; set; }
    }

    #region Plugin Config Models

    /// <summary>
    /// 插件信息
    /// </summary>
    public class PluginInfo
    {
        [JsonPropertyName("pluginName")]
        public string PluginName { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("author")]
        public JsonElement Author { get; set; }

        [JsonPropertyName("authorLink")]
        public JsonElement AuthorLink { get; set; }

        [JsonPropertyName("link")]
        public JsonElement Link { get; set; }

        [JsonPropertyName("isV2")]
        public bool IsV2 { get; set; }

        [JsonPropertyName("isV3")]
        public bool IsV3 { get; set; }

        [JsonPropertyName("isV4")]
        public bool? IsV4 { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("iconColor")]
        public string? IconColor { get; set; }

        [JsonPropertyName("iconPath")]
        public string? IconPath { get; set; }

        /// <summary>
        /// 获取字符串值（处理字符串或数组情况）
        /// </summary>
        private string GetStringValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) 
                return "";
            if (value.ValueKind == JsonValueKind.String) 
                return value.GetString() ?? "";
            if (value.ValueKind == JsonValueKind.Array)
            {
                var items = new List<string>();
                foreach (var element in value.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                        items.Add(element.GetString() ?? "");
                }
                return string.Join(", ", items);
            }
            return value.ToString() ?? "";
        }

        /// <summary>
        /// 获取作者字符串
        /// </summary>
        public string GetAuthorString() => GetStringValue(Author);

        /// <summary>
        /// 获取作者链接字符串
        /// </summary>
        public string GetAuthorLinkString() => GetStringValue(AuthorLink);

        /// <summary>
        /// 获取链接字符串
        /// </summary>
        public string GetLinkString() => GetStringValue(Link);
    }

    /// <summary>
    /// 配置项
    /// </summary>
    public class SchemaItem
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("bottomHelpMessage")]
        public string? BottomHelpMessage { get; set; }

        [JsonPropertyName("component")]
        public string Component { get; set; } = "";

        [JsonPropertyName("componentProps")]
        public ComponentProps? ComponentProps { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }
    }

    /// <summary>
    /// 组件属性
    /// </summary>
    public class ComponentProps
    {
        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }

        [JsonPropertyName("options")]
        public List<OptionItem>? Options { get; set; }

        [JsonPropertyName("min")]
        public decimal? Min { get; set; }

        [JsonPropertyName("max")]
        public decimal? Max { get; set; }
    }

    /// <summary>
    /// 选项项
    /// </summary>
    public class OptionItem
    {
        [JsonPropertyName("lable")]
        public string Label { get; set; } = "";

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    #endregion

    #region User Config Models

    /// <summary>
    /// 用户配置项
    /// </summary>
    public class UserConfigItem
    {
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }
    }

    #endregion

    #region Protocol Config Models

    /// <summary>
    /// 协议配置
    /// </summary>
    public class ProtocolConfig
    {
        [JsonPropertyName("stdin")]
        public Dictionary<string, ProtocolConfigItem>? Stdin { get; set; }

        [JsonPropertyName("onebotv11")]
        public Dictionary<string, ProtocolConfigItem>? Onebotv11 { get; set; }
    }

    /// <summary>
    /// 协议配置项
    /// </summary>
    public class ProtocolConfigItem
    {
        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    #endregion

    #region File System Models

    /// <summary>
    /// 文件系统目录信息
    /// </summary>
    public class FsDirectoryInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("children")]
        public List<FsChildInfo>? Children { get; set; }
    }

    /// <summary>
    /// 文件系统子项信息
    /// </summary>
    public class FsChildInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("mtime")]
        public string? Mtime { get; set; }
    }

    #endregion
}
