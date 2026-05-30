using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanel.Services
{
    /// <summary>
    /// 插件管理服务
    /// </summary>
    public class PluginService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticatedApiService _apiService;

        public PluginService()
        {
            _httpClient = new HttpClient();
            _apiService = new AuthenticatedApiService();
        }

        /// <summary>
        /// 获取插件列表
        /// </summary>
        public async Task<PluginListResponse?> GetPluginsListAsync()
        {
            try
            {
                var session = SessionService.Instance;
                if (!session.IsLoggedIn || session.CurrentServer == null)
                {
                    return null;
                }

                var serverAddress = session.CurrentServer.ServerAddress;
                var url = $"{serverAddress}/api/plugins/get";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PluginService: 获取插件列表响应 - {json}");

                return JsonSerializer.Deserialize<PluginListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginService: 获取插件列表失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 添加插件
        /// </summary>
        public async Task<PluginListResponse?> AddPluginAsync(PluginType plugin)
        {
            try
            {
                var session = SessionService.Instance;
                if (!session.IsLoggedIn || session.CurrentServer == null)
                {
                    return null;
                }

                var serverAddress = session.CurrentServer.ServerAddress;
                var url = $"{serverAddress}/api/plugins/add";

                var json = JsonSerializer.Serialize(plugin);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PluginService: 添加插件响应 - {responseJson}");

                return JsonSerializer.Deserialize<PluginListResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginService: 添加插件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新插件
        /// </summary>
        public async Task<PluginListResponse?> UpdatePluginAsync(string index, PluginType plugin)
        {
            try
            {
                var session = SessionService.Instance;
                if (!session.IsLoggedIn || session.CurrentServer == null)
                {
                    return null;
                }

                var serverAddress = session.CurrentServer.ServerAddress;
                var url = $"{serverAddress}/api/plugins/put?index={index}";

                var json = JsonSerializer.Serialize(plugin);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PluginService: 更新插件响应 - {responseJson}");

                return JsonSerializer.Deserialize<PluginListResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginService: 更新插件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除插件
        /// </summary>
        public async Task<PluginListResponse?> DeletePluginAsync(int index)
        {
            try
            {
                var session = SessionService.Instance;
                if (!session.IsLoggedIn || session.CurrentServer == null)
                {
                    return null;
                }

                var serverAddress = session.CurrentServer.ServerAddress;
                var url = $"{serverAddress}/api/plugins/delete?index={index}";

                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PluginService: 删除插件响应 - {json}");

                return JsonSerializer.Deserialize<PluginListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginService: 删除插件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取图片JSON
        /// </summary>
        public async Task<string?> GetImageJsonAsync(string id, string hash)
        {
            try
            {
                var session = SessionService.Instance;
                if (!session.IsLoggedIn || session.CurrentServer == null)
                {
                    return null;
                }

                var serverAddress = session.CurrentServer.ServerAddress;
                var url = $"{serverAddress}/api/plugins/imgJSON?id={id}&hash={hash}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PluginService: 获取图片JSON响应 - {json}");

                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (result != null && result.TryGetValue("data", out var data))
                {
                    return data?.ToString();
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginService: 获取图片JSON失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取按钮JSON
        /// </summary>
        public async Task<PluginElementResponse?> GetButtonJsonAsync(PluginType plugin)
        {
            try
            {
                var session = SessionService.Instance;
                if (!session.IsLoggedIn || session.CurrentServer == null)
                {
                    return null;
                }

                var serverAddress = session.CurrentServer.ServerAddress;
                var url = $"{serverAddress}/api/plugins/btnJSON";

                var json = JsonSerializer.Serialize(plugin);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PluginService: 获取按钮JSON响应 - {responseJson}");

                return JsonSerializer.Deserialize<PluginElementResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginService: 获取按钮JSON失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取正则表达式标志选项
        /// </summary>
        public static List<RegexFlagOption> GetRegexFlagOptions()
        {
            return new List<RegexFlagOption>
            {
                new() { Description = "默认Flags", Value = "" },
                new() { Description = "(g)全局搜索", Value = "g" },
                new() { Description = "(i)不区分大小写", Value = "i" },
                new() { Description = "(m)多行搜索", Value = "m" },
                new() { Description = "(s)允许 . 匹配换行符", Value = "s" },
                new() { Description = "(u)使用 unicode 码的模式进行匹配", Value = "u" },
                new() { Description = "(y)匹配从目标字符串的当前位置开始", Value = "y" }
            };
        }

        /// <summary>
        /// 获取消息段类型选项
        /// </summary>
        public static List<MessageSegmentOption> GetMessageSegmentOptions()
        {
            return new List<MessageSegmentOption>
            {
                new() { Name = "文本", Type = "text", DefaultValue = new MessageType { Type = "text", Data = "" } },
                new() { Name = "图片", Type = "image", DefaultValue = new MessageType { Type = "image", Url = "", Data = "", Hash = "" } },
                new() { Name = "音频", Type = "record", DefaultValue = new MessageType { Type = "record", Url = "", Data = "" } },
                new() { Name = "视频", Type = "video", DefaultValue = new MessageType { Type = "video", Url = "", Data = "" } },
                new() { Name = "表情", Type = "face", DefaultValue = new MessageType { Type = "face", Data = 0 } },
                new() { Name = "戳一戳", Type = "poke", DefaultValue = new MessageType { Type = "poke", Data = 0 } },
                new() { Name = "骰子", Type = "dice", DefaultValue = new MessageType { Type = "dice", Data = 1 } },
                new() { Name = "猜拳", Type = "rps", DefaultValue = new MessageType { Type = "rps", Data = 1 } },
                new() { Name = "代码", Type = "code", DefaultValue = new MessageType { Type = "code", Data = "" } }
            };
        }
    }
}
