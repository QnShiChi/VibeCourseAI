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
