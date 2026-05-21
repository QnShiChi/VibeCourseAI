using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class GenerationJobRepository : IGenerationJobRepository
{
    private readonly AppDbContext _dbContext;

    public GenerationJobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(GenerationJob job)
    {
        return _dbContext.GenerationJobs.AddAsync(job).AsTask();
    }

    public async Task<GenerationJob?> GetByIdAsync(Guid id)
    {
        return await _dbContext.GenerationJobs
            .Include(job => job.Syllabus)
            .Include(job => job.Course)
            .Include(job => job.CreatedByUser)
            .FirstOrDefaultAsync(job => job.Id == id);
    }

    public async Task<IReadOnlyList<GenerationJob>> GetAllAsync()
    {
        return await _dbContext.GenerationJobs
            .Include(job => job.Syllabus)
            .Include(job => job.Course)
            .Include(job => job.CreatedByUser)
            .OrderByDescending(job => job.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<GenerationJob>> GetRecoverableLessonContentJobsAsync()
    {
        return await _dbContext.GenerationJobs
            .Where(job =>
                (job.JobType == "GenerateLessonContent" || job.JobType == "RegenerateLessonContent") &&
                (job.Status == "Pending" || job.Status == "GeneratingLessonContent" || job.Status == "RegeneratingLessonContent"))
            .OrderBy(job => job.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<GenerationJob>> GetRecoverableLessonAudioJobsAsync()
    {
        return await _dbContext.GenerationJobs
            .Where(job =>
                (job.JobType == "GenerateLessonAudio" || job.JobType == "RegenerateLessonAudio") &&
                (job.Status == "Pending" || job.Status == "GeneratingLessonAudio"))
            .OrderBy(job => job.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<GenerationJob>> GetRecoverableLessonVideoJobsAsync()
    {
        return await _dbContext.GenerationJobs
            .Where(job =>
                (job.JobType == "GenerateLessonVideo" || job.JobType == "RegenerateLessonVideo") &&
                (job.Status == "Pending" || job.Status == "GeneratingLessonVideo"))
            .OrderBy(job => job.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasRunningJobForSyllabusAsync(Guid syllabusId)
    {
        return await _dbContext.GenerationJobs.AnyAsync(job =>
            job.SyllabusId == syllabusId &&
            (job.Status == "Pending" || job.Status == "Processing"));
    }

    public async Task<bool> HasCompletedJobForSyllabusAsync(Guid syllabusId)
    {
        return await _dbContext.GenerationJobs.AnyAsync(job =>
            job.SyllabusId == syllabusId && job.Status == "Completed");
    }

    public async Task<bool> HasRunningLessonContentJobForCourseAsync(Guid courseId)
    {
        return await _dbContext.GenerationJobs.AnyAsync(job =>
            job.CourseId == courseId &&
            (job.JobType == "GenerateLessonContent" || job.JobType == "RegenerateLessonContent") &&
            (job.Status == "Pending" || job.Status == "GeneratingLessonContent" || job.Status == "RegeneratingLessonContent"));
    }

    public async Task<bool> HasRunningLessonAudioJobForCourseAsync(Guid courseId)
    {
        return await _dbContext.GenerationJobs.AnyAsync(job =>
            job.CourseId == courseId &&
            (job.JobType == "GenerateLessonAudio" || job.JobType == "RegenerateLessonAudio") &&
            (job.Status == "Pending" || job.Status == "GeneratingLessonAudio"));
    }

    public async Task<bool> HasRunningLessonVideoJobForCourseAsync(Guid courseId)
    {
        return await _dbContext.GenerationJobs.AnyAsync(job =>
            job.CourseId == courseId &&
            (job.JobType == "GenerateLessonVideo" || job.JobType == "RegenerateLessonVideo") &&
            (job.Status == "Pending" || job.Status == "GeneratingLessonVideo"));
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
