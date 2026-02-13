// Services/LoggingService.cs
using System;
using PersonalPPEManager.DataAccess; // 引用数据访问层
using PersonalPPEManager.Models;   // 引用数据模型

namespace PersonalPPEManager.Services
{
    public static class LoggingService
    {
        /// <summary>
        /// 记录一条操作日志到数据库。
        /// </summary>
        /// <param name="operationType">操作类型 (例如: "员工添加", "数据导入")</param>
        /// <param name="description">操作的详细描述</param>
        public static void LogAction(string operationType, string description)
        {
            try
            {
                LogEntry newLog = new LogEntry
                {
                    // SQLite 通常期望 'YYYY-MM-DD HH:MM:SS' 格式
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    OperationType = operationType,
                    Description = description
                };

                bool success = SQLiteDataAccess.AddLogEntry(newLog);

                if (!success)
                {
                    // 如果日志记录本身失败，可以在调试输出中记录一条消息
                    // 避免因日志记录失败导致更严重的问题或无限循环
                    System.Diagnostics.Debug.WriteLine($"Failed to write log entry: {operationType} - {description}");
                }
            }
            catch (Exception ex)
            {
                // 捕获所有可能的异常，并在调试输出中记录
                System.Diagnostics.Debug.WriteLine($"Error in LoggingService.LogAction: {ex.Message}");
                // 在实际应用中，你可能还想将这个错误记录到文件或其他地方
            }
        }
    }
}