using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MicroPanel.Models;

namespace MicroPanel.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl = string.Empty;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public string GetBaseUrl() => _baseUrl;

        public async Task<ApiResponse<string>?> LoginAsync(string username, string password)
        {
            try
            {
                var request = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/login", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<UserInfoResponse>?> GetUserInfoAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/user/info");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<UserInfoResponse>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserInfoResponse>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<SystemStatusResponse>?> GetSystemStatusAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/bot/status");
                var responseJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[API Debug] GetSystemStatus response: {responseJson}");

                try
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<SystemStatusResponse>>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });

                    if (result?.Data != null)
                    {
                        Console.WriteLine($"[API Debug] Parsed successfully - CpuInfo: {result.Data.CpuInfo}, RamInfo: {result.Data.RamInfo}, DiskSizeInfo: {result.Data.DiskSizeInfo}");
                    }
                    else
                    {
                        Console.WriteLine($"[API Debug] Parsed but Data is null. Code: {result?.Code}, Message: {result?.Message}");
                    }

                    return result;
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"[API Debug] JSON Parse Error: {jsonEx.Message}");
                    Console.WriteLine($"[API Debug] JSON Content: {responseJson}");
                    return new ApiResponse<SystemStatusResponse>
                    {
                        Code = 500,
                        Message = $"JSON解析失败: {jsonEx.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Debug] GetSystemStatus error: {ex.Message}");
                return new ApiResponse<SystemStatusResponse>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        public async Task<bool> TestConnectionAsync(string serverAddress)
        {
            try
            {
                var testClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                var response = await testClient.GetAsync($"{serverAddress.TrimEnd('/')}/api/bot/info");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #region Redis Database API

        /// <summary>
        /// 获取所有Redis键
        /// </summary>
        public async Task<ApiResponse<List<RedisKeyNode>>?> GetRedisKeysAsync(string token, string sep = ":")
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/database/redis/allkeys?sep={sep}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<List<RedisKeyNode>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RedisKeyNode>>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取某个键值
        /// </summary>
        public async Task<ApiResponse<string>?> GetRedisKeyAsync(string token, string key)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/database/redis/getkey?key={Uri.EscapeDataString(key)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 设置键值
        /// </summary>
        public async Task<ApiResponse<string>?> SetRedisKeyAsync(string token, string key, string value)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var request = new { key, value };
                var json = JsonSerializer.Serialize(request);
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
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除键
        /// </summary>
        public async Task<ApiResponse<string>?> DelRedisKeyAsync(string token, string key)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/database/redis/delkey?key={Uri.EscapeDataString(key)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量删除键
        /// </summary>
        public async Task<ApiResponse<string>?> DelRedisKeysAsync(string token, string key)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/database/redis/delkeys?key={Uri.EscapeDataString(key)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        #endregion

        #region Bot Config API

        /// <summary>
        /// 获取Bot配置
        /// </summary>
        public async Task<ApiResponse<Dictionary<string, ConfigItem>>?> GetBotConfigAsync(string token, string name)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/bot/getcfg?name={Uri.EscapeDataString(name)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<Dictionary<string, ConfigItem>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<Dictionary<string, ConfigItem>>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 设置Bot配置
        /// </summary>
        public async Task<ApiResponse<string>?> SetBotConfigAsync(string token, string name, Dictionary<string, object> data)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/bot/setcfg?name={Uri.EscapeDataString(name)}", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        #endregion

        #region Plugin Config API

        /// <summary>
        /// 获取插件信息列表
        /// </summary>
        public async Task<ApiResponse<List<PluginInfo>>?> GetPluginInfoAsync(string token, string source = "guoba")
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/plugins/getinfo?source={source}");
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[Plugin API] GetPluginInfo response: {responseJson}");

                var result = JsonSerializer.Deserialize<ApiResponse<List<PluginInfo>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
                
                System.Diagnostics.Debug.WriteLine($"[Plugin API] Deserialized: Code={result?.Code}, DataCount={result?.Data?.Count}");
                if (result?.Data != null)
                {
                    foreach (var plugin in result.Data)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Plugin API] Plugin: {plugin.Name}, Title={plugin.Title}");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Plugin API] GetPluginInfo error: {ex}");
                return new ApiResponse<List<PluginInfo>>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取插件配置
        /// </summary>
        public async Task<ApiResponse<List<SchemaItem>>?> GetPluginConfigAsync(string token, string pluginName, string source = "guoba")
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/plugins/getcfg?source={source}&pluginName={Uri.EscapeDataString(pluginName)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<List<SchemaItem>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<SchemaItem>>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 设置插件配置
        /// </summary>
        public async Task<ApiResponse<object>?> SetPluginConfigAsync(string token, string pluginName, string source, List<SchemaItem> config)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                // 直接发送 SchemaItem 数组，保持与 micro-plugin 后端一致的格式
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"[Plugin API] SetPluginConfig request: {json}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/plugins/setcfg?source={source}&pluginName={pluginName}", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[Plugin API] SetPluginConfig response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Plugin API] SetPluginConfig error: {ex}");
                return new ApiResponse<object>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        #endregion

        #region User Config API

        /// <summary>
        /// 获取用户配置
        /// </summary>
        public async Task<ApiResponse<List<Dictionary<string, UserConfigItem>>>?> GetUserConfigAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/user/getcfg");
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[User API] GetUserConfig response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<List<Dictionary<string, UserConfigItem>>>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[User API] GetUserConfig error: {ex}");
                return new ApiResponse<List<Dictionary<string, UserConfigItem>>>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 设置用户配置
        /// </summary>
        public async Task<ApiResponse<string>?> SetUserConfigAsync(string token, List<Dictionary<string, UserConfigItem>> config)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var json = JsonSerializer.Serialize(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/user/setcfg", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[User API] SetUserConfig response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[User API] SetUserConfig error: {ex}");
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        #endregion

        #region Protocol Config API

        /// <summary>
        /// 获取协议配置
        /// </summary>
        public async Task<ApiResponse<ProtocolConfig>?> GetProtocolConfigAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/protocol/getcfg");
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[Protocol API] GetProtocolConfig response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<ProtocolConfig>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Protocol API] GetProtocolConfig error: {ex}");
                return new ApiResponse<ProtocolConfig>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 设置协议配置
        /// </summary>
        public async Task<ApiResponse<string>?> SetProtocolConfigAsync(string token, ProtocolConfig config)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var json = JsonSerializer.Serialize(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/protocol/setcfg", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[Protocol API] SetProtocolConfig response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Protocol API] SetProtocolConfig error: {ex}");
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        #endregion

        #region File System API

        /// <summary>
        /// 获取目录列表
        /// </summary>
        public async Task<ApiResponse<FsDirectoryInfo>?> GetDirectoryListAsync(string token, string path)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/fs/listdir?path={Uri.EscapeDataString(path)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[FS API] GetDirectoryList response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<FsDirectoryInfo>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FS API] GetDirectoryList error: {ex}");
                return new ApiResponse<FsDirectoryInfo>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        public async Task<ApiResponse<string>?> DeleteDirectoryAsync(string token, string path)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/fs/rmdir?path={Uri.EscapeDataString(path)}");
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[FS API] DeleteDirectory response: {responseJson}");

                return JsonSerializer.Deserialize<ApiResponse<string>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FS API] DeleteDirectory error: {ex}");
                return new ApiResponse<string>
                {
                    Code = 500,
                    Message = $"请求失败: {ex.Message}"
                };
            }
        }

        #endregion
    }
}
