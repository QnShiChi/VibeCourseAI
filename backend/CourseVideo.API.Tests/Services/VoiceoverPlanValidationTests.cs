using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class VoiceoverPlanValidationTests
{
    [Fact]
    public void ParseAndValidate_AcceptsCamelCasePayload()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate(
            "{\"estimatedDurationMinutes\":8,\"tone\":\"Clear\",\"pacing\":\"Moderate\",\"targetAudience\":\"Students\",\"pronunciationNotes\":\"OOP\"}"
        );

        action.Should().NotThrow();
    }

    [Fact]
    public void ParseAndValidate_AcceptsPascalCasePayload()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate(
            "{\"EstimatedDurationMinutes\":8,\"Tone\":\"Clear\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        );

        action.Should().NotThrow();
    }

    [Fact]
    public void ParseAndValidate_Throws_WhenJsonIsMalformed()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate("{bad json}");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Voiceover plan JSON không hợp lệ.");
    }

    [Fact]
    public void ParseAndValidate_Throws_WhenFieldsAreInvalid()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate(
            "{\"EstimatedDurationMinutes\":0,\"Tone\":\"\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        );

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Voiceover plan phải có estimatedDurationMinutes, tone, pacing, targetAudience và pronunciationNotes hợp lệ.");
    }
}
