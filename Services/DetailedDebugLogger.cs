using System;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;

namespace MicroPanel.Services
{
    /// <summary>
    /// 详细的调试日志记录器
    /// 在调试模式下记录操作、请求、响应等信息
    /// </summary>
    public static class AppDebugLogger
    {
        private static readonly object _lockObj = new object();
        
        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public static bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// 记录方法调用
        /// </summary>
        public static void LogMethod(string className, string methodName, string? message = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[METHOD] {className}.{methodName}";
            if (!string.IsNullOrEmpty(message))
            {
                logMessage += $" - {message}";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录用户操作
        /// </summary>
        public static void LogUserAction(string action, string? details = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[USER ACTION] {action}";
            if (!string.IsNullOrEmpty(details))
            {
                logMessage += $" - {details}";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录路由切换
        /// </summary>
        public static void LogNavigation(string from, string to, string? details = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[NAVIGATION] {from} -> {to}";
            if (!string.IsNullOrEmpty(details))
            {
                logMessage += $" ({details})";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录 API 请求
        /// </summary>
        public static void LogApiRequest(string method, string endpoint, object? requestBody = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[API REQUEST] {method} {endpoint}";
            
            if (requestBody != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // 截断过长的请求体
                    if (json.Length > 500)
                    {
                        json = json.Substring(0, 500) + "... [TRUNCATED]";
                    }
                    
                    logMessage += $"\n    Body: {json}";
                }
                catch (Exception ex)
                {
                    logMessage += $"\n    Body: [Serialization failed: {ex.Message}]";
                }
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录 API 响应
        /// </summary>
        public static void LogApiResponse(string endpoint, int statusCode, object? responseBody = null, long elapsedMs = 0)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[API RESPONSE] {endpoint} - Status: {statusCode}";
            
            if (elapsedMs > 0)
            {
                logMessage += $" ({elapsedMs}ms)";
            }
            
            if (responseBody != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // 截断过长的响应体
                    if (json.Length > 1000)
                    {
                        json = json.Substring(0, 1000) + "... [TRUNCATED]";
                    }
                    
                    logMessage += $"\n    Body: {json}";
                }
                catch (Exception ex)
                {
                    logMessage += $"\n    Body: [Serialization failed: {ex.Message}]";
                }
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录组件交互
        /// </summary>
        public static void LogComponentInteraction(string componentName, string interactionType, string? additionalInfo = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[COMPONENT] {componentName} - {interactionType}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                logMessage += $" - {additionalInfo}";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录事件触发
        /// </summary>
        public static void LogEvent(string eventName, string? details = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[EVENT] {eventName}";
            if (!string.IsNullOrEmpty(details))
            {
                logMessage += $" - {details}";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录状态变更
        /// </summary>
        public static void LogStateChange(string stateName, object? oldValue, object? newValue)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[STATE] {stateName}";
            
            try
            {
                var oldJson = oldValue != null ? JsonSerializer.Serialize(oldValue) : "null";
                var newJson = newValue != null ? JsonSerializer.Serialize(newValue) : "null";
                
                if (oldJson.Length > 100) oldJson = oldJson.Substring(0, 100) + "...";
                if (newJson.Length > 100) newJson = newJson.Substring(0, 100) + "...";
                
                logMessage += $"\n    From: {oldJson}\n    To: {newJson}";
            }
            catch
            {
                logMessage += $"\n    From: {oldValue}\n    To: {newValue}";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        public static void LogException(string context, Exception ex)
        {
            var logMessage = $"[EXCEPTION] {context}\n    Type: {ex.GetType().Name}\n    Message: {ex.Message}\n    StackTrace: {ex.StackTrace}";
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录登录信息（特别详细）
        /// </summary>
        public static void LogLogin(string serverAddress, string username, object? requestBody = null, object? responseBody = null, bool success = false)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[LOGIN] Server: {serverAddress}, User: {username}, Success: {success}";
            
            if (requestBody != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // 隐藏密码
                    json = System.Text.RegularExpressions.Regex.Replace(json, @"""password""\s*:\s*""[^""]*""", "\"password\": \"***\"");
                    
                    logMessage += $"\n    Request: {json}";
                }
                catch { }
            }
            
            if (responseBody != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (json.Length > 500)
                    {
                        json = json.Substring(0, 500) + "...";
                    }
                    
                    logMessage += $"\n    Response: {json}";
                }
                catch { }
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 记录配置变更
        /// </summary>
        public static void LogConfigChange(string configType, string itemName, object? oldValue, object? newValue)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var logMessage = $"[CONFIG] {configType} - {itemName}";
            
            try
            {
                var oldJson = oldValue != null ? JsonSerializer.Serialize(oldValue) : "null";
                var newJson = newValue != null ? JsonSerializer.Serialize(newValue) : "null";
                
                logMessage += $"\n    Changed: {oldJson} -> {newJson}";
            }
            catch
            {
                logMessage += $"\n    Changed: {oldValue} -> {newValue}";
            }
            
            WriteLog(logMessage);
        }

        /// <summary>
        /// 分隔日志输出（用于区分不同的操作阶段）
        /// </summary>
        public static void LogSeparator(string? title = null)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            var separator = title != null ? $"========== {title} ==========" : "==========================================";
            WriteLog(separator);
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        private static void WriteLog(string message)
        {
            if (!DebugModeService.IsDebugMode && !EnableDetailedLogging) return;
            
            lock (_lockObj)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] {message}";
                
                // 同时输出到 Debug 和 Console
                Debug.WriteLine(logLine);
                Console.WriteLine(logLine);
            }
        }
    }
}
