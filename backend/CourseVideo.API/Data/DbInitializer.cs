using CourseVideo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext dbContext)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(3);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (dbContext.Database.GetMigrations().Any())
                {
                    dbContext.Database.Migrate();
                }
                else
                {
                    dbContext.Database.EnsureCreated();
                }

                Seed(dbContext);
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException("Database initialization failed after multiple attempts.");
    }

    public static void Seed(AppDbContext dbContext)
    {
        if (!dbContext.Courses.Any())
        {
            dbContext.Courses.Add(new Course
            {
                Title = "Sample Course",
                Description = "Skeleton course created during initial project setup.",
                IsPublished = false
            });
        }

        if (!dbContext.Users.Any())
        {
            dbContext.Users.Add(new User
            {
                FullName = "System Admin",
                Email = "admin@example.com",
                PasswordHash = "change-me",
                RoleId = 1,
                IsActive = true
            });
        }

        dbContext.SaveChanges();
    }
}
