# Video Render V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an end-to-end video rendering pipeline that turns lesson slides plus lesson audio into an MP4, exposes admin video generation controls and progress, and makes published learner lessons play a real video via `VideoUrl`.

**Architecture:** `ASP.NET Core Web API` remains the orchestration layer for validation, job creation, progress tracking, and persistence. A new `video-worker` service receives a lesson render payload, renders slide PNGs, assembles a timeline based on audio segment durations, muxes lesson audio into MP4, and returns a storage-backed `VideoUrl`.

**Tech Stack:** ASP.NET Core Web API, EF Core/SQL Server, existing background job pattern, Python FastAPI worker, Pillow for slide-to-PNG rendering, FFmpeg CLI for image timeline + audio -> MP4, React/Vite frontend, Vitest, Python unittest.

---

### Task 1: Extend lesson video domain state and learner/admin DTOs

**Files:**
- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Modify: `backend/CourseVideo.API/DTOs/Courses/LessonStructureResponse.cs`
- Modify: `backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonAudioValidationTests.cs` or a new focused backend service test file if test infra is usable

- [ ] **Step 1: Write the failing test case or verification target**

Because the backend unit-test project has been partially unstable in this repo, first add or update a focused test only if restore works. Otherwise define the verification target now:

```csharp
// Expected shape after this task
new CourseLearnLessonResponse
{
    VideoUrl = "/storage/video/lesson-id.mp4",
    VideoGenerationStatus = "Completed",
    VideoGenerationError = ""
};
```

Expected verification later:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/
```

- [ ] **Step 2: Add lesson video status fields to the model**

Update `Lesson` with:

```csharp
public string VideoGenerationStatus { get; set; } = "NotGenerated";
public string? VideoGenerationError { get; set; }
public DateTime? VideoGeneratedAt { get; set; }
```

Keep the existing `VideoUrl` property.

- [ ] **Step 3: Ensure schema creation/upgrade adds the new lesson columns**

Update `DbInitializer` so the `Lessons` table includes:

```sql
VideoGenerationStatus nvarchar(50) NOT NULL DEFAULT 'NotGenerated'
VideoGenerationError nvarchar(2000) NULL
VideoGeneratedAt datetime2 NULL
```

Also add an idempotent ensure-columns block matching the current pattern already used for audio/content columns.

- [ ] **Step 4: Expose video status in admin and learner DTOs**

Update `LessonStructureResponse`:

```csharp
public string VideoGenerationStatus { get; set; } = string.Empty;
public string VideoGenerationError { get; set; } = string.Empty;
public string VideoUrl { get; set; } = string.Empty;
```

Update `CourseLearnLessonResponse`:

```csharp
public string? VideoUrl { get; set; }
public string VideoGenerationStatus { get; set; } = string.Empty;
public string VideoGenerationError { get; set; } = string.Empty;
```

- [ ] **Step 5: Run build to verify the API still compiles**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/
```

Expected: `0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Models/Lesson.cs \
  backend/CourseVideo.API/Data/DbInitializer.cs \
  backend/CourseVideo.API/DTOs/Courses/LessonStructureResponse.cs \
  backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs
git commit -m "feat: add lesson video generation state"
```

### Task 2: Add backend validation and service contracts for video generation

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Courses/GenerateLessonVideoResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Lessons/LessonVideoResponse.cs`
- Create: `backend/CourseVideo.API/Services/LessonVideoValidation.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonVideoGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonVideoJobQueue.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonService.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonVideoValidationTests.cs`

- [ ] **Step 1: Write the failing validation tests**

Create `LessonVideoValidationTests.cs` with cases like:

```csharp
[Fact]
public void ValidateReadyForVideoGeneration_Fails_WhenAudioUrlMissing()
{
    var lesson = new Lesson
    {
        SlideOutlineJson = "[]",
        AudioUrl = null,
        AudioSegmentsJson = "[]",
        AudioGenerationStatus = "Completed"
    };

    var result = LessonVideoValidation.ValidateReadyForVideoGeneration(lesson);

    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("Lesson chưa có audio để render video.");
}
```

And:

```csharp
[Fact]
public void ValidateReadyForVideoGeneration_Passes_WithSlidesAudioAndSegments()
```

- [ ] **Step 2: Run the test to verify it fails**

Run if the test project restores:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj \
  --filter LessonVideoValidationTests
```

Expected: fail because `LessonVideoValidation` does not exist yet or methods are missing.

- [ ] **Step 3: Implement the validation helper and DTOs**

Add validation result:

```csharp
public sealed class LessonVideoValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
}
```

Add validator:

```csharp
public static LessonVideoValidationResult ValidateReadyForVideoGeneration(Lesson lesson)
{
    if (string.IsNullOrWhiteSpace(lesson.SlideOutlineJson))
    {
        return new() { IsValid = false, ErrorMessage = "Lesson chưa có slide để render video." };
    }

    if (!string.Equals(lesson.AudioGenerationStatus, "Completed", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(lesson.AudioUrl))
    {
        return new() { IsValid = false, ErrorMessage = "Lesson chưa có audio để render video." };
    }

    if (string.IsNullOrWhiteSpace(lesson.AudioSegmentsJson))
    {
        return new() { IsValid = false, ErrorMessage = "Lesson chưa có metadata segment audio để render video." };
    }

    return new() { IsValid = true };
}
```

Add response DTOs:

```csharp
public class GenerateLessonVideoResponse
{
    public Guid JobId { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

```csharp
public class LessonVideoResponse
{
    public string VideoUrl { get; set; } = string.Empty;
    public string VideoGenerationStatus { get; set; } = string.Empty;
    public string VideoGenerationError { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Extend service interfaces**

In `ICourseService` add:

```csharp
Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
Task<GenerateLessonVideoResponse> RegenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
```

In `ILessonService` add:

```csharp
Task<LessonVideoResponse?> GetVideoAsync(Guid id);
```

Create:

```csharp
public interface ILessonVideoGenerationService
{
    Task<GenerateLessonVideoResponse> GenerateCourseVideoAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task ProcessCourseJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
```

And queue interface:

```csharp
public interface ILessonVideoJobQueue
{
    void Enqueue(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Run tests/build to verify the contract compiles**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/
```

If tests restore, also run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj \
  --filter LessonVideoValidationTests
```

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/DTOs/Courses/GenerateLessonVideoResponse.cs \
  backend/CourseVideo.API/DTOs/Lessons/LessonVideoResponse.cs \
  backend/CourseVideo.API/Services/LessonVideoValidation.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonVideoGenerationService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonVideoJobQueue.cs \
  backend/CourseVideo.API/Services/Interfaces/ICourseService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonService.cs \
  backend/CourseVideo.API.Tests/Services/LessonVideoValidationTests.cs
git commit -m "feat: add lesson video validation contracts"
```

### Task 3: Add backend video job orchestration and worker integration

**Files:**
- Create: `backend/CourseVideo.API/Services/LessonVideoJobQueue.cs`
- Create: `backend/CourseVideo.API/Services/LessonVideoGenerationWorker.cs`
- Create: `backend/CourseVideo.API/Services/LessonVideoGenerationService.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/IGenerationJobRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/GenerationJobRepository.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonAudioGenerationServiceTests.cs` or a new focused video service test file

- [ ] **Step 1: Write the failing service test or explicit verification target**

If backend tests are usable, add a focused test:

```csharp
[Fact]
public async Task GenerateLessonVideoAsync_CreatesPendingJob()
{
    // Arrange a lesson with completed audio and slides
    // Assert response contains job id and repository persists a pending video job
}
```

If tests are not usable, define verification target:

```text
POST /api/courses/{id}/generate-lesson-video should return 200 with { jobId, message }
and set lesson.VideoGenerationStatus to GeneratingVideo during processing.
```

- [ ] **Step 2: Add repository helpers for video jobs**

Mirror the audio-job pattern with methods like:

```csharp
Task<IReadOnlyList<GenerationJob>> GetRecoverableLessonVideoJobsAsync();
Task<bool> HasRunningLessonVideoJobForCourseAsync(Guid courseId);
```

Support job types:

```csharp
"GenerateLessonVideo"
"RegenerateLessonVideo"
```

- [ ] **Step 3: Implement queue, hosted worker, and generation service**

Follow the existing audio/content structure:

- `LessonVideoJobQueue` uses `Channel<Guid>`
- `LessonVideoGenerationWorker` dequeues job ids and calls `ILessonVideoGenerationService.ProcessCourseJobAsync`
- `LessonVideoGenerationService`:
  - creates jobs
  - marks lesson status
  - calls `video-worker`
  - updates `VideoUrl`, `VideoGenerationStatus`, `VideoGenerationError`, `VideoGeneratedAt`, and `Duration`

Introduce an internal payload type with snake_case serialization:

```csharp
private sealed class VideoWorkerLessonRequest
{
    [JsonPropertyName("lesson_id")]
    public string LessonId { get; set; } = string.Empty;

    [JsonPropertyName("lesson_title")]
    public string LessonTitle { get; set; } = string.Empty;

    [JsonPropertyName("slide_outline_json")]
    public string SlideOutlineJson { get; set; } = string.Empty;

    [JsonPropertyName("audio_url")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("audio_segments_json")]
    public string AudioSegmentsJson { get; set; } = string.Empty;
}
```

Worker response type:

```csharp
private sealed class VideoWorkerLessonResponse
{
    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; } = string.Empty;

    [JsonPropertyName("duration_seconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}
```

- [ ] **Step 4: Wire services into `CourseService`, `LessonService`, and DI**

Update `CourseService` methods to delegate to `ILessonVideoGenerationService`.

Update `LessonService.GetVideoAsync`:

```csharp
return new LessonVideoResponse
{
    VideoUrl = lesson.VideoUrl ?? string.Empty,
    VideoGenerationStatus = lesson.VideoGenerationStatus,
    VideoGenerationError = lesson.VideoGenerationError ?? string.Empty
};
```

Update `Program.cs`:

```csharp
builder.Services.AddSingleton<ILessonVideoJobQueue, LessonVideoJobQueue>();
builder.Services.AddHostedService<LessonVideoGenerationWorker>();
builder.Services.AddScoped<ILessonVideoGenerationService, LessonVideoGenerationService>();
builder.Services.AddHttpClient("VideoWorker", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["VIDEO_WORKER_BASE_URL"] ?? "http://video-worker:8001");
    client.Timeout = TimeSpan.FromMinutes(15);
});
```

- [ ] **Step 5: Build the backend and verify DI compiles**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/
```

Expected: `0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Services/LessonVideoJobQueue.cs \
  backend/CourseVideo.API/Services/LessonVideoGenerationWorker.cs \
  backend/CourseVideo.API/Services/LessonVideoGenerationService.cs \
  backend/CourseVideo.API/Repositories/Interfaces/IGenerationJobRepository.cs \
  backend/CourseVideo.API/Repositories/GenerationJobRepository.cs \
  backend/CourseVideo.API/Services/CourseService.cs \
  backend/CourseVideo.API/Services/LessonService.cs \
  backend/CourseVideo.API/Program.cs
git commit -m "feat: add backend video generation jobs"
```

### Task 4: Expose backend video endpoints and propagate video state in course payloads

**Files:**
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`
- Modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Test: existing API smoke tests or manual curl verification

- [ ] **Step 1: Add controller endpoints**

In `CoursesController` add:

```csharp
[HttpPost("{id:guid}/generate-lesson-video")]
public async Task<ActionResult<GenerateLessonVideoResponse>> GenerateLessonVideo(Guid id)
```

and:

```csharp
[HttpPost("{courseId:guid}/lessons/{lessonId:guid}/regenerate-lesson-video")]
public async Task<ActionResult<GenerateLessonVideoResponse>> RegenerateLessonVideo(Guid courseId, Guid lessonId)
```

In `LessonsController` add:

```csharp
[HttpGet("{id:guid}/video")]
public async Task<ActionResult<LessonVideoResponse>> GetVideo(Guid id)
```

- [ ] **Step 2: Map video state into admin and learner responses**

Update admin lesson mapping in `CourseService`:

```csharp
VideoGenerationStatus = lesson.VideoGenerationStatus,
VideoGenerationError = lesson.VideoGenerationError ?? string.Empty,
VideoUrl = lesson.VideoUrl ?? string.Empty
```

Update learner mapping:

```csharp
VideoUrl = lesson.VideoUrl,
VideoGenerationStatus = lesson.VideoGenerationStatus,
VideoGenerationError = lesson.VideoGenerationError ?? string.Empty,
Duration = lesson.Duration
```

- [ ] **Step 3: Build and smoke-test endpoints**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/
```

Then after redeploy:

```bash
curl -sS http://localhost:5000/api/lessons/<lesson-id>/video
```

Expected shape:

```json
{
  "videoUrl": "",
  "videoGenerationStatus": "NotGenerated",
  "videoGenerationError": ""
}
```

- [ ] **Step 4: Commit**

```bash
git add backend/CourseVideo.API/Controllers/CoursesController.cs \
  backend/CourseVideo.API/Controllers/LessonsController.cs \
  backend/CourseVideo.API/Services/CourseService.cs
git commit -m "feat: expose lesson video api endpoints"
```

### Task 5: Add video invalidation when source lesson content or audio changes

**Files:**
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
- Modify: `backend/CourseVideo.API/Services/LessonContentGenerationService.cs`
- Modify: `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
- Test: backend service test or manual verification via DB/API

- [ ] **Step 1: Write the failing invalidation test or explicit verification target**

Target behavior:

```csharp
lesson.VideoUrl = null;
lesson.VideoGenerationStatus = "NotGenerated";
lesson.VideoGenerationError = null;
lesson.VideoGeneratedAt = null;
```

This must happen whenever:

- generated content is manually edited
- lesson content is regenerated
- audio is regenerated

- [ ] **Step 2: Implement a shared reset pattern in each update path**

In `LessonService.UpdateGeneratedContentAsync`, after resetting audio, also reset video:

```csharp
lesson.VideoUrl = null;
lesson.VideoGenerationStatus = "NotGenerated";
lesson.VideoGenerationError = null;
lesson.VideoGeneratedAt = null;
```

In `LessonContentGenerationService.ApplyResult`, after resetting audio, reset video too.

In `LessonAudioGenerationService`, before or after successful audio replacement, clear any stale video:

```csharp
lesson.VideoUrl = null;
lesson.VideoGenerationStatus = "NotGenerated";
lesson.VideoGenerationError = null;
lesson.VideoGeneratedAt = null;
```

- [ ] **Step 3: Build and verify no regressions**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/
```

Expected: build passes.

- [ ] **Step 4: Commit**

```bash
git add backend/CourseVideo.API/Services/LessonService.cs \
  backend/CourseVideo.API/Services/LessonContentGenerationService.cs \
  backend/CourseVideo.API/Services/LessonAudioGenerationService.cs
git commit -m "feat: invalidate stale lesson videos"
```

### Task 6: Create the standalone `video-worker` service and Python tests

**Files:**
- Create: `video-worker/Dockerfile`
- Create: `video-worker/requirements.txt`
- Create: `video-worker/app/main.py`
- Create: `video-worker/app/models.py`
- Create: `video-worker/app/render.py`
- Create: `video-worker/app/timeline.py`
- Create: `video-worker/app/storage.py`
- Create: `video-worker/tests/test_timeline.py`
- Create: `video-worker/tests/test_render.py`

- [ ] **Step 1: Write failing Python tests for timeline and render outputs**

`test_timeline.py`:

```python
def test_build_slide_timeline_uses_audio_segment_durations():
    segments = [
        {"slideNumber": 1, "durationSeconds": 3.2},
        {"slideNumber": 2, "durationSeconds": 5.0},
    ]
    timeline = build_slide_timeline(segments)
    assert timeline[0]["start_seconds"] == 0
    assert timeline[0]["duration_seconds"] == 3.2
    assert timeline[1]["start_seconds"] == 3.2
```

`test_render.py`:

```python
def test_render_slide_png_creates_output_file(tmp_path: Path):
    output = tmp_path / "slide-1.png"
    render_slide_png(
        output,
        slide_number=1,
        title="Intro",
        bullet_points=["A", "B"],
    )
    assert output.exists()
    assert output.stat().st_size > 0
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
PYTHONPATH=video-worker python3 -m unittest discover -s video-worker/tests -v
```

Expected: import errors because the service files do not exist yet.

- [ ] **Step 3: Implement worker models, timeline builder, and slide PNG renderer**

`models.py` should include request/response shapes:

```python
class VideoWorkerLessonRequest(BaseModel):
    lesson_id: str
    lesson_title: str
    slide_outline_json: str
    audio_url: str
    audio_segments_json: str
```

Timeline helper:

```python
def build_slide_timeline(audio_segments: list[dict]) -> list[dict]:
    start = 0.0
    items = []
    for segment in audio_segments:
        duration = float(segment["durationSeconds"])
        items.append({
            "slide_number": segment["slideNumber"],
            "start_seconds": start,
            "duration_seconds": duration,
            "end_seconds": start + duration,
        })
        start += duration
    return items
```

Render helper should use Pillow to draw:

- slide number badge
- title
- bullet points

On a fixed-size canvas, e.g. `1280x720`.

- [ ] **Step 4: Add FastAPI route and response shape**

`main.py` should expose:

```python
@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/jobs/generate-lesson-video", response_model=VideoWorkerLessonResponse)
def generate_lesson_video(request: VideoWorkerLessonRequest):
```

At this step it is acceptable for the route to stop after generating PNGs and validating timeline while returning a temporary `NotImplementedError` for FFmpeg assembly, but the route shape, parsing, and tests must be in place before Task 7.

- [ ] **Step 5: Run tests to verify they pass**

Run:

```bash
PYTHONPATH=video-worker python3 -m unittest discover -s video-worker/tests -v
python3 -m py_compile video-worker/app/*.py
```

Expected: tests pass, Python files compile.

- [ ] **Step 6: Commit**

```bash
git add video-worker
git commit -m "feat: scaffold video worker"
```

### Task 7: Implement FFmpeg-based MP4 assembly in `video-worker`

**Files:**
- Modify: `video-worker/app/main.py`
- Modify: `video-worker/app/render.py`
- Modify: `video-worker/app/storage.py`
- Create or Modify: `video-worker/app/ffmpeg_pipeline.py`
- Test: `video-worker/tests/test_render.py`

- [ ] **Step 1: Write the failing integration-style worker test**

Add a test that uses a short local WAV fixture or generated WAV and expects an MP4 output path:

```python
def test_assemble_video_creates_mp4_from_png_and_audio(tmp_path: Path):
    png = tmp_path / "slide-1.png"
    wav = tmp_path / "audio.wav"
    mp4 = tmp_path / "lesson.mp4"
    render_slide_png(png, slide_number=1, title="Intro", bullet_points=["A"])
    write_test_wav(wav, seconds=1.0)
    assemble_video(
        slide_paths=[png],
        durations=[1.0],
        audio_path=wav,
        output_path=mp4,
    )
    assert mp4.exists()
    assert mp4.stat().st_size > 0
```

- [ ] **Step 2: Run the worker tests to verify the new test fails**

Run:

```bash
PYTHONPATH=video-worker python3 -m unittest discover -s video-worker/tests -v
```

Expected: failure because `assemble_video` does not exist yet.

- [ ] **Step 3: Implement FFmpeg assembly**

Add an assembly helper that:

1. writes a concat manifest or per-slide loop inputs
2. uses FFmpeg to create an MP4 timeline based on slide durations
3. muxes the lesson audio file into the MP4

Representative command shape:

```bash
ffmpeg -y \
  -loop 1 -t 3.2 -i slide-1.png \
  -loop 1 -t 5.0 -i slide-2.png \
  -i lesson.wav \
  -filter_complex "[0:v][1:v]concat=n=2:v=1:a=0,format=yuv420p[v]" \
  -map "[v]" -map 2:a \
  -c:v libx264 -c:a aac -shortest output.mp4
```

Hide this behind a Python helper:

```python
def assemble_video(slide_paths: list[Path], durations: list[float], audio_path: Path, output_path: Path) -> float:
```

- [ ] **Step 4: Update the worker route to generate final `video_url`**

Route flow:

- parse slides
- load/resolve local audio path from `/app/storage/...`
- render slide PNGs into `/app/storage/video/frames/<lesson-id>/`
- assemble MP4 into `/app/storage/video/{lesson-id}.mp4`
- return:

```python
return VideoWorkerLessonResponse(
    video_url=f"/storage/video/{lesson_id}.mp4",
    duration_seconds=duration_seconds,
    error_message=None,
)
```

- [ ] **Step 5: Run tests and a local worker smoke check**

Run:

```bash
PYTHONPATH=video-worker python3 -m unittest discover -s video-worker/tests -v
python3 -m py_compile video-worker/app/*.py
```

Then later in Docker:

```bash
curl -sS http://localhost:8001/health
```

Expected: `{"status":"ok"}`

- [ ] **Step 6: Commit**

```bash
git add video-worker/app video-worker/tests video-worker/requirements.txt video-worker/Dockerfile
git commit -m "feat: render lesson video in video worker"
```

### Task 8: Add `video-worker` to Docker Compose and environment configuration

**Files:**
- Modify: `docker-compose.yml`
- Modify: `.env.example`
- Modify: `.env`

- [ ] **Step 1: Add environment keys**

Add to env files:

```env
VIDEO_WORKER_BASE_URL=http://video-worker:8001
VIDEO_WORKER_PORT=8001
```

If FFmpeg-specific tuning is needed, optionally add:

```env
VIDEO_RENDER_WIDTH=1280
VIDEO_RENDER_HEIGHT=720
```

- [ ] **Step 2: Add the compose service**

Extend `docker-compose.yml`:

```yaml
  video-worker:
    build:
      context: ./video-worker
    container_name: course_video_worker
    restart: unless-stopped
    env_file:
      - .env
    ports:
      - "${VIDEO_WORKER_PORT}:8001"
    volumes:
      - ./storage:/app/storage
```

Ensure backend can resolve `http://video-worker:8001`.

- [ ] **Step 3: Build and run the new service**

Run:

```bash
docker compose build video-worker
docker compose up -d video-worker
curl -sS http://localhost:8001/health
```

Expected:

```json
{"status":"ok"}
```

- [ ] **Step 4: Commit**

```bash
git add docker-compose.yml .env .env.example
git commit -m "chore: add video worker service config"
```

### Task 9: Add admin video controls, progress wiring, and preview UI

**Files:**
- Modify: `frontend/src/api/lessonContentService.js`
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
- Modify: `frontend/src/components/course/LessonContentStatusBadge.jsx`
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Write the failing frontend tests**

Add tests similar to audio/content:

```jsx
it("starts lesson video generation and shows video controls", async () => {
  mockGenerateLessonVideo.mockResolvedValue({ jobId: "video-job-1", message: "Queued" });
  render(<CourseStructurePage />);
  await user.click(await screen.findByRole("button", { name: /Generate video/i }));
  expect(mockGenerateLessonVideo).toHaveBeenCalled();
  expect(await screen.findByText(/Queued/i)).toBeInTheDocument();
});
```

And:

```jsx
it("shows lesson video preview when videoUrl exists", async () => {
  // lesson payload contains videoUrl
  expect(await screen.findByText(/Video bài học/i)).toBeInTheDocument();
});
```

- [ ] **Step 2: Run the targeted frontend test to verify it fails**

Run in the writable verify copy if needed:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm test -- --run src/pages/CourseStructurePage.test.jsx
```

Expected: fail because video API methods/UI do not exist yet.

- [ ] **Step 3: Add API methods and page handlers**

In `lessonContentService.js` add:

```js
export async function generateCourseLessonVideo(courseId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/generate-lesson-video`);
  return data;
}

export async function regenerateLessonVideo(courseId, lessonId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/lessons/${lessonId}/regenerate-lesson-video`);
  return data;
}

export async function getLessonVideo(lessonId) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/video`);
  return data;
}
```

Update `CourseStructurePage.jsx` to:

- add course button `Generate video khóa học`
- add lesson button `Generate video` / `Generate lại video`
- reuse active job panel for video job types
- store `videoByLessonId`
- show `<video controls preload="none" src={...}>`

- [ ] **Step 4: Add video badges and styling**

Extend `LessonContentStatusBadge.jsx` for:

```jsx
case "GeneratingFrames":
case "RenderingVideo":
```

Add styles:

```css
.video-preview-card video {
  width: 100%;
  border-radius: 18px;
  background: #111;
}
```

- [ ] **Step 5: Run frontend tests and production build**

Run:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm test -- --run src/pages/CourseStructurePage.test.jsx
npm run build
```

Expected: tests pass, build succeeds.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/lessonContentService.js \
  frontend/src/pages/CourseStructurePage.jsx \
  frontend/src/components/course/LessonContentStatusBadge.jsx \
  frontend/src/pages/CourseStructurePage.test.jsx \
  frontend/src/styles/theme.css
git commit -m "feat: add admin video generation controls"
```

### Task 10: Update learner page to play rendered lesson videos

**Files:**
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Modify: `frontend/src/api/courseService.js` if payload shape needs adjustment
- Add or Modify: `frontend/src/pages/CourseLearnPage.test.jsx`

- [ ] **Step 1: Write the failing learner page test**

Add a test such as:

```jsx
it("renders a video player when the selected lesson has a videoUrl", async () => {
  mockGetCourseLearnPayload.mockResolvedValue({
    courseTitle: "OOP",
    selectedLessonId: "lesson-1",
    selectedLesson: {
      lessonId: "lesson-1",
      lessonTitle: "Intro",
      description: "Desc",
      videoUrl: "/storage/video/lesson-1.mp4",
      videoGenerationStatus: "Completed",
      videoGenerationError: "",
      contentSeed: "Seed"
    },
    modules: []
  });

  render(<CourseLearnPage />);
  expect(await screen.findByRole("video")).toBeInTheDocument();
});
```

If `role="video"` is awkward, assert on the `<video>` element via `container.querySelector("video")`.

- [ ] **Step 2: Run the learner test to verify it fails**

Run:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm test -- --run src/pages/CourseLearnPage.test.jsx
```

Expected: fail because the page still renders a placeholder shell.

- [ ] **Step 3: Replace placeholder with real video player and fallback state**

In `CourseLearnPage.jsx`:

```jsx
{selectedLesson.videoUrl ? (
  <video controls preload="metadata" src={selectedLesson.videoUrl}>
    Trình duyệt của bạn không hỗ trợ phát video.
  </video>
) : (
  <div className="learn-stage__player-shell">
    <div className="learn-stage__player-icon">⏳</div>
    <div className="learn-stage__player-text">
      <strong>{selectedLesson.lessonTitle}</strong>
      <span>Bài học đang được chuẩn bị video.</span>
    </div>
  </div>
)}
```

Do not remove existing content preview/sidebar behavior.

- [ ] **Step 4: Run learner tests and build**

Run:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm test -- --run src/pages/CourseLearnPage.test.jsx
npm run build
```

Expected: tests pass, build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/CourseLearnPage.jsx frontend/src/pages/CourseLearnPage.test.jsx
git commit -m "feat: play lesson videos on learner page"
```

### Task 11: End-to-end verification and deployment

**Files:**
- Modify only if verification reveals real defects

- [ ] **Step 1: Rebuild and redeploy the affected services**

Run:

```bash
docker compose build backend video-worker frontend
docker compose up -d --force-recreate backend video-worker frontend
```

- [ ] **Step 2: Verify service health**

Run:

```bash
curl -sS http://localhost:5000/api/health
curl -sS http://localhost:8001/health
```

Expected:

```json
{"status":"ok"}
```

- [ ] **Step 3: Generate a lesson video on live data**

Use an existing course/lesson that already has slides and completed audio:

```bash
curl -X POST http://localhost:5000/api/courses/<course-id>/lessons/<lesson-id>/regenerate-lesson-video \
  -H "Authorization: Bearer <admin-token>"
```

Expected: response contains `jobId`.

Then poll the job until completion:

```bash
curl http://localhost:5000/api/generation-jobs/<job-id>
```

Expected final state:

```json
{
  "status": "Completed"
}
```

- [ ] **Step 4: Verify output artifact and learner playback**

Check:

```bash
ls -lh storage/video
```

Expected: `<lesson-id>.mp4` exists.

Then open learner route for a published course and confirm:

- the selected lesson shows a `<video>` player
- the player loads `VideoUrl`
- video is playable

- [ ] **Step 5: Commit any final fixes and summarize verification**

```bash
git status
```

If clean or only intentional files changed, create the final commit:

```bash
git add .
git commit -m "feat: add lesson video render pipeline"
```

Record the exact verification evidence in the final handoff.
