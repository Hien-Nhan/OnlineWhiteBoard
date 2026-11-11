using System.Text;

namespace WhiteboardServer
{
    /// <summary>Mức độ của dòng log. Dòng có mức thấp hơn mức cấu hình sẽ không được ghi vào file.</summary>
    public enum LogLevel
    {
        Debug = 0, // chi tiết: từng gói DRAW, từng lần broadcast (rất nhiều dòng)
        Info = 1,  // sự kiện chính: khởi động, client vào/ra phòng, xóa bảng, đồng bộ lịch sử
        Warn = 2,  // bất thường nhưng server vẫn chạy: client rớt mạng, gửi tin thất bại
        Error = 3  // lỗi nghiêm trọng: lỗi SQLite, server dừng
    }

    /// <summary>
    /// Ghi log của Server ra console và ra file "serverlog_yyyy-MM-dd.log" (mỗi ngày một file).
    /// - An toàn đa luồng (mỗi client chạy một thread riêng).
    /// - Console giữ nguyên hành vi cũ (hiện mọi dòng); file chỉ ghi từ mức LogLevel trở lên.
    /// - Lỗi khi ghi file không bao giờ làm server sập: logger tự tắt ghi file và báo ra console.
    /// </summary>
    public static class ServerLogger
    {
        private const string FilePrefix = "serverlog_";
        private const string FileExtension = ".log";
        private const int MaxLineLength = 2000; // cắt bớt dòng quá dài để file log không phình to

        private static readonly object _sync = new object();
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private static bool _fileEnabled = false; // chỉ bật sau khi gọi Configure()
        private static string _logDir = "logs";
        private static LogLevel _fileLevel = LogLevel.Info;

        private static StreamWriter? _writer;
        private static DateTime _writerDate;

        /// <summary>Đường dẫn đầy đủ tới thư mục chứa file log.</summary>
        public static string LogDirectoryFullPath
        {
            get { lock (_sync) { return Path.GetFullPath(_logDir); } }
        }

        /// <summary>Bật/tắt và cấu hình ghi file. Gọi một lần sau khi đã đọc config.ini.</summary>
        public static void Configure(bool enabled, string logDir, LogLevel level, int keepDays)
        {
            lock (_sync)
            {
                CloseWriter();
                _fileEnabled = enabled;
                _logDir = string.IsNullOrWhiteSpace(logDir) ? "logs" : logDir;
                _fileLevel = level;

                if (enabled && keepDays > 0)
                {
                    DeleteOldLogs(keepDays);
                }
            }
        }

        /// <summary>Đóng file log (gọi khi server dừng).</summary>
        public static void Shutdown()
        {
            lock (_sync)
            {
                CloseWriter();
            }
        }

        public static bool TryParseLevel(string text, out LogLevel level)
        {
            string t = (text ?? "").Trim();
            if (t.Equals("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                level = LogLevel.Warn;
                return true;
            }
            return Enum.TryParse(t, true, out level) && Enum.IsDefined(level);
        }

        public static void Debug(string message) => Write(LogLevel.Debug, message, null);
        public static void Info(string message) => Write(LogLevel.Info, message, null);
        public static void Warn(string message, Exception? ex = null) => Write(LogLevel.Warn, message, ex);
        public static void Error(string message, Exception? ex = null) => Write(LogLevel.Error, message, ex);

        private static void Write(LogLevel level, string message, Exception? ex)
        {
            string text = ex == null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}";

            lock (_sync)
            {
                WriteConsole(level, text);

                if (_fileEnabled && level >= _fileLevel)
                {
                    WriteFile(level, text, level == LogLevel.Error ? ex : null);
                }
            }
        }

        private static void WriteConsole(LogLevel level, string text)
        {
            ConsoleColor? color = level switch
            {
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => null
            };

            if (color.HasValue)
            {
                ConsoleColor old = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                Console.WriteLine(text);
                Console.ForegroundColor = old;
            }
            else
            {
                Console.WriteLine(text);
            }
        }

        private static void WriteFile(LogLevel level, string text, Exception? errorWithStack)
        {
            try
            {
                EnsureWriter();

                string tag = level switch
                {
                    LogLevel.Debug => "DEBUG",
                    LogLevel.Info => "INFO ",
                    LogLevel.Warn => "WARN ",
                    _ => "ERROR"
                };

                if (text.Length > MaxLineLength)
                {
                    text = text.Substring(0, MaxLineLength) + "...(đã cắt bớt)";
                }

                _writer!.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{tag}] {text}");

                if (errorWithStack?.StackTrace != null)
                {
                    _writer.WriteLine(errorWithStack.StackTrace);
                }
            }
            catch (Exception ioEx)
            {
                // Không để lỗi ghi log (hết ổ đĩa, thiếu quyền...) làm sập server.
                _fileEnabled = false;
                CloseWriter();
                Console.WriteLine($"[LOGGER] Không ghi được file log, đã tắt ghi file: {ioEx.Message}");
            }
        }

        // Mở file của ngày hôm nay; sang ngày mới thì tự đóng file cũ và mở file mới.
        private static void EnsureWriter()
        {
            DateTime today = DateTime.Today;
            if (_writer != null && _writerDate == today) return;

            CloseWriter();
            Directory.CreateDirectory(_logDir);

            string path = Path.Combine(_logDir, $"{FilePrefix}{today:yyyy-MM-dd}{FileExtension}");

            // FileShare.ReadWrite: cho phép mở file bằng Notepad / "Get-Content -Wait" trong lúc server đang ghi.
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(stream, Utf8NoBom) { AutoFlush = true };
            _writerDate = today;
        }

        private static void CloseWriter()
        {
            try { _writer?.Dispose(); } catch { /* bỏ qua */ }
            _writer = null;
        }

        // Xóa các file serverlog_*.log cũ hơn keepDays ngày.
        private static void DeleteOldLogs(int keepDays)
        {
            try
            {
                if (!Directory.Exists(_logDir)) return;

                DateTime limit = DateTime.Now.AddDays(-keepDays);
                foreach (string file in Directory.GetFiles(_logDir, FilePrefix + "*" + FileExtension))
                {
                    if (File.GetLastWriteTime(file) < limit)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Dọn file cũ thất bại thì bỏ qua, không ảnh hưởng server.
            }
        }
    }
}
