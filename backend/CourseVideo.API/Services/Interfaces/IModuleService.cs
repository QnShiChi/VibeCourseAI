using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.Modules;

namespace CourseVideo.API.Services.Interfaces;

public interface IModuleService
{
    Task<ModuleStructureResponse?> UpdateAsync(Guid id, UpdateModuleRequest request);
}
