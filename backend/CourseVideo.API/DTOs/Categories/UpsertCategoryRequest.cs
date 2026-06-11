namespace CourseVideo.API.DTOs.Categories;

// This DTO is used for both creating and updating a category
public class UpsertCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
