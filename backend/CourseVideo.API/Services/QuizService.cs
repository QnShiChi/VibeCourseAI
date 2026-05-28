using CourseVideo.API.DTOs.Quizzes;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class QuizService : IQuizService
{
    private readonly IQuizRepository _quizRepository;

    public QuizService(IQuizRepository quizRepository)
    {
        _quizRepository = quizRepository;
    }

    public async Task<QuizResponse?> GetLessonQuizAsync(Guid lessonId, Guid userId, bool canPreviewDraft, CancellationToken cancellationToken = default)
    {
        var quiz = await _quizRepository.GetLessonQuizAsync(lessonId, cancellationToken);
        return quiz is null ? null : MapQuiz(quiz);
    }

    public async Task<QuizResponse?> GetFinalQuizAsync(Guid courseId, Guid userId, bool canPreviewDraft, CancellationToken cancellationToken = default)
    {
        var quiz = await _quizRepository.GetCourseFinalQuizAsync(courseId, cancellationToken);
        return quiz is null ? null : MapQuiz(quiz);
    }

    public async Task<CreateQuizAttemptResponse> StartAttemptAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default)
    {
        var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken)
            ?? throw new KeyNotFoundException("Khong tim thay quiz.");

        var attempt = new Models.QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = quiz.Questions.Count,
            CreatedAt = DateTime.UtcNow
        };

        await _quizRepository.AddAttemptAsync(attempt, cancellationToken);
        await _quizRepository.SaveChangesAsync(cancellationToken);

        return new CreateQuizAttemptResponse
        {
            AttemptId = attempt.Id,
            StartedAt = attempt.StartedAt
        };
    }

    public async Task<SubmitQuizAttemptResponse> SubmitAttemptAsync(Guid quizId, Guid attemptId, Guid userId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken)
            ?? throw new KeyNotFoundException("Khong tim thay quiz.");

        var attempt = quiz.Attempts.FirstOrDefault(item => item.Id == attemptId && item.UserId == userId)
            ?? throw new KeyNotFoundException("Khong tim thay luot lam quiz.");

        var answerResults = quiz.Questions.Select(question =>
        {
            var submitted = request.Answers.FirstOrDefault(item => item.QuestionId == question.Id)
                ?? throw new InvalidOperationException("Thieu cau tra loi cho quiz.");
            var correctOption = question.Options.Single(item => item.IsCorrect);
            var isCorrect = submitted.SelectedOptionId == correctOption.Id;

            return new SubmitQuizAttemptAnswerResultResponse
            {
                QuestionId = question.Id,
                SelectedOptionId = submitted.SelectedOptionId,
                CorrectOptionId = correctOption.Id,
                IsCorrect = isCorrect,
                Explanation = question.Explanation
            };
        }).ToList();

        attempt.Answers = answerResults.Select(result => new Models.QuizAttemptAnswer
        {
            Id = Guid.NewGuid(),
            QuizAttemptId = attempt.Id,
            QuizQuestionId = result.QuestionId,
            SelectedOptionId = result.SelectedOptionId,
            IsCorrect = result.IsCorrect,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.CorrectCount = answerResults.Count(item => item.IsCorrect);
        attempt.TotalQuestions = quiz.Questions.Count;
        attempt.Score = quiz.Questions.Count == 0 ? 0 : Math.Round((decimal)attempt.CorrectCount * 100 / quiz.Questions.Count, 2);
        attempt.UpdatedAt = DateTime.UtcNow;

        await _quizRepository.SaveChangesAsync(cancellationToken);

        return new SubmitQuizAttemptResponse
        {
            AttemptId = attempt.Id,
            Score = attempt.Score,
            CorrectCount = attempt.CorrectCount,
            TotalQuestions = attempt.TotalQuestions,
            Answers = answerResults
        };
    }

    public async Task<IReadOnlyList<QuizAttemptHistoryItemResponse>> GetAttemptHistoryAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default)
    {
        var attempts = await _quizRepository.GetAttemptsAsync(quizId, userId, cancellationToken);
        return attempts.Select(attempt => new QuizAttemptHistoryItemResponse
        {
            AttemptId = attempt.Id,
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt,
            Score = attempt.Score,
            CorrectCount = attempt.CorrectCount,
            TotalQuestions = attempt.TotalQuestions
        }).ToList();
    }

    private static QuizResponse MapQuiz(Models.Quiz quiz)
    {
        return new QuizResponse
        {
            QuizId = quiz.Id,
            Title = quiz.Title,
            Status = quiz.Status,
            QuestionCount = quiz.Questions.Count,
            Questions = quiz.Questions
                .OrderBy(question => question.OrderIndex)
                .Select(question => new QuizQuestionResponse
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    Explanation = question.Explanation,
                    Options = question.Options
                        .OrderBy(option => option.OrderIndex)
                        .Select(option => new QuizOptionResponse
                        {
                            OptionId = option.Id,
                            OptionText = option.OptionText
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
