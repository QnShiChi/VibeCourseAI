namespace CourseVideo.API.Models;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; } 
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    public User? User { get; set; }
}
