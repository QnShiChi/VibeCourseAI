using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class CourseGenerationServiceTests
{
    [Fact]
    public async Task GenerateFromSyllabusAsync_CreatesJobCourseModulesAndLessons_FromOpenRouter_WhenSyllabusIsValid()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "Lap trinh huong doi tuong",
            Description = "Mo ta khoa hoc",
            ExtractedText = "Noi dung de cuong"
        };
        GenerationJob? capturedJob = null;
        Course? capturedCourse = null;
        List<Module>? capturedModules = null;
        List<Lesson>? capturedLessons = null;

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabus.Id)).ReturnsAsync(syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>()))
            .Callback<GenerationJob>(job => capturedJob = job)
            .Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.AddAsync(It.IsAny<Course>()))
            .Callback<Course>(course => capturedCourse = course)
            .Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Module>>()))
            .Callback<IReadOnlyCollection<Module>>(items => capturedModules = items.ToList())
            .Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Lesson>>()))
            .Callback<IReadOnlyCollection<Lesson>>(items => capturedLessons = items.ToList())
            .Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        openRouterService.Setup(x => x.GenerateStructureAsync(syllabus.ExtractedText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedCourseStructure
            {
                CourseTitle = "Lap trinh huong doi tuong AI",
                CourseDescription = "Mo ta do AI sinh",
                Modules =
                [
                    new ParsedModuleStructure
                    {
                        Title = "Chuong 1",
                        Description = "Tong quan",
                        Lessons =
                        [
                            new ParsedLessonStructure
                            {
                                Title = "Bai 1",
                                Description = "Mo dau",
                                ContentSeed = "Hat giong noi dung"
                            }
                        ]
                    }
                ]
            });

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);

        var result = await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        result.Status.Should().Be("Completed");
        result.CourseTitle.Should().Be("Lap trinh huong doi tuong AI");
        result.CourseId.Should().NotBeNull();
        capturedJob.Should().NotBeNull();
        capturedJob!.Status.Should().Be("Completed");
        capturedJob.CourseId.Should().Be(result.CourseId);
        capturedCourse.Should().NotBeNull();
        capturedCourse!.SyllabusId.Should().Be(syllabus.Id);
        capturedCourse.Title.Should().Be("Lap trinh huong doi tuong AI");
        capturedCourse.Description.Should().Be("Mo ta do AI sinh");
        capturedCourse.IsPublished.Should().BeFalse();
        capturedModules.Should().NotBeNullOrEmpty();
        capturedLessons.Should().NotBeNullOrEmpty();
        capturedLessons.Should().OnlyContain(lesson => !string.IsNullOrWhiteSpace(lesson.ContentSeed));
        parser.Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_Throws_WhenSyllabusHasRunningJob()
    {
        var service = CreateServiceForStateChecks(out var syllabusRepository, out var generationJobRepository, out var syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(true);

        var action = async () => await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Đề cương này đang có job generate đang chạy.");
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_Throws_WhenSyllabusAlreadyGeneratedSuccessfully()
    {
        var service = CreateServiceForStateChecks(out var syllabusRepository, out var generationJobRepository, out var syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(true);

        var action = async () => await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Đề cương này đã được generate khóa học thành công rồi.");
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_ThrowsKeyNotFound_WhenSyllabusDoesNotExist()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabusId = Guid.NewGuid();

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabusId)).ReturnsAsync((Syllabus?)null);

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);
        var action = async () => await service.GenerateFromSyllabusAsync(syllabusId, Guid.NewGuid(), "Admin User");

        await action.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Không tìm thấy đề cương.");
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_MarksJobFailed_WhenModuleSaveThrows()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "AI",
            Description = "",
            ExtractedText = "Noi dung hop le"
        };
        GenerationJob? capturedJob = null;

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabus.Id)).ReturnsAsync(syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>()))
            .Callback<GenerationJob>(job => capturedJob = job)
            .Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.AddAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Module>>())).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new Exception("module save fail"));
        openRouterService.Setup(x => x.GenerateStructureAsync(syllabus.ExtractedText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedCourseStructure
            {
                CourseTitle = "AI",
                CourseDescription = "Mo ta do AI sinh",
                Modules =
                [
                    new ParsedModuleStructure
                    {
                        Title = "Chuong 1",
                        Description = "Tong quan",
                        Lessons =
                        [
                            new ParsedLessonStructure
                            {
                                Title = "Bai 1",
                                Description = "Mo dau",
                                ContentSeed = "Hat giong noi dung"
                            }
                        ]
                    }
                ]
            });

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);
        var action = async () => await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Không thể tạo khóa học từ đề cương.");
        capturedJob.Should().NotBeNull();
        capturedJob!.Status.Should().Be("Failed");
        capturedJob.ErrorMessage.Should().Be("module save fail");
        capturedJob.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_FallsBackToParser_WhenOpenRouterTechnicalFailureOccurs()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "OOP",
            Description = "Mo ta goc",
            ExtractedText = "Noi dung de cuong"
        };
        Course? capturedCourse = null;

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabus.Id)).ReturnsAsync(syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>())).Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.AddAsync(It.IsAny<Course>()))
            .Callback<Course>(course => capturedCourse = course)
            .Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Module>>())).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Lesson>>())).Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        openRouterService.Setup(x => x.GenerateStructureAsync(syllabus.ExtractedText, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OpenRouterTechnicalException("timeout"));
        parser.Setup(x => x.Parse(syllabus.ExtractedText)).Returns(new ParsedCourseStructure
        {
            Modules =
            [
                new ParsedModuleStructure
                {
                    Title = "Chuong 1",
                    Description = "Tong quan",
                    Lessons =
                    [
                        new ParsedLessonStructure
                        {
                            Title = "Bai 1",
                            Description = "Mo dau",
                            ContentSeed = "Noi dung fallback"
                        }
                    ]
                }
            ]
        });

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);

        var result = await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        result.Status.Should().Be("Completed");
        capturedCourse.Should().NotBeNull();
        capturedCourse!.Title.Should().Be("OOP");
        parser.Verify(x => x.Parse(syllabus.ExtractedText), Times.Once);
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_Fails_WhenOpenRouterReturnsSchemaInvalidOutput()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "OOP",
            Description = "Mo ta goc",
            ExtractedText = "Noi dung de cuong"
        };
        GenerationJob? capturedJob = null;

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabus.Id)).ReturnsAsync(syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>()))
            .Callback<GenerationJob>(job => capturedJob = job)
            .Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        openRouterService.Setup(x => x.GenerateStructureAsync(syllabus.ExtractedText, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OpenRouterValidationException("schema invalid"));

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);
        var action = async () => await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("schema invalid");
        parser.Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
        capturedJob.Should().NotBeNull();
        capturedJob!.Status.Should().Be("Failed");
        capturedJob.ErrorMessage.Should().Be("schema invalid");
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_FallsBackToParser_WhenOpenRouterReturnsSparseLessonValidation()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "OOP",
            Description = "Mo ta goc",
            ExtractedText = "Noi dung de cuong"
        };

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabus.Id)).ReturnsAsync(syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>())).Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.AddAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Module>>())).Returns(Task.CompletedTask);
        moduleRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyCollection<Lesson>>())).Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        openRouterService.Setup(x => x.GenerateStructureAsync(syllabus.ExtractedText, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OpenRouterValidationException("OpenRouter tra ve cau truc qua it lesson cho de cuong dau vao."));
        parser.Setup(x => x.Parse(syllabus.ExtractedText)).Returns(new ParsedCourseStructure
        {
            Modules =
            [
                new ParsedModuleStructure
                {
                    Title = "Tuan 1",
                    Description = "Tong quan",
                    Lessons =
                    [
                        new ParsedLessonStructure
                        {
                            Title = "Buoi 1",
                            Description = "Mo dau",
                            ContentSeed = "Noi dung fallback"
                        }
                    ]
                }
            ]
        });

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);

        var result = await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        result.Status.Should().Be("Completed");
        parser.Verify(x => x.Parse(syllabus.ExtractedText), Times.Once);
    }

    [Fact]
    public async Task GenerateFromSyllabusAsync_FailsWithSpecificMessage_WhenOpenRouterConfigMissing()
    {
        var syllabusRepository = new Mock<ISyllabusRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "OOP",
            Description = "Mo ta goc",
            ExtractedText = "Noi dung de cuong"
        };

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(syllabus.Id)).ReturnsAsync(syllabus);
        generationJobRepository.Setup(x => x.HasRunningJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.HasCompletedJobForSyllabusAsync(syllabus.Id)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>())).Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        openRouterService.Setup(x => x.GenerateStructureAsync(syllabus.ExtractedText, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OpenRouterConfigurationException("Thiếu cấu hình OPENROUTER_API_KEY."));

        var service = new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);
        var action = async () => await service.GenerateFromSyllabusAsync(syllabus.Id, Guid.NewGuid(), "Admin User");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Thiếu cấu hình OPENROUTER_API_KEY.");
        parser.Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
    }

    private static CourseGenerationService CreateServiceForStateChecks(
        out Mock<ISyllabusRepository> syllabusRepository,
        out Mock<IGenerationJobRepository> generationJobRepository,
        out Syllabus syllabus)
    {
        syllabusRepository = new Mock<ISyllabusRepository>();
        generationJobRepository = new Mock<IGenerationJobRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var categoryRepository = CreateCategoryRepository();
        var moduleRepository = new Mock<IModuleRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var openRouterService = new Mock<IOpenRouterCourseStructureService>();
        var parser = new Mock<ICourseStructureParser>();
        var cancellationTracker = new Mock<IJobCancellationTracker>();
        syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "Web",
            Description = "Mo ta",
            ExtractedText = "Text"
        };
        var seededSyllabus = syllabus;

        syllabusRepository.Setup(x => x.GetEntityByIdAsync(seededSyllabus.Id)).ReturnsAsync(seededSyllabus);

        return new CourseGenerationService(
            syllabusRepository.Object,
            generationJobRepository.Object,
            courseRepository.Object,
            categoryRepository.Object,
            moduleRepository.Object,
            lessonRepository.Object,
            openRouterService.Object,
            parser.Object,
            cancellationTracker.Object);
    }

    private static Mock<ICategoryRepository> CreateCategoryRepository()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(x => x.GetDefaultForAssignmentAsync())
            .ReturnsAsync(new Category
            {
                Id = Guid.NewGuid(),
                Name = "Development",
                Status = CategoryStatus.Visible
            });
        return categoryRepository;
    }
}
