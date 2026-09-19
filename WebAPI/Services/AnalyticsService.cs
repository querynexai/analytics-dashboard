using Npgsql;
using AnalyticsDashboard.Api.Models;

namespace AnalyticsDashboard.Api.Services;

public class AnalyticsService
{
    private readonly string _connectionString;

    public AnalyticsService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured.");
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Func<NpgsqlDataReader, T> map)
    {
        var results = new List<T>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(map(reader));
        return results;
    }

    public Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync() =>
        QueryAsync(
            "SELECT month, order_count, unique_customers, revenue, avg_order_value " +
            "FROM mv_monthly_revenue ORDER BY month",
            r => new MonthlyRevenueDto(
                r.GetDateTime(0), r.GetInt32(1), r.GetInt32(2),
                r.GetDecimal(3), r.GetDecimal(4)));

    public Task<List<TopProductDto>> GetTopProductsAsync(int limit = 10) =>
        QueryAsync(
            $"SELECT product_id, name, category, units_sold, revenue " +
            $"FROM mv_top_products ORDER BY revenue DESC LIMIT {limit}",
            r => new TopProductDto(
                r.GetInt32(0), r.GetString(1), r.GetString(2),
                r.GetInt32(3), r.GetDecimal(4)));

    public Task<List<CustomerGrowthDto>> GetCustomerGrowthAsync() =>
        QueryAsync(
            "SELECT month, new_customers, cumulative_customers " +
            "FROM mv_customer_growth ORDER BY month",
            r => new CustomerGrowthDto(
                r.GetDateTime(0), r.GetInt32(1), r.GetInt64(2).ToString() is var s ? int.Parse(s) : 0));

    public Task<List<CategoryRevenueDto>> GetRevenueByCategoryAsync() =>
        QueryAsync(
            "SELECT category, revenue, units_sold FROM mv_revenue_by_category ORDER BY revenue DESC",
            r => new CategoryRevenueDto(
                r.GetString(0), r.GetDecimal(1), r.GetInt32(2)));

    public Task<List<OrderStatusDto>> GetOrderStatusAsync() =>
        QueryAsync(
            "SELECT status, order_count, total_value FROM mv_order_status ORDER BY order_count DESC",
            r => new OrderStatusDto(
                r.GetString(0), r.GetInt32(1), r.GetDecimal(2)));

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var revenue = await QueryAsync(
            "SELECT COALESCE(SUM(revenue),0), COALESCE(SUM(order_count),0), " +
            "COALESCE(SUM(unique_customers),0), COALESCE(AVG(avg_order_value),0) " +
            "FROM mv_monthly_revenue",
            r => new
            {
                Revenue = r.GetDecimal(0),
                Orders = r.GetInt64(1).ToString() is var s1 ? int.Parse(s1) : 0,
                Customers = r.GetInt64(2).ToString() is var s2 ? int.Parse(s2) : 0,
                Avg = r.GetDecimal(3)
            });

        var total = revenue.FirstOrDefault();
        if (total is null)
            return new DashboardSummaryDto(0, 0, 0, 0, 0);

        // Compute MoM change
        var months = await GetMonthlyRevenueAsync();
        decimal change = 0;
        if (months.Count >= 2)
        {
            var last = months[^1].Revenue;
            var prev = months[^2].Revenue;
            if (prev > 0) change = Math.Round(((last - prev) / prev) * 100, 1);
        }

        return new DashboardSummaryDto(
            total.Revenue, total.Orders, total.Customers, total.Avg, change);
    }
}