using SkiaSharp;
using CourseVideo.API.DTOs.VideoWorker;

namespace CourseVideo.API.Services.Video;

public class RenderService : IRenderService
{
    private readonly IImageProvider _imageProvider;

    public RenderService(IImageProvider imageProvider)
    {
        _imageProvider = imageProvider;
    }

    public async Task RenderSlidePngAsync(string outputPath, SlideItem slide)
    {
        var imageBytes = await _imageProvider.FetchImageForSlideAsync(slide.ImageKeyword);
        SKBitmap? illustration = null;
        if (imageBytes != null)
        {
            illustration = SKBitmap.Decode(imageBytes);
        }

        bool hasImage = illustration != null;
        SKColor bgColor = new SKColor(18, 18, 26);

        if (hasImage)
        {
            // Extract dominant color
            using var tinyBitmap = illustration!.Resize(new SKImageInfo(1, 1), new SKSamplingOptions(SKFilterMode.Linear));
            if (tinyBitmap != null)
            {
                var pixel = tinyBitmap.GetPixel(0, 0);
                bgColor = new SKColor(
                    (byte)Math.Clamp(pixel.Red * 0.15, 0, 255),
                    (byte)Math.Clamp(pixel.Green * 0.15, 0, 255),
                    (byte)Math.Clamp(pixel.Blue * 0.20, 0, 255)
                );
            }
        }

        var info = new SKImageInfo(1280, 720);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        canvas.Clear(bgColor);

        int panelMargin = 56;

        // Shadow
        for (int i = 1; i <= 4; i++)
        {
            int shadowOffset = 8 + i * 6;
            byte shadowAlpha = (byte)(50 / i);
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, shadowAlpha),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(
                panelMargin + shadowOffset,
                panelMargin + shadowOffset,
                1280 - panelMargin * 2,
                720 - panelMargin * 2,
                32, 32, shadowPaint);
        }

        // Panel (Glassmorphism)
        using var panelBgPaint = new SKPaint
        {
            Color = new SKColor(30, 30, 47, 210),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(
            panelMargin, panelMargin,
            1280 - panelMargin * 2, 720 - panelMargin * 2,
            32, 32, panelBgPaint);

        using var panelBorderPaint = new SKPaint
        {
            Color = new SKColor(90, 90, 120, 255),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawRoundRect(
            panelMargin, panelMargin,
            1280 - panelMargin * 2, 720 - panelMargin * 2,
            32, 32, panelBorderPaint);

        // Text rendering
        using var titleTypeface = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) 
            ?? SKTypeface.Default;
        using var bodyTypeface = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.Default;

        using var titleFont = new SKFont(titleTypeface, 42);
        using var bodyFont = new SKFont(bodyTypeface, 28);
        using var titlePaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var bodyPaint = new SKPaint { Color = new SKColor(176, 176, 192), IsAntialias = true };
        using var bulletPaint = new SKPaint { Color = new SKColor(0, 229, 255), IsAntialias = true };

        int textWidthTitle = hasImage ? 20 : 34;
        int textWidthBullet = hasImage ? 30 : 54;

        var titleTitle = string.IsNullOrWhiteSpace(slide.Title) ? "Untitled slide" : slide.Title;
        var titleLines = WrapText(titleTitle, textWidthTitle).Take(3).ToList();
        
        float y = 120;
        foreach (var line in titleLines)
        {
            canvas.DrawText(line, 96, y, titleFont, titlePaint);
            y += 52;
        }

        y += 24;
        float bulletIndent = 122;

        foreach (var bullet in slide.BulletPoints.Take(8))
        {
            var wrapped = WrapText(bullet, textWidthBullet);
            if (!wrapped.Any()) wrapped.Add(bullet);
            
            // Draw bullet
            canvas.DrawOval(102, y - 6, 6, 6, bulletPaint); // roughly center with text

            for (int i = 0; i < wrapped.Count; i++)
            {
                canvas.DrawText(wrapped[i], bulletIndent, y + (i * 34), bodyFont, bodyPaint);
            }
            y += Math.Max(42, wrapped.Count * 34 + 14);
        }

        // Draw image
        if (hasImage && illustration != null)
        {
            int targetW = 480;
            int targetH = 520;

            // Crop/scale illustration to fill target
            float scale = Math.Max((float)targetW / illustration.Width, (float)targetH / illustration.Height);
            int sw = (int)(targetW / scale);
            int sh = (int)(targetH / scale);
            int sx = (illustration.Width - sw) / 2;
            int sy = (illustration.Height - sh) / 2;
            
            using var croppedImage = new SKBitmap(targetW, targetH);
            using var resizerCanvas = new SKCanvas(croppedImage);
            resizerCanvas.DrawBitmap(illustration, new SKRect(sx, sy, sx + sw, sy + sh), new SKRect(0, 0, targetW, targetH));

            int imgX = 1280 - panelMargin - targetW - 40;
            int imgY = (720 - targetH) / 2;

            // Rounded corner mask
            using var imgPaint = new SKPaint { IsAntialias = true };
            using var shader = SKShader.CreateBitmap(croppedImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, SKMatrix.CreateTranslation(imgX, imgY));
            imgPaint.Shader = shader;

            canvas.DrawRoundRect(imgX, imgY, targetW, targetH, 24, 24, imgPaint);
            illustration.Dispose();
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    private List<string> WrapText(string text, int maxChars)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word)) continue;
            
            if (currentLine.Length + word.Length + 1 <= maxChars)
            {
                currentLine += (string.IsNullOrEmpty(currentLine) ? "" : " ") + word;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
        }
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }
        
        return lines;
    }
}
