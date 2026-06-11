using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class QuizRepository : IQuizRepository
{
    private readonly AppDbContext _dbContext;

    public QuizRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Quiz?> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(x => x.LessonId == lessonId, cancellationToken);

    public Task<Quiz?> GetCourseFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(x => x.CourseId == courseId && x.Type == "Final", cancellationToken);

    public Task<Quiz?> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.Options.OrderBy(o => o.OrderIndex))
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == quizId, cancellationToken);

    public Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes.AddAsync(quiz, cancellationToken).AsTask();

    public Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default) =>
        _dbContext.QuizAttempts.AddAsync(attempt, cancellationToken).AsTask();

    public Task AddAttemptAnswersAsync(IReadOnlyCollection<QuizAttemptAnswer> answers, CancellationToken cancellationToken = default) =>
        _dbContext.QuizAttemptAnswers.AddRangeAsync(answers, cancellationToken);

    public async Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.QuizAttempts
            .Include(x => x.Answers)
            .Where(x => x.QuizId == quizId && x.UserId == userId)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
