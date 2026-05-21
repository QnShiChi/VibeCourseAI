# TTS Audio V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add lesson-level and course-level TTS audio generation that creates one final audio file per lesson while preserving slide alignment through per-slide narration and per-slide audio segments.

**Architecture:** Extend the lesson model and background job flow so the backend can enqueue audio-generation jobs, persist lesson audio metadata, and expose progress. Implement the media-heavy work in `ai-worker`: derive narration per slide, call OpenAI TTS for each slide, concatenate segments into one lesson file, and write segment/final audio files into `storage/audio`.

**Tech Stack:** React, Vite, Vitest, ASP.NET Core, C#, FastAPI, Python, OpenAI Audio API, filesystem storage

---

## File Structure

- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
  Add lesson audio metadata fields needed for runtime status and segment storage.
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
  Ensure new lesson audio columns are created without requiring a separate migration system.
- Create: `backend/CourseVideo.API/DTOs/Lessons/LessonAudioSegmentResponse.cs`
  Represent one slide-aligned audio segment.
- Create: `backend/CourseVideo.API/DTOs/Lessons/LessonAudioResponse.cs`
  Represent lesson-level audio state for frontend consumption.
- Create: `backend/CourseVideo.API/DTOs/Lessons/GenerateLessonAudioRequest.cs`
  Request payload for single-lesson audio generation if needed.
- Modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`
  Add lesson audio get/generate/regenerate endpoints.
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`
  Add course-wide audio generation endpoint.
- Create: `backend/CourseVideo.API/Services/VoiceoverPlanParser.cs`
  Parse normalized voiceover plan objects for audio generation orchestration.
- Create: `backend/CourseVideo.API/Services/LessonAudioValidation.cs`
  Validate lesson readiness for audio generation.
- Create: `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
  Enqueue course/lesson audio jobs and persist progress updates.
- Create: `backend/CourseVideo.API/Services/LessonAudioGenerationWorker.cs`
  Background worker that dispatches audio work to `ai-worker`.
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonAudioGenerationService.cs`
  Interface for the audio generation service.
- Modify: `backend/CourseVideo.API/Program.cs`
  Register new services/background worker and HTTP client settings for `ai-worker`.
- Modify: `backend/CourseVideo.API/DTOs/GenerationJobs/*` or existing generation-job DTOs/services
  Extend job typing/progress for audio jobs.
- Modify: `backend/CourseVideo.API.Tests/...`
  Add targeted backend tests for validation and job orchestration where feasible.
- Modify: `ai-worker/app/main.py`
  Add narration generation, TTS generation, concatenation endpoints, and health-safe orchestration.
- Create: `ai-worker/app/models.py`
  Pydantic request/response models for audio generation.
- Create: `ai-worker/app/openai_tts.py`
  OpenAI TTS client wrapper.
- Create: `ai-worker/app/narration.py`
  Slide-aligned narration derivation logic.
- Create: `ai-worker/app/audio_pipeline.py`
  Segment generation, concatenation, and storage path logic.
- Modify: `ai-worker/requirements.txt`
  Add OpenAI client and any audio-processing dependency used for concatenation.
- Modify: `frontend/src/api/lessonContentService.js`
  Add lesson audio API calls.
- Modify: `frontend/src/api/generationJobService.js`
  Reuse/extend job detail polling for audio jobs if needed.
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
  Add lesson-level/course-level audio controls, progress UI, and audio preview.
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`
  Cover audio generation controls and progress behavior.
- Modify: `frontend/src/styles/theme.css`
  Add audio status/player styling if needed.

### Task 1: Extend Lesson Data Model for Audio Metadata

**Files:**
- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`

- [ ] **Step 1: Add the failing backend shape expectations to an existing lesson-oriented test or create a minimal test**

```csharp
[Fact]
public void Lesson_ShouldExposeAudioMetadataFields()
{
    var lesson = new Lesson();

    lesson.AudioUrl.Should().BeNull();
    lesson.AudioSegmentsJson.Should().BeNull();
    lesson.AudioGenerationStatus.Should().Be("NotGenerated");
    lesson.AudioGenerationError.Should().BeNull();
}
```

- [ ] **Step 2: Run the targeted backend test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj \
  --filter Lesson_ShouldExposeAudioMetadataFields \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.Tests.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.Tests.bin/
```

Expected: FAIL because the new lesson audio fields do not exist yet or the backend test environment still blocks restore.

- [ ] **Step 3: Add the lesson audio fields and DB initializer support**

```csharp
public class Lesson : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentSeed { get; set; } = string.Empty;
    public string? TeachingScript { get; set; }
    public string? SlideOutlineJson { get; set; }
    public string? VoiceoverPlanJson { get; set; }
    public string ContentGenerationStatus { get; set; } = "NotGenerated";
    public DateTime? ContentGeneratedAt { get; set; }
    public string? ContentGenerationError { get; set; }
    public string? VideoUrl { get; set; }
    public string? AudioUrl { get; set; }
    public int? Duration { get; set; }
    public string? AudioSegmentsJson { get; set; }
    public string AudioGenerationStatus { get; set; } = "NotGenerated";
    public string? AudioGenerationError { get; set; }
    public DateTime? AudioGeneratedAt { get; set; }
    public Module? Module { get; set; }
}
```

```sql
IF COL_LENGTH('Lessons', 'AudioSegmentsJson') IS NULL
BEGIN
    ALTER TABLE [Lessons] ADD [AudioSegmentsJson] nvarchar(max) NULL;
END

IF COL_LENGTH('Lessons', 'AudioGenerationStatus') IS NULL
BEGIN
    ALTER TABLE [Lessons] ADD [AudioGenerationStatus] nvarchar(100) NOT NULL CONSTRAINT DF_Lessons_AudioGenerationStatus DEFAULT 'NotGenerated';
END

IF COL_LENGTH('Lessons', 'AudioGenerationError') IS NULL
BEGIN
    ALTER TABLE [Lessons] ADD [AudioGenerationError] nvarchar(max) NULL;
END

IF COL_LENGTH('Lessons', 'AudioGeneratedAt') IS NULL
BEGIN
    ALTER TABLE [Lessons] ADD [AudioGeneratedAt] datetime2 NULL;
END
```

- [ ] **Step 4: Run backend build verification**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/ \
  -p:GenerateAssemblyInfo=false \
  -p:GenerateTargetFrameworkAttribute=false
```

Expected: PASS with `0 Warning(s), 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Models/Lesson.cs backend/CourseVideo.API/Data/DbInitializer.cs
git commit -m "feat: add lesson audio metadata fields"
```

### Task 2: Add Lesson Audio DTOs and Validation

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Lessons/LessonAudioSegmentResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Lessons/LessonAudioResponse.cs`
- Create: `backend/CourseVideo.API/Services/VoiceoverPlanParser.cs`
- Create: `backend/CourseVideo.API/Services/LessonAudioValidation.cs`
- Create: `backend/CourseVideo.API.Tests/Services/LessonAudioValidationTests.cs`

- [ ] **Step 1: Write the failing validation tests**

```csharp
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonAudioValidationTests
{
    [Fact]
    public void ValidateReadyForAudio_Throws_WhenTeachingScriptMissing()
    {
        var lesson = new Lesson
        {
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Intro\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"N\"}]",
            VoiceoverPlanJson = "{\"EstimatedDurationMinutes\":8,\"Tone\":\"Clear\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        };

        var action = () => LessonAudioValidation.ValidateReadyForAudio(lesson);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Lesson phải có teaching script trước khi generate audio.");
    }

    [Fact]
    public void ValidateReadyForAudio_DoesNotThrow_WhenLessonHasRequiredInputs()
    {
        var lesson = new Lesson
        {
            TeachingScript = "Script",
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Intro\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"N\"}]",
            VoiceoverPlanJson = "{\"EstimatedDurationMinutes\":8,\"Tone\":\"Clear\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        };

        var action = () => LessonAudioValidation.ValidateReadyForAudio(lesson);

        action.Should().NotThrow();
    }
}
```

- [ ] **Step 2: Run the targeted validation test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj \
  --filter LessonAudioValidationTests \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.Tests.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.Tests.bin/
```

Expected: FAIL because `LessonAudioValidation` and DTOs do not exist yet or the test environment still blocks full execution.

- [ ] **Step 3: Implement minimal DTOs and validation**

```csharp
namespace CourseVideo.API.DTOs.Lessons;

public class LessonAudioSegmentResponse
{
    public int SlideNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
}
```

```csharp
namespace CourseVideo.API.DTOs.Lessons;

public class LessonAudioResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public string AudioGenerationStatus { get; set; } = string.Empty;
    public string AudioGenerationError { get; set; } = string.Empty;
    public DateTime? AudioGeneratedAt { get; set; }
    public List<LessonAudioSegmentResponse> Segments { get; set; } = [];
}
```

```csharp
using CourseVideo.API.Models;

namespace CourseVideo.API.Services;

public static class LessonAudioValidation
{
    public static void ValidateReadyForAudio(Lesson lesson)
    {
        if (string.IsNullOrWhiteSpace(lesson.TeachingScript))
        {
            throw new InvalidOperationException("Lesson phải có teaching script trước khi generate audio.");
        }

        if (string.IsNullOrWhiteSpace(lesson.SlideOutlineJson))
        {
            throw new InvalidOperationException("Lesson phải có slide outline hợp lệ trước khi generate audio.");
        }

        if (string.IsNullOrWhiteSpace(lesson.VoiceoverPlanJson))
        {
            throw new InvalidOperationException("Lesson phải có voiceover plan hợp lệ trước khi generate audio.");
        }

        SlideOutlineValidation.ParseAndValidate(lesson.SlideOutlineJson);
        VoiceoverPlanValidation.ParseAndValidate(lesson.VoiceoverPlanJson);
    }
}
```

```csharp
using System.Text.Json;

namespace CourseVideo.API.Services;

public static class VoiceoverPlanParser
{
    public static JsonElement Parse(string json)
    {
        VoiceoverPlanValidation.ParseAndValidate(json);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
```

- [ ] **Step 4: Run backend build verification**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/ \
  -p:GenerateAssemblyInfo=false \
  -p:GenerateTargetFrameworkAttribute=false
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/DTOs/Lessons/LessonAudioSegmentResponse.cs backend/CourseVideo.API/DTOs/Lessons/LessonAudioResponse.cs backend/CourseVideo.API/Services/VoiceoverPlanParser.cs backend/CourseVideo.API/Services/LessonAudioValidation.cs backend/CourseVideo.API.Tests/Services/LessonAudioValidationTests.cs
git commit -m "feat: add lesson audio DTOs and validation"
```

### Task 3: Add Audio Generation Job Types and Service Contracts

**Files:**
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonAudioGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
- Modify: existing generation job models/services/controllers as needed
- Modify: `backend/CourseVideo.API/Program.cs`

- [ ] **Step 1: Write the failing orchestration test or service-level assertion**

```csharp
[Fact]
public async Task EnqueueLessonAudioGenerationAsync_CreatesPendingJob()
{
    // Arrange service dependencies

    // Act
    var result = await service.EnqueueLessonAudioGenerationAsync(courseId, lessonId);

    // Assert
    result.Status.Should().Be("Pending");
    result.TotalLessons.Should().Be(1);
}
```

- [ ] **Step 2: Run the targeted backend test to verify it fails**

Run the relevant `dotnet test --filter` command for the new orchestration test.

Expected: FAIL because the service contract and implementation do not exist yet.

- [ ] **Step 3: Implement minimal service contract and job enqueue flow**

```csharp
namespace CourseVideo.API.Services.Interfaces;

public interface ILessonAudioGenerationService
{
    Task<GenerationJobEnqueueResponse> EnqueueCourseAudioGenerationAsync(Guid courseId);
    Task<GenerationJobEnqueueResponse> EnqueueLessonAudioGenerationAsync(Guid courseId, Guid lessonId);
}
```

```csharp
public class LessonAudioGenerationService : ILessonAudioGenerationService
{
    public async Task<GenerationJobEnqueueResponse> EnqueueCourseAudioGenerationAsync(Guid courseId)
    {
        // Resolve lessons ready for audio, create one generation job, persist Pending state
    }

    public async Task<GenerationJobEnqueueResponse> EnqueueLessonAudioGenerationAsync(Guid courseId, Guid lessonId)
    {
        // Resolve one lesson, validate it, create one generation job, persist Pending state
    }
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddScoped<ILessonAudioGenerationService, LessonAudioGenerationService>();
builder.Services.AddHostedService<LessonAudioGenerationWorker>();
```

- [ ] **Step 4: Run backend build verification**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/ \
  -p:GenerateAssemblyInfo=false \
  -p:GenerateTargetFrameworkAttribute=false
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/Interfaces/ILessonAudioGenerationService.cs backend/CourseVideo.API/Services/LessonAudioGenerationService.cs backend/CourseVideo.API/Program.cs
git commit -m "feat: add audio generation service contracts"
```

### Task 4: Add Backend Audio Endpoints

**Files:**
- Modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`

- [ ] **Step 1: Add failing API expectations**

```csharp
[Fact]
public async Task PostLessonAudioGeneration_ReturnsPendingJob()
{
    // Call POST /api/courses/{courseId}/lessons/{lessonId}/generate-audio
    // Expect 200 with Pending job payload
}
```

- [ ] **Step 2: Run the targeted API/controller test to verify it fails**

Run the relevant controller test filter if present; otherwise rely on build failure until endpoint methods exist.

Expected: FAIL because the endpoints do not exist yet.

- [ ] **Step 3: Implement lesson/course audio endpoints**

```csharp
[HttpPost("{courseId:guid}/generate-lesson-audio")]
public async Task<IActionResult> GenerateLessonAudio(Guid courseId)
{
    var result = await _lessonAudioGenerationService.EnqueueCourseAudioGenerationAsync(courseId);
    return Ok(result);
}
```

```csharp
[HttpPost("{id:guid}/generate-audio")]
public async Task<IActionResult> GenerateAudio(Guid id, [FromQuery] Guid courseId)
{
    var result = await _lessonAudioGenerationService.EnqueueLessonAudioGenerationAsync(courseId, id);
    return Ok(result);
}

[HttpGet("{id:guid}/audio")]
public async Task<IActionResult> GetAudio(Guid id)
{
    var audio = await _lessonService.GetAudioAsync(id);
    return audio is null ? NotFound() : Ok(audio);
}
```

- [ ] **Step 4: Run backend build verification**

Run the same `dotnet build` command.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Controllers/LessonsController.cs backend/CourseVideo.API/Controllers/CoursesController.cs
git commit -m "feat: add lesson and course audio endpoints"
```

### Task 5: Implement AI Worker Request Models and OpenAI TTS Client

**Files:**
- Create: `ai-worker/app/models.py`
- Create: `ai-worker/app/openai_tts.py`
- Modify: `ai-worker/requirements.txt`

- [ ] **Step 1: Write the failing worker test or smoke script expectation**

```python
def test_openai_tts_client_builds_speech_request():
    client = OpenAITtsClient(api_key="test", model="gpt-4o-mini-tts", voice="alloy")
    payload = client.build_payload("Hello")
    assert payload["model"] == "gpt-4o-mini-tts"
    assert payload["voice"] == "alloy"
```

- [ ] **Step 2: Run the worker-focused test or import smoke check to verify it fails**

Run: `python -m pytest ai-worker/tests/test_openai_tts.py -q`

Expected: FAIL because the worker client and models do not exist yet. If no test harness exists, note the absence and use `python -c "from app.openai_tts import OpenAITtsClient"` as the fail-first check.

- [ ] **Step 3: Add worker models and OpenAI TTS wrapper**

```python
from pydantic import BaseModel


class NarrationSegment(BaseModel):
    slide_number: int
    title: str
    narration_text: str


class LessonAudioJobRequest(BaseModel):
    lesson_id: str
    lesson_title: str
    teaching_script: str
    slide_outline_json: str
    voiceover_plan_json: str
```

```python
import os
from openai import OpenAI


class OpenAITtsClient:
    def __init__(self, api_key: str | None = None, model: str | None = None, voice: str | None = None):
        self.client = OpenAI(api_key=api_key or os.getenv("OPENAI_API_KEY"))
        self.model = model or os.getenv("OPENAI_TTS_MODEL", "gpt-4o-mini-tts")
        self.voice = voice or os.getenv("OPENAI_TTS_VOICE", "alloy")

    def build_payload(self, text: str) -> dict:
        return {
            "model": self.model,
            "voice": self.voice,
            "input": text,
        }

    def synthesize_to_bytes(self, text: str) -> bytes:
        response = self.client.audio.speech.create(**self.build_payload(text))
        return response.read()
```

Update `requirements.txt`:

```text
fastapi==0.115.0
uvicorn[standard]==0.30.6
openai>=1.0.0
```

- [ ] **Step 4: Run the worker import/smoke verification**

Run:

```bash
python3 - <<'PY'
from app.openai_tts import OpenAITtsClient
client = OpenAITtsClient(api_key="test", model="gpt-4o-mini-tts", voice="alloy")
print(client.build_payload("hello")["voice"])
PY
```

Expected: prints `alloy`.

- [ ] **Step 5: Commit**

```bash
git add ai-worker/app/models.py ai-worker/app/openai_tts.py ai-worker/requirements.txt
git commit -m "feat: add ai worker OpenAI TTS client"
```

### Task 6: Implement Narration-Per-Slide Derivation

**Files:**
- Create: `ai-worker/app/narration.py`
- Modify: `ai-worker/app/models.py`

- [ ] **Step 1: Write the failing narration derivation test**

```python
def test_build_narration_segments_prefers_speaker_notes():
    slide_outline = [
        {
            "slideNumber": 1,
            "title": "Intro",
            "bulletPoints": ["A"],
            "speakerNotes": "Welcome to the lesson."
        }
    ]
    segments = build_narration_segments(
        teaching_script="Longer fallback script.",
        slide_outline=slide_outline,
        voiceover_plan={"tone": "Clear"}
    )
    assert segments[0].narration_text.startswith("Welcome")
```

- [ ] **Step 2: Run the narration test or smoke check to verify it fails**

Run the relevant worker test or import-smoke command.

Expected: FAIL because `build_narration_segments` does not exist yet.

- [ ] **Step 3: Implement a first-pass narration derivation**

```python
import json
from app.models import NarrationSegment


def build_narration_segments(teaching_script: str, slide_outline_json: str, voiceover_plan_json: str) -> list[NarrationSegment]:
    slide_outline = json.loads(slide_outline_json)
    segments: list[NarrationSegment] = []

    for index, slide in enumerate(slide_outline, start=1):
        notes = (
            slide.get("speakerNotes")
            or slide.get("SpeakerNotes")
            or ""
        ).strip()

        narration_text = notes or teaching_script.strip()
        if not narration_text:
            raise ValueError(f"Missing narration text for slide {index}.")

        segments.append(
            NarrationSegment(
                slide_number=int(slide.get("slideNumber") or slide.get("SlideNumber") or index),
                title=str(slide.get("title") or slide.get("Title") or f"Slide {index}"),
                narration_text=narration_text,
            )
        )

    return segments
```

This initial implementation intentionally prefers `speakerNotes` and only falls back to the full teaching script so the pipeline stays simple for version one. The later AI-refinement step can be layered onto this structure without redesigning the worker contract.

- [ ] **Step 4: Run narration verification**

Run the worker test or a Python smoke script with one sample slide JSON.

Expected: PASS with one segment returned per slide.

- [ ] **Step 5: Commit**

```bash
git add ai-worker/app/narration.py ai-worker/app/models.py
git commit -m "feat: add slide narration derivation"
```

### Task 7: Implement Audio Segment Generation and Concatenation Pipeline

**Files:**
- Create: `ai-worker/app/audio_pipeline.py`
- Modify: `ai-worker/app/openai_tts.py`

- [ ] **Step 1: Write the failing audio pipeline smoke expectation**

```python
def test_build_audio_paths():
    segment_path, final_path = build_audio_paths("lesson-1", 1)
    assert "lesson-1" in segment_path
    assert segment_path.endswith(".mp3")
    assert final_path.endswith(".mp3")
```

- [ ] **Step 2: Run the worker smoke test to verify it fails**

Run the targeted test or import-smoke command.

Expected: FAIL because the audio pipeline module does not exist yet.

- [ ] **Step 3: Implement path generation and concatenation shell-out**

```python
from pathlib import Path
import subprocess


STORAGE_AUDIO_DIR = Path("/app/storage/audio")


def ensure_audio_dir() -> None:
    STORAGE_AUDIO_DIR.mkdir(parents=True, exist_ok=True)


def build_segment_path(lesson_id: str, slide_number: int) -> Path:
    ensure_audio_dir()
    return STORAGE_AUDIO_DIR / f"{lesson_id}-slide-{slide_number}.mp3"


def build_final_path(lesson_id: str) -> Path:
    ensure_audio_dir()
    return STORAGE_AUDIO_DIR / f"{lesson_id}.mp3"


def concatenate_mp3_files(segment_paths: list[Path], final_path: Path) -> None:
    with open(final_path, "wb") as destination:
        for path in segment_paths:
            destination.write(path.read_bytes())
```

For version one, byte concatenation is acceptable only if segment encoding stays consistent. If it proves unreliable in smoke tests, replace it with an ffmpeg concat step before continuing:

```python
def concatenate_mp3_files(segment_paths: list[Path], final_path: Path) -> None:
    concat_list = final_path.with_suffix(".txt")
    concat_list.write_text(
        "".join(f"file '{path.as_posix()}'\n" for path in segment_paths),
        encoding="utf-8"
    )
    subprocess.run(
        ["ffmpeg", "-y", "-f", "concat", "-safe", "0", "-i", str(concat_list), "-c", "copy", str(final_path)],
        check=True,
    )
```

- [ ] **Step 4: Run the pipeline smoke verification**

Run a Python smoke script that creates two temp segment files and concatenates them.

Expected: final file exists and has non-zero size.

- [ ] **Step 5: Commit**

```bash
git add ai-worker/app/audio_pipeline.py ai-worker/app/openai_tts.py
git commit -m "feat: add audio segment pipeline"
```

### Task 8: Add AI Worker Audio Generation Endpoint

**Files:**
- Modify: `ai-worker/app/main.py`
- Modify: `ai-worker/app/models.py`
- Modify: `ai-worker/app/narration.py`
- Modify: `ai-worker/app/audio_pipeline.py`

- [ ] **Step 1: Write the failing endpoint smoke expectation**

```python
def test_worker_exposes_generate_audio_route(client):
    response = client.post("/jobs/generate-lesson-audio", json={...})
    assert response.status_code == 200
    assert "audio_url" in response.json()
```

- [ ] **Step 2: Run the worker route test or import-smoke verification to confirm it fails**

Run the relevant test or start the app and call the missing route.

Expected: FAIL or `404` because the route does not exist yet.

- [ ] **Step 3: Implement the worker endpoint**

```python
from fastapi import FastAPI, HTTPException
from app.audio_pipeline import build_final_path, build_segment_path, concatenate_mp3_files
from app.models import LessonAudioJobRequest
from app.narration import build_narration_segments
from app.openai_tts import OpenAITtsClient

app = FastAPI(title="Course Video AI Worker")


@app.post("/jobs/generate-lesson-audio")
def generate_lesson_audio(request: LessonAudioJobRequest):
    try:
        tts = OpenAITtsClient()
        narration_segments = build_narration_segments(
            teaching_script=request.teaching_script,
            slide_outline_json=request.slide_outline_json,
            voiceover_plan_json=request.voiceover_plan_json,
        )

        segment_results = []
        segment_paths = []

        for segment in narration_segments:
            path = build_segment_path(request.lesson_id, segment.slide_number)
            path.write_bytes(tts.synthesize_to_bytes(segment.narration_text))
            segment_paths.append(path)
            segment_results.append({
                "slideNumber": segment.slide_number,
                "title": segment.title,
                "narrationText": segment.narration_text,
                "audioUrl": f"/storage/audio/{path.name}",
                "durationSeconds": 0.0,
            })

        final_path = build_final_path(request.lesson_id)
        concatenate_mp3_files(segment_paths, final_path)

        return {
            "audioUrl": f"/storage/audio/{final_path.name}",
            "durationSeconds": 0.0,
            "segments": segment_results,
        }
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
```

- [ ] **Step 4: Run the worker health and route verification**

Run:

```bash
uvicorn app.main:app --host 0.0.0.0 --port 8000
curl -i http://localhost:8000/health
curl -i -X POST http://localhost:8000/jobs/generate-lesson-audio -H 'Content-Type: application/json' -d '<sample payload>'
```

Expected:

- `/health` returns `200`
- audio route returns either a valid payload or a provider-configuration error that proves the route is wired correctly

- [ ] **Step 5: Commit**

```bash
git add ai-worker/app/main.py ai-worker/app/models.py ai-worker/app/narration.py ai-worker/app/audio_pipeline.py
git commit -m "feat: add ai worker lesson audio endpoint"
```

### Task 9: Connect Backend Worker Orchestration

**Files:**
- Create: `backend/CourseVideo.API/Services/LessonAudioGenerationWorker.cs`
- Modify: `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`

- [ ] **Step 1: Write the failing orchestration expectation**

```csharp
[Fact]
public async Task Worker_CompletesLessonAudioJob_AndUpdatesLessonAudioMetadata()
{
    // Arrange pending audio job and mock ai-worker response
    // Act worker tick
    // Assert lesson AudioUrl, AudioSegmentsJson, AudioGenerationStatus updated
}
```

- [ ] **Step 2: Run the targeted backend test to verify it fails**

Run the new worker/orchestration filter.

Expected: FAIL because the worker logic does not exist yet.

- [ ] **Step 3: Implement backend worker-to-ai-worker flow**

```csharp
public class LessonAudioGenerationWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Poll one pending audio job
            // Mark current phase
            // POST lesson payload to ai-worker /jobs/generate-lesson-audio
            // Persist AudioUrl, AudioSegmentsJson, Duration, AudioGenerationStatus
            // Mark job progress / failure
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
```

The worker should update lesson fields roughly like:

```csharp
lesson.AudioUrl = response.AudioUrl;
lesson.AudioSegmentsJson = JsonSerializer.Serialize(response.Segments);
lesson.AudioGenerationStatus = "Completed";
lesson.AudioGenerationError = null;
lesson.AudioGeneratedAt = DateTime.UtcNow;
lesson.UpdatedAt = DateTime.UtcNow;
```

- [ ] **Step 4: Run backend build verification**

Run the same `dotnet build` command.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/LessonAudioGenerationWorker.cs backend/CourseVideo.API/Services/LessonAudioGenerationService.cs backend/CourseVideo.API/Program.cs
git commit -m "feat: connect backend audio jobs to ai worker"
```

### Task 10: Add Frontend Audio Controls and Preview

**Files:**
- Modify: `frontend/src/api/lessonContentService.js`
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Extend the failing page test**

```javascript
it("starts lesson audio generation and shows audio controls", async () => {
  mockGetCourseStructure.mockResolvedValue({
    ...baseCourse,
    modules: [
      {
        ...baseCourse.modules[0],
        lessons: [
          {
            ...baseCourse.modules[0].lessons[0],
            contentGenerationStatus: "Completed",
            audioGenerationStatus: "NotGenerated",
            audioUrl: ""
          }
        ]
      }
    ]
  });
  mockGenerateLessonAudio.mockResolvedValue({
    jobId: "audio-job-1",
    status: "Pending",
    totalLessons: 1,
    failedLessons: 0,
    message: "Đã tạo job generate audio cho lesson."
  });

  renderPage();

  fireEvent.click(await screen.findByRole("button", { name: "Generate audio" }));

  await waitFor(() => expect(mockGenerateLessonAudio).toHaveBeenCalled());
});
```

- [ ] **Step 2: Run the course page test to verify it fails**

Run:

```bash
npm run test -- --run src/pages/CourseStructurePage.test.jsx
```

Expected: FAIL because audio controls and API functions do not exist yet.

- [ ] **Step 3: Implement minimal lesson/course audio UI**

Add API functions:

```javascript
export async function generateLessonAudio(courseId, lessonId) {
  const { data } = await api.post(`/lessons/${lessonId}/generate-audio`, null, {
    params: { courseId }
  });
  return data;
}

export async function generateCourseAudio(courseId) {
  const { data } = await api.post(`/courses/${courseId}/generate-lesson-audio`);
  return data;
}

export async function getLessonAudio(lessonId) {
  const { data } = await api.get(`/lessons/${lessonId}/audio`);
  return data;
}
```

In `CourseStructurePage.jsx`, add:

- course-level `Generate audio khóa học` button
- lesson-level `Generate audio` / `Generate lại audio`
- audio status badge
- audio player:

```jsx
{lesson.audioUrl ? (
  <audio controls preload="none" src={lesson.audioUrl}>
    Trình duyệt không hỗ trợ audio preview.
  </audio>
) : null}
```

- [ ] **Step 4: Run focused frontend verification in the writable copy**

Run:

```bash
cp frontend/src/pages/CourseStructurePage.jsx /tmp/vibecourseai-frontend-verify/frontend/src/pages/CourseStructurePage.jsx
cp frontend/src/pages/CourseStructurePage.test.jsx /tmp/vibecourseai-frontend-verify/frontend/src/pages/CourseStructurePage.test.jsx
cp frontend/src/api/lessonContentService.js /tmp/vibecourseai-frontend-verify/frontend/src/api/lessonContentService.js
cp frontend/src/styles/theme.css /tmp/vibecourseai-frontend-verify/frontend/src/styles/theme.css
cd /tmp/vibecourseai-frontend-verify/frontend
npm run test -- --run src/pages/CourseStructurePage.test.jsx
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/lessonContentService.js frontend/src/pages/CourseStructurePage.jsx frontend/src/pages/CourseStructurePage.test.jsx frontend/src/styles/theme.css
git commit -m "feat: add audio generation controls to course page"
```

### Task 11: Full Verification and Runtime Smoke Test

**Files:**
- Modify: all touched backend/frontend/worker files

- [ ] **Step 1: Run focused frontend tests in the writable verify copy**

Run:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm run test -- --run src/pages/CourseStructurePage.test.jsx src/components/course/LessonContentEditor.test.jsx src/components/course/LessonContentPreview.test.jsx
```

Expected: PASS.

- [ ] **Step 2: Run frontend production build**

Run:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm run build
```

Expected: PASS with generated `dist/assets/index-*.js`.

- [ ] **Step 3: Run backend build**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/ \
  -p:GenerateAssemblyInfo=false \
  -p:GenerateTargetFrameworkAttribute=false
```

Expected: PASS.

- [ ] **Step 4: Rebuild and recreate containers**

Run:

```bash
docker compose build frontend backend ai-worker
docker compose up -d frontend backend ai-worker
curl -i http://localhost:5000/api/health
curl -i http://localhost:8000/health
```

Expected: both services return `200 OK`.

- [ ] **Step 5: Run one live lesson audio smoke test**

Run a Python or curl script that:

1. logs in as admin
2. calls lesson audio generation
3. polls the job status
4. confirms:
   - lesson `audioUrl` is populated
   - `storage/audio` contains segment files and a final lesson audio file
   - lesson `audioGenerationStatus` becomes `Completed`

Example verification commands:

```bash
find storage/audio -maxdepth 1 -type f | sort
```

Expected: newly created lesson audio files present.

- [ ] **Step 6: Commit the completed integration**

```bash
git add backend/CourseVideo.API backend/CourseVideo.API.Tests ai-worker frontend
git commit -m "feat: add lesson and course TTS audio generation"
```
