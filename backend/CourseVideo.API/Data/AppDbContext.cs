using CourseVideo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonComment> LessonComments => Set<LessonComment>();
    public DbSet<LessonCommentReaction> LessonCommentReactions => Set<LessonCommentReaction>();
    public DbSet<Syllabus> Syllabuses => Set<Syllabus>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "User" }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(course => course.Id);
            entity.Property(course => course.Title).HasMaxLength(200).IsRequired();
            entity.Property(course => course.Description).HasMaxLength(2000).IsRequired();
            entity.HasOne(course => course.Syllabus)
                .WithMany(syllabus => syllabus.Courses)
                .HasForeignKey(course => course.SyllabusId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(module => module.Id);
            entity.Property(module => module.Title).HasMaxLength(200).IsRequired();
            entity.Property(module => module.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(module => new { module.CourseId, module.OrderIndex });
            entity.HasOne(module => module.Course)
                .WithMany(course => course.Modules)
                .HasForeignKey(module => module.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(lesson => lesson.Id);
            entity.Property(lesson => lesson.Title).HasMaxLength(200).IsRequired();
            entity.Property(lesson => lesson.Description).HasMaxLength(2000).IsRequired();
            entity.Property(lesson => lesson.ContentSeed).IsRequired();
            entity.Property(lesson => lesson.ContentGenerationStatus).HasMaxLength(50).IsRequired();
            entity.Property(lesson => lesson.ContentGenerationError).HasMaxLength(2000);
            entity.Property(lesson => lesson.VideoUrl).HasMaxLength(1000);
            entity.Property(lesson => lesson.AudioUrl).HasMaxLength(1000);
            entity.HasIndex(lesson => new { lesson.ModuleId, lesson.OrderIndex });
            entity.HasOne(lesson => lesson.Module)
                .WithMany(module => module.Lessons)
                .HasForeignKey(lesson => lesson.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonComment>(entity =>
        {
            entity.HasKey(comment => comment.Id);
            entity.Property(comment => comment.Content).HasMaxLength(4000).IsRequired();
            entity.HasIndex(comment => new { comment.LessonId, comment.CreatedAt });
            entity.HasOne(comment => comment.Lesson)
                .WithMany()
                .HasForeignKey(comment => comment.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(comment => comment.User)
                .WithMany()
                .HasForeignKey(comment => comment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(comment => comment.ParentComment)
                .WithMany(comment => comment.Replies)
                .HasForeignKey(comment => comment.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(comment => comment.ReplyToUser)
                .WithMany()
                .HasForeignKey(comment => comment.ReplyToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonCommentReaction>(entity =>
        {
            entity.HasKey(reaction => reaction.Id);
            entity.Property(reaction => reaction.Emoji).HasMaxLength(32).IsRequired();
            entity.HasIndex(reaction => new { reaction.CommentId, reaction.UserId, reaction.Emoji }).IsUnique();
            entity.HasOne(reaction => reaction.Comment)
                .WithMany(comment => comment.Reactions)
                .HasForeignKey(reaction => reaction.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(reaction => reaction.User)
                .WithMany()
                .HasForeignKey(reaction => reaction.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Syllabus>(entity =>
        {
            entity.HasKey(syllabus => syllabus.Id);
            entity.Property(syllabus => syllabus.Title).HasMaxLength(255).IsRequired();
            entity.Property(syllabus => syllabus.Description).HasMaxLength(2000).IsRequired();
            entity.Property(syllabus => syllabus.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(syllabus => syllabus.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(syllabus => syllabus.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(syllabus => syllabus.FileType).HasMaxLength(50).IsRequired();
            entity.Property(syllabus => syllabus.ExtractedText).IsRequired();
            entity.HasIndex(syllabus => syllabus.CreatedAt);
            entity.HasOne(syllabus => syllabus.UploadedByUser)
                .WithMany(user => user.Syllabuses)
                .HasForeignKey(syllabus => syllabus.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GenerationJob>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.Property(job => job.JobType).HasMaxLength(100);
            entity.Property(job => job.Status).HasMaxLength(50).IsRequired();
            entity.Property(job => job.ErrorMessage).HasMaxLength(2000);
            entity.Property(job => job.ProgressMessage).HasMaxLength(500);
            entity.HasIndex(job => job.CreatedAt);
            entity.HasOne(job => job.Syllabus)
                .WithMany(syllabus => syllabus.GenerationJobs)
                .HasForeignKey(job => job.SyllabusId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.Course)
                .WithMany(course => course.GenerationJobs)
                .HasForeignKey(job => job.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(job => job.CreatedByUser)
                .WithMany(user => user.CreatedGenerationJobs)
                .HasForeignKey(job => job.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(500).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(500);
            entity.Property(token => token.CreatedByIp).HasMaxLength(100);
            entity.Property(token => token.RevokedByIp).HasMaxLength(100);
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId);
        });
    }
}
