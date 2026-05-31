using CourseVideo.API.DTOs.AudioWorker;

namespace CourseVideo.API.Services.Audio;

public interface INarrationService
{
    List<NarrationSegment> BuildNarrationSegments(string teachingScript, string slideOutlineJson, string voiceoverPlanJson);
}
