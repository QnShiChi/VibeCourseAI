using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.Modules;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class ModuleService : IModuleService
{
    private readonly IModuleRepository _moduleRepository;

    public ModuleService(IModuleRepository moduleRepository)
    {
        _moduleRepository = moduleRepository;
    }

    public async Task<ModuleStructureResponse?> UpdateAsync(Guid id, UpdateModuleRequest request)
    {
        var module = await _moduleRepository.GetByIdAsync(id);
        if (module is null)
        {
            return null;
        }

        module.Title = request.Title.Trim();
        module.Description = request.Description.Trim();
        module.UpdatedAt = DateTime.UtcNow;
        await _moduleRepository.SaveChangesAsync();

        return new ModuleStructureResponse
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            OrderIndex = module.OrderIndex,
            Lessons = module.Lessons
                .OrderBy(lesson => lesson.OrderIndex)
                .Select(lesson => new LessonStructureResponse
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    Description = lesson.Description,
                    OrderIndex = lesson.OrderIndex,
                    ContentSeed = lesson.ContentSeed
                })
                .ToList()
        };
    }
}
