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
