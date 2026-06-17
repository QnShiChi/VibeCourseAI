using CourseVideo.API.DTOs.Syllabuses;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;

namespace CourseVideo.API.Services;

public class SyllabusService : ISyllabusService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".docx", ".pdf"
    };

    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IWebHostEnvironment _environment;

    public SyllabusService(ISyllabusRepository syllabusRepository, IWebHostEnvironment environment)
    {
        _syllabusRepository = syllabusRepository;
        _environment = environment;
    }

    public async Task<ImportSyllabusResponse> ImportAsync(ImportSyllabusRequest request, Guid uploadedByUserId, string uploadedByName)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Tiêu đề đề cương là bắt buộc.");
        }

        if (request.File is null || request.File.Length == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn file đề cương hợp lệ.");
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Chỉ hỗ trợ file pdf, docx hoặc txt.");
        }

        var storageDirectory = Path.Combine(_environment.ContentRootPath, "storage", "syllabuses");
        Directory.CreateDirectory(storageDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(storageDirectory, storedFileName);
        // await using dùng xong thì sẽ tự động đóng lại
        await using (var fileStream = File.Create(fullPath)) // Mở 1 file trên ổ đĩa để ghi nội dung upload vào, đảm bảo file được tạo mới và không bị ghi đè
        {
            await request.File.CopyToAsync(fileStream);
            await fileStream.FlushAsync(); // Bước này là optional vì using sẽ tự động đóng fileStream, nhưng để chắc chắn dữ liệu đã được ghi vào ổ đĩa thì có thể gọi FlushAsync() trước khi đóng
        }

        string extractedText;
        try
        {
            extractedText = await ExtractTextAsync(fullPath, extension);
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException($"Không thể trích xuất nội dung từ file {extension.ToLowerInvariant()}.");
            }
        }
        catch
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            throw;
        }

        var syllabus = new Syllabus
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            OriginalFileName = request.File.FileName,
            StoredFileName = storedFileName,
            FilePath = Path.Combine("storage", "syllabuses", storedFileName).Replace("\\", "/"),
            FileType = extension.TrimStart('.').ToLowerInvariant(),
            FileSize = request.File.Length,
            ExtractedText = extractedText,
            UploadedByUserId = uploadedByUserId
        };

        await _syllabusRepository.AddAsync(syllabus);
        await _syllabusRepository.SaveChangesAsync();

        return new ImportSyllabusResponse
        {
            Id = syllabus.Id,
            Title = syllabus.Title,
            Description = syllabus.Description,
            OriginalFileName = syllabus.OriginalFileName,
            FileType = syllabus.FileType,
            FileSize = syllabus.FileSize,
            ExtractedText = syllabus.ExtractedText,
            UploadedByName = uploadedByName,
            CreatedAt = syllabus.CreatedAt
        };
    }

    public async Task<IReadOnlyList<SyllabusListItemResponse>> GetAllAsync()
    {
        var syllabuses = await _syllabusRepository.GetAllAsync();
        return syllabuses.Select(MapListItem).ToList();
    }

    public async Task<SyllabusDetailResponse?> GetByIdAsync(Guid id)
    {
        var syllabus = await _syllabusRepository.GetByIdAsync(id);
        return syllabus is null ? null : MapDetail(syllabus);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var syllabus = await _syllabusRepository.GetByIdAsync(id);
        if (syllabus is null)
        {
            return false;
        }

        var fullPath = Path.Combine(_environment.ContentRootPath, syllabus.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await _syllabusRepository.DeleteAsync(syllabus);
        await _syllabusRepository.SaveChangesAsync();
        return true;
    }

    private static SyllabusListItemResponse MapListItem(Syllabus syllabus)
    {
        return new SyllabusListItemResponse
        {
            Id = syllabus.Id,
            Title = syllabus.Title,
            OriginalFileName = syllabus.OriginalFileName,
            FileType = syllabus.FileType,
            FileSize = syllabus.FileSize,
            CreatedAt = syllabus.CreatedAt,
            UploadedByName = syllabus.UploadedByUser?.FullName ?? string.Empty
        };
    }

    private static SyllabusDetailResponse MapDetail(Syllabus syllabus)
    {
        return new SyllabusDetailResponse
        {
            Id = syllabus.Id,
            Title = syllabus.Title,
            Description = syllabus.Description,
            OriginalFileName = syllabus.OriginalFileName,
            StoredFileName = syllabus.StoredFileName,
            FilePath = syllabus.FilePath,
            FileType = syllabus.FileType,
            FileSize = syllabus.FileSize,
            ExtractedText = syllabus.ExtractedText,
            UploadedByUserId = syllabus.UploadedByUserId,
            UploadedByName = syllabus.UploadedByUser?.FullName ?? string.Empty,
            CreatedAt = syllabus.CreatedAt
        };
    }

    private static async Task<string> ExtractTextAsync(string filePath, string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => (await File.ReadAllTextAsync(filePath)).Trim(),
            ".docx" => ExtractDocxText(filePath),
            ".pdf" => ExtractPdfText(filePath),
            _ => throw new InvalidOperationException("Định dạng file không được hỗ trợ.")
        };
    }

    private static string ExtractDocxText(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        return document.MainDocumentPart?.Document.Body?.InnerText?.Trim()
            ?? throw new InvalidOperationException("Không thể trích xuất nội dung từ file DOCX.");
    }

    private static string ExtractPdfText(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var text = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
        return string.IsNullOrWhiteSpace(text)
            ? throw new InvalidOperationException("Không thể trích xuất nội dung từ file PDF.")
            : text.Trim();
    }
}
