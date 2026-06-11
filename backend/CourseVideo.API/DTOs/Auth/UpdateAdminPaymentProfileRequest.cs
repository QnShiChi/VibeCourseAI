namespace CourseVideo.API.DTOs.Auth;

public class UpdateAdminPaymentProfileRequest
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}
