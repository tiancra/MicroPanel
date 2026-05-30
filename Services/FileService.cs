using MicroPanel.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanel.Services
{
    /// <summary>
    /// 文件管理服务
    /// </summary>
    public class FileService
    {
        private readonly HttpClient _httpClient;

        public FileService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 获取会话Token
        /// </summary>
        private string? GetToken()
        {
            return SessionService.Instance.Token;
        }

        /// <summary>
        /// 获取服务器地址
        /// </summary>
        private string GetServerAddress()
        {
            return SessionService.Instance.CurrentServer?.ServerAddress ?? "";
        }

        /// <summary>
        /// 创建带认证的请求
        /// </summary>
        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return request;
        }

        /// <summary>
        /// 列出目录内容
        /// </summary>
        public async Task<DirectoryListResponse?> ListDirectoryAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/listdir/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DirectoryListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 列出目录失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        public async Task<FileContentResponse?> ReadFileAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/open/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileContentResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 读取文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存文件内容
        /// </summary>
        public async Task<FileOperationResponse?> SaveFileAsync(string path, string content)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/savefile/?path={Uri.EscapeDataString(path)}";

                var jsonContent = JsonSerializer.Serialize(new { content });
                var request = CreateRequest(HttpMethod.Post, url);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 保存文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        public async Task<FileOperationResponse?> CreateFileAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/create/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 创建文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        public async Task<FileOperationResponse?> CreateDirectoryAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/mkdir/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 创建目录失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public async Task<FileOperationResponse?> DeleteFileAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/rmfile/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Delete, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 删除文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        public async Task<FileOperationResponse?> DeleteDirectoryAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/rmdir/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Delete, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 删除目录失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        public async Task<FileOperationResponse?> RenameFileAsync(string path, string newPath)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/renamefile/?path={Uri.EscapeDataString(path)}&newPath={Uri.EscapeDataString(newPath)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 重命名文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 重命名目录
        /// </summary>
        public async Task<FileOperationResponse?> RenameDirectoryAsync(string path, string newPath)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/renamedir/?path={Uri.EscapeDataString(path)}&newPath={Uri.EscapeDataString(newPath)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 重命名目录失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        public async Task<FileOperationResponse?> CopyFileAsync(string path, string newPath)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/copyfile/?path={Uri.EscapeDataString(path)}&newPath={Uri.EscapeDataString(newPath)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 复制文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 复制目录
        /// </summary>
        public async Task<FileOperationResponse?> CopyDirectoryAsync(string path, string newPath)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/copydir/?path={Uri.EscapeDataString(path)}&newPath={Uri.EscapeDataString(newPath)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 复制目录失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        public async Task<FileOperationResponse?> MoveFileAsync(string path, string newPath)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/movefile/?path={Uri.EscapeDataString(path)}&newPath={Uri.EscapeDataString(newPath)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 移动文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        public async Task<FileOperationResponse?> MoveDirectoryAsync(string path, string newPath)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/movedir/?path={Uri.EscapeDataString(path)}&newPath={Uri.EscapeDataString(newPath)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileOperationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 移动目录失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 搜索文件
        /// </summary>
        public async Task<DirectoryListResponse?> SearchFilesAsync(string path, string keyword)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/search/?path={Uri.EscapeDataString(path)}&keyWord={Uri.EscapeDataString(keyword)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DirectoryListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 搜索文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        public async Task<FileSizeResponse?> GetFileSizeAsync(string path, string type)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/filesize/?path={Uri.EscapeDataString(path)}&type={type}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileSizeResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 获取文件大小失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        public async Task<Stream?> DownloadFileAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/download/?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 下载文件失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 读取媒体文件
        /// </summary>
        public async Task<Stream?> ReadMediaFileAsync(string path)
        {
            try
            {
                var serverAddress = GetServerAddress();
                var url = $"{serverAddress}/api/fs/media?path={Uri.EscapeDataString(path)}";

                var request = CreateRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileService: 读取媒体文件失败 - {ex.Message}");
                return null;
            }
        }
    }
}
