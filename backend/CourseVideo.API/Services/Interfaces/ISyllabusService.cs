using CourseVideo.API.DTOs.Syllabuses;

namespace CourseVideo.API.Services.Interfaces;

public interface ISyllabusService
{
    Task<ImportSyllabusResponse> ImportAsync(ImportSyllabusRequest request, Guid uploadedByUserId, string uploadedByName);
    Task<IReadOnlyList<SyllabusListItemResponse>> GetAllAsync();
    Task<SyllabusDetailResponse?> GetByIdAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
