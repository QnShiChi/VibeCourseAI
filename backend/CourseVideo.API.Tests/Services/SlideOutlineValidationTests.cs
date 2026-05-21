using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class SlideOutlineValidationTests
{
    [Fact]
    public void ParseAndValidate_Throws_WhenJsonIsMalformed()
    {
        var action = () => SlideOutlineValidation.ParseAndValidate("{bad json}");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Slide outline JSON không hợp lệ.");
    }

    [Fact]
    public void ParseAndValidate_Throws_WhenSlideMissingRequiredFields()
    {
        var action = () => SlideOutlineValidation.ParseAndValidate(
            "[{\"slideNumber\":1,\"title\":\"\",\"bulletPoints\":[],\"speakerNotes\":\"\"}]"
        );

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Slide outline phải có title, bulletPoints và speakerNotes hợp lệ.");
    }
}
