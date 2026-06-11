namespace CourseVideo.API.DTOs.Auth;

public class CurrentUserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string PaymentBankCode { get; set; } = string.Empty;
    public string PaymentBankName { get; set; } = string.Empty;
    public string PaymentBankAccountNumber { get; set; } = string.Empty;
    public string PaymentAccountHolderName { get; set; } = string.Empty;
}
