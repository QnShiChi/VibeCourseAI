using System.Text.Json;

namespace CourseVideo.API.Services;

public static class SlideOutlineValidation
{
    public static void ParseAndValidate(string json)
    {
        try
        {
            var slides = JsonSerializer.Deserialize<List<SlideOutlineItem>>(json);
            if (slides is null || slides.Count == 0)
            {
                throw new InvalidOperationException("Slide outline phải có ít nhất một slide.");
            }

            if (slides.Any(slide =>
                string.IsNullOrWhiteSpace(slide.Title) ||
                slide.BulletPoints is null ||
                slide.BulletPoints.Count == 0 ||
                slide.BulletPoints.Any(string.IsNullOrWhiteSpace) ||
                string.IsNullOrWhiteSpace(slide.SpeakerNotes)))
            {
                throw new InvalidOperationException("Slide outline phải có title, bulletPoints và speakerNotes hợp lệ.");
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Slide outline JSON không hợp lệ.", exception);
        }
    }

    private sealed class SlideOutlineItem
    {
        public int SlideNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageKeyword { get; set; } = string.Empty;
        public List<string> BulletPoints { get; set; } = [];
        public string SpeakerNotes { get; set; } = string.Empty;
    }
}
