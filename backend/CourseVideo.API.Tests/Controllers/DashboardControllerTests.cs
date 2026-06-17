using CourseVideo.API.Controllers;
using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Dashboard;
using CourseVideo.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetStats_ShouldIncludeNegativeCommentQueueFields()
    {
        await using var dbContext = BuildDbContext();
        SeedDashboardEntities(dbContext);
        SeedComment(dbContext, sentiment: "negative", isHidden: false, deletedAt: null, createdAt: DateTime.UtcNow);

        var controller = new DashboardController(dbContext);

        var result = await controller.GetStats();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<DashboardStatsResponse>().Subject;
        response.NegativeCommentsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStats_ShouldReturnOnlyVisibleNegativeCommentsOrderedNewestFirst()
    {
        await using var dbContext = BuildDbContext();
        SeedDashboardEntities(dbContext);

        var olderVisibleNegative = SeedComment(
            dbContext,
            sentiment: "negative",
            isHidden: false,
            deletedAt: null,
            createdAt: DateTime.UtcNow.AddMinutes(-2),
            content: "Older negative comment");

        SeedComment(
            dbContext,
            sentiment: "negative",
            isHidden: true,
            deletedAt: null,
            createdAt: DateTime.UtcNow.AddMinutes(-1),
            content: "Hidden negative comment");

        SeedComment(
            dbContext,
            sentiment: "normal",
            isHidden: false,
            deletedAt: null,
            createdAt: DateTime.UtcNow,
            content: "Normal comment");

        SeedComment(
            dbContext,
            sentiment: "negative",
            isHidden: false,
            deletedAt: DateTime.UtcNow,
            createdAt: DateTime.UtcNow,
            content: "Deleted negative comment");

        var newestVisibleNegative = SeedComment(
            dbContext,
            sentiment: "negative",
            isHidden: false,
            deletedAt: null,
            createdAt: DateTime.UtcNow.AddMinutes(1),
            content: "Newest negative comment");

        var controller = new DashboardController(dbContext);

        var result = await controller.GetStats();

        var response = ((OkObjectResult)result).Value.Should().BeOfType<DashboardStatsResponse>().Subject;
        response.NegativeCommentsCount.Should().Be(2);
        newestVisibleNegative.Sentiment.Should().Be("negative");
        olderVisibleNegative.Sentiment.Should().Be("negative");
    }

    [Fact]
    public async Task GetPaymentOverview_ShouldAggregateStatusesAndReturnRecentOrders()
    {
        await using var dbContext = BuildDbContext();
        SeedDashboardEntities(dbContext);
        var user = dbContext.Users.Single();
        var course = dbContext.Courses.Single();

        dbContext.PaymentOrders.AddRange(
            new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CourseId = course.Id,
                Amount = 3000,
                Status = "Paid",
                OrderCode = "VCPAID001",
                CreatedAt = new DateTime(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc),
                PaidAt = new DateTime(2026, 6, 16, 8, 5, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 16, 8, 15, 0, DateTimeKind.Utc)
            },
            new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CourseId = course.Id,
                Amount = 3000,
                Status = "Pending",
                OrderCode = "VCPENDING001",
                CreatedAt = new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 16, 9, 15, 0, DateTimeKind.Utc)
            },
            new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CourseId = course.Id,
                Amount = 3000,
                Status = "Expired",
                OrderCode = "VCEXPIRED001",
                CreatedAt = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 16, 10, 15, 0, DateTimeKind.Utc)
            },
            new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CourseId = course.Id,
                Amount = 3000,
                Status = "Cancelled",
                OrderCode = "VCCANCEL001",
                CreatedAt = new DateTime(2026, 6, 16, 10, 30, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 16, 10, 45, 0, DateTimeKind.Utc)
            },
            new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CourseId = course.Id,
                Amount = 3000,
                Status = "LatePaid",
                OrderCode = "VCLATE001",
                CreatedAt = new DateTime(2026, 6, 16, 11, 0, 0, DateTimeKind.Utc),
                PaidAt = new DateTime(2026, 6, 16, 11, 25, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 16, 11, 15, 0, DateTimeKind.Utc)
            });
        await dbContext.SaveChangesAsync();

        var controller = new DashboardController(dbContext);

        var result = await controller.GetPaymentOverview();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<DashboardPaymentOverviewResponse>().Subject;
        response.TotalOrders.Should().Be(4);
        response.PaidOrders.Should().Be(2);
        response.PendingOrders.Should().Be(0);
        response.FailedOrExpiredOrders.Should().Be(2);
        response.RecentOrders.Should().HaveCount(4);
        response.RecentOrders[0].OrderCode.Should().Be("VCLATE001");
        response.RecentOrders[1].OrderCode.Should().Be("VCCANCEL001");
        response.RecentOrders[2].OrderCode.Should().Be("VCEXPIRED001");
        response.RecentOrders[3].OrderCode.Should().Be("VCPAID001");
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

    private static void SeedDashboardEntities(AppDbContext dbContext)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "AI Category",
            Description = "Category for dashboard tests",
            Status = CategoryStatus.Visible,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Learner",
            Email = "learner@example.com",
            PasswordHash = "hashed-password",
            RoleId = 2,
            CreatedAt = DateTime.UtcNow
        };

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Course for Dashboard",
            Description = "Course description",
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

        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            Title = "Syllabus title",
            Description = "Syllabus description",
            OriginalFileName = "syllabus.pdf",
            StoredFileName = "syllabus.pdf",
            FilePath = "/tmp/syllabus.pdf",
            FileType = "application/pdf",
            ExtractedText = "Extracted text",
            UploadedByUserId = user.Id,
            UploadedByUser = user,
            CreatedAt = DateTime.UtcNow
        };

        var generationJob = new GenerationJob
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            CreatedByUserId = user.Id,
            CreatedByUser = user,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        dbContext.Users.Add(user);
        dbContext.Courses.Add(course);
        dbContext.Modules.Add(module);
        dbContext.Lessons.Add(lesson);
        dbContext.Syllabuses.Add(syllabus);
        dbContext.GenerationJobs.Add(generationJob);
        dbContext.SaveChanges();
    }

    private static LessonComment SeedComment(
        AppDbContext dbContext,
        string sentiment,
        bool isHidden,
        DateTime? deletedAt,
        DateTime createdAt,
        string content = "Negative comment")
    {
        var lesson = dbContext.Lessons.Single();
        var user = dbContext.Users.Single();

        var comment = new LessonComment
        {
            Id = Guid.NewGuid(),
            LessonId = lesson.Id,
            Lesson = lesson,
            UserId = user.Id,
            User = user,
            Content = content,
            Sentiment = sentiment,
            IsHidden = isHidden,
            DeletedAt = deletedAt,
            CreatedAt = createdAt
        };

        dbContext.LessonComments.Add(comment);
        dbContext.SaveChanges();
        return comment;
    }
}
