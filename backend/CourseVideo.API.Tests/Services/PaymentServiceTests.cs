using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateOrdersAsync_ShouldKeepCartItems_WhenOrderIsCreatedButNotPaid()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);

        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        dbContext.Categories.Add(new Category
        {
            Id = categoryId,
            Name = "Development"
        });

        dbContext.Courses.Add(new Course
        {
            Id = courseId,
            Title = "Lập trình Mobile",
            Description = "Khóa học mobile",
            CategoryId = categoryId,
            IsPublished = true,
            Price = 2000
        });

        dbContext.CartItems.Add(new CartItem
        {
            UserId = userId,
            CourseId = courseId
        });

        await dbContext.SaveChangesAsync();

        var service = new PaymentService(
            dbContext,
            Mock.Of<IHttpClientFactory>(),
            Options.Create(new SepayOptions
            {
                BankCode = "TPBANK",
                BankName = "TPBank",
                BankAccountNumber = "10004125521",
                AccountHolderName = "DINH NGUYEN TUAN KIET",
                StoreName = "VibeCourseAI"
            }));

        var orders = await service.CreateOrdersAsync(userId, [courseId]);

        orders.Should().ContainSingle();
        orders[0].Status.Should().Be("Pending");
        dbContext.CartItems.Should().ContainSingle(item => item.UserId == userId && item.CourseId == courseId);
    }
}
