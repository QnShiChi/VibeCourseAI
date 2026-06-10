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
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonComment> LessonComments => Set<LessonComment>();
    public DbSet<LessonCommentReaction> LessonCommentReactions => Set<LessonCommentReaction>();
    public DbSet<Syllabus> Syllabuses => Set<Syllabus>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LessonVoiceSession> LessonVoiceSessions => Set<LessonVoiceSession>();
    public DbSet<LessonVoiceTurn> LessonVoiceTurns => Set<LessonVoiceTurn>();
    public DbSet<LessonVoiceMessage> LessonVoiceMessages => Set<LessonVoiceMessage>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<PaymentTransactionLog> PaymentTransactionLogs => Set<PaymentTransactionLog>();

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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(400).IsRequired();
            entity.Property(category => category.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(CategoryStatus.Visible)
                .IsRequired();
            entity.Property(category => category.SortOrder).HasDefaultValue(0);
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(course => course.Id);
            entity.Property(course => course.Title).HasMaxLength(200).IsRequired();
            entity.Property(course => course.Description).HasMaxLength(2000).IsRequired();
            entity.Property(course => course.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(course => course.Price).HasDefaultValue(599000);
            entity.Property(course => course.CategoryId).IsRequired();
            entity.HasOne(course => course.Category)
                .WithMany(category => category.Courses)
                .HasForeignKey(course => course.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(course => course.Syllabus)
                .WithMany(syllabus => syllabus.Courses)
                .HasForeignKey(course => course.SyllabusId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(lesson => lesson.NarrationVoiceKey).HasMaxLength(200);
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
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<LessonVoiceSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Status).HasMaxLength(50).IsRequired();
            entity.Property(session => session.VoiceProfileKey).HasMaxLength(200).IsRequired();
            entity.Property(session => session.ContextScope).HasMaxLength(100).IsRequired();
            entity.HasIndex(session => new { session.LessonId, session.UserId, session.Status });
            entity.HasOne(session => session.Lesson)
                .WithMany()
                .HasForeignKey(session => session.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(session => session.User)
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonVoiceTurn>(entity =>
        {
            entity.HasKey(turn => turn.Id);
            entity.Property(turn => turn.Status).HasMaxLength(50).IsRequired();
            entity.Property(turn => turn.UserAudioUrl).HasMaxLength(1000);
            entity.Property(turn => turn.ErrorCode).HasMaxLength(100);
            entity.Property(turn => turn.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(turn => new { turn.SessionId, turn.TurnNumber });
            entity.HasOne(turn => turn.Session)
                .WithMany(session => session.Turns)
                .HasForeignKey(turn => turn.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonVoiceMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Role).HasMaxLength(30).IsRequired();
            entity.Property(message => message.ContentSourceType).HasMaxLength(50).IsRequired();
            entity.Property(message => message.AudioUrl).HasMaxLength(1000);
            entity.HasIndex(message => new { message.SessionId, message.TurnNumber, message.SequenceIndex });
            entity.HasOne(message => message.Session)
                .WithMany(session => session.Messages)
                .HasForeignKey(message => message.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(quiz => quiz.Id);
            entity.Property(quiz => quiz.Type).HasMaxLength(30).IsRequired();
            entity.Property(quiz => quiz.Status).HasMaxLength(30).IsRequired();
            entity.Property(quiz => quiz.Title).HasMaxLength(300).IsRequired();
            entity.Property(quiz => quiz.SourceContentVersion).HasMaxLength(100);
            entity.Property(quiz => quiz.GenerationError).HasMaxLength(2000);
            entity.HasIndex(quiz => quiz.LessonId).IsUnique().HasFilter("[LessonId] IS NOT NULL");
            entity.HasIndex(quiz => quiz.CourseId).IsUnique().HasFilter("[CourseId] IS NOT NULL AND [Type] = 'Final'");
            entity.HasOne(quiz => quiz.Lesson)
                .WithMany()
                .HasForeignKey(quiz => quiz.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(quiz => quiz.Course)
                .WithMany(course => course.Quizzes)
                .HasForeignKey(quiz => quiz.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(question => question.Id);
            entity.Property(question => question.QuestionText).HasMaxLength(2000).IsRequired();
            entity.Property(question => question.Explanation).HasMaxLength(2000).IsRequired();
            entity.HasIndex(question => new { question.QuizId, question.OrderIndex });
            entity.HasOne(question => question.Quiz)
                .WithMany(quiz => quiz.Questions)
                .HasForeignKey(question => question.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizOption>(entity =>
        {
            entity.HasKey(option => option.Id);
            entity.Property(option => option.OptionText).HasMaxLength(1000).IsRequired();
            entity.HasIndex(option => new { option.QuizQuestionId, option.OrderIndex });
            entity.HasOne(option => option.QuizQuestion)
                .WithMany(question => question.Options)
                .HasForeignKey(option => option.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(attempt => attempt.Id);
            entity.Property(attempt => attempt.Score).HasPrecision(5, 2);
            entity.HasIndex(attempt => new { attempt.QuizId, attempt.UserId, attempt.StartedAt });
            entity.HasOne(attempt => attempt.Quiz)
                .WithMany(quiz => quiz.Attempts)
                .HasForeignKey(attempt => attempt.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(attempt => attempt.User)
                .WithMany()
                .HasForeignKey(attempt => attempt.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuizAttemptAnswer>(entity =>
        {
            entity.HasKey(answer => answer.Id);
            entity.HasIndex(answer => new { answer.QuizAttemptId, answer.QuizQuestionId }).IsUnique();
            entity.HasOne(answer => answer.QuizAttempt)
                .WithMany(attempt => attempt.Answers)
                .HasForeignKey(answer => answer.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(answer => answer.QuizQuestion)
                .WithMany()
                .HasForeignKey(answer => answer.QuizQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.GuestCartToken).HasMaxLength(100);
            entity.HasIndex(item => new { item.UserId, item.CourseId }).IsUnique().HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(item => new { item.GuestCartToken, item.CourseId }).IsUnique().HasFilter("[GuestCartToken] IS NOT NULL");
            entity.HasOne(item => item.User)
                .WithMany(user => user.CartItems)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Course)
                .WithMany(course => course.CartItems)
                .HasForeignKey(item => item.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.HasKey(enrollment => enrollment.Id);
            entity.HasIndex(enrollment => new { enrollment.UserId, enrollment.CourseId }).IsUnique();
            entity.HasOne(enrollment => enrollment.User)
                .WithMany(user => user.Enrollments)
                .HasForeignKey(enrollment => enrollment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(enrollment => enrollment.Course)
                .WithMany(course => course.Enrollments)
                .HasForeignKey(enrollment => enrollment.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(enrollment => enrollment.PaymentOrder)
                .WithMany()
                .HasForeignKey(enrollment => enrollment.PaymentOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.OrderCode).HasMaxLength(32).IsRequired();
            entity.Property(order => order.Status).HasMaxLength(30).IsRequired();
            entity.Property(order => order.BankCode).HasMaxLength(50);
            entity.Property(order => order.BankName).HasMaxLength(200);
            entity.Property(order => order.BankAccountNumber).HasMaxLength(50);
            entity.Property(order => order.AccountHolderName).HasMaxLength(200);
            entity.Property(order => order.TransferContent).HasMaxLength(200).IsRequired();
            entity.HasIndex(order => order.OrderCode).IsUnique();
            entity.HasIndex(order => new { order.UserId, order.CourseId, order.Status });
            entity.HasOne(order => order.User)
                .WithMany(user => user.PaymentOrders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Course)
                .WithMany(course => course.PaymentOrders)
                .HasForeignKey(order => order.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentTransactionLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Gateway).HasMaxLength(100).IsRequired();
            entity.Property(log => log.TransactionDateText).HasMaxLength(50).IsRequired();
            entity.Property(log => log.AccountNumber).HasMaxLength(50).IsRequired();
            entity.Property(log => log.SubAccount).HasMaxLength(100);
            entity.Property(log => log.Code).HasMaxLength(100);
            entity.Property(log => log.Content).HasMaxLength(500).IsRequired();
            entity.Property(log => log.TransferType).HasMaxLength(10).IsRequired();
            entity.Property(log => log.Description).HasMaxLength(1000);
            entity.Property(log => log.ReferenceCode).HasMaxLength(100);
            entity.HasIndex(log => log.SepayTransactionId).IsUnique();
            entity.HasOne(log => log.MatchedPaymentOrder)
                .WithMany()
                .HasForeignKey(log => log.MatchedPaymentOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
