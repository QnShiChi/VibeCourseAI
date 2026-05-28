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

    public async Task RenderSlidePngAsync(string outputPath, SlideItem slide, CancellationToken cancellationToken = default)
    {
        var imageBytes = await _imageProvider.FetchImageForSlideAsync(slide.ImageKeyword, cancellationToken);
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
        else
        {
            DrawProceduralIllustration(canvas, slide);
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

    private static void DrawProceduralIllustration(SKCanvas canvas, SlideItem slide)
    {
        const int targetW = 480;
        const int targetH = 520;
        const int panelMargin = 56;
        var imgX = 1280 - panelMargin - targetW - 40;
        var imgY = (720 - targetH) / 2;

        var seed = Math.Abs((slide.ImageKeyword ?? slide.Title ?? "lesson").GetHashCode());
        var baseHue = seed % 360;
        var accentColor = SKColor.FromHsl(baseHue, 75, 60);
        var secondaryColor = SKColor.FromHsl((baseHue + 35) % 360, 70, 45);
        var tertiaryColor = SKColor.FromHsl((baseHue + 320) % 360, 55, 70);

        using var clipPath = new SKPath();
        clipPath.AddRoundRect(new SKRect(imgX, imgY, imgX + targetW, imgY + targetH), 24, 24);
        canvas.Save();
        canvas.ClipPath(clipPath, antialias: true);

        using var gradient = SKShader.CreateLinearGradient(
            new SKPoint(imgX, imgY),
            new SKPoint(imgX + targetW, imgY + targetH),
            new[] { new SKColor(33, 36, 58), new SKColor(20, 24, 40) },
            null,
            SKShaderTileMode.Clamp);
        using var backgroundPaint = new SKPaint { Shader = gradient, IsAntialias = true };
        canvas.DrawRect(new SKRect(imgX, imgY, imgX + targetW, imgY + targetH), backgroundPaint);

        using var glowPaint = new SKPaint
        {
            Color = accentColor.WithAlpha(70),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 36)
        };
        canvas.DrawCircle(imgX + 120, imgY + 120, 88, glowPaint);
        canvas.DrawCircle(imgX + 360, imgY + 180, 104, glowPaint);

        using var linePaint = new SKPaint
        {
            Color = tertiaryColor.WithAlpha(130),
            StrokeWidth = 3,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var nodePaint = new SKPaint
        {
            Color = accentColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var secondaryNodePaint = new SKPaint
        {
            Color = secondaryColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        var points = new[]
        {
            new SKPoint(imgX + 96, imgY + 140),
            new SKPoint(imgX + 210, imgY + 96),
            new SKPoint(imgX + 334, imgY + 150),
            new SKPoint(imgX + 388, imgY + 276),
            new SKPoint(imgX + 284, imgY + 388),
            new SKPoint(imgX + 144, imgY + 356)
        };

        for (var i = 0; i < points.Length; i++)
        {
            var next = points[(i + 1) % points.Length];
            canvas.DrawLine(points[i], next, linePaint);
            canvas.DrawCircle(points[i], 14 + (i % 3) * 3, i % 2 == 0 ? nodePaint : secondaryNodePaint);
        }

        using var ringPaint = new SKPaint
        {
            Color = tertiaryColor.WithAlpha(180),
            StrokeWidth = 10,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawCircle(imgX + 240, imgY + 256, 86, ringPaint);
        canvas.DrawCircle(imgX + 240, imgY + 256, 42, linePaint);

        canvas.Restore();

        using var borderPaint = new SKPaint
        {
            Color = tertiaryColor.WithAlpha(120),
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRoundRect(imgX, imgY, targetW, targetH, 24, 24, borderPaint);
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
