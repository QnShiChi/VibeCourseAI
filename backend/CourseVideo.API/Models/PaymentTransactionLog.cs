namespace CourseVideo.API.Models;

public class PaymentTransactionLog : BaseEntity
{
    public int SepayTransactionId { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public string TransactionDateText { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? SubAccount { get; set; }
    public string? Code { get; set; }
    public string Content { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TransferAmount { get; set; }
    public long Accumulated { get; set; }
    public string? ReferenceCode { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public Guid? MatchedPaymentOrderId { get; set; }
    public PaymentOrder? MatchedPaymentOrder { get; set; }
    public bool IsDuplicate { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
