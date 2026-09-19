namespace AnalyticsDashboard.Api.Models;

public record MonthlyRevenueDto(
    DateTime Month,
    int OrderCount,
    int UniqueCustomers,
    decimal Revenue,
    decimal AvgOrderValue);

public record TopProductDto(
    int ProductId,
    string Name,
    string Category,
    int UnitsSold,
    decimal Revenue);

public record CustomerGrowthDto(
    DateTime Month,
    int NewCustomers,
    int CumulativeCustomers);

public record CategoryRevenueDto(
    string Category,
    decimal Revenue,
    int UnitsSold);

public record OrderStatusDto(
    string Status,
    int OrderCount,
    decimal TotalValue);

public record DashboardSummaryDto(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    decimal AvgOrderValue,
    decimal RevenueChangePercent);

public record AiInsightRequest(string Question);
public record AiInsightResponse(string Answer, string? ChartJson = null, string? Error = null);