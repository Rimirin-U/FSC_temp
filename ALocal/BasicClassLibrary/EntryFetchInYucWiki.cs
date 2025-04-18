﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class EntryFetchInYucWiki : IEntryFetch
    {
        private const string BaseUrl = "https://yuc.wiki/202504/";//存储目标网站的地址
        private readonly HttpClient _httpClient;//HttpClient 是 .NET 中用于发送 HTTP 请求和接收 HTTP 响应的核心类

        public EntryFetchInYucWiki()//构造函数
        { 
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public List<EntryInfoSet> Fetch()
        {
            try
            {
                // 1. 从固定API端点获取数据
                HttpResponseMessage response = _httpClient.GetAsync("api/entries").Result;
                //同步请求：调用 GetAsync 发送GET请求到 api/entries（最终URL为 https://yuc.wiki/202504/api/entries）
                //.Result：阻塞当前线程，等待异步操作完成
                response.EnsureSuccessStatusCode();//确保请求成功

                // 2. 解析JSON响应
                string json = response.Content.ReadAsStringAsync().Result;//读取响应内容转化为字符串
                var apiResponse = JsonConvert.DeserializeObject<YucWikiApiResponse>(json);
                //反序列化JSON：使用 JsonConvert（Newtonsoft.Json库）将JSON字符串转换为 YucWikiApiResponse 对象。


                //// 3. 转换为EntryInfoSet列表
                //return apiResponse?.Entries.Select(item => new EntryInfoSet(
                //    translatedName: item.TranslatedName,
                //    originalName: item.OriginalName,
                //    releaseDate: DateTime.Parse(item.ReleaseDate),
                //    category: item.Category,
                //    metadata: new Dictionary<string, string>
                //    {
                //        ["CoverUrl"] = item.CoverUrl??string.Empty,
                //        ["Author"] = item.Author?? string.Empty,
                //        ["Description"] = item.Description ?? string.Empty
                //    }.AsReadOnly()
                //)).ToList() ?? new List<EntryInfoSet>();


                // 3. 转换为EntryInfoSet列表
                return apiResponse?.Entries?.Select(item =>
                {
                    // 安全解析日期，处理可能的 null 或无效格式
                    DateTime? parsedDate = null;
                    if (!string.IsNullOrWhiteSpace(item.ReleaseDate))
                    {
                        DateTime tempDate;
                        if (DateTime.TryParse(item.ReleaseDate, out tempDate))
                        {
                            parsedDate = tempDate;
                        }
                    }

                    return new EntryInfoSet(
                        translatedName: item.TranslatedName ?? string.Empty,
                        originalName: item.OriginalName ?? string.Empty,
                        releaseDate: parsedDate ?? DateTime.MinValue, // 提供默认值
                        category: item.Category ?? string.Empty,
                        metadata: new Dictionary<string, string>
                        {
                            ["CoverUrl"] = item.CoverUrl ?? string.Empty,
                            ["Author"] = item.Author ?? string.Empty,
                            ["Description"] = item.Description ?? string.Empty
                        }.AsReadOnly()
                    );
                })?.ToList() ?? new List<EntryInfoSet>();
            }
            catch (Exception ex)
            {
                throw new EntrySearchException("yuc.wiki", $"获取条目失败: {ex.Message}");
            }
        }

        // API响应数据结构
        private class YucWikiApiResponse
        {
            [JsonProperty("entries")]
            public List<YucWikiEntry>? Entries { get; set; }
        }

        private class YucWikiEntry
        {
            [JsonProperty("translated_name")]
            public string? TranslatedName { get; set; }

            [JsonProperty("original_name")]
            public string? OriginalName { get; set; }

            [JsonProperty("release_date")]
            public string? ReleaseDate { get; set; }

            [JsonProperty("category")]
            public string? Category { get; set; }

            [JsonProperty("cover_url")]
            public string? CoverUrl { get; set; }

            [JsonProperty("author")]
            public string? Author { get; set; }

            [JsonProperty("description")]
            public string? Description { get; set; }
        }
    }

    public class EntrySearchException : Exception
    {
        public string Site { get; }

        public EntrySearchException(string site, string message)
            : base($"[{site}] {message}")
        {
            Site = site;
        }
    }
}
