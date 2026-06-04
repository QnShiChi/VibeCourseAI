using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class QuizGenerationService : IQuizGenerationService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IQuizRepository _quizRepository;
    private readonly IOpenRouterQuizGenerationService _openRouterQuizGenerationService;

    public QuizGenerationService(
        ICourseRepository courseRepository,
        IQuizRepository quizRepository,
        IOpenRouterQuizGenerationService openRouterQuizGenerationService)
    {
        _courseRepository = courseRepository;
        _quizRepository = quizRepository;
        _openRouterQuizGenerationService = openRouterQuizGenerationService;
    }

    public async Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Khong tim thay khoa hoc.");

        var module = course.Modules.FirstOrDefault(item => item.Lessons.Any(lesson => lesson.Id == lessonId))
            ?? throw new KeyNotFoundException("Khong tim thay module cua lesson.");
        var lesson = module.Lessons.First(item => item.Id == lessonId);

        var generated = await _openRouterQuizGenerationService.GenerateLessonQuizAsync(course, module, lesson, cancellationToken);
        var existingQuiz = await _quizRepository.GetLessonQuizAsync(lessonId, cancellationToken);
        var quiz = existingQuiz ?? new Quiz
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            CourseId = courseId,
            Type = "Lesson",
            CreatedAt = DateTime.UtcNow
        };

        ApplyGeneratedQuiz(quiz, generated);

        if (existingQuiz is null)
        {
            await _quizRepository.AddAsync(quiz, cancellationToken);
        }

        await _quizRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Khong tim thay khoa hoc.");
        var lessons = course.Modules
            .OrderBy(module => module.OrderIndex)
            .SelectMany(module => module.Lessons.OrderBy(lesson => lesson.OrderIndex))
            .ToList();

        var generated = await _openRouterQuizGenerationService.GenerateFinalQuizAsync(course, lessons, cancellationToken);
        var existingQuiz = await _quizRepository.GetCourseFinalQuizAsync(courseId, cancellationToken);
        var quiz = existingQuiz ?? new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Type = "Final",
            CreatedAt = DateTime.UtcNow
        };

        ApplyGeneratedQuiz(quiz, generated);

        if (existingQuiz is null)
        {
            await _quizRepository.AddAsync(quiz, cancellationToken);
        }

        await _quizRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RegenerateQuizAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken)
            ?? throw new KeyNotFoundException("Khong tim thay quiz.");

        if (quiz.Type == "Lesson" && quiz.CourseId.HasValue && quiz.LessonId.HasValue)
        {
            await GenerateLessonQuizAsync(quiz.CourseId.Value, quiz.LessonId.Value, cancellationToken);
            return;
        }

        if (quiz.Type == "Final" && quiz.CourseId.HasValue)
        {
            await GenerateFinalQuizAsync(quiz.CourseId.Value, cancellationToken);
            return;
        }

        throw new InvalidOperationException("Quiz khong co target de regenerate.");
    }

    private static void ApplyGeneratedQuiz(Quiz quiz, DTOs.OpenRouter.OpenRouterQuizGenerationResult generated)
    {
        quiz.Title = generated.Title;
        quiz.Status = "Ready";
        quiz.QuestionCount = generated.Questions.Count;
        quiz.GenerationError = null;
        quiz.LastGeneratedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        quiz.Questions = generated.Questions.Select((question, index) => new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            QuestionText = question.QuestionText,
            Explanation = question.Explanation,
            OrderIndex = index + 1,
            CreatedAt = DateTime.UtcNow,
            Options = question.Options.Select((option, optionIndex) => new QuizOption
            {
                Id = Guid.NewGuid(),
                OptionText = option.OptionText,
                OrderIndex = optionIndex + 1,
                IsCorrect = option.IsCorrect,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        }).ToList();
    }
}
