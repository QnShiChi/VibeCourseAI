namespace CourseVideo.API.Configuration;

public class SepayOptions
{
    public string ApiToken { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox";
    public string WebhookApiKey { get; set; } = string.Empty;
    public int OrderExpiryMinutes { get; set; } = 5;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string StoreName { get; set; } = "VibeCourseAI";
}
