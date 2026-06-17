namespace CourseVideo.API.DTOs.Dashboard;

public class DashboardPaymentOverviewResponse
{
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int PendingOrders { get; set; }
    public int FailedOrExpiredOrders { get; set; }
    public IReadOnlyList<DashboardPaymentTimelinePointResponse> Timeline { get; set; } = [];
    public IReadOnlyList<DashboardRecentPaymentOrderItemResponse> RecentOrders { get; set; } = [];
}
