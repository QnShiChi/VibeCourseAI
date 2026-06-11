namespace CourseVideo.API.Models;

public class PaymentOrder : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public int Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? SepayTransactionId { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? AccountHolderName { get; set; }
    public string TransferContent { get; set; } = string.Empty;
}
