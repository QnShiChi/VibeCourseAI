using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class LessonsControllerTests
{
    [Fact]
    public async Task GetGeneratedContent_ReturnsOk_WhenLessonContentExists()
    {
        var lessonService = new Mock<ILessonService>();
        var lessonId = Guid.NewGuid();
        lessonService.Setup(x => x.GetGeneratedContentAsync(lessonId))
            .ReturnsAsync(new LessonGeneratedContentResponse
            {
                LessonId = lessonId,
                LessonTitle = "Bai 1",
                TeachingScript = "Script",
                ContentGenerationStatus = "Completed"
            });

        var controller = new LessonsController(
            lessonService.Object,
            Mock.Of<ILessonAudioGenerationService>(),
            Mock.Of<ILessonVideoGenerationService>());

        var result = await controller.GetGeneratedContent(lessonId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<LessonGeneratedContentResponse>();
    }

    [Fact]
    public async Task UpdateGeneratedContent_ReturnsOk_WhenPayloadIsValid()
    {
        var lessonService = new Mock<ILessonService>();
        var lessonId = Guid.NewGuid();
        lessonService.Setup(x => x.UpdateGeneratedContentAsync(lessonId, It.IsAny<UpdateLessonGeneratedContentRequest>()))
            .ReturnsAsync(new LessonGeneratedContentResponse
            {
                LessonId = lessonId,
                LessonTitle = "Bai 1",
                TeachingScript = "Script moi",
                ContentGenerationStatus = "ManuallyEdited"
            });

        var controller = new LessonsController(
            lessonService.Object,
            Mock.Of<ILessonAudioGenerationService>(),
            Mock.Of<ILessonVideoGenerationService>());

        var result = await controller.UpdateGeneratedContent(lessonId, new UpdateLessonGeneratedContentRequest
        {
            TeachingScript = " Script moi ",
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"S1\",\"BulletPoints\":[\"BP1\"],\"SpeakerNotes\":\"Note\"}]",
            VoiceoverPlanJson = "{\"EstimatedDurationMinutes\":5,\"Tone\":\"clear\",\"Pacing\":\"normal\",\"TargetAudience\":\"all\",\"PronunciationNotes\":\"none\"}"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<LessonGeneratedContentResponse>();
    }
}
