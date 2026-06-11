namespace CourseVideo.API.Models;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CategoryStatus Status { get; set; } = CategoryStatus.Visible;
    public int SortOrder { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
