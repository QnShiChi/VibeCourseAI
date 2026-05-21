using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonAudioValidationTests
{
    [Fact]
    public void ValidateReadyForAudio_Throws_WhenTeachingScriptMissing()
    {
        var lesson = new Lesson
        {
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Intro\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"N\"}]",
            VoiceoverPlanJson = "{\"EstimatedDurationMinutes\":8,\"Tone\":\"Clear\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        };

        var action = () => LessonAudioValidation.ValidateReadyForAudio(lesson);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Lesson phải có teaching script trước khi generate audio.");
    }

    [Fact]
    public void ValidateReadyForAudio_DoesNotThrow_WhenLessonHasRequiredInputs()
    {
        var lesson = new Lesson
        {
            TeachingScript = "Script",
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Intro\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"N\"}]",
            VoiceoverPlanJson = "{\"EstimatedDurationMinutes\":8,\"Tone\":\"Clear\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        };

        var action = () => LessonAudioValidation.ValidateReadyForAudio(lesson);

        action.Should().NotThrow();
    }
}
