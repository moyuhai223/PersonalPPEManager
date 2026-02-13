// Models/LogEntry.cs
using System;

namespace PersonalPPEManager.Models
{
    public class LogEntry
    {
        public int LogID { get; set; }             // 日志ID (主键, 自增)
        public string Timestamp { get; set; }       // 操作时间戳 (YYYY-MM-DD HH:MM:SS)
        public string OperationType { get; set; }   // 操作类型
        public string Description { get; set; }     // 操作描述

        // 为了方便显示，可以考虑将Timestamp字符串转换为DateTime对象，但模型本身保持简单
        // public DateTime TimestampDT => DateTime.TryParse(Timestamp, out DateTime dt) ? dt : DateTime.MinValue;
    }
}