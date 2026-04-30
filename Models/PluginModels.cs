using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MicroPanelAvalonia.Models
{
    /// <summary>
    /// 插件类型
    /// </summary>
    public class PluginType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("event")]
        public string Event { get; set; } = "message";

        [JsonPropertyName("reg")]
        public string Reg { get; set; } = "";

        [JsonPropertyName("cron")]
        public string Cron { get; set; } = "";

        [JsonPropertyName("delayTime")]
        public int DelayTime { get; set; } = 0;

        [JsonPropertyName("flag")]
        public string Flag { get; set; } = "";

        [JsonPropertyName("isGlobal")]
        public bool IsGlobal { get; set; } = true;

        [JsonPropertyName("isAt")]
        public bool IsAt { get; set; } = false;

        [JsonPropertyName("isQuote")]
        public bool IsQuote { get; set; } = false;

        [JsonPropertyName("groups")]
        public List<string> Groups { get; set; } = new();

        [JsonPropertyName("friends")]
        public List<string> Friends { get; set; } = new();

        [JsonPropertyName("message")]
        public List<MessageType> Message { get; set; } = new();
    }

    /// <summary>
    /// 消息类型
    /// </summary>
    public class MessageType
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("json")]
        public string? Json { get; set; }

        [JsonPropertyName("content")]
        public object? Content { get; set; }
    }

    /// <summary>
    /// 插件列表响应
    /// </summary>
    public class PluginListResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public List<PluginType> Data { get; set; } = new();
    }

    /// <summary>
    /// 插件元素响应
    /// </summary>
    public class PluginElementResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public PluginType? Data { get; set; }
    }

    /// <summary>
    /// 正则表达式标志选项
    /// </summary>
    public class RegexFlagOption
    {
        public string Description { get; set; } = "";
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// 消息段类型选项
    /// </summary>
    public class MessageSegmentOption
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public MessageType DefaultValue { get; set; } = new();
    }

    /// <summary>
    /// 按钮元素
    /// </summary>
    public class ButtonElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("render_data")]
        public ButtonRenderData RenderData { get; set; } = new();

        [JsonPropertyName("action")]
        public ButtonAction Action { get; set; } = new();
    }

    public class ButtonRenderData
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("visited_label")]
        public string VisitedLabel { get; set; } = "";

        [JsonPropertyName("style")]
        public int Style { get; set; } = 0;
    }

    public class ButtonAction
    {
        [JsonPropertyName("type")]
        public int Type { get; set; } = 0;

        [JsonPropertyName("permission")]
        public ButtonPermission Permission { get; set; } = new();

        [JsonPropertyName("data")]
        public string Data { get; set; } = "";

        [JsonPropertyName("reply")]
        public bool Reply { get; set; } = false;

        [JsonPropertyName("enter")]
        public bool Enter { get; set; } = false;
    }

    public class ButtonPermission
    {
        [JsonPropertyName("type")]
        public int Type { get; set; } = 0;

        [JsonPropertyName("specify_user_ids")]
        public List<string> SpecifyUserIds { get; set; } = new();
    }

    /// <summary>
    /// 按钮内容
    /// </summary>
    public class ButtonContent
    {
        [JsonPropertyName("appid")]
        public string AppId { get; set; } = "";

        [JsonPropertyName("rows")]
        public List<ButtonRow> Rows { get; set; } = new();
    }

    public class ButtonRow
    {
        [JsonPropertyName("buttons")]
        public List<ButtonElement> Buttons { get; set; } = new();
    }
}
