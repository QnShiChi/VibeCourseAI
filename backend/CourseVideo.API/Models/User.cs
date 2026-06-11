namespace CourseVideo.API.Models;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PaymentBankCode { get; set; }
    public string? PaymentBankName { get; set; }
    public string? PaymentBankAccountNumber { get; set; }
    public string? PaymentAccountHolderName { get; set; }
    public DateTime? PaymentSettingsUpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Syllabus> Syllabuses { get; set; } = new List<Syllabus>();
    public ICollection<GenerationJob> CreatedGenerationJobs { get; set; } = new List<GenerationJob>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordTokenExpiry { get; set; }
}
