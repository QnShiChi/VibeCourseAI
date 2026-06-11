using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.Payments;

public class SepayTransactionsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<SepayTransactionDto> Data { get; set; } = [];
}
