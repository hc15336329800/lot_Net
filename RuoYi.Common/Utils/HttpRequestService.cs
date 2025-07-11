using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RuoYi.Common.Utils
{
    public class HttpRequestService
    {
        private readonly HttpClient _client;
        private readonly ILogger<HttpRequestService> _logger;

        public HttpRequestService(IHttpClientFactory clientFactory, ILogger<HttpRequestService> logger)
        {
            _client = clientFactory.CreateClient("GeneralClient"); //使用配置的 HttpClient
            _logger = logger;
        }

        /// <summary>
        /// 通用 GET 请求方法，返回带错误处理的结果
        /// </summary>
        public async Task<(bool isSuccess, TResponse? data, string errorMessage)> GetAsync<TResponse>(string url) where TResponse : class
        {
            try
            {
                var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return (true, JsonSerializer.Deserialize<TResponse>(result), string.Empty);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"GET 请求失败: {ex.Message}");
                return (false, null, $"请求失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"请求发生错误: {ex.Message}");
                return (false, null, $"服务器错误: {ex.Message}");
            }
        }


        /// <summary>
        /// 通用 POST 请求方法，返回带错误处理的结果
        /// </summary>
        public async Task<(bool isSuccess, string data, string errorMessage)> PostAsync<TRequest>(string url, TRequest data)
     where TRequest : class
        {
            string result = string.Empty; // 在 try 块外部定义 result

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"服务器返回的原始内容: {result}");

                // 检查响应内容是否为 JSON 格式，如果不是，直接返回原始内容
                if (string.IsNullOrWhiteSpace(result) || (!result.Trim().StartsWith("{") && !result.Trim().StartsWith("[")))
                {
                    return (true, result, string.Empty); // 非 JSON 格式，直接返回内容
                }

                // 尝试反序列化为指定类型
                var dataResult = JsonSerializer.Deserialize<string>(result) ?? result;
                return (true, dataResult, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"POST 请求失败: {ex.Message}");
                return (false, string.Empty, $"请求失败: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError($"JSON 反序列化失败: {ex.Message}");
                return (true, result, string.Empty); // 如果 JSON 反序列化失败，返回原始内容
            }
            catch (Exception ex)
            {
                _logger.LogError($"请求发生错误: {ex.Message}");
                return (false, string.Empty, $"服务器错误: {ex.Message}");
            }
        }





        /// <summary>
        /// PUT 请求
        /// </summary>
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _client.PutAsync(url, content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"PUT 请求失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// DELETE 请求
        /// </summary>
        public async Task<bool> DeleteAsync(string url)
        {
            try
            {
                var response = await _client.DeleteAsync(url);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DELETE 请求失败: {ex.Message}");
                throw;
            }
        }
    }
}
