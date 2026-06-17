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
    public void ParseVietnamTime_ShouldConvertVietnamLocalTimeToUtc()
    {
        var result = PaymentService.ParseVietnamTime("2026-06-15 17:55:00");

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 15, 10, 55, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetCartAsync_ShouldRemoveOwnedCoursesFromUserCart()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);

        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var paymentOrderId = Guid.NewGuid();

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

        dbContext.PaymentOrders.Add(new PaymentOrder
        {
            Id = paymentOrderId,
            UserId = userId,
            CourseId = courseId,
            Amount = 2000,
            Status = "Paid",
            OrderCode = "VCTEST001",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            PaidAt = DateTime.UtcNow
        });

        dbContext.CourseEnrollments.Add(new CourseEnrollment
        {
            UserId = userId,
            CourseId = courseId,
            PaymentOrderId = paymentOrderId,
            GrantedAt = DateTime.UtcNow
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
            Options.Create(new SepayOptions()));

        var response = await service.GetCartAsync(userId, null);

        response.Items.Should().BeEmpty();
        dbContext.CartItems.Should().BeEmpty();
    }

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

    [Fact]
    public async Task CancelOrderAsync_ShouldMarkPendingOrderAsCancelled()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);

        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

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

        dbContext.PaymentOrders.Add(new PaymentOrder
        {
            Id = orderId,
            UserId = userId,
            CourseId = courseId,
            Amount = 2000,
            Status = "Pending",
            OrderCode = "VCCANCEL001",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });

        await dbContext.SaveChangesAsync();

        var service = new PaymentService(
            dbContext,
            Mock.Of<IHttpClientFactory>(),
            Options.Create(new SepayOptions()));

        var response = await service.CancelOrderAsync(userId, orderId, isAdmin: false);

        response.Should().NotBeNull();
        response!.Status.Should().Be("Cancelled");
        dbContext.PaymentOrders.Single(item => item.Id == orderId).Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task HandleSepayWebhookAsync_ShouldIgnoreCancelledOrders()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);

        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

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

        dbContext.PaymentOrders.Add(new PaymentOrder
        {
            Id = orderId,
            UserId = userId,
            CourseId = courseId,
            Amount = 2000,
            Status = "Cancelled",
            OrderCode = "VCCANCEL002",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            BankAccountNumber = "10004125521",
            BankName = "TPBank",
            TransferContent = "VCCANCEL002"
        });

        await dbContext.SaveChangesAsync();

        var service = new PaymentService(
            dbContext,
            Mock.Of<IHttpClientFactory>(),
            Options.Create(new SepayOptions()));

        await service.HandleSepayWebhookAsync(
            new DTOs.Payments.SepayWebhookPayload
            {
                Id = 777,
                Gateway = "TPBank",
                TransactionDate = "2026-06-16 22:00:00",
                AccountNumber = "10004125521",
                Content = "VCCANCEL002",
                TransferType = "in",
                TransferAmount = 2000
            },
            rawPayload: "{}",
            apiKeyHeader: null,
            validateWebhookCredential: false);

        dbContext.PaymentOrders.Single(item => item.Id == orderId).Status.Should().Be("Cancelled");
        dbContext.CourseEnrollments.Should().BeEmpty();
    }
}
