using CourseVideo.API.DTOs.Categories;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryListItemResponse>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? sort)
    {
        var categories = await _categoryService.GetAdminCategoriesAsync(status, search, sort);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryListItemResponse>> Create([FromBody] UpsertCategoryRequest request)
    {
        try
        {
            var created = await _categoryService.CreateAsync(request);
            return Ok(created);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCategoryListItemResponse>> Update(Guid id, [FromBody] UpsertCategoryRequest request)
    {
        try
        {
            var updated = await _categoryService.UpdateAsync(id, request);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderCategoriesRequest request)
    {
        try
        {
            await _categoryService.ReorderAsync(request.CategoryIds);
            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _categoryService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}
