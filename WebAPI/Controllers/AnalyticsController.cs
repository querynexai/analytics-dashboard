using Microsoft.AspNetCore.Mvc;
using AnalyticsDashboard.Api.Models;
using AnalyticsDashboard.Api.Services;

namespace AnalyticsDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analytics;
    private readonly InsightService _insights;

    public AnalyticsController(AnalyticsService analytics, InsightService insights)
    {
        _analytics = analytics;
        _insights = insights;
    }

    [HttpGet("summary")]
    public Task<DashboardSummaryDto> GetSummary() => _analytics.GetSummaryAsync();

    [HttpGet("monthly-revenue")]
    public Task<List<MonthlyRevenueDto>> GetMonthlyRevenue() => _analytics.GetMonthlyRevenueAsync();

    [HttpGet("top-products")]
    public Task<List<TopProductDto>> GetTopProducts([FromQuery] int limit = 10) =>
        _analytics.GetTopProductsAsync(limit);

    [HttpGet("customer-growth")]
    public Task<List<CustomerGrowthDto>> GetCustomerGrowth() => _analytics.GetCustomerGrowthAsync();

    [HttpGet("revenue-by-category")]
    public Task<List<CategoryRevenueDto>> GetRevenueByCategory() =>
        _analytics.GetRevenueByCategoryAsync();

    [HttpGet("order-status")]
    public Task<List<OrderStatusDto>> GetOrderStatus() => _analytics.GetOrderStatusAsync();

    [HttpPost("insights")]
    public async Task<ActionResult<AiInsightResponse>> GetInsights([FromBody] AiInsightRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new AiInsightResponse("", null, "Question cannot be empty."));

        try
        {
            var result = await _insights.AnswerAsync(request.Question);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AiInsightResponse("", null, ex.Message));
        }
    }
}