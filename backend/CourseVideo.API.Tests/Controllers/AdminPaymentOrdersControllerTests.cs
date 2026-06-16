using CourseVideo.API.Controllers;
using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Payments;
using CourseVideo.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class AdminPaymentOrdersControllerTests
{
    [Fact]
    public async Task GetPaymentOrders_ShouldReturnAllStatusesIncludingPending()
    {
        await using var dbContext = BuildDbContext();
        var seeded = await SeedPaymentOrdersAsync(dbContext);
        var controller = new AdminPaymentOrdersController(dbContext);

        var result = await controller.GetPaymentOrders(query: null, status: null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeAssignableTo<IReadOnlyList<AdminPaymentOrderListItemResponse>>().Subject;
        response.Select(order => order.OrderCode).Should().Equal(
            seeded.Failed.OrderCode,
            seeded.Expired.OrderCode,
            seeded.Paid.OrderCode,
            seeded.Pending.OrderCode);
        response.Select(order => order.Status).Should().Contain(["Pending", "Paid", "Expired", "Failed"]);
    }

    [Fact]
    public async Task GetPaymentOrders_ShouldFilterByStatus()
    {
        await using var dbContext = BuildDbContext();
        var seeded = await SeedPaymentOrdersAsync(dbContext);
        var controller = new AdminPaymentOrdersController(dbContext);

        var result = await controller.GetPaymentOrders(query: null, status: "Pending");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeAssignableTo<IReadOnlyList<AdminPaymentOrderListItemResponse>>().Subject;
        response.Should().ContainSingle();
        response[0].OrderCode.Should().Be(seeded.Pending.OrderCode);
        response[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetPaymentOrders_ShouldFilterByQueryAgainstOrderCodeAndUserFields()
    {
        await using var dbContext = BuildDbContext();
        var seeded = await SeedPaymentOrdersAsync(dbContext);
        var controller = new AdminPaymentOrdersController(dbContext);

        var orderCodeResult = await controller.GetPaymentOrders(query: seeded.Pending.OrderCode, status: null);
        var orderCodeResponse = ((OkObjectResult)orderCodeResult).Value.Should()
            .BeAssignableTo<IReadOnlyList<AdminPaymentOrderListItemResponse>>().Subject;
        orderCodeResponse.Should().ContainSingle();
        orderCodeResponse[0].OrderCode.Should().Be(seeded.Pending.OrderCode);

        var userQueryResult = await controller.GetPaymentOrders(query: "Phuong Nguyen", status: null);
        var userQueryResponse = ((OkObjectResult)userQueryResult).Value.Should()
            .BeAssignableTo<IReadOnlyList<AdminPaymentOrderListItemResponse>>().Subject;
        userQueryResponse.Should().HaveCount(3);
        userQueryResponse.Select(order => order.OrderCode).Should().Contain([
            seeded.Pending.OrderCode,
            seeded.Paid.OrderCode,
            seeded.Expired.OrderCode
        ]);

        var emailQueryResult = await controller.GetPaymentOrders(query: "second@example.com", status: null);
        var emailQueryResponse = ((OkObjectResult)emailQueryResult).Value.Should()
            .BeAssignableTo<IReadOnlyList<AdminPaymentOrderListItemResponse>>().Subject;
        emailQueryResponse.Should().ContainSingle();
        emailQueryResponse[0].OrderCode.Should().Be(seeded.Failed.OrderCode);
    }

    [Fact]
    public async Task GetPaymentOrderDetail_ShouldReturnFullOrderInformation()
    {
        await using var dbContext = BuildDbContext();
        var seeded = await SeedPaymentOrdersAsync(dbContext);
        var controller = new AdminPaymentOrdersController(dbContext);

        var result = await controller.GetPaymentOrderDetail(seeded.Paid.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AdminPaymentOrderDetailResponse>().Subject;
        response.PaymentOrderId.Should().Be(seeded.Paid.Id);
        response.OrderCode.Should().Be(seeded.Paid.OrderCode);
        response.UserFullName.Should().Be("Phuong Nguyen");
        response.UserEmail.Should().Be("phuong@example.com");
        response.CourseTitle.Should().Be("Tri Tue Nhan Tao Ung Dung");
        response.Amount.Should().Be(3000);
        response.Status.Should().Be("Paid");
        response.BankCode.Should().Be("VCB");
        response.BankName.Should().Be("Vietcombank");
        response.BankAccountNumber.Should().Be("123456789");
        response.AccountHolderName.Should().Be("PHUONG NGUYEN");
        response.TransferContent.Should().Be("VCPAID001");
        response.SepayTransactionId.Should().Be(9001);
        response.PaidAt.Should().Be(new DateTime(2026, 6, 15, 10, 55, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetPaymentOrderDetail_ShouldReturnNotFoundForMissingOrder()
    {
        await using var dbContext = BuildDbContext();
        await SeedPaymentOrdersAsync(dbContext);
        var controller = new AdminPaymentOrdersController(dbContext);

        var result = await controller.GetPaymentOrderDetail(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
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

    private static async Task<SeededPaymentOrders> SeedPaymentOrdersAsync(AppDbContext dbContext)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Thanh toan",
            Description = "Danh muc thanh toan",
            Status = CategoryStatus.Visible,
            SortOrder = 1,
            CreatedAt = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var firstUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Phuong Nguyen",
            Email = "phuong@example.com",
            PasswordHash = "hashed-password",
            RoleId = 2,
            CreatedAt = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var secondUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Second Learner",
            Email = "second@example.com",
            PasswordHash = "hashed-password",
            RoleId = 2,
            CreatedAt = new DateTime(2026, 6, 10, 0, 5, 0, DateTimeKind.Utc)
        };

        var firstCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Tri Tue Nhan Tao Ung Dung",
            Description = "Course description",
            CategoryId = category.Id,
            Category = category,
            Price = 3000,
            IsPublished = true,
            CreatedAt = new DateTime(2026, 6, 10, 0, 10, 0, DateTimeKind.Utc)
        };

        var secondCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Lap Trinh Huong Doi Tuong",
            Description = "Course description",
            CategoryId = category.Id,
            Category = category,
            Price = 3000,
            IsPublished = true,
            CreatedAt = new DateTime(2026, 6, 10, 0, 20, 0, DateTimeKind.Utc)
        };

        var pending = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = "VCPENDING001",
            UserId = firstUser.Id,
            User = firstUser,
            CourseId = firstCourse.Id,
            Course = firstCourse,
            Amount = 3000,
            Status = "Pending",
            CreatedAt = new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 6, 15, 11, 15, 0, DateTimeKind.Utc),
            TransferContent = "VCPENDING001"
        };

        var paid = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = "VCPAID001",
            UserId = firstUser.Id,
            User = firstUser,
            CourseId = firstCourse.Id,
            Course = firstCourse,
            Amount = 3000,
            Status = "Paid",
            CreatedAt = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 6, 15, 10, 45, 0, DateTimeKind.Utc),
            PaidAt = new DateTime(2026, 6, 15, 10, 55, 0, DateTimeKind.Utc),
            BankCode = "VCB",
            BankName = "Vietcombank",
            BankAccountNumber = "123456789",
            AccountHolderName = "PHUONG NGUYEN",
            TransferContent = "VCPAID001",
            SepayTransactionId = 9001
        };

        var expired = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = "VCEXPIRED001",
            UserId = firstUser.Id,
            User = firstUser,
            CourseId = secondCourse.Id,
            Course = secondCourse,
            Amount = 3000,
            Status = "Expired",
            CreatedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 6, 15, 12, 15, 0, DateTimeKind.Utc),
            TransferContent = "VCEXPIRED001"
        };

        var failed = new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderCode = "VCFAILED001",
            UserId = secondUser.Id,
            User = secondUser,
            CourseId = secondCourse.Id,
            Course = secondCourse,
            Amount = 3000,
            Status = "Failed",
            CreatedAt = new DateTime(2026, 6, 15, 13, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 6, 15, 13, 15, 0, DateTimeKind.Utc),
            TransferContent = "VCFAILED001"
        };

        dbContext.Categories.Add(category);
        dbContext.Users.AddRange(firstUser, secondUser);
        dbContext.Courses.AddRange(firstCourse, secondCourse);
        dbContext.PaymentOrders.AddRange(pending, paid, expired, failed);
        await dbContext.SaveChangesAsync();

        return new SeededPaymentOrders(pending, paid, expired, failed);
    }

    private sealed record SeededPaymentOrders(
        PaymentOrder Pending,
        PaymentOrder Paid,
        PaymentOrder Expired,
        PaymentOrder Failed);
}
