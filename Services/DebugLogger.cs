using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MicroPanel.Services
{
    /// <summary>
    /// 调试日志重定向器 - 将 Debug 输出重定向到控制台
    /// </summary>
    public class DebugLogger : TextWriter
    {
        private static DebugLogger? _instance;
        private readonly TextWriter? _originalOut;
        private readonly TextWriter? _originalError;

        public static DebugLogger Instance => _instance ??= new DebugLogger();

        public override Encoding Encoding => Encoding.UTF8;

        private DebugLogger()
        {
            _originalOut = Console.Out;
            _originalError = Console.Error;
        }

        /// <summary>
        /// 启用日志重定向
        /// </summary>
        public void Enable()
        {
            // 重定向标准输出和错误输出到控制台
            Console.SetOut(this);
            Console.SetError(this);

            // 添加 Trace 监听器
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;

            // 输出启动信息
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] === 日志重定向已启用 ===");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] 所有应用程序日志将显示在此控制台中");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] =====================================");
        }

        /// <summary>
        /// 禁用日志重定向
        /// </summary>
        public void Disable()
        {
            if (_originalOut != null)
                Console.SetOut(_originalOut);
            if (_originalError != null)
                Console.SetError(_originalError);
        }

        public override void Write(string? value)
        {
            _originalOut?.Write(value);
        }

        public override void WriteLine(string? value)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {value}";
            
            _originalOut?.WriteLine(logLine);
        }

        public override void Write(char value)
        {
            _originalOut?.Write(value);
        }

        public override void Flush()
        {
            _originalOut?.Flush();
            base.Flush();
        }
    }

    /// <summary>
    /// 控制台跟踪监听器
    /// </summary>
    public class ConsoleTraceListener : TraceListener
    {
        public override void Write(string? message)
        {
            if (DebugModeService.IsDebugMode)
            {
                Console.Write(message);
            }
        }

        public override void WriteLine(string? message)
        {
            if (DebugModeService.IsDebugMode)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Console.WriteLine($"[{timestamp}] [TRACE] {message}");
            }
        }
    }
}
