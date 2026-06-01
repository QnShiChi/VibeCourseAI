namespace CourseVideo.API.DTOs.Categories;

public class ReorderCategoriesRequest
{
    public IReadOnlyList<Guid> CategoryIds { get; set; } = [];
}
