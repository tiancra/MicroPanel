using MicroPanelAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Services
{
    public class AuthenticatedApiService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl = string.Empty;

        public AuthenticatedApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/') + "/api";
        }

        private void SetToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Remove("token");
            _httpClient.DefaultRequestHeaders.Add("token", token);
        }

        // 1. 获取Bot信息
        public async Task<ApiResponse<List<BotInfo>>?> GetBotInfoAsync(string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/bot/info");
                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"GetBotInfoAsync: 响应JSON: {responseJson}");
                return JsonSerializer.Deserialize<ApiResponse<List<BotInfo>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBotInfoAsync: 异常 - {ex.Message}");
                return new ApiResponse<List<BotInfo>> { Code = 500, Message = ex.Message };
            }
        }

        // 2. 获取系统状态
        public async Task<ApiResponse<SystemStatusData>?> GetSystemStatusAsync(string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/bot/status");
                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"GetSystemStatusAsync: 响应JSON: {responseJson}");
                return JsonSerializer.Deserialize<ApiResponse<SystemStatusData>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSystemStatusAsync: 异常 - {ex.Message}");
                return new ApiResponse<SystemStatusData> { Code = 500, Message = ex.Message };
            }
        }

        // 3. 获取日志
        public async Task<ApiResponse<LogData>?> GetLogsAsync(string token, int page = 0, int pageSize = 50)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/bot/logs?page={page}&size={pageSize}");
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<LogData>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<LogData> { Code = 500, Message = ex.Message };
            }
        }

        // 4. 获取插件列表
        public async Task<ApiResponse<List<PluginListItem>>?> GetPluginsAsync(string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/plugins/get");
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<List<PluginListItem>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PluginListItem>> { Code = 500, Message = ex.Message };
            }
        }

        // 5. 获取文件列表
        public async Task<ApiResponse<FileListData>?> GetFileListAsync(string token, string path)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/fs/listdir?path={Uri.EscapeDataString(path)}");
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<FileListData>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<FileListData> { Code = 500, Message = ex.Message };
            }
        }

        // 6. 获取配置
        public async Task<ApiResponse<ConfigData>?> GetConfigAsync(string token, string name)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/bot/getcfg?name={name}");
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<ConfigData>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfigData> { Code = 500, Message = ex.Message };
            }
        }

        // 7. 保存配置
        public async Task<ApiResponse<string>?> SaveConfigAsync(string token, string name, object data)
        {
            try
            {
                SetToken(token);
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/bot/setcfg?name={name}", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Code = 500, Message = ex.Message };
            }
        }

        // 8. Redis 获取所有键
        public async Task<ApiResponse<List<string>>?> GetRedisKeysAsync(string token, string? sep = null)
        {
            try
            {
                SetToken(token);
                var url = $"{_baseUrl}/api/database/redis/allkeys";
                if (!string.IsNullOrEmpty(sep))
                    url += $"?sep={Uri.EscapeDataString(sep)}";
                var response = await _httpClient.GetAsync(url);
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<List<string>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<string>> { Code = 500, Message = ex.Message };
            }
        }

        // 9. Redis 获取键值
        public async Task<ApiResponse<string>?> GetRedisKeyAsync(string token, string key)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/database/redis/getkey?key={Uri.EscapeDataString(key)}");
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Code = 500, Message = ex.Message };
            }
        }

        // 10. Redis 设置键值
        public async Task<ApiResponse<string>?> SetRedisKeyAsync(string token, string key, string value)
        {
            try
            {
                SetToken(token);
                var json = JsonSerializer.Serialize(new { key, value });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/database/redis/setkey", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Code = 500, Message = ex.Message };
            }
        }

        // 11. Redis 删除键
        public async Task<ApiResponse<string>?> DeleteRedisKeyAsync(string token, string key)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/database/redis/delkey?key={Uri.EscapeDataString(key)}");
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Code = 500, Message = ex.Message };
            }
        }
    }

    // 数据模型
    public class BotInfo
    {
        public string? Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public BotContacts? CountContacts { get; set; }
        public BotMessageCount? MessageCount { get; set; }
        public string? BotVersion { get; set; }
        public string? Platform { get; set; }
        public string? BotRunTime { get; set; }
    }

    public class BotContacts
    {
        public int Friend { get; set; }
        public int Group { get; set; }
        public int GroupMember { get; set; }
    }

    public class BotMessageCount
    {
        public string? Recv { get; set; }
        public string? Sent { get; set; }
        public int Screenshot { get; set; }
    }

    public class LogData
    {
        public List<string>? Logs { get; set; }
        public int Total { get; set; }
    }

    public class PluginListItem
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public bool Enabled { get; set; }
    }

    public class FileListData
    {
        public List<FileItem>? Files { get; set; }
        public List<DirectoryItem>? Directories { get; set; }
    }

    public class FileItem
    {
        public string? Name { get; set; }
        public long Size { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    public class DirectoryItem
    {
        public string? Name { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    public class ConfigData
    {
        public object? Config { get; set; }
    }

    // 系统状态数据模型
    public class SystemStatusData
    {
        public CpuInfo? CpuInfo { get; set; }
        public object? GpuInfo { get; set; }  // 可能是 false 或 GpuInfo 对象
        public SwapInfo? SwapInfo { get; set; }
        public RamInfo? RamInfo { get; set; }
        public List<DiskInfo>? DiskSizeInfo { get; set; }
        public NodeInfo? NodeInfo { get; set; }
        public List<NetworkInfo>? NetworkInfo { get; set; }
        public List<OtherInfoItem>? OtherInfo { get; set; }
    }

    public class CpuInfo
    {
        public double Inner { get; set; }
        public string[]? Info { get; set; }
    }

    public class GpuInfo
    {
        public double Inner { get; set; }
        public string[]? Info { get; set; }
    }

    public class SwapInfo
    {
        public string? Inner { get; set; }
        public string? Title { get; set; }
        public string[]? Info { get; set; }
    }

    public class RamInfo
    {
        public string? Inner { get; set; }
        public string? Title { get; set; }
        public string[]? Info { get; set; }
    }

    public class DiskInfo
    {
        public string? Fs { get; set; }
        public string? Type { get; set; }
        public string? Size { get; set; }
        public string? Used { get; set; }
        public long Available { get; set; }
        public int Use { get; set; }
        public string? Mount { get; set; }
        public bool Rw { get; set; }
        public double Percentage { get; set; }
    }

    public class NodeInfo
    {
        public double Inner { get; set; }
        public string? Title { get; set; }
        public NodeInfoDetail? Info { get; set; }
    }

    public class NodeInfoDetail
    {
        public string? Rss { get; set; }
        public string? HeapTotal { get; set; }
        public string? HeapUsed { get; set; }
        public double Occupy { get; set; }
    }

    public class NetworkInfo
    {
        public string? Iface { get; set; }
        public string? RxBytes { get; set; }
        public string? TxBytes { get; set; }
    }

    public class OtherInfoItem
    {
        public string? First { get; set; }
        public object? Tail { get; set; }
    }
}
