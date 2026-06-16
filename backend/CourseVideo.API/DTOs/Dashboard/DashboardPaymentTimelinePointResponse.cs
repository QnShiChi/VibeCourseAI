namespace CourseVideo.API.DTOs.Dashboard;

public class DashboardPaymentTimelinePointResponse
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int PaidOrders { get; set; }
    public int PendingOrders { get; set; }
    public int FailedOrExpiredOrders { get; set; }
}
