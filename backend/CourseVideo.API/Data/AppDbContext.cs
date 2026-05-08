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
            // 1 User thuộc về 1 Role
            // 1 Role có nhiều User
            // khóa ngoại là RoleId
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(course => course.Id);
            entity.Property(course => course.Title).HasMaxLength(200).IsRequired();
            entity.Property(course => course.Description).HasMaxLength(2000).IsRequired();
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
