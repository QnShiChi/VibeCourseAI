using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IGenerationJobRepository
{
    Task AddAsync(GenerationJob job);
    Task<GenerationJob?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<GenerationJob>> GetAllAsync();
    Task<IReadOnlyList<GenerationJob>> GetRecoverableLessonContentJobsAsync();
    Task<bool> HasRunningJobForSyllabusAsync(Guid syllabusId);
    Task<bool> HasCompletedJobForSyllabusAsync(Guid syllabusId);
    Task<bool> HasRunningLessonContentJobForCourseAsync(Guid courseId);
    Task SaveChangesAsync();
}
