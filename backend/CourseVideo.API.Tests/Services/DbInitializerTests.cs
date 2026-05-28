using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class DbInitializerTests
{
    [Fact]
    public void Initialize_ShouldCreateAdminUserFromConfiguration_WhenMissing()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AppDbContext(options);
        var adminOptions = Options.Create(new AdminSeedOptions
        {
            FullName = "Seeded Admin",
            Email = "seeded-admin@example.com",
            Password = "ChangeMe@123"
        });

        DbInitializer.Initialize(dbContext, adminOptions);

        dbContext.Users.Should().ContainSingle(user => user.Email == "seeded-admin@example.com");
    }

    [Fact]
    public void AppDbContext_ShouldConfigureCourseQuizIndex_AsUniqueOnlyForFinalQuiz()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AppDbContext(options);
        var quizEntity = dbContext.Model.FindEntityType(typeof(Quiz));

        var courseIdIndex = quizEntity!
            .GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name).SequenceEqual(["CourseId"]));

        courseIdIndex.IsUnique.Should().BeTrue();
        courseIdIndex.GetFilter().Should().Be("[CourseId] IS NOT NULL AND [Type] = 'Final'");
    }
}
