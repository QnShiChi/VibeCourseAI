using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Tutoring;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonTutorSegmenterTests
{
    private readonly ILessonTutorSegmenter _segmenter = new LessonTutorSegmenter();

    [Fact]
    public void PushText_ReturnsSegment_WhenSentenceBoundaryArrives()
    {
        var flushed = new List<string>();
        flushed.AddRange(_segmenter.PushText("Day la cau thu nhat."));

        Assert.Single(flushed);
        Assert.Equal("Day la cau thu nhat.", flushed[0]);
    }

    [Fact]
    public void PushText_WaitsUntilThreshold_WhenNoPunctuationExists()
    {
        var longText = string.Concat(Enumerable.Repeat("motdoanvanbanratdai ", 12));

        var flushed = _segmenter.PushText(longText).ToList();

        Assert.NotEmpty(flushed);
        Assert.All(flushed, segment => Assert.False(string.IsNullOrWhiteSpace(segment)));
    }

    [Fact]
    public void FlushRemaining_ReturnsTail_WhenBufferStillHasText()
    {
        _segmenter.PushText("Doan cuoi chua co dau");

        var tail = _segmenter.FlushRemaining().ToList();

        Assert.Single(tail);
        Assert.Equal("Doan cuoi chua co dau.", tail[0]);
    }
}
