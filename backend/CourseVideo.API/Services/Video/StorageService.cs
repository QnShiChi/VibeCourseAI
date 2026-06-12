using Microsoft.Extensions.Hosting;

namespace CourseVideo.API.Services.Video;

public class StorageService : IStorageService
{
    private readonly string _storageRoot;
    private readonly string _storageVideoDir;
    private readonly string _storageVideoFramesDir;

    public StorageService(IHostEnvironment environment)
    {
        _storageRoot = Path.Combine(environment.ContentRootPath, "storage");
        _storageVideoDir = Path.Combine(_storageRoot, "video");
        _storageVideoFramesDir = Path.Combine(_storageVideoDir, "frames");
    }

    private void EnsureVideoDirs()
    {
        if (!Directory.Exists(_storageVideoDir))
            Directory.CreateDirectory(_storageVideoDir);
            
        if (!Directory.Exists(_storageVideoFramesDir))
            Directory.CreateDirectory(_storageVideoFramesDir);
    }

    public string BuildVideoOutputPath(string lessonId)
    {
        EnsureVideoDirs();
        return Path.Combine(_storageVideoDir, $"{lessonId}.mp4");
    }

    public string BuildVideoFramesDir(string lessonId)
    {
        EnsureVideoDirs();
        var framesDir = Path.Combine(_storageVideoFramesDir, lessonId);
        if (!Directory.Exists(framesDir))
            Directory.CreateDirectory(framesDir);
        return framesDir;
    }

    public string ResolveStoragePathFromUrl(string storageUrl)
    {
        if (!storageUrl.StartsWith("/storage/"))
        {
            throw new ArgumentException("Asset URL phải bắt đầu bằng /storage/.");
        }
        
        var relative = storageUrl.Substring("/storage/".Length);
        return Path.Combine(_storageRoot, relative);
    }

    public string GetStorageDirectory()
    {
        return _storageRoot;
    }
}
