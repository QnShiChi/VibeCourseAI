using CourseVideo.API.Controllers;
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class AdminCommentsControllerTests
{
    [Fact]
    public async Task GetComments_ShouldFilterBySentimentAndAuthorName()
    {
        await using var dbContext = BuildDbContext();
        SeedModerationEntities(dbContext);

        SeedComment(dbContext, "negative", "Duy Duong Van", "Can xu ly");
        SeedComment(dbContext, "positive", "Another User", "Rat huu ich");
        SeedComment(dbContext, "negative", "Nguyen Van A", "Khac ten");

        var controller = BuildController(dbContext);

        var result = await controller.GetComments("negative", "duy");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var comments = ok.Value.Should().BeAssignableTo<IReadOnlyList<object>>().Subject;
        comments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetComments_ShouldReturnPinnedPositiveCommentsFirst()
    {
        await using var dbContext = BuildDbContext();
        SeedModerationEntities(dbContext);

        var olderPinned = SeedComment(dbContext, "positive", "Positive User", "Older pinned", DateTime.UtcNow.AddMinutes(-5));
        olderPinned.PinnedAt = DateTime.UtcNow.AddMinutes(-1);

        var newestUnpinned = SeedComment(dbContext, "positive", "Positive User", "Newest unpinned", DateTime.UtcNow);
        dbContext.SaveChanges();

        var controller = BuildController(dbContext);

        var result = await controller.GetComments("positive", null);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        dynamic first = ok.Value.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject.Cast<object>().First();
        Assert.Equal("Older pinned", (string)first.Content);
        Assert.Equal("positive", (string)first.Sentiment);
        Assert.NotNull(first.PinnedAt);
        Assert.NotEqual(Guid.Empty, newestUnpinned.Id);
    }

    [Fact]
    public async Task PinComment_ShouldSetPinnedAt_ForPositiveComment()
    {
        await using var dbContext = BuildDbContext();
        SeedModerationEntities(dbContext);

        var comment = SeedComment(dbContext, "positive", "Positive User", "Hay qua");
        var controller = BuildController(dbContext);

        var result = await controller.PinComment(comment.Id);

        result.Should().BeOfType<NoContentResult>();
        dbContext.LessonComments.Single(item => item.Id == comment.Id).PinnedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPositiveCourseHighlights_ShouldReturnAllCoursesOrderedByPositiveRatio()
    {
        await using var dbContext = BuildDbContext();
        var context = SeedModerationEntities(dbContext);

        SeedComment(dbContext, "positive", "User A", "Khoa hoc 1 rat hay", DateTime.UtcNow.AddMinutes(-5), context.firstLesson.Id);
        SeedComment(dbContext, "positive", "User B", "Rat de hieu", DateTime.UtcNow.AddMinutes(-4), context.firstLesson.Id);
        SeedComment(dbContext, "negative", "User C", "Van co diem tru", DateTime.UtcNow.AddMinutes(-3), context.secondLesson.Id);
        SeedComment(dbContext, "positive", "User D", "Khoa hoc 2 on", DateTime.UtcNow.AddMinutes(-2), context.thirdLesson.Id);
        SeedComment(dbContext, "negative", "User E", "Khoa hoc 2 can cai thien", DateTime.UtcNow.AddMinutes(-1), context.thirdLesson.Id);

        var controller = BuildController(dbContext);

        var result = await controller.GetPositiveCourseHighlights();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        dynamic first = ok.Value.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject.Cast<object>().First();
        Assert.Equal(context.firstCourse.Title, (string)first.CourseTitle);
        Assert.Equal(3, (int)first.TotalCommentCount);
        Assert.Equal(2, (int)first.PositiveCommentCount);
        Assert.Equal(2d / 3d, (double)first.PositiveRatio);
    }

    private static AdminCommentsController BuildController(AppDbContext dbContext)
    {
        var lessonCommentService = new Mock<ILessonCommentService>();
        return new AdminCommentsController(lessonCommentService.Object, dbContext);
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private static (Course firstCourse, Course secondCourse, Lesson firstLesson, Lesson secondLesson, Lesson thirdLesson) SeedModerationEntities(AppDbContext dbContext)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "AI Category",
            Description = "Category for moderation tests",
            Status = CategoryStatus.Visible,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Course for moderation",
            Description = "Course description",
            CategoryId = category.Id,
            Category = category,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        var secondCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Second course for moderation",
            Description = "Second course description",
            CategoryId = category.Id,
            Category = category,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        var module = new Module
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Course = course,
            Title = "Module 1",
            Description = "Module description",
            OrderIndex = 1,
            CreatedAt = DateTime.UtcNow
        };

        var secondModule = new Module
        {
            Id = Guid.NewGuid(),
            CourseId = secondCourse.Id,
            Course = secondCourse,
            Title = "Module 2",
            Description = "Module description 2",
            OrderIndex = 1,
            CreatedAt = DateTime.UtcNow
        };

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Module = module,
            Title = "Lesson 1",
            Description = "Lesson description",
            ContentSeed = "Seed content",
            OrderIndex = 1,
            ContentGenerationStatus = "Done",
            CreatedAt = DateTime.UtcNow
        };

        var secondLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Module = module,
            Title = "Lesson 2",
            Description = "Lesson description 2",
            ContentSeed = "Seed content 2",
            OrderIndex = 2,
            ContentGenerationStatus = "Done",
            CreatedAt = DateTime.UtcNow
        };

        var thirdLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = secondModule.Id,
            Module = secondModule,
            Title = "Lesson 3",
            Description = "Lesson description 3",
            ContentSeed = "Seed content 3",
            OrderIndex = 1,
            ContentGenerationStatus = "Done",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        dbContext.Courses.Add(course);
        dbContext.Courses.Add(secondCourse);
        dbContext.Modules.Add(module);
        dbContext.Modules.Add(secondModule);
        dbContext.Lessons.Add(lesson);
        dbContext.Lessons.Add(secondLesson);
        dbContext.Lessons.Add(thirdLesson);
        dbContext.SaveChanges();
        return (course, secondCourse, lesson, secondLesson, thirdLesson);
    }

    private static LessonComment SeedComment(
        AppDbContext dbContext,
        string sentiment,
        string authorName,
        string content,
        DateTime? createdAt = null,
        Guid? lessonId = null)
    {
        var lesson = lessonId.HasValue
            ? dbContext.Lessons.Single(item => item.Id == lessonId.Value)
            : dbContext.Lessons.OrderBy(item => item.OrderIndex).First();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = authorName,
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hashed-password",
            RoleId = 2,
            CreatedAt = DateTime.UtcNow
        };

        var comment = new LessonComment
        {
            Id = Guid.NewGuid(),
            LessonId = lesson.Id,
            Lesson = lesson,
            UserId = user.Id,
            User = user,
            Content = content,
            Sentiment = sentiment,
            IsHidden = false,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.LessonComments.Add(comment);
        dbContext.SaveChanges();
        return comment;
    }
}
