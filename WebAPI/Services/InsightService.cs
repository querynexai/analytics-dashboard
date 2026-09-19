using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AnalyticsDashboard.Api.Models;

namespace AnalyticsDashboard.Api.Services;

public class InsightService
{
    private readonly HttpClient _http;
    private readonly AnalyticsService _analytics;
    private readonly string _apiKey;
    private readonly string _model;

    public InsightService(HttpClient http, AnalyticsService analytics, IConfiguration config)
    {
        _http = http;
        _analytics = analytics;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not set");
        _model = config["Groq:Model"] ?? "openai/gpt-oss-120b";

        _http.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<AiInsightResponse> AnswerAsync(string question)
    {
        // Define available tools
        var tools = new object[]
        {
            new {
                type = "function",
                function = new {
                    name = "get_monthly_revenue",
                    description = "Returns monthly revenue, order count, and unique customers.",
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_top_products",
                    description = "Returns the top products by revenue.",
                    parameters = new {
                        type = "object",
                        properties = new { limit = new { type = "integer", description = "Number of products" } },
                        required = new[] { "limit" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_customer_growth",
                    description = "Returns new and cumulative customers per month.",
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_revenue_by_category",
                    description = "Returns revenue and units sold per product category.",
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_order_status",
                    description = "Returns count and value of orders grouped by status.",
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_summary",
                    description = "Returns overall KPIs: total revenue, orders, customers, avg order value, MoM change.",
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            }
        };

        var messages = new List<object>
        {
            new { role = "system", content =
                "You are a senior data analyst. Answer questions about the e-commerce business using the provided tools. " +
                "Call the minimum number of tools needed. After getting data, answer clearly and concisely. " +
                "If relevant, include a short bullet list of insights. Never invent numbers — use only tool results." },
            new { role = "user", content = question }
        };

        // First call
        var firstResponse = await ChatAsync(messages, tools);
        var choice = firstResponse["choices"]![0]!;
        var message = choice["message"]!;

        // Check for tool calls
        var toolCalls = message["tool_calls"]?.AsArray();
        if (toolCalls is null || toolCalls.Count == 0)
        {
            var content = message["content"]?.GetValue<string>() ?? "No answer.";
            return new AiInsightResponse(content);
        }

        // Append assistant message with tool calls
        messages.Add(message.Deserialize<Dictionary<string, object>>()!);

        // Execute each tool call
        foreach (var call in toolCalls)
        {
            var fn = call!["function"]!;
            var name = fn["name"]!.GetValue<string>();
            var argsJson = fn["arguments"]!.GetValue<string>();
            var args = string.IsNullOrWhiteSpace(argsJson) ? new JsonObject() : JsonNode.Parse(argsJson)!.AsObject();

            var result = await ExecuteToolAsync(name, args);

            messages.Add(new
            {
                role = "tool",
                tool_call_id = call["id"]!.GetValue<string>(),
                content = JsonSerializer.Serialize(result)
            });
        }

        // Second call with tool results
        var secondResponse = await ChatAsync(messages, tools);
        var finalContent = secondResponse["choices"]![0]!["message"]!["content"]?.GetValue<string>()
            ?? "No answer.";

        return new AiInsightResponse(finalContent);
    }

    private async Task<JsonNode> ChatAsync(List<object> messages, object[] tools)
    {
        var payload = new
        {
            model = _model,
            messages,
            tools,
            tool_choice = "auto",
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("chat/completions", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Groq error ({response.StatusCode}): {body}");

        return JsonNode.Parse(body)!;
    }

    private async Task<object> ExecuteToolAsync(string name, JsonObject args)
    {
        return name switch
        {
            "get_monthly_revenue" => await _analytics.GetMonthlyRevenueAsync(),
            "get_top_products" => await _analytics.GetTopProductsAsync(args["limit"]?.GetValue<int>() ?? 10),
            "get_customer_growth" => await _analytics.GetCustomerGrowthAsync(),
            "get_revenue_by_category" => await _analytics.GetRevenueByCategoryAsync(),
            "get_order_status" => await _analytics.GetOrderStatusAsync(),
            "get_summary" => await _analytics.GetSummaryAsync(),
            _ => new { error = $"Unknown tool: {name}" }
        };
    }
}