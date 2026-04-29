using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PupilCare.Services
{
    /// <summary>
    /// Gemini AI integration for generating educational insights.
    /// Sends structured student/class data and receives analysis.
    /// </summary>
    public class GeminiInsightService : IAiInsightService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeminiInsightService> _logger;

        public GeminiInsightService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<GeminiInsightService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GenerateInsightAsync(
            string insightScope, string scopeLabel, object structuredData)
        {
            try
            {
                var gemini = _config.GetSection("Gemini");
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? gemini["ApiKey"];
                var model = gemini["Model"] ?? "gemini-flash-latest";
                var endpointTemplate = gemini["Endpoint"]
                    ?? "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

                var endpoint = endpointTemplate.Replace("{model}", model);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return BuildFallbackInsight(insightScope, scopeLabel, structuredData);
                }

                var dataJson = JsonSerializer.Serialize(structuredData, new JsonSerializerOptions { WriteIndented = true });

                var prompt = $"""
You are an educational analyst for PupilCare, a school management system in Bangladesh.
You have been given structured data about: {scopeLabel}
Insight Scope: {insightScope}

Data:
{dataJson}

Please provide a comprehensive analysis with the following sections:
1. **Overall Summary** - Key statistics and general performance overview
2. **Strengths** - Notable positives observed in the data
3. **Areas of Concern** - Issues that need attention (poor attendance, low marks, behavioral patterns)
4. **Actionable Recommendations** - Specific steps teachers or admin should take

Be empathetic, professional, and constructive. Keep the response focused and practical.
""";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1500
                    }
                };

                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(requestBody);
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-goog-api-key", apiKey);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                    return BuildFallbackInsight(insightScope, scopeLabel, structuredData);
                }

                var parsed = JsonDocument.Parse(responseBody);
                if (parsed.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                    return text ?? "No insight generated.";
                }

                _logger.LogWarning("Gemini returned unexpected response: {Body}", responseBody);
                return BuildFallbackInsight(insightScope, scopeLabel, structuredData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return BuildFallbackInsight(insightScope, scopeLabel, structuredData);
            }
        }

        private static string BuildFallbackInsight(string scope, string label, object data)
        {
            // When no API key is configured, return a structured placeholder
            return $"""
## AI Insight - {label}
**Scope:** {scope}
**Generated:** {DateTime.Now:dd MMM yyyy, hh:mm tt}

> *AI analysis is currently unavailable. Please configure `Gemini:ApiKey` with .NET user secrets or set the `GEMINI_API_KEY` environment variable to enable automatic insights.*

**Data Summary:**
The system has collected the relevant student data for this scope. Once the AI service is configured, it will analyze attendance patterns, academic performance trends, behavioral records, and provide actionable recommendations.

**To enable AI Insights:**
1. Obtain a Gemini API key from [Google AI Studio](https://aistudio.google.com)
2. Store it with .NET user secrets under `Gemini:ApiKey`, or set the `GEMINI_API_KEY` environment variable
3. Re-generate this insight
""";
        }
    }
}
