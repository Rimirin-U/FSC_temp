﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SQLite;
using Serilog.Context;
using Serilog.Parsing;
using Microsoft.Data.Sqlite;
using System.Data;

namespace BasicClassLibrary
{
    // 日志记录服务
    /*public static class LoggerService
    {
        // 静态日志记录器实例
        private static readonly ILogger _logger = LoggerConfig.CreateLogger();
        // 预定义常用模板提升性能
        private static readonly MessageTemplate _baseTemplate
            = new MessageTemplateParser().Parse("[{LogType}] {Data}");
        // 记录观看日志方法
        public static void LogWatch(int episodeId)
        {
            // 创建观看日志条目对象
            var logEntry = new WatchLogEntry { EpisodeId = episodeId };
            // 调用内部记录方法，指定日志类型为"Watch"
            LogWithType("Watch", logEntry);
        }

        public static void LogReview(int noteId)
        {
            var logEntry = new ReviewLogEntry { NoteId = noteId };
            LogWithType("Review", logEntry);
        }

        public static void LogRating(int entryId, int score)
        {
            var logEntry = new RatingLogEntry { EntryId = entryId, Score = score };
            LogWithType("Rating", logEntry);
        }
        // 通用日志记录方法（泛型约束T必须继承自LogEntry）
        private static void LogWithType<T>(string logType, T logEntry) where T : LogEntry
        {
            var logEvent = new LogEvent(
                DateTimeOffset.Now,  // 当前时间作为时间戳
                LogEventLevel.Information,// 日志级别设为Information
                null,      // 异常对象（无异常时为null
                _baseTemplate,
                new List<LogEventProperty>// 日志属性列表
                {
                    new LogEventProperty("LogType", new ScalarValue(logType)),// 添加LogType属性
                    new LogEventProperty("Data", new ScalarValue(logEntry)) // 添加数据对象
                });
            // 使用日志上下文推入LogType属性
            using (LogContext.PushProperty("LogType", logType))
            {
                // 写入日志事件
                _logger.Write(logEvent);
            }
        }
        // 日志查询服务
        // 获取日志的泛型方法
        public static List<T> GetLogs<T>(string logType, DateTime? start = null, DateTime? end = null) where T : LogEntry
        {
            // 数据库连接字符串
            const string connectionString = "Data Source=logs.db";
            // 构建基础查询语句
            var query = new StringBuilder("SELECT * FROM Logs WHERE LogType = @logType");
            // 参数列表（初始包含logType参数）
            var parameters = new List<SQLiteParameter> { new SQLiteParameter("@logType", logType) };
            // 添加时间范围过滤条件
            if (start.HasValue)
            {
                query.Append(" AND Timestamp >= @start");
                parameters.Add(new SQLiteParameter("@start", start.Value));
            }

            if (end.HasValue)
            {
                query.Append(" AND Timestamp <= @end");
                parameters.Add(new SQLiteParameter("@end", end.Value));
            }
            // 准备返回结果列表
            var logs = new List<T>();
            // 使用数据库连接
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                // 创建命令对象
                using (var cmd = new SQLiteCommand(query.ToString(), conn))
                {
                    // 添加参数集合
                    cmd.Parameters.AddRange(parameters.ToArray());
                    // 执行查询
                    using (var reader = cmd.ExecuteReader())
                    {
                        // 遍历结果集
                        while (reader.Read())
                        {
                            //从LogEvent列获取JSON数据
                            var json = reader["LogEvent"].ToString();
                            // 反序列化为指定类型对象
                            var logEntry = JsonConvert.DeserializeObject<T>(json);
                            // 设置时间戳（从数据库原始字段获取）
                            logEntry.Timestamp = Convert.ToDateTime(reader["Timestamp"]);
                            logs.Add(logEntry);
                            // 添加到结果列表
                        }
                    }
                }
            }
            return logs;
        }
    }*/
    public static class LoggerService
    {
        private static readonly ILogger _logger = LoggerConfig.CreateLogger();

        // 记录观看日志
        public static void LogWatch(int episodeId)
        {
            var logEntry = new WatchLogEntry { EpisodeId = episodeId };
            LogWithType("Watch", logEntry);
        }

        // 记录评价日志
        public static void LogReview(int noteId)
        {
            var logEntry = new ReviewLogEntry { NoteId = noteId };
            LogWithType("Review", logEntry);
        }

        // 记录评分日志
        public static void LogRating(int entryId, int score)
        {
            var logEntry = new RatingLogEntry { EntryId = entryId, Score = score };
            LogWithType("Rating", logEntry);
        }

        // 通用日志记录方法
        private static void LogWithType<T>(string logType, T logEntry) where T : LogEntry
            //logType：日志类型（如 "Watch"、"Review" 或 "Rating"）。
            //logEntry：具体的日志条目对象（如 WatchLogEntry、ReviewLogEntry 或 RatingLogEntry）
        {
            try
            {
                var logEvent = new LogEvent(
                    DateTimeOffset.Now,
                    Serilog.Events.LogEventLevel.Information,
                    null,
                    new MessageTemplateParser().Parse("[{LogType}] {Data}"),
                    new List<LogEventProperty>
                    {
                        new LogEventProperty("LogType", new ScalarValue(logType)),
                        new LogEventProperty("Data", new ScalarValue(logEntry))
                    });

                using (LogContext.PushProperty("LogType", logType))
                {
                    _logger.Write(logEvent);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to log event of type {logType}: {ex.Message}");
            }
        }

        // 查询日志
        public static List<T> GetLogs<T>(string logType, DateTime? start = null, DateTime? end = null) where T : LogEntry
        {
            const string connectionString = "Data Source=logs.db";
            var logs = new List<T>();

            try
            {
                var query = BuildQuery(logType, start, end, out var parameters);

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var json = reader["LogEvent"]?.ToString();
                                if (!string.IsNullOrEmpty(json) && TryParseLogEntry<T>(json, reader, out var logEntry))
                                {
                                    logs.Add(logEntry);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving logs for type {logType}: {ex.Message}");
            }

            return logs;
        }

        // 构建查询字符串
        private static string BuildQuery(string logType, DateTime? start, DateTime? end, out List<SqliteParameter> parameters)
           // parameters：SQL 查询所需的参数列表。
        {
            var query = new StringBuilder("SELECT * FROM Logs WHERE LogType = @logType");
            parameters = new List<SqliteParameter> { new SqliteParameter("@logType", DbType.String) { Value = logType } };

            if (start.HasValue)
            {
                query.Append(" AND Timestamp >= @start");
                parameters.Add(new SqliteParameter("@start", DbType.DateTime) { Value = start.Value });
            }

            if (end.HasValue)
            {
                query.Append(" AND Timestamp <= @end");
                parameters.Add(new SqliteParameter("@end", DbType.DateTime) { Value = end.Value });
            }

            return query.ToString();
        }

        // 反序列化日志条目
        private static bool TryParseLogEntry<T>(string json, SqliteDataReader reader, out T logEntry) where T : LogEntry
        {
            logEntry = null;

            try
            {
                logEntry = JsonConvert.DeserializeObject<T>(json);
                if (logEntry != null && reader["Timestamp"] is string timestampStr && DateTime.TryParse(timestampStr, out var timestamp))
                {
                    logEntry.Timestamp = timestamp;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to parse log entry: {ex.Message}");
            }

            return false;
        }
    }
}
