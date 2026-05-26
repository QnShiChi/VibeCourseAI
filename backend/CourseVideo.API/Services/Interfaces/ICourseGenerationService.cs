using CourseVideo.API.DTOs.GenerationJobs;

namespace CourseVideo.API.Services.Interfaces;

public interface ICourseGenerationService
{
    Task<GenerateCourseResponse> GenerateFromSyllabusAsync(Guid syllabusId, Guid createdByUserId, string createdByName);
    Task<IReadOnlyList<GenerationJobListItemResponse>> GetAllJobsAsync();
    Task<GenerationJobDetailResponse?> GetJobByIdAsync(Guid id);
    Task CancelJobAsync(Guid id);
}
