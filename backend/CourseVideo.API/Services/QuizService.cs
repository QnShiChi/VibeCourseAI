using CourseVideo.API.DTOs.Quizzes;
using CourseVideo.API.Repositories.Interfaces;

namespace CourseVideo.API.Services;

public class QuizService
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
