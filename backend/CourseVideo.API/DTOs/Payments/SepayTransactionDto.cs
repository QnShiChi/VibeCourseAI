using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.Payments;

public class SepayTransactionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("transaction_date")]
    public string TransactionDate { get; set; } = string.Empty;

    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("transfer_type")]
    public string TransferType { get; set; } = string.Empty;

    [JsonPropertyName("amount_in")]
    public int AmountIn { get; set; }

    [JsonPropertyName("accumulated")]
    public long Accumulated { get; set; }

    [JsonPropertyName("transaction_content")]
    public string TransactionContent { get; set; } = string.Empty;

    [JsonPropertyName("reference_number")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("bank_brand_name")]
    public string? BankBrandName { get; set; }
}
