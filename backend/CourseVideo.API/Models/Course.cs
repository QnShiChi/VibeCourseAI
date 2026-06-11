namespace CourseVideo.API.Models;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public int Price { get; set; } = 599000;
    public Guid? SyllabusId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Category? Category { get; set; }
    public Syllabus? Syllabus { get; set; }
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
}
