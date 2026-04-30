using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MicroPanelAvalonia.Models;

namespace MicroPanelAvalonia.Services
{
    public class ServerManager
    {
        private readonly string _configPath;
        private List<ServerInfo> _servers = new();
        private readonly ApiService _apiService;

        public IReadOnlyList<ServerInfo> Servers => _servers;

        public event EventHandler<ServerInfo>? ServerAdded;
        public event EventHandler<ServerInfo>? ServerRemoved;
        public event EventHandler? ServersChanged;

        public ServerManager(ApiService apiService)
        {
            _apiService = apiService;
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MicroPanel",
                "servers.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            LoadServers();
        }

        public void LoadServers()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var servers = JsonSerializer.Deserialize<List<ServerInfo>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (servers != null)
                    {
                        _servers = servers;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载服务器配置失败: {ex.Message}");
            }
        }

        public void SaveServers()
        {
            try
            {
                var json = JsonSerializer.Serialize(_servers, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存服务器配置失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> AddServerAsync(string serverAddress, string username, string password)
        {
            try
            {
                var normalizedAddress = serverAddress.Trim().TrimEnd('/');
                if (!normalizedAddress.StartsWith("http://") && !normalizedAddress.StartsWith("https://"))
                {
                    normalizedAddress = "http://" + normalizedAddress;
                }

                var existingServer = _servers.FirstOrDefault(s =>
                    s.ServerAddress.Equals(normalizedAddress, StringComparison.OrdinalIgnoreCase));

                if (existingServer != null)
                {
                    var existingUser = existingServer.Users.FirstOrDefault(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (existingUser != null)
                    {
                        return (false, "该服务器已存在此用户");
                    }

                    _apiService.SetBaseUrl(normalizedAddress);
                    var response = await _apiService.LoginAsync(username, password);

                    if (response?.IsSuccess != true)
                    {
                        return (false, response?.Data ?? "登录失败");
                    }

                    var newUser = new ServerUser
                    {
                        Username = username,
                        Password = password,
                        Token = response.Data,
                        TokenExpiry = DateTime.Now.AddHours(24)
                    };

                    existingServer.Users.Add(newUser);
                    SaveServers();
                    ServersChanged?.Invoke(this, EventArgs.Empty);

                    return (true, "用户添加成功");
                }
                else
                {
                    _apiService.SetBaseUrl(normalizedAddress);
                    var response = await _apiService.LoginAsync(username, password);

                    if (response?.IsSuccess != true)
                    {
                        return (false, response?.Data ?? "登录失败");
                    }

                    var server = new ServerInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        ServerAddress = normalizedAddress,
                        ServerName = new Uri(normalizedAddress).Host,
                        Users = new List<ServerUser>
                        {
                            new()
                            {
                                Username = username,
                                Password = password,
                                Token = response.Data,
                                TokenExpiry = DateTime.Now.AddHours(24)
                            }
                        }
                    };

                    _servers.Add(server);
                    SaveServers();
                    ServerAdded?.Invoke(this, server);
                    ServersChanged?.Invoke(this, EventArgs.Empty);

                    _ = RefreshServerStatusAsync(server);

                    return (true, "服务器添加成功");
                }
            }
            catch (Exception ex)
            {
                return (false, $"添加服务器失败: {ex.Message}");
            }
        }

        public void RemoveServer(ServerInfo server)
        {
            _servers.Remove(server);
            SaveServers();
            ServerRemoved?.Invoke(this, server);
            ServersChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task RefreshServerStatusAsync(ServerInfo server)
        {
            try
            {
                _apiService.SetBaseUrl(server.ServerAddress);

                var user = server.Users.FirstOrDefault();
                if (user?.Token == null)
                {
                    server.IsOnline = false;
                    return;
                }

                if (user.TokenExpiry.HasValue && user.TokenExpiry.Value < DateTime.Now)
                {
                    var loginResponse = await _apiService.LoginAsync(user.Username, user.Password);
                    if (loginResponse?.IsSuccess != true)
                    {
                        server.IsOnline = false;
                        return;
                    }
                    user.Token = loginResponse.Data;
                    user.TokenExpiry = DateTime.Now.AddHours(24);
                    SaveServers();
                }

                var statusResponse = await _apiService.GetSystemStatusAsync(user.Token);

                if (statusResponse?.IsSuccess == true && statusResponse.Data != null)
                {
                    Console.WriteLine($"[ServerManager] Updating status for {server.ServerAddress}");

                    // 提取并格式化 CPU 信息
                    var cpuInfo = statusResponse.Data.CpuInfo;
                    var cpuDisplay = cpuInfo?.Info != null && cpuInfo.Info.Count > 0
                        ? string.Join(" | ", cpuInfo.Info)
                        : $"{cpuInfo?.Inner * 100:F0}%";

                    // 提取并格式化内存信息
                    var ramInfo = statusResponse.Data.RamInfo;
                    var ramDisplay = ramInfo?.Info != null && ramInfo.Info.Count > 0
                        ? string.Join(" | ", ramInfo.Info)
                        : ramInfo?.Inner ?? "--";

                    // 提取并格式化磁盘信息（取第一个磁盘）
                    var diskInfo = statusResponse.Data.DiskSizeInfo?.FirstOrDefault();
                    var diskDisplay = diskInfo != null
                        ? $"{diskInfo.Mount}: {diskInfo.Used} / {diskInfo.Size} ({diskInfo.Use}%)"
                        : "--";

                    Console.WriteLine($"[ServerManager] CpuDisplay: {cpuDisplay}");
                    Console.WriteLine($"[ServerManager] RamDisplay: {ramDisplay}");
                    Console.WriteLine($"[ServerManager] DiskDisplay: {diskDisplay}");

                    // 使用 Dispatcher 在 UI 线程更新
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        server.Status = new SystemStatus
                        {
                            CpuInfo = cpuDisplay,
                            RamInfo = ramDisplay,
                            DiskSizeInfo = diskDisplay
                        };
                        server.IsOnline = true;
                        server.LastUpdate = DateTime.Now;
                    });
                }
                else
                {
                    Console.WriteLine($"[ServerManager] Failed to get status for {server.ServerAddress}: Code={statusResponse?.Code}, Message={statusResponse?.Message}");
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        server.IsOnline = false;
                    });
                }
            }
            catch
            {
                server.IsOnline = false;
            }
        }

        public async Task RefreshAllServersAsync()
        {
            foreach (var server in _servers)
            {
                await RefreshServerStatusAsync(server);
            }
        }

        public bool UpdateUser(ServerInfo server, ServerUser user, string newPassword)
        {
            try
            {
                user.Password = newPassword;
                SaveServers();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerManager] Failed to update user: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool success, string message)> AddUserToServerAsync(ServerInfo server, string username, string password)
        {
            try
            {
                // 检查用户是否已存在
                if (server.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "该用户已存在");
                }

                // 验证登录信息
                _apiService.SetBaseUrl(server.ServerAddress);
                var response = await _apiService.LoginAsync(username, password);

                if (response?.IsSuccess != true)
                {
                    return (false, response?.Data ?? "登录验证失败，请检查账号密码");
                }

                // 添加用户
                var newUser = new ServerUser
                {
                    Username = username,
                    Password = password,
                    Token = response.Data,
                    TokenExpiry = DateTime.Now.AddHours(24)
                };

                server.Users.Add(newUser);
                SaveServers();
                ServersChanged?.Invoke(this, EventArgs.Empty);

                return (true, "用户添加成功");
            }
            catch (Exception ex)
            {
                return (false, $"添加用户失败: {ex.Message}");
            }
        }

        public bool RemoveUser(ServerInfo server, ServerUser user)
        {
            try
            {
                if (server.Users.Count <= 1)
                {
                    return false; // 至少保留一个用户
                }

                server.Users.Remove(user);
                SaveServers();
                ServersChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerManager] Failed to remove user: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool success, string? token)> LoginWithUserAsync(ServerInfo server, ServerUser user)
        {
            try
            {
                _apiService.SetBaseUrl(server.ServerAddress);

                if (user.Token != null && user.TokenExpiry.HasValue && user.TokenExpiry.Value > DateTime.Now)
                {
                    return (true, user.Token);
                }

                var response = await _apiService.LoginAsync(user.Username, user.Password);

                if (response?.IsSuccess == true)
                {
                    user.Token = response.Data;
                    user.TokenExpiry = DateTime.Now.AddHours(24);
                    SaveServers();
                    return (true, response.Data);
                }

                return (false, response?.Data ?? "登录失败");
            }
            catch (Exception ex)
            {
                return (false, $"登录失败: {ex.Message}");
            }
        }
    }
}
