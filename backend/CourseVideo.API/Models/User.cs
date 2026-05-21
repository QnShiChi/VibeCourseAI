namespace CourseVideo.API.Models;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Syllabus> Syllabuses { get; set; } = new List<Syllabus>();
    public ICollection<GenerationJob> CreatedGenerationJobs { get; set; } = new List<GenerationJob>();
}
