using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.Payments;

public class SepayBankAccountsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<SepayBankAccountDto> Data { get; set; } = [];
}
