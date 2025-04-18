﻿using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BasicClassLibrary
{
   

    public class ResourceSearchInAnimesGarden 
    {
        /*
        // 共享的HttpClient实例（静态，避免重复创建）
        private static readonly HttpClient _sharedClient = new();
        // 当前使用的HttpClient实例（实例级或共享）
        private readonly HttpClient _httpClient;
        // 是否释放当前HttpClient（区分共享实例和独立实例）
        private readonly bool _disposeClient;
        // 构造函数重载1：使用共享客户端，不释放（默认）
        public ResourceSearch() : this(_sharedClient, false) { }
        // 构造函数重载2：传入自定义HttpClient，不释放（默认）
        public ResourceSearch(HttpClient httpClient) : this(httpClient, false) { }

        // 私有构造函数：初始化核心字段
        private ResourceSearch(HttpClient httpClient, bool disposeClient)
        {
            // 检查HttpClient是否为null，避免空引用异常
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeClient = disposeClient; // 是否在Dispose时释放客户端
        }
        */
        private readonly HttpClient _httpClient;// 用于发送 HTTP 请求的客户端（不可变字段）
        public ResourceSearchInAnimesGarden()
        {
            _httpClient = new HttpClient();// 构造函数中创建新的 HttpClient 实例
        }
        
        public async Task<List<ResourceItem>> SearchAsync(string keyword, CancellationToken ct = default)
        // 搜索关键词（必填） CancellationToken ct = default // 取消令牌（默认无，可以先不管这个参数）
        {
            try
            {
                // 对关键词和固定类型 "动画" 进行 URL 编码
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var encodedType = Uri.EscapeDataString("动画");
                // 硬编码 API 地址，拼接查询参数
                var url = $"https://api.animes.garden/feed.xml?include={encodedKeyword}&type={encodedType}";

                // 使用流式处理优化内存
                // 异步获取 HTTP 响应流（使用 using 自动释放流资源）
                using var responseStream = await _httpClient.GetStreamAsync(url, ct);

                // 异步解析XML
                var xdoc = await XDocument.LoadAsync(responseStream, LoadOptions.None, ct);

                return xdoc.Descendants("item")
                    .Select(item => new ResourceItem
                    {
                        /* Title = item.Element("title")?.Value,
                         Link = item.Element("link")?.Value,
                         Guid = item.Element("guid")?.Value,
                         PubDate = ParsePubDate(item.Element("pubDate")?.Value),  // 日期转换
                         EnclosureUrl = item.Element("enclosure")?.Attribute("url")?.Value*/

                        // 清理字符串（去除前后空格，处理null）
                        Title = SanitizeString(item.Element("title")?.Value),
                        // 解析日期（处理RFC 822格式，如"Thu, 01 Jan 2020 12:00:00 GMT"）
                        PubDate = ParseDate(item.Element("pubDate")?.Value),
                        // 提取enclosure标签的url属性
                        DownloadUrl= SanitizeString(item.Element("enclosure")?.Attribute("url")?.Value)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索发生错误: {ex.Message}");
                return new List<ResourceItem>();
            }
        }

        // 日期解析方法
        // 日期解析方法（允许输入参数为null）
        private static DateTime? ParseDate(string? value) // 修改参数类型为string?
        {
            // 显式处理null和空白值
            if (string.IsNullOrWhiteSpace(value)) return null;

            // 尝试按RFC 822格式（"r"格式说明符）解析日期
            return DateTime.TryParseExact(
                value, // 此时value已被验证为非空
                "r",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out var dt)
                ? dt
                : null;
        }

        /* private static string SanitizeString(string？ value)
     => value?.Trim() ?? ""; // 去除前后空格，null转换为空字符串*/
        public static string SanitizeString(string? value)
        {
            return value?.Trim() ?? string.Empty; // 显式处理null
        }
    }
}

