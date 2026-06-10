using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.Payments;

public class SepayBankAccountDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("account_holder_name")]
    public string AccountHolderName { get; set; } = string.Empty;

    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public int Active { get; set; }

    [JsonPropertyName("bank_short_name")]
    public string BankShortName { get; set; } = string.Empty;

    [JsonPropertyName("bank_full_name")]
    public string BankFullName { get; set; } = string.Empty;

    [JsonPropertyName("bank_code")]
    public string BankCode { get; set; } = string.Empty;
}
