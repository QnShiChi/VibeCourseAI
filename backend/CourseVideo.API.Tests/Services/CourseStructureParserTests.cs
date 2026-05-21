using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class CourseStructureParserTests
{
    [Fact]
    public void Parse_ShouldCreateModulesAndLessons_WhenHeadingsExist()
    {
        var parser = new CourseStructureParser();
        var text = "Chuong 1: Tong quan\nBai 1: Gioi thieu\nNoi dung bai mot\nBai 2: Nen tang\nNoi dung bai hai";

        var result = parser.Parse(text);

        result.Modules.Should().HaveCount(1);
        result.Modules[0].Title.Should().Be("Chuong 1: Tong quan");
        result.Modules[0].Lessons.Should().HaveCount(2);
        result.Modules[0].Lessons[0].Title.Should().Be("Bai 1: Gioi thieu");
        result.Modules[0].Lessons[0].ContentSeed.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Parse_ShouldFallbackToDefaultModule_WhenNoHeadingsExist()
    {
        var parser = new CourseStructureParser();
        var text = "Khoi kien thuc 1\nDoan noi dung A\n\nDoan noi dung B";

        var result = parser.Parse(text);

        result.Modules.Should().ContainSingle(module => module.Title == "Tong quan khoa hoc");
        result.Modules[0].Lessons.Should().NotBeEmpty();
        result.Modules[0].Lessons.Should().OnlyContain(lesson => !string.IsNullOrWhiteSpace(lesson.ContentSeed));
    }
}
