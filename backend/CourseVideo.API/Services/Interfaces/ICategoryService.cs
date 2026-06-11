using CourseVideo.API.DTOs.Categories;

namespace CourseVideo.API.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<AdminCategoryListItemResponse>> GetAdminCategoriesAsync(string? status, string? search, string? sort);
    Task<IReadOnlyList<CategoryOptionResponse>> GetVisibleCategoryOptionsAsync();
    Task<AdminCategoryListItemResponse> CreateAsync(UpsertCategoryRequest request);
    Task<AdminCategoryListItemResponse?> UpdateAsync(Guid id, UpsertCategoryRequest request);
    Task ReorderAsync(IReadOnlyList<Guid> categoryIds);
    Task DeleteAsync(Guid id);
}
