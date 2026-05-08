# Auth + Authorization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the placeholder auth flow with JWT authentication, refresh-token-backed sessions, admin/user authorization, admin bootstrap from environment configuration, and Vietnamese user-facing auth text.

**Architecture:** Keep the current layered ASP.NET Core structure and extend it with focused auth units: persistence for users and refresh tokens, a token service for JWT creation and hashing, an auth service for business flows, and protected controllers using bearer auth. Add a small test project under `backend/` so auth logic can be validated independently of the frontend and worker services, then update auth-related frontend screens so visible labels and messages are Vietnamese with proper diacritics.

**Tech Stack:** ASP.NET Core 8, EF Core SQL Server, JWT Bearer Authentication, `PasswordHasher<User>`, xUnit, Moq, FluentAssertions

---

## File Map

### Create

- `backend/CourseVideo.API/Models/RefreshToken.cs`
- `backend/CourseVideo.API/DTOs/Auth/RegisterRequest.cs`
- `backend/CourseVideo.API/DTOs/Auth/RefreshTokenRequest.cs`
- `backend/CourseVideo.API/DTOs/Auth/ChangePasswordRequest.cs`
- `backend/CourseVideo.API/DTOs/Auth/AuthResponse.cs`
- `backend/CourseVideo.API/DTOs/Auth/CurrentUserResponse.cs`
- `backend/CourseVideo.API/DTOs/Users/UserListItemResponse.cs`
- `backend/CourseVideo.API/DTOs/Users/UpdateUserActiveRequest.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IUserRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IRefreshTokenRepository.cs`
- `backend/CourseVideo.API/Repositories/UserRepository.cs`
- `backend/CourseVideo.API/Repositories/RefreshTokenRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/ITokenService.cs`
- `backend/CourseVideo.API/Services/TokenService.cs`
- `backend/CourseVideo.API/Configuration/JwtOptions.cs`
- `backend/CourseVideo.API/Configuration/AdminSeedOptions.cs`
- `backend/CourseVideo.API/Controllers/UsersController.cs`
- `backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj`
- `backend/CourseVideo.API.Tests/Services/TokenServiceTests.cs`
- `backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/UsersControllerTests.cs`

### Modify

- `backend/CourseVideo.sln`
- `backend/CourseVideo.API/CourseVideo.API.csproj`
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`
- `backend/CourseVideo.API/Models/User.cs`
- `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`
- `backend/CourseVideo.API/Services/AuthService.cs`
- `backend/CourseVideo.API/Controllers/AuthController.cs`
- `backend/CourseVideo.API/appsettings.json`
- `backend/CourseVideo.API/appsettings.Development.json`
- `.env.example`
- `frontend/src/pages/LoginPage.jsx`
- `frontend/src/pages/RegisterPage.jsx`

### Verify

- `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj`
- `docker build -t vibecourseai-backend-auth-check backend/CourseVideo.API`
- `make up`
- `curl -s http://localhost:5000/api/health`

---

### Task 1: Add Test Scaffold And Auth Configuration Types

**Files:**
- Create: `backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj`
- Create: `backend/CourseVideo.API.Tests/Services/TokenServiceTests.cs`
- Create: `backend/CourseVideo.API/Configuration/JwtOptions.cs`
- Create: `backend/CourseVideo.API/Configuration/AdminSeedOptions.cs`
- Modify: `backend/CourseVideo.sln`
- Modify: `backend/CourseVideo.API/CourseVideo.API.csproj`
- Modify: `backend/CourseVideo.API/appsettings.json`
- Modify: `backend/CourseVideo.API/appsettings.Development.json`
- Modify: `.env.example`

- [ ] **Step 1: Write the failing token-service test project and first test**

Create `backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.8" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CourseVideo.API\CourseVideo.API.csproj" />
  </ItemGroup>
</Project>
```

Create `backend/CourseVideo.API.Tests/Services/TokenServiceTests.cs`:

```csharp
using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void CreateAccessToken_ShouldEmbedUserIdentityAndRoleClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "vibe-course-ai",
            Audience = "vibe-course-ai-client",
            SecretKey = "super-secret-key-with-at-least-32-chars",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        });

        var service = new TokenService(options);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "System Admin",
            Email = "admin@example.com",
            Role = new Role { Id = 1, Name = "Admin" }
        };

        var token = service.CreateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~TokenServiceTests"
```

Expected:

```text
FAIL
error CS0246: The type or namespace name 'JwtOptions' could not be found
error CS0246: The type or namespace name 'TokenService' could not be found
```

- [ ] **Step 3: Add the minimal auth config classes and package references**

Update `backend/CourseVideo.API/CourseVideo.API.csproj` to include JWT bearer support:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.8">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.8" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
</ItemGroup>
```

Create `backend/CourseVideo.API/Configuration/JwtOptions.cs`:

```csharp
namespace CourseVideo.API.Configuration;

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
}
```

Create `backend/CourseVideo.API/Configuration/AdminSeedOptions.cs`:

```csharp
namespace CourseVideo.API.Configuration;

public class AdminSeedOptions
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

Add config placeholders to `backend/CourseVideo.API/appsettings.json`:

```json
"Jwt": {
  "Issuer": "VibeCourseAI",
  "Audience": "VibeCourseAI.Client",
  "SecretKey": "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY",
  "AccessTokenMinutes": 30,
  "RefreshTokenDays": 7
},
"AdminSeed": {
  "FullName": "",
  "Email": "",
  "Password": ""
}
```

Mirror the same sections in `backend/CourseVideo.API/appsettings.Development.json`, and add matching variables to `.env.example`:

```env
JWT__ISSUER=VibeCourseAI
JWT__AUDIENCE=VibeCourseAI.Client
JWT__SECRETKEY=CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY
JWT__ACCESSTOKENMINUTES=30
JWT__REFRESHTOKENDAYS=7
ADMINSEED__FULLNAME=System Admin
ADMINSEED__EMAIL=admin@vibecourse.local
ADMINSEED__PASSWORD=ChangeMe@123
```

- [ ] **Step 4: Add the test project to the solution**

Run:

```bash
dotnet sln backend/CourseVideo.sln add backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj
```

Expected:

```text
Project `CourseVideo.API.Tests/CourseVideo.API.Tests.csproj` added to the solution.
```

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.sln backend/CourseVideo.API/CourseVideo.API.csproj backend/CourseVideo.API/Configuration backend/CourseVideo.API/appsettings.json backend/CourseVideo.API/appsettings.Development.json backend/CourseVideo.API.Tests .env.example
git commit -m "test: scaffold auth configuration and test project"
```

### Task 2: Add Refresh Token Persistence And Admin Bootstrap Data Model

**Files:**
- Create: `backend/CourseVideo.API/Models/RefreshToken.cs`
- Modify: `backend/CourseVideo.API/Models/User.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Create: `backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs`

- [ ] **Step 1: Write the failing auth bootstrap test**

Create `backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs`:

```csharp
using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class AuthBootstrapTests
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
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~AuthBootstrapTests"
```

Expected:

```text
FAIL
error CS1501: No overload for method 'Initialize' takes 2 arguments
```

- [ ] **Step 3: Add refresh-token model and wire it into EF**

Create `backend/CourseVideo.API/Models/RefreshToken.cs`:

```csharp
namespace CourseVideo.API.Models;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    public User? User { get; set; }
}
```

Update `backend/CourseVideo.API/Models/User.cs`:

```csharp
public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
```

Update `backend/CourseVideo.API/Data/AppDbContext.cs` to include:

```csharp
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

and configure the entity:

```csharp
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
```

- [ ] **Step 4: Update DB initialization to support env admin seed**

Update `backend/CourseVideo.API/Data/DbInitializer.cs` signature and behavior:

```csharp
using CourseVideo.API.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public static void Initialize(AppDbContext dbContext, IOptions<AdminSeedOptions> adminSeedOptions)
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
    if (!dbContext.Roles.Any())
    {
        dbContext.Roles.AddRange(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "User" }
        );
    }

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
```

- [ ] **Step 5: Run the bootstrap test to verify it passes**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~AuthBootstrapTests"
```

Expected:

```text
PASS
```

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Models/RefreshToken.cs backend/CourseVideo.API/Models/User.cs backend/CourseVideo.API/Data/AppDbContext.cs backend/CourseVideo.API/Data/DbInitializer.cs backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs
git commit -m "feat: add refresh token persistence and admin bootstrap"
```

### Task 3: Add Repositories And Token Service

**Files:**
- Create: `backend/CourseVideo.API/Repositories/Interfaces/IUserRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/IRefreshTokenRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/UserRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/RefreshTokenRepository.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ITokenService.cs`
- Create: `backend/CourseVideo.API/Services/TokenService.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/TokenServiceTests.cs`

- [ ] **Step 1: Strengthen the failing token-service test**

Replace `backend/CourseVideo.API.Tests/Services/TokenServiceTests.cs` with:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void CreateAccessToken_ShouldEmbedUserIdentityAndRoleClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "vibe-course-ai",
            Audience = "vibe-course-ai-client",
            SecretKey = "super-secret-key-with-at-least-32-chars",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        });

        var service = new TokenService(options);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "System Admin",
            Email = "admin@example.com",
            Role = new Role { Id = 1, Name = "Admin" }
        };

        var token = service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == userId.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "admin@example.com");
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Name && claim.Value == "System Admin");
    }

    [Fact]
    public void HashRefreshToken_ShouldReturnStableHashForSameInput()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "vibe-course-ai",
            Audience = "vibe-course-ai-client",
            SecretKey = "super-secret-key-with-at-least-32-chars",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        });

        var service = new TokenService(options);

        var hash1 = service.HashRefreshToken("refresh-token-value");
        var hash2 = service.HashRefreshToken("refresh-token-value");

        hash1.Should().Be(hash2);
    }
}
```

- [ ] **Step 2: Run the token tests to verify they fail for real behavior**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~TokenServiceTests"
```

Expected:

```text
FAIL
error CS0246: The type or namespace name 'TokenService' could not be found
```

- [ ] **Step 3: Create repositories and token service**

Create `backend/CourseVideo.API/Repositories/Interfaces/IUserRepository.cs`:

```csharp
using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
```

Create `backend/CourseVideo.API/Repositories/Interfaces/IRefreshTokenRepository.cs`:

```csharp
using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task RevokeAllByUserIdAsync(Guid userId, string? revokedByIp);
    Task SaveChangesAsync();
}
```

Create `backend/CourseVideo.API/Repositories/UserRepository.cs`:

```csharp
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _dbContext.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _dbContext.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _dbContext.Users
            .Include(user => user.Role)
            .OrderBy(user => user.CreatedAt)
            .ToListAsync();
    }

    public Task AddAsync(User user)
    {
        return _dbContext.Users.AddAsync(user).AsTask();
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
```

Create `backend/CourseVideo.API/Repositories/RefreshTokenRepository.cs`:

```csharp
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(RefreshToken refreshToken)
    {
        return _dbContext.RefreshTokens.AddAsync(refreshToken).AsTask();
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return _dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user!.Role)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, string? revokedByIp)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
            token.UpdatedAt = DateTime.UtcNow;
        }
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
```

Create `backend/CourseVideo.API/Services/Interfaces/ITokenService.cs`:

```csharp
using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetRefreshTokenExpiryUtc();
}
```

Create `backend/CourseVideo.API/Services/TokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CourseVideo.API.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    public DateTime GetRefreshTokenExpiryUtc()
    {
        return DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
    }
}
```

- [ ] **Step 4: Run the token tests to verify they pass**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~TokenServiceTests"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Repositories backend/CourseVideo.API/Services/Interfaces/ITokenService.cs backend/CourseVideo.API/Services/TokenService.cs backend/CourseVideo.API.Tests/Services/TokenServiceTests.cs
git commit -m "feat: add token service and auth repositories"
```

### Task 4: Implement Auth Service Flows And Auth DTOs

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Auth/RegisterRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Auth/RefreshTokenRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Auth/ChangePasswordRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Auth/AuthResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Auth/CurrentUserResponse.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`
- Modify: `backend/CourseVideo.API/Services/AuthService.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs`

- [ ] **Step 1: Replace the bootstrap test file with auth service behavior tests**

Replace `backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs` with:

```csharp
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateUserWithUserRoleAndReturnTokens()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByEmailAsync("new-user@example.com"))
            .ReturnsAsync((User?)null);

        userRepository.Setup(repository => repository.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        userRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        refreshTokenRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        tokenService.Setup(service => service.CreateRefreshToken())
            .Returns("refresh-token");
        tokenService.Setup(service => service.HashRefreshToken("refresh-token"))
            .Returns("refresh-token-hash");
        tokenService.Setup(service => service.GetRefreshTokenExpiryUtc())
            .Returns(DateTime.UtcNow.AddDays(7));

        var authService = new AuthService(
            userRepository.Object,
            refreshTokenRepository.Object,
            tokenService.Object,
            new PasswordHasher<User>());

        var response = await authService.RegisterAsync(new RegisterRequest
        {
            FullName = "New User",
            Email = "new-user@example.com",
            Password = "ChangeMe@123"
        }, "127.0.0.1");

        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("refresh-token");
        response.User.Role.Should().Be("User");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordIsInvalid()
    {
        var seededUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "existing@example.com",
            Role = new Role { Id = 2, Name = "User" },
            RoleId = 2
        };

        var passwordHasher = new PasswordHasher<User>();
        seededUser.PasswordHash = passwordHasher.HashPassword(seededUser, "CorrectPassword@123");

        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(seededUser);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var tokenService = new Mock<ITokenService>();

        var authService = new AuthService(
            userRepository.Object,
            refreshTokenRepository.Object,
            tokenService.Object,
            passwordHasher);

        var action = async () => await authService.LoginAsync(new LoginRequest
        {
            Email = "existing@example.com",
            Password = "WrongPassword@123"
        }, "127.0.0.1");

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
```

- [ ] **Step 2: Run the auth service tests to verify they fail**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"
```

Expected:

```text
FAIL
error CS1061: 'IAuthService' does not contain a definition for 'RegisterAsync'
```

- [ ] **Step 3: Create auth DTOs and implement auth service contracts**

Create `backend/CourseVideo.API/DTOs/Auth/RegisterRequest.cs`:

```csharp
namespace CourseVideo.API.DTOs.Auth;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

Create `backend/CourseVideo.API/DTOs/Auth/RefreshTokenRequest.cs`:

```csharp
namespace CourseVideo.API.DTOs.Auth;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

Create `backend/CourseVideo.API/DTOs/Auth/ChangePasswordRequest.cs`:

```csharp
namespace CourseVideo.API.DTOs.Auth;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
```

Create `backend/CourseVideo.API/DTOs/Auth/AuthResponse.cs`:

```csharp
namespace CourseVideo.API.DTOs.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public AuthUserResponse User { get; set; } = new();
}
```

Create `backend/CourseVideo.API/DTOs/Auth/CurrentUserResponse.cs`:

```csharp
namespace CourseVideo.API.DTOs.Auth;

public class CurrentUserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

Update `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`:

```csharp
using System.Security.Claims;
using CourseVideo.API.DTOs.Auth;

namespace CourseVideo.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress);
    Task LogoutAsync(Guid currentUserId, RefreshTokenRequest request, string? ipAddress);
    Task LogoutAllAsync(Guid currentUserId, string? ipAddress);
    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task ChangePasswordAsync(Guid currentUserId, ChangePasswordRequest request, string? ipAddress);
}
```

Update `backend/CourseVideo.API/Services/AuthService.cs` to use repositories, password hasher, and token service:

```csharp
using System.Security.Claims;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace CourseVideo.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        PasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            RoleId = 2,
            Role = new Role { Id = 2, Name = "User" },
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("User account is inactive.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.RevokedAt is not null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.User is null || !storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        var nextRefreshToken = _tokenService.CreateRefreshToken();
        storedToken.ReplacedByTokenHash = _tokenService.HashRefreshToken(nextRefreshToken);

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = storedToken.ReplacedByTokenHash,
            ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress
        });

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = _tokenService.CreateAccessToken(storedToken.User),
            RefreshToken = nextRefreshToken,
            User = new AuthUserResponse
            {
                Id = storedToken.User.Id,
                FullName = storedToken.User.FullName,
                Email = storedToken.User.Email,
                Role = storedToken.User.Role?.Name ?? string.Empty
            }
        };
    }

    public async Task LogoutAsync(Guid currentUserId, RefreshTokenRequest request, string? ipAddress)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken is null || storedToken.UserId != currentUserId)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        await _refreshTokenRepository.SaveChangesAsync();
    }

    public Task LogoutAllAsync(Guid currentUserId, string? ipAddress)
    {
        return RevokeAllUserSessionsAsync(currentUserId, ipAddress);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing user id claim.");

        var userId = Guid.Parse(subject);
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        return new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive
        };
    }

    public async Task ChangePasswordAsync(Guid currentUserId, ChangePasswordRequest request, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
        await RevokeAllUserSessionsAsync(user.Id, ipAddress);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, string? ipAddress)
    {
        var refreshToken = _tokenService.CreateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress
        });
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = _tokenService.CreateAccessToken(user),
            RefreshToken = refreshToken,
            User = new AuthUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.Name ?? string.Empty
            }
        };
    }

    private Task RevokeAllUserSessionsAsync(Guid userId, string? ipAddress)
    {
        return _refreshTokenRepository.RevokeAllByUserIdAsync(userId, ipAddress);
    }
}
```

- [ ] **Step 4: Run the auth service tests to verify they pass**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/DTOs/Auth backend/CourseVideo.API/Services/Interfaces/IAuthService.cs backend/CourseVideo.API/Services/AuthService.cs backend/CourseVideo.API.Tests/Services/AuthServiceTests.cs
git commit -m "feat: implement auth service flows"
```

### Task 5: Wire JWT Middleware, Protected Controllers, And Admin User APIs

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Users/UserListItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Users/UpdateUserActiveRequest.cs`
- Create: `backend/CourseVideo.API/Controllers/UsersController.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/UsersControllerTests.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `backend/CourseVideo.API/Controllers/AuthController.cs`

- [ ] **Step 1: Write the failing admin-controller authorization test**

Create `backend/CourseVideo.API.Tests/Controllers/UsersControllerTests.cs`:

```csharp
using System.Security.Claims;
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.Users;
using CourseVideo.API.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task UpdateActive_ShouldReturnNoContent_WhenServiceCompletes()
    {
        var user = new CourseVideo.API.Models.User
        {
            Id = Guid.NewGuid(),
            FullName = "Learner",
            Email = "learner@example.com",
            RoleId = 2,
            IsActive = false
        };

        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        userRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(repository => repository.RevokeAllByUserIdAsync(user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        refreshTokenRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var controller = new UsersController(userRepository.Object, refreshTokenRepository.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") },
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "Test"))
                }
            }
        };

        var result = await controller.UpdateActive(user.Id, new UpdateUserActiveRequest { IsActive = false });

        result.Should().BeOfType<NoContentResult>();
    }
}
```

- [ ] **Step 2: Run the controller test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~UsersControllerTests"
```

Expected:

```text
FAIL
error CS0246: The type or namespace name 'UsersController' could not be found
```

- [ ] **Step 3: Add a small admin user service and wire controllers**

Create `backend/CourseVideo.API/DTOs/Users/UserListItemResponse.cs`:

```csharp
namespace CourseVideo.API.DTOs.Users;

public class UserListItemResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Create `backend/CourseVideo.API/DTOs/Users/UpdateUserActiveRequest.cs`:

```csharp
namespace CourseVideo.API.DTOs.Users;

public class UpdateUserActiveRequest
{
    public bool IsActive { get; set; }
}
```

Create `backend/CourseVideo.API/Controllers/UsersController.cs`:

```csharp
using CourseVideo.API.DTOs.Users;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public UsersController(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemResponse>>> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        var response = users.Select(user => new UserListItemResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }).ToList();

        return Ok(response);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> UpdateActive(Guid id, [FromBody] UpdateUserActiveRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        if (!request.IsActive)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
            await _refreshTokenRepository.SaveChangesAsync();
        }

        return NoContent();
    }
}
```

Update `backend/CourseVideo.API/Controllers/AuthController.cs`:

```csharp
using System.Security.Claims;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        await _authService.LogoutAsync(currentUserId, request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        await _authService.LogoutAllAsync(currentUserId, HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetCurrentUserAsync(User);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        await _authService.ChangePasswordAsync(currentUserId, request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }
}
```

Update `backend/CourseVideo.API/Program.cs`:

```csharp
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing database connection string.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing JWT configuration.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PasswordHasher<User>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>();
    DbInitializer.Initialize(db, adminOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

- [ ] **Step 4: Run the controller test and backend build**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~UsersControllerTests"
docker build -t vibecourseai-backend-auth-check backend/CourseVideo.API
```

Expected:

```text
PASS
...
Successfully tagged vibecourseai-backend-auth-check:latest
```

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/DTOs/Users backend/CourseVideo.API/Controllers/AuthController.cs backend/CourseVideo.API/Controllers/UsersController.cs backend/CourseVideo.API/Program.cs backend/CourseVideo.API.Tests/Controllers/UsersControllerTests.cs
git commit -m "feat: wire jwt middleware and protected auth endpoints"
```

### Task 6: Full Auth Verification And Runtime Smoke Checks

**Files:**
- Verify only

- [ ] **Step 1: Run the full backend test suite**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj
```

Expected:

```text
Passed!
```

- [ ] **Step 2: Start the runtime stack**

Run:

```bash
make up
```

Expected:

```text
course_sqlserver   Running
course_backend     Running
course_frontend    Running
course_ai_worker   Running
```

- [ ] **Step 3: Verify health and auth wiring**

Run:

```bash
curl -s http://localhost:5000/api/health
curl -s -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" -d '{"email":"admin@vibecourse.local","password":"ChangeMe@123"}'
```

Expected:

```text
{"status":"ok"}
{"accessToken":"...","refreshToken":"...","user":{"email":"admin@vibecourse.local",...}}
```

- [ ] **Step 4: Verify admin-only endpoint with bearer token**

Run:

```bash
curl -s http://localhost:5000/api/users -H "Authorization: Bearer <ACCESS_TOKEN>"
```

Expected:

```text
[{"email":"admin@vibecourse.local","role":"Admin",...}]
```

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "test: verify auth authorization flow end to end"
```

### Task 7: Localize Auth Screens To Vietnamese

**Files:**
- Modify: `frontend/src/pages/LoginPage.jsx`
- Modify: `frontend/src/pages/RegisterPage.jsx`

- [ ] **Step 1: Inspect current auth page text and write the expected Vietnamese labels**

Target visible text for `frontend/src/pages/LoginPage.jsx`:

```text
Đăng nhập
Email
Mật khẩu
Đăng nhập vào hệ thống
Bạn chưa có tài khoản?
Đăng ký ngay
```

Target visible text for `frontend/src/pages/RegisterPage.jsx`:

```text
Đăng ký
Họ và tên
Email
Mật khẩu
Tạo tài khoản
Bạn đã có tài khoản?
Đăng nhập
```

- [ ] **Step 2: Update `LoginPage.jsx` to use Vietnamese text with diacritics**

Replace visible auth strings in `frontend/src/pages/LoginPage.jsx` with Vietnamese equivalents such as:

```jsx
<h1>Đăng nhập</h1>
<label htmlFor="email">Email</label>
<label htmlFor="password">Mật khẩu</label>
<button type="submit">Đăng nhập vào hệ thống</button>
<p>Bạn chưa có tài khoản?</p>
<Link to="/register">Đăng ký ngay</Link>
```

If the page currently contains placeholder status or helper text, rewrite it into concise Vietnamese with proper diacritics instead of English.

- [ ] **Step 3: Update `RegisterPage.jsx` to use Vietnamese text with diacritics**

Replace visible auth strings in `frontend/src/pages/RegisterPage.jsx` with Vietnamese equivalents such as:

```jsx
<h1>Đăng ký</h1>
<label htmlFor="fullName">Họ và tên</label>
<label htmlFor="email">Email</label>
<label htmlFor="password">Mật khẩu</label>
<button type="submit">Tạo tài khoản</button>
<p>Bạn đã có tài khoản?</p>
<Link to="/login">Đăng nhập</Link>
```

If there are inline validation or request-state messages on the page, convert them to Vietnamese with diacritics as well.

- [ ] **Step 4: Run frontend smoke verification**

Run:

```bash
make up
curl -I http://localhost:3000
```

Expected:

```text
HTTP/1.1 200 OK
```

Then manually verify in the browser that the login and register screens show Vietnamese text with diacritics and no obvious English placeholders remain on those screens.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/LoginPage.jsx frontend/src/pages/RegisterPage.jsx
git commit -m "feat: localize auth screens to vietnamese"
```

---

## Self-Review

### Spec Coverage

- self-register `User`: Task 4
- JWT login: Task 3 and Task 4
- refresh token storage and rotation: Task 2, Task 3, Task 4
- logout current session and logout all: Task 4 and Task 5
- `me` and change password: Task 4 and Task 5
- admin bootstrap from env: Task 1 and Task 2
- admin-only user list and active toggle: Task 5
- Vietnamese auth UI text: Task 7
- runtime verification: Task 6 and Task 7

### Placeholder Scan

- No `TBD` or `TODO`
- All commands, file paths, and target classes are explicit
- Each code-changing step includes concrete code

### Type Consistency Check

- `AuthResponse` is the single token response type
- `RefreshTokenRequest` is used by refresh and logout
- `JwtOptions` and `AdminSeedOptions` names stay consistent across config, DI, and services
- `IUserRepository` and `IRefreshTokenRepository` stay the only new persistence interfaces
