using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterPromptFactoryTests
{
    [Fact]
    public void Create_ShouldRequireRicherLessonBreakdownInPrompt()
    {
        var factory = new OpenRouterPromptFactory();

        var request = factory.Create("openai/gpt-4.1-mini", "Noi dung de cuong");
        var prompt = string.Join("\n", request.Messages.Select(message => message.Content));

        prompt.Should().NotContain("3-6 lesson");
        prompt.Should().Contain("muc do chi tiet");
        prompt.Should().Contain("nhieu muc hoc");
        prompt.Should().Contain("Tuan");
        prompt.Should().Contain("Buoi");
        prompt.Should().Contain("Chu de");
    }
}
