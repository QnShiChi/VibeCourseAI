using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext dbContext, IOptions<AdminSeedOptions> adminSeedOptions)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(3);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (dbContext.Database.IsRelational() && dbContext.Database.GetMigrations().Any())
                {
                    dbContext.Database.Migrate();
                }
                else
                {
                    dbContext.Database.EnsureCreated();
                }

                Seed(dbContext, adminSeedOptions.Value);
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException("Database initialization failed after multiple attempts.");
    }

    public static void Seed(AppDbContext dbContext, AdminSeedOptions adminSeed)
    {
        // Nếu không có bất kỳ khóa học nào, thì tạo một khóa học mẫu để có dữ liệu ban đầu
        if (!dbContext.Courses.Any())
        {
            dbContext.Courses.Add(new Course
            {
                Title = "Sample Course",
                Description = "Skeleton course created during initial project setup.",
                IsPublished = false
            });
        }

        var hasAdminSeed = !string.IsNullOrWhiteSpace(adminSeed.Email)
            && !string.IsNullOrWhiteSpace(adminSeed.Password)
            && !string.IsNullOrWhiteSpace(adminSeed.FullName);

        var adminRole = dbContext.Roles.First(role => role.Name == "Admin");
        var hasAdminUser = dbContext.Users.Any(user => user.RoleId == adminRole.Id);

        // Nếu chưa có tài khoản admin và đã cấu hình AdminSeed, thì tạo admin mặc định
        if (hasAdminSeed && !hasAdminUser)
        {
            var adminUser = new User
            {
                FullName = adminSeed.FullName,
                Email = adminSeed.Email,
                RoleId = adminRole.Id,
                IsActive = true
            };

            var passwordHasher = new PasswordHasher<User>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminSeed.Password);
            dbContext.Users.Add(adminUser);
        }

        dbContext.SaveChanges();
    }
}
