using CourseVideo.API.DTOs.Categories;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<AdminCategoryListItemResponse>> GetAdminCategoriesAsync(string? status, string? search, string? sort)
    {
        var categories = await _categoryRepository.GetAllWithCoursesAsync();
        var filtered = categories.AsEnumerable(); // AsEnumerable để có thể áp dụng các phép lọc và sắp xếp LINQ trên bộ sưu tập trong bộ nhớ

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(category => category.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant(); 
            filtered = filtered.Where(category =>
                category.Name.ToLowerInvariant().Contains(keyword)
                || category.Description.ToLowerInvariant().Contains(keyword));
        }

        filtered = (sort ?? "latest").ToLowerInvariant() switch
        {
            "alpha" => filtered.OrderBy(category => category.Name),
            "manual" => filtered.OrderBy(category => category.SortOrder).ThenBy(category => category.Name),
            "oldest" => filtered.OrderBy(category => category.CreatedAt),
            _ => filtered.OrderByDescending(category => category.CreatedAt)
        };

        return filtered.Select(MapListItem).ToList();
    }

    public async Task<IReadOnlyList<CategoryOptionResponse>> GetVisibleCategoryOptionsAsync()
    {
        var categories = await _categoryRepository.GetVisibleAsync();
        return categories.Select(category => new CategoryOptionResponse
        {
            Id = category.Id,
            Name = category.Name,
            Status = category.Status.ToString()
        }).ToList();
    }

    public async Task<AdminCategoryListItemResponse> CreateAsync(UpsertCategoryRequest request)
    {
        var name = NormalizeName(request.Name); 
        var description = NormalizeDescription(request.Description);
        var status = ParseStatus(request.Status);

        if (await _categoryRepository.ExistsByNameAsync(name))
        {
            throw new InvalidOperationException("Tên category đã tồn tại.");
        }

        var existing = await _categoryRepository.GetAllWithCoursesAsync();
        var nextSortOrder = existing.Count == 0 ? 100 : existing.Max(category => category.SortOrder) + 100; 
        var category = new Category
        {
            Name = name,
            Description = description,
            Status = status,
            SortOrder = nextSortOrder
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return MapListItem(category);
    }

    public async Task<AdminCategoryListItemResponse?> UpdateAsync(Guid id, UpsertCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return null;
        }

        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var status = ParseStatus(request.Status);

        if (await _categoryRepository.ExistsByNameAsync(name, id))
        {
            throw new InvalidOperationException("Tên category đã tồn tại.");
        }

        category.Name = name;
        category.Description = description;
        category.Status = status;
        category.UpdatedAt = DateTime.UtcNow;
        await _categoryRepository.SaveChangesAsync();

        return MapListItem(category);
    }

    public async Task ReorderAsync(IReadOnlyList<Guid> categoryIds)
    {
        if (categoryIds.Count == 0)
        {
            return;
        }

        var categories = await _categoryRepository.GetAllWithCoursesAsync();
        var lookup = categories.ToDictionary(category => category.Id); // Tạo một dictionary để tra cứu nhanh category theo ID, giúp kiểm tra tính hợp lệ của danh sách ID và cập nhật thứ tự sắp xếp một cách hiệu quả

        if (categoryIds.Any(id => !lookup.ContainsKey(id))) // Kiểm tra xem tất cả ID trong danh sách có tồn tại trong cơ sở dữ liệu hay không, nếu có ID nào không tồn tại thì ném ra lỗi
        {
            throw new InvalidOperationException("Danh sách category sắp xếp không hợp lệ.");
        }

        for (var index = 0; index < categoryIds.Count; index += 1)
        {
            var category = lookup[categoryIds[index]];
            category.SortOrder = (index + 1) * 100; // Cập nhật thứ tự sắp xếp của category dựa trên vị trí của nó trong danh sách, nhân với 100 để dễ dàng chèn thêm category mới vào giữa các category hiện có mà không cần phải cập nhật lại toàn bộ thứ tự sắp xếp
            category.UpdatedAt = DateTime.UtcNow;
        }

        await _categoryRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            throw new KeyNotFoundException("Không tìm thấy category.");
        }

        if (category.Courses.Count > 0)
        {
            throw new InvalidOperationException("Không thể xóa category vì vẫn còn khóa học đang sử dụng.");
        }

        _categoryRepository.Remove(category);
        await _categoryRepository.SaveChangesAsync();
    }

    private static AdminCategoryListItemResponse MapListItem(Category category)
    {
        return new AdminCategoryListItemResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Status = category.Status.ToString(),
            SortOrder = category.SortOrder,
            CourseCount = category.Courses.Count,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private static CategoryStatus ParseStatus(string value)
    {
        if (!Enum.TryParse<CategoryStatus>(value, true, out var status)) // TryParse với ignoreCase = true để cho phép người dùng nhập trạng thái không phân biệt chữ hoa chữ thường, nếu giá trị không hợp lệ thì ném ra lỗi
        {
            throw new InvalidOperationException("Trạng thái category không hợp lệ.");
        }

        return status;
    }

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Tên category là bắt buộc.");
        }

        return normalized;
    }

    private static string NormalizeDescription(string value)
    {
        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Chưa có mô tả ngắn." : normalized;
    }
}
