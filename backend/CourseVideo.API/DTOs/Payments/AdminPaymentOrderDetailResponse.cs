namespace CourseVideo.API.DTOs.Payments;

public class AdminPaymentOrderDetailResponse
{
    public Guid PaymentOrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public int? SepayTransactionId { get; set; }
}
