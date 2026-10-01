using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SalesManagement.Services.Interfaces;

namespace SalesManagement.Services.Implementations;

public class GeminiAiService : IGeminiAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string ApiKey =>
        Environment.GetEnvironmentVariable("GEMINI_API_KEY")
        ?? _configuration["Gemini:ApiKey"]
        ?? string.Empty;

    private string Model => _configuration["Gemini:Model"] ?? "gemini-2.0-flash";

    public Task<string> GenerateAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return Task.FromResult(
                "Chưa cấu hình khóa Gemini API. Hãy đặt biến môi trường GEMINI_API_KEY hoặc mục Gemini:ApiKey trong appsettings.json để sử dụng tính năng AI.");
        }

        return SendAsync(prompt);
    }

    private async Task<string> SendAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"v1beta/models/{Model}:generateContent?key={ApiKey}";
            using var response = await _httpClient.PostAsync(url, content);

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Không thể gọi Gemini API ({(int)response.StatusCode}). {Truncate(responseJson, 300)}";
            }

            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var contentElement) &&
                contentElement.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString() ?? "Không nhận được nội dung từ Gemini.";
            }

            return "Không nhận được phản hồi từ Gemini.";
        }
        catch (Exception ex)
        {
            return $"Lỗi khi gọi Gemini API: {ex.Message}";
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
