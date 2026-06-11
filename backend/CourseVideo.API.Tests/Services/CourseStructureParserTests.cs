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

    [Fact]
    public void Parse_ShouldRecognizeWeekAndSessionHeadings_WhenSyllabusUsesTeachingScheduleFormat()
    {
        var parser = new CourseStructureParser();
        var text = """
                   Tuan 1: Nhap mon
                   Buoi 1: Tong quan ve mon hoc
                   Noi dung mo dau
                   Buoi 2: Cai dat moi truong
                   Huong dan thuc hanh
                   Tuan 2: Nen tang
                   Buoi 3: Bien va kieu du lieu
                   Bai tap ap dung
                   """;

        var result = parser.Parse(text);

        result.Modules.Should().HaveCount(2);
        result.Modules[0].Title.Should().Be("Tuan 1: Nhap mon");
        result.Modules[0].Lessons.Should().HaveCount(2);
        result.Modules[0].Lessons[0].Title.Should().Be("Buoi 1: Tong quan ve mon hoc");
        result.Modules[1].Lessons.Should().ContainSingle(lesson => lesson.Title == "Buoi 3: Bien va kieu du lieu");
    }

    [Fact]
    public void Parse_ShouldSplitFallbackBulletOutlineIntoManyLessons_WhenNoFormalLessonHeadingsExist()
    {
        var parser = new CourseStructureParser();
        var text = """
                   Noi dung hoc phan Lap trinh huong doi tuong
                   - Tong quan ve OOP
                   - Lop va doi tuong
                   - Dong goi
                   - Ke thua
                   - Da hinh
                   - Truu tuong
                   """;

        var result = parser.Parse(text);

        result.Modules.Should().ContainSingle(module => module.Title == "Tong quan khoa hoc");
        result.Modules[0].Lessons.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void Parse_ShouldExpandLongLessonIntoSubLessons_WhenLessonContainsEnumeratedSections()
    {
        var parser = new CourseStructureParser();
        var text = """
                   Tuan 1: Nhap mon
                   Buoi 1: Tong quan ve lap trinh huong doi tuong
                   1. Khai niem va dac diem cua OOP
                   Giai thich cac thanh phan cot loi
                   2. Lop va doi tuong
                   Vi du thuc te va cach mo hinh hoa
                   3. Dong goi
                   Lien he voi bao ve du lieu
                   4. Ke thua
                   Ap dung trong tai su dung ma nguon
                   """;

        var result = parser.Parse(text);

        result.Modules.Should().ContainSingle();
        result.Modules[0].Lessons.Should().HaveCountGreaterThanOrEqualTo(4);
        result.Modules[0].Lessons.Should().Contain(lesson => lesson.Title.Contains("Khai niem", StringComparison.OrdinalIgnoreCase));
        result.Modules[0].Lessons.Should().Contain(lesson => lesson.Title.Contains("Dong goi", StringComparison.OrdinalIgnoreCase));
    }
}
