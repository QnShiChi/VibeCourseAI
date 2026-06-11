using CourseVideo.API.Services.Audio;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class EdgeTtsServiceTests
{
    [Fact]
    public void SplitIntoChunksForSynthesis_PreservesWholeSentencesBeforeFallbackSplitting()
    {
        const string text = "Chào mừng các bạn đến với bài học 'Một số khái niệm cơ bản' trong học phần Lập trình hướng đối tượng. Mục tiêu của bài học này là giúp các bạn nắm vững những khái niệm nền tảng nhất, làm tiền đề cho các bài học sau. Chúng ta sẽ đi qua các định nghĩa về Đối tượng, Lớp, Thuộc tính, Phương thức và cuối cùng là so sánh sự khác biệt giữa lập trình hướng đối tượng và lập trình cấu trúc.";

        var chunks = EdgeTtsService.SplitIntoChunksForSynthesis(text, 80);

        chunks.Should().HaveCount(3);
        chunks[0].Should().Be("Chào mừng các bạn đến với bài học 'Một số khái niệm cơ bản' trong học phần Lập trình hướng đối tượng.");
        chunks[1].Should().Be("Mục tiêu của bài học này là giúp các bạn nắm vững những khái niệm nền tảng nhất, làm tiền đề cho các bài học sau.");
        chunks[2].Should().Be("Chúng ta sẽ đi qua các định nghĩa về Đối tượng, Lớp, Thuộc tính, Phương thức và cuối cùng là so sánh sự khác biệt giữa lập trình hướng đối tượng và lập trình cấu trúc.");
    }
}
