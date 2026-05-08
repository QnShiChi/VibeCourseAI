using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task RevokeAllByUserIdAsync(Guid userId, string? revokedByIp);
    Task SaveChangesAsync();
}
