using CourseVideo.API.Services.Audio;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class NarrationServiceTests
{
    [Fact]
    public void BuildNarrationSegments_UsesTeachingScriptParagraphsInsteadOfSpeakerNotesAndBullets()
    {
        var service = new NarrationService();
        const string teachingScript = """
        Chúng ta bắt đầu với khái niệm lập trình hướng đối tượng và lý do cách tiếp cận này quan trọng.

        Tiếp theo, hãy tập trung vào bốn trụ cột cốt lõi để thấy cách mô hình hóa hành vi trong phần mềm.
        """;
        const string slideOutlineJson = """
        [
          {
            "slideNumber": 1,
            "title": "Giới thiệu",
            "bulletPoints": ["Định nghĩa", "Lợi ích"],
            "speakerNotes": "Ở slide này chúng ta đọc định nghĩa và lợi ích."
          },
          {
            "slideNumber": 2,
            "title": "Bốn trụ cột",
            "bulletPoints": ["Đóng gói", "Kế thừa"],
            "speakerNotes": "Trong slide này hãy nhìn vào bốn trụ cột."
          }
        ]
        """;

        var result = service.BuildNarrationSegments(teachingScript, slideOutlineJson, "{}");

        result.Should().HaveCount(2);
        result[0].NarrationText.Should().Be("Chúng ta bắt đầu với khái niệm lập trình hướng đối tượng và lý do cách tiếp cận này quan trọng.");
        result[1].NarrationText.Should().Be("Tiếp theo, hãy tập trung vào bốn trụ cột cốt lõi để thấy cách mô hình hóa hành vi trong phần mềm.");
        result.Select(segment => segment.NarrationText).Should().OnlyContain(text => !text.Contains("slide", StringComparison.OrdinalIgnoreCase));
        result.Select(segment => segment.NarrationText).Should().OnlyContain(text => !text.Contains("Định nghĩa", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildNarrationSegments_SplitsTeachingScriptAcrossSlidesWithoutInjectingSlideLanguage()
    {
        var service = new NarrationService();
        const string teachingScript = "Lập trình hướng đối tượng giúp tổ chức mã nguồn rõ ràng hơn. Cách tiếp cận này tập trung vào đối tượng và hành vi. Khi nắm chắc nền tảng, người học sẽ dễ mở rộng sang thiết kế hệ thống.";
        const string slideOutlineJson = """
        [
          {
            "slideNumber": 1,
            "title": "Ý 1",
            "bulletPoints": ["A"],
            "speakerNotes": "Ở slide này nhấn mạnh A."
          },
          {
            "slideNumber": 2,
            "title": "Ý 2",
            "bulletPoints": ["B"],
            "speakerNotes": "Ở slide này nhấn mạnh B."
          },
          {
            "slideNumber": 3,
            "title": "Ý 3",
            "bulletPoints": ["C"],
            "speakerNotes": "Ở slide này nhấn mạnh C."
          }
        ]
        """;

        var result = service.BuildNarrationSegments(teachingScript, slideOutlineJson, "{}");

        result.Should().HaveCount(3);
        result.Select(segment => segment.NarrationText).Should().OnlyContain(text => !text.Contains("slide", StringComparison.OrdinalIgnoreCase));
        string.Join(" ", result.Select(segment => segment.NarrationText))
            .Should()
            .Contain("Lập trình hướng đối tượng giúp tổ chức mã nguồn rõ ràng hơn.")
            .And.Contain("Cách tiếp cận này tập trung vào đối tượng và hành vi.")
            .And.Contain("Khi nắm chắc nền tảng, người học sẽ dễ mở rộng sang thiết kế hệ thống.");
    }

    [Fact]
    public void BuildNarrationSegments_StripsForbiddenSlideLanguageFromTeachingScriptBeforeAudio()
    {
        var service = new NarrationService();
        const string teachingScript = """
        Slide này giới thiệu định nghĩa cơ bản về trí tuệ nhân tạo và phạm vi ứng dụng của nó.

        Ở slide này, ta so sánh các giai đoạn phát triển quan trọng để thấy vì sao lĩnh vực này bùng nổ.
        """;
        const string slideOutlineJson = """
        [
          {
            "slideNumber": 1,
            "title": "Định nghĩa",
            "bulletPoints": ["A"],
            "speakerNotes": "Speaker notes 1"
          },
          {
            "slideNumber": 2,
            "title": "Lịch sử",
            "bulletPoints": ["B"],
            "speakerNotes": "Speaker notes 2"
          }
        ]
        """;

        var result = service.BuildNarrationSegments(teachingScript, slideOutlineJson, "{}");

        result.Should().HaveCount(2);
        result[0].NarrationText.Should().Be("Giới thiệu định nghĩa cơ bản về trí tuệ nhân tạo và phạm vi ứng dụng của nó.");
        result[1].NarrationText.Should().Be("Ta so sánh các giai đoạn phát triển quan trọng để thấy vì sao lĩnh vực này bùng nổ.");
        result.Select(segment => segment.NarrationText).Should().OnlyContain(text => !text.Contains("slide", StringComparison.OrdinalIgnoreCase));
    }
}
