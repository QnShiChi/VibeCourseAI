# Lesson Voice Tutor V2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the existing lesson voice tutor into a voice-first V2 that starts speaking back earlier, removes answer text from the learner UI, and replaces the large tutor panel with a compact floating mic control.

**Architecture:** Keep the current lesson voice tutor session model, SignalR transport, and provider stack, but refactor the turn pipeline from full-response orchestration to incremental response streaming. On the frontend, replace the block panel with a focused floating action button component bound to the existing lesson video area and simplify the tutor hook so it manages audio playback and state without rendering text answers.

**Tech Stack:** ASP.NET Core 8, SignalR, OpenRouter chat completions, OpenAI transcription API, `edge-tts`, React, Vite, Vitest, Testing Library.

---

## File Structure

### Backend files to modify

- Modify: `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`
  - simplify hub events around streaming speech segments
- Modify: `backend/CourseVideo.API/Program.cs`
  - register new streaming services and keep hub transport configuration aligned
- Modify: `backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`
  - change the orchestration flow from full-result to incremental segment emission
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorService.cs`
  - update method contract to support streaming events
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs`
  - expose per-segment synthesis contract if not already present
- Modify: `backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs`
  - synthesize individual speech segments immediately and return URLs in order
- Modify: `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorAnswerService.cs`
  - split or replace current full-answer logic with streaming support

### Backend files to create

- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorResponseStreamService.cs`
  - streaming LLM abstraction
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorSegmenter.cs`
  - sentence and threshold-based segment boundary abstraction
- Create: `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorResponseStreamService.cs`
  - OpenRouter streaming implementation
- Create: `backend/CourseVideo.API/Services/Tutoring/LessonTutorSegmenter.cs`
  - concrete token buffer segmenter

### Backend tests to modify or create

- Modify: `backend/CourseVideo.API.Tests/Services/LessonVoiceTutorServiceTests.cs`
  - update for streaming orchestration contract
- Create: `backend/CourseVideo.API.Tests/Services/LessonTutorSegmenterTests.cs`
  - verify punctuation and threshold splitting behavior
- Create: `backend/CourseVideo.API.Tests/Hubs/LessonVoiceTutorHubTests.cs`
  - verify emitted event sequence for streaming segments if feasible in current test setup

### Frontend files to modify

- Modify: `frontend/src/hooks/useLessonVoiceTutor.js`
  - remove UI-facing text state and support speech-completion-driven actions
- Modify: `frontend/src/api/lessonVoiceTutorService.js`
  - align SignalR event wiring with the V2 contract
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
  - mount floating mic control inside the video area instead of rendering a panel below
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
  - update page integration tests for the new control and removed panel behavior
- Modify: `frontend/src/styles/theme.css`
  - add floating mic control styling and remove dead panel-specific styling if safe

### Frontend files to replace

- Replace: `frontend/src/components/course/LessonVoiceTutorPanel.jsx`
  - with a new floating mic component
- Replace: `frontend/src/components/course/LessonVoiceTutorPanel.test.jsx`
  - with a new test file for the floating control

### Frontend files to create

- Create: `frontend/src/components/course/LessonVoiceTutorFab.jsx`
  - floating mic control with compact label and follow-up actions
- Create: `frontend/src/components/course/LessonVoiceTutorFab.test.jsx`
  - unit tests for visual states and decision actions

---

### Task 1: Add backend streaming abstractions

**Files:**
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorResponseStreamService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorSegmenter.cs`
- Create: `backend/CourseVideo.API/Services/Tutoring/LessonTutorSegmenter.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonTutorSegmenterTests.cs`

- [ ] **Step 1: Write the failing segmenter tests**

```csharp
using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Tutoring;

namespace CourseVideo.API.Tests.Services;

public class LessonTutorSegmenterTests
{
    private readonly ILessonTutorSegmenter _segmenter = new LessonTutorSegmenter();

    [Fact]
    public void PushToken_ReturnsSegment_WhenSentenceBoundaryArrives()
    {
        var flushed = new List<string>();
        flushed.AddRange(_segmenter.PushText("Day la cau thu nhat."));

        Assert.Single(flushed);
        Assert.Equal("Day la cau thu nhat.", flushed[0]);
    }

    [Fact]
    public void PushToken_WaitsUntilThreshold_WhenNoPunctuationExists()
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonTutorSegmenterTests`

Expected: FAIL because `ILessonTutorSegmenter` and `LessonTutorSegmenter` do not exist yet.

- [ ] **Step 3: Add the segmenter interfaces and minimal implementation**

`backend/CourseVideo.API/Services/Interfaces/ILessonTutorSegmenter.cs`

```csharp
namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorSegmenter
{
    IReadOnlyList<string> PushText(string text);
    IReadOnlyList<string> FlushRemaining();
}
```

`backend/CourseVideo.API/Services/Interfaces/ILessonTutorResponseStreamService.cs`

```csharp
using System.Runtime.CompilerServices;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorResponseStreamService
{
    IAsyncEnumerable<string> StreamAnswerAsync(
        LessonTutorAnswerRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken);
}
```

`backend/CourseVideo.API/Services/Tutoring/LessonTutorSegmenter.cs`

```csharp
using System.Text;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services.Tutoring;

public class LessonTutorSegmenter : ILessonTutorSegmenter
{
    private const int SoftCharacterLimit = 160;
    private readonly StringBuilder _buffer = new();

    public IReadOnlyList<string> PushText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        _buffer.Append(text);
        return DrainCompletedSegments();
    }

    public IReadOnlyList<string> FlushRemaining()
    {
        var remaining = Normalize(_buffer.ToString());
        _buffer.Clear();
        return string.IsNullOrWhiteSpace(remaining) ? [] : [EnsureTerminalPunctuation(remaining)];
    }

    private List<string> DrainCompletedSegments()
    {
        var output = new List<string>();

        while (TryExtractSegment(out var segment))
        {
            output.Add(segment);
        }

        return output;
    }

    private bool TryExtractSegment(out string segment)
    {
        segment = string.Empty;
        var current = _buffer.ToString();
        if (string.IsNullOrWhiteSpace(current))
        {
            return false;
        }

        var punctuationIndex = current.LastIndexOfAny(['.', '!', '?', '\n']);
        if (punctuationIndex >= 0 && punctuationIndex + 1 <= current.Length)
        {
            segment = Normalize(current[..(punctuationIndex + 1)]);
            _buffer.Remove(0, punctuationIndex + 1);
            return !string.IsNullOrWhiteSpace(segment);
        }

        if (current.Length < SoftCharacterLimit)
        {
            return false;
        }

        var commaIndex = current.LastIndexOf(',');
        var splitIndex = commaIndex >= 0 ? commaIndex + 1 : SoftCharacterLimit;
        segment = EnsureTerminalPunctuation(Normalize(current[..splitIndex]));
        _buffer.Remove(0, splitIndex);
        return !string.IsNullOrWhiteSpace(segment);
    }

    private static string Normalize(string value)
    {
        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string EnsureTerminalPunctuation(string value)
    {
        return value.EndsWith('.') || value.EndsWith('!') || value.EndsWith('?')
            ? value
            : $"{value}.";
    }
}
```

`backend/CourseVideo.API/Program.cs`

```csharp
builder.Services.AddScoped<ILessonTutorResponseStreamService, OpenRouterLessonTutorResponseStreamService>();
builder.Services.AddScoped<ILessonTutorSegmenter, LessonTutorSegmenter>();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonTutorSegmenterTests`

Expected: PASS with 3 passing tests.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/Interfaces/ILessonTutorResponseStreamService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonTutorSegmenter.cs \
  backend/CourseVideo.API/Services/Tutoring/LessonTutorSegmenter.cs \
  backend/CourseVideo.API/Program.cs \
  backend/CourseVideo.API.Tests/Services/LessonTutorSegmenterTests.cs
git commit -m "feat: add tutor response segmenter"
```

### Task 2: Add OpenRouter streaming response service

**Files:**
- Create: `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorResponseStreamService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/OpenRouterLessonTutorResponseStreamServiceTests.cs`

- [ ] **Step 1: Write the failing streaming service tests**

```csharp
using System.Net;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Tutoring;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterLessonTutorResponseStreamServiceTests
{
    [Fact]
    public async Task StreamAnswerAsync_YieldsContentTokens_FromSsePayload()
    {
        const string body = """
data: {"choices":[{"delta":{"content":"Xin chao "}}]}

data: {"choices":[{"delta":{"content":"ban."}}]}

data: [DONE]

""";

        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "key",
            Model = "model",
            BaseUrl = "https://openrouter.ai/api/v1"
        });

        ILessonTutorResponseStreamService service = new OpenRouterLessonTutorResponseStreamService(client, options);

        var chunks = new List<string>();
        await foreach (var chunk in service.StreamAnswerAsync(
            new LessonTutorAnswerRequest(
                new LessonTutorContext("Course", "Module", "Lesson", "Desc", "Script", "[]", "Transcript", 10),
                "Cau hoi",
                null),
            CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(["Xin chao ", "ban."], chunks);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter OpenRouterLessonTutorResponseStreamServiceTests`

Expected: FAIL because the streaming service test target does not exist yet.

- [ ] **Step 3: Add the streaming service**

`backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorResponseStreamService.cs`

```csharp
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services.Tutoring;

public class OpenRouterLessonTutorResponseStreamService : ILessonTutorResponseStreamService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterLessonTutorResponseStreamService(HttpClient httpClient, IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        LessonTutorAnswerRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Missing OPENROUTER_API_KEY for lesson voice tutor.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Missing OPENROUTER_MODEL for lesson voice tutor.");
        }

        var payload = new OpenRouterChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = 0.2,
            Stream = true,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = "Ban la tro giang giong noi cho mot bai hoc video bang tieng Viet. Tra loi ngan gon, tu nhien, de doc thanh giong noi."
                },
                new OpenRouterMessage
                {
                    Role = "user",
                    Content = $"""
                    Course: {request.Context.CourseTitle}
                    Module: {request.Context.ModuleTitle}
                    Lesson: {request.Context.LessonTitle}
                    Lesson description: {request.Context.LessonDescription}
                    Playback second: {request.Context.PlaybackTimeSeconds}

                    Teaching script:
                    {request.Context.TeachingScript}

                    Slide outline:
                    {request.Context.SlideOutlineJson}

                    Transcript:
                    {request.Context.TranscriptText}

                    Conversation summary:
                    {request.ConversationSummary ?? string.Empty}

                    Learner question:
                    {request.QuestionText}
                    """
                }
            ]
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var payloadLine = line["data: ".Length..];
            if (payloadLine == "[DONE]")
            {
                yield break;
            }

            using var document = JsonDocument.Parse(payloadLine);
            var delta = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentElement))
            {
                var content = contentElement.GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    yield return content;
                }
            }
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter OpenRouterLessonTutorResponseStreamServiceTests`

Expected: PASS with the SSE parsing test succeeding.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorResponseStreamService.cs \
  backend/CourseVideo.API/Program.cs \
  backend/CourseVideo.API.Tests/Services/OpenRouterLessonTutorResponseStreamServiceTests.cs
git commit -m "feat: add streaming openrouter tutor service"
```

### Task 3: Refactor tutor turn orchestration for incremental speech events

**Files:**
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorService.cs`
- Modify: `backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`
- Modify: `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs`
- Modify: `backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonVoiceTutorServiceTests.cs`

- [ ] **Step 1: Write the failing orchestration tests**

```csharp
[Fact]
public async Task CompleteTurnAsync_EmitsSegments_AsSoonAsTheyAreReady()
{
    var sessions = new Mock<ILessonVoiceSessionRepository>();
    var contextBuilder = new Mock<ILessonContextBuilder>();
    var transcription = new Mock<ITranscriptionService>();
    var streaming = new Mock<ILessonTutorResponseStreamService>();
    var segmenter = new Mock<ILessonTutorSegmenter>();
    var speech = new Mock<ILessonTutorSpeechService>();

    var session = new LessonVoiceSession
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        LessonId = Guid.NewGuid(),
        VoiceProfileKey = "vi-VN-HoaiMyNeural",
        Status = "Active",
        Turns = []
    };

    sessions.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
    contextBuilder.Setup(x => x.BuildAsync(session.LessonId, 25, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new LessonTutorContext("Course", "Module", "Lesson", "Desc", "Script", "[]", "Transcript", 25));
    transcription.Setup(x => x.TranscribeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new TranscriptionResult("Cau hoi", 1m));
    streaming.Setup(x => x.StreamAnswerAsync(It.IsAny<LessonTutorAnswerRequest>(), It.IsAny<CancellationToken>()))
        .Returns(ToAsyncEnumerable(["Doan mot. ", "Doan hai."]));
    segmenter.SetupSequence(x => x.PushText(It.IsAny<string>()))
        .Returns(["Doan mot."])
        .Returns(["Doan hai."]);
    segmenter.Setup(x => x.FlushRemaining()).Returns([]);
    speech.Setup(x => x.SynthesizeSegmentAsync("vi-VN-HoaiMyNeural", "Doan mot.", It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new LessonTutorAudioSegment(0, "Doan mot.", "/storage/voice-tutor/assistant-answers/1.mp3", 1));
    speech.Setup(x => x.SynthesizeSegmentAsync("vi-VN-HoaiMyNeural", "Doan hai.", It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new LessonTutorAudioSegment(1, "Doan hai.", "/storage/voice-tutor/assistant-answers/2.mp3", 1));

    var service = new LessonVoiceTutorService(sessions.Object, contextBuilder.Object, transcription.Object, streaming.Object, segmenter.Object, speech.Object);

    var result = await service.CompleteTurnAsync(session.Id, session.UserId, 25, [1, 2, 3], CancellationToken.None);

    Assert.Equal(2, result.AudioSegments.Count);
    Assert.Equal("Doan mot. Doan hai.", result.AnswerText);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonVoiceTutorServiceTests`

Expected: FAIL because the service constructor and orchestration contract do not yet support the streaming collaborators.

- [ ] **Step 3: Refactor the service and hub**

`backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs`

```csharp
namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorSpeechService
{
    Task<LessonTutorAudioSegment> SynthesizeSegmentAsync(
        string voiceProfileKey,
        string answerSegment,
        int sequenceIndex,
        CancellationToken cancellationToken);
}
```

`backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`

```csharp
public class LessonVoiceTutorService : ILessonVoiceTutorService
{
    private readonly ILessonVoiceSessionRepository _sessionRepository;
    private readonly ILessonContextBuilder _contextBuilder;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILessonTutorResponseStreamService _responseStreamService;
    private readonly ILessonTutorSegmenter _segmenter;
    private readonly ILessonTutorSpeechService _speechService;

    public LessonVoiceTutorService(
        ILessonVoiceSessionRepository sessionRepository,
        ILessonContextBuilder contextBuilder,
        ITranscriptionService transcriptionService,
        ILessonTutorResponseStreamService responseStreamService,
        ILessonTutorSegmenter segmenter,
        ILessonTutorSpeechService speechService)
    {
        _sessionRepository = sessionRepository;
        _contextBuilder = contextBuilder;
        _transcriptionService = transcriptionService;
        _responseStreamService = responseStreamService;
        _segmenter = segmenter;
        _speechService = speechService;
    }

    public async Task<LessonVoiceTurnResult> CompleteTurnAsync(Guid sessionId, Guid userId, double playbackTimeSeconds, byte[] audioBytes, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.UserId != userId)
        {
            throw new InvalidOperationException("Cannot access another user's voice session.");
        }

        var context = await _contextBuilder.BuildAsync(session.LessonId, playbackTimeSeconds, cancellationToken);
        var transcription = await _transcriptionService.TranscribeAsync(audioBytes, cancellationToken);
        var answerRequest = new LessonTutorAnswerRequest(context, transcription.Text, session.ConversationSummary);

        var collectedSegments = new List<LessonTutorAudioSegment>();
        var answerTextParts = new List<string>();
        var sequenceIndex = 0;

        await foreach (var chunk in _responseStreamService.StreamAnswerAsync(answerRequest, cancellationToken))
        {
            foreach (var segment in _segmenter.PushText(chunk))
            {
                answerTextParts.Add(segment);
                collectedSegments.Add(
                    await _speechService.SynthesizeSegmentAsync(
                        session.VoiceProfileKey,
                        segment,
                        sequenceIndex++,
                        cancellationToken));
            }
        }

        foreach (var tail in _segmenter.FlushRemaining())
        {
            answerTextParts.Add(tail);
            collectedSegments.Add(
                await _speechService.SynthesizeSegmentAsync(
                    session.VoiceProfileKey,
                    tail,
                    sequenceIndex++,
                    cancellationToken));
        }

        var fullAnswerText = string.Join(" ", answerTextParts).Trim();

        await PersistTurnAsync(session, transcription.Text, fullAnswerText, playbackTimeSeconds, collectedSegments, cancellationToken);

        return new LessonVoiceTurnResult("AwaitingFollowUpDecision", transcription.Text, fullAnswerText, "Mixed", collectedSegments);
    }
}
```

`backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`

```csharp
await Clients.Caller.SendAsync("TranscriptionStarted", sessionId, cancellationToken);
var result = await _voiceTutorService.CompleteTurnAsync(sessionId, userId, playbackTimeSeconds, audioPayload, cancellationToken);
await Clients.Caller.SendAsync("TranscriptionCompleted", cancellationToken);

foreach (var segment in result.AudioSegments)
{
    await Clients.Caller.SendAsync(
        "AssistantSpeechSegmentReady",
        segment.SequenceIndex,
        segment.AudioUrl,
        segment.DurationSeconds,
        cancellationToken);
}

await Clients.Caller.SendAsync("AssistantSpeechCompleted", sessionId, cancellationToken);
await Clients.Caller.SendAsync("AwaitingFollowUpDecision", sessionId, cancellationToken);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonVoiceTutorServiceTests`

Expected: PASS with the orchestration test updated for streaming segment synthesis.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs \
  backend/CourseVideo.API/Services/LessonVoiceTutorService.cs \
  backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs \
  backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs \
  backend/CourseVideo.API.Tests/Services/LessonVoiceTutorServiceTests.cs
git commit -m "feat: stream tutor speech segments"
```

### Task 4: Replace the large panel with a floating mic control

**Files:**
- Create: `frontend/src/components/course/LessonVoiceTutorFab.jsx`
- Create: `frontend/src/components/course/LessonVoiceTutorFab.test.jsx`
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Write the failing component tests**

```jsx
import { fireEvent, render, screen } from "@testing-library/react";
import LessonVoiceTutorFab from "./LessonVoiceTutorFab";

describe("LessonVoiceTutorFab", () => {
  it("shows compact idle label and mic action", () => {
    render(
      <LessonVoiceTutorFab
        state="idle"
        errorMessage=""
        onStartRecording={() => {}}
        onStopRecording={() => {}}
        onRequestFollowUp={() => {}}
        onResumeLearning={() => {}}
      />
    );

    expect(screen.getByRole("button", { name: /hoi ngay/i })).toBeInTheDocument();
  });

  it("shows follow-up actions after speech completes", () => {
    render(
      <LessonVoiceTutorFab
        state="awaitingDecision"
        errorMessage=""
        onStartRecording={() => {}}
        onStopRecording={() => {}}
        onRequestFollowUp={() => {}}
        onResumeLearning={() => {}}
      />
    );

    expect(screen.getByRole("button", { name: /hoi tiep/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /tiep tuc hoc/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run LessonVoiceTutorFab.test.jsx`

Expected: FAIL because `LessonVoiceTutorFab.jsx` does not exist.

- [ ] **Step 3: Add the new floating component and integrate it**

`frontend/src/components/course/LessonVoiceTutorFab.jsx`

```jsx
export default function LessonVoiceTutorFab({
  state,
  errorMessage,
  onStartRecording,
  onStopRecording,
  onRequestFollowUp,
  onResumeLearning
}) {
  const isRecording = state === "recording";
  const isBusy = state === "thinking" || state === "speaking" || state === "uploading";
  const showDecision = state === "awaitingDecision";

  const label = isRecording
    ? "Dang nghe"
    : isBusy
      ? "Dang tra loi"
      : "Hoi ngay";

  return (
    <div className="lesson-voice-fab">
      <div className="lesson-voice-fab__dock">
        <button
          type="button"
          className={`lesson-voice-fab__button${isRecording ? " is-recording" : ""}`}
          disabled={isBusy}
          aria-label={label}
          onClick={isRecording ? onStopRecording : onStartRecording}
        >
          <span aria-hidden="true">🎤</span>
        </button>
        <span className="lesson-voice-fab__label">{label}</span>
      </div>

      {showDecision ? (
        <div className="lesson-voice-fab__actions">
          <button type="button" onClick={onRequestFollowUp}>Hoi tiep</button>
          <button type="button" onClick={onResumeLearning}>Tiep tuc hoc</button>
        </div>
      ) : null}

      {errorMessage ? <p className="lesson-voice-fab__error">{errorMessage}</p> : null}
    </div>
  );
}
```

`frontend/src/pages/CourseLearnPage.jsx`

```jsx
<div className="course-learn-hero__media">
  <video ref={videoRef} ... />
  {selectedLesson?.videoUrl ? (
    <LessonVoiceTutorFab
      state={tutor.state}
      errorMessage={tutor.errorMessage}
      onStartRecording={() => tutor.startRecording(videoRef.current?.currentTime ?? 0)}
      onStopRecording={tutor.stopRecording}
      onRequestFollowUp={tutor.requestFollowUp}
      onResumeLearning={tutor.resumeLearning}
    />
  ) : null}
</div>
```

`frontend/src/styles/theme.css`

```css
.lesson-voice-fab {
  position: absolute;
  right: 16px;
  bottom: 72px;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 10px;
  z-index: 3;
}

.lesson-voice-fab__dock {
  display: flex;
  align-items: center;
  gap: 10px;
}

.lesson-voice-fab__button {
  width: 56px;
  height: 56px;
  border-radius: 999px;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `npm test -- --run LessonVoiceTutorFab.test.jsx CourseLearnPage.test.jsx`

Expected: PASS with the new floating control tests and updated page integration.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/course/LessonVoiceTutorFab.jsx \
  frontend/src/components/course/LessonVoiceTutorFab.test.jsx \
  frontend/src/pages/CourseLearnPage.jsx \
  frontend/src/pages/CourseLearnPage.test.jsx \
  frontend/src/styles/theme.css
git commit -m "feat: replace tutor panel with floating mic control"
```

### Task 5: Simplify the frontend hook and adopt the V2 SignalR contract

**Files:**
- Modify: `frontend/src/api/lessonVoiceTutorService.js`
- Modify: `frontend/src/hooks/useLessonVoiceTutor.js`
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
- Test: `frontend/src/hooks/useLessonVoiceTutor.test.jsx`

- [ ] **Step 1: Write the failing hook tests**

```jsx
import { act, renderHook } from "@testing-library/react";
import { vi } from "vitest";
import { useLessonVoiceTutor } from "./useLessonVoiceTutor";

vi.mock("../api/lessonVoiceTutorService", () => ({
  createLessonVoiceSession: vi.fn(),
  closeLessonVoiceSession: vi.fn(),
  createLessonVoiceTutorConnection: vi.fn()
}));

describe("useLessonVoiceTutor", () => {
  it("does not expose answer text state", () => {
    const { result } = renderHook(() =>
      useLessonVoiceTutor({
        lessonId: "lesson-id",
        enabled: false,
        onPauseVideo: () => {},
        onResumeVideo: () => {}
      })
    );

    expect(result.current.answerText).toBeUndefined();
    expect(result.current.transcriptText).toBeUndefined();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run useLessonVoiceTutor.test.jsx`

Expected: FAIL because the hook currently exposes text state and no dedicated test file exists.

- [ ] **Step 3: Update the hook and API contract**

`frontend/src/api/lessonVoiceTutorService.js`

```js
connection.on("AssistantSpeechSegmentReady", handler);
connection.on("AssistantSpeechCompleted", handler);
connection.on("TutorFailed", handler);
```

`frontend/src/hooks/useLessonVoiceTutor.js`

```js
const [state, setState] = useState("idle");
const [errorMessage, setErrorMessage] = useState("");
const assistantCompletedRef = useRef(false);

connection.on("TranscriptionCompleted", () => {
  setState("thinking");
});

connection.on("AssistantSpeechSegmentReady", (sequenceIndex, audioUrl, durationSeconds) => {
  queueRef.current.push({ sequenceIndex, audioUrl, durationSeconds });
  queueRef.current.sort((left, right) => left.sequenceIndex - right.sequenceIndex);
  setState("speaking");
  playQueuedAudio(queueRef, isPlayingRef, () => {
    if (assistantCompletedRef.current && queueRef.current.length === 0) {
      setState("awaitingDecision");
    }
  });
});

connection.on("AssistantSpeechCompleted", () => {
  assistantCompletedRef.current = true;
  if (queueRef.current.length === 0 && !isPlayingRef.current) {
    setState("awaitingDecision");
  }
});

connection.on("TutorFailed", (message) => {
  setErrorMessage(message || "Tro giang hien chua the tra loi.");
  setState("error");
});

return {
  state,
  errorMessage,
  startRecording,
  stopRecording,
  requestFollowUp,
  resumeLearning
};
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `npm test -- --run useLessonVoiceTutor.test.jsx CourseLearnPage.test.jsx`

Expected: PASS with no remaining assertions against answer text UI.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/lessonVoiceTutorService.js \
  frontend/src/hooks/useLessonVoiceTutor.js \
  frontend/src/hooks/useLessonVoiceTutor.test.jsx \
  frontend/src/pages/CourseLearnPage.test.jsx
git commit -m "feat: simplify tutor hook for voice-first flow"
```

### Task 6: End-to-end verification and cleanup

**Files:**
- Modify: `frontend/src/components/course/LessonVoiceTutorPanel.jsx`
- Modify: `frontend/src/components/course/LessonVoiceTutorPanel.test.jsx`
- Optional delete or stop importing unused panel component if no longer needed

- [ ] **Step 1: Remove dead panel usage and ensure no imports remain**

Run:

```bash
rg -n "LessonVoiceTutorPanel" frontend/src
```

Expected: only dead files or no remaining references after the FAB replacement.

- [ ] **Step 2: Delete or retire the old panel component**

If fully unused:

```bash
git rm frontend/src/components/course/LessonVoiceTutorPanel.jsx
git rm frontend/src/components/course/LessonVoiceTutorPanel.test.jsx
```

If you prefer a softer migration, keep the files but remove imports and rename tests accordingly.

- [ ] **Step 3: Run full targeted verification**

Backend:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonVoiceTutorServiceTests|LessonTutorSegmenterTests|OpenRouterLessonTutorResponseStreamServiceTests"
```

Frontend:

```bash
npm test -- --run LessonVoiceTutorFab.test.jsx useLessonVoiceTutor.test.jsx CourseLearnPage.test.jsx
npm run build
```

Expected:

- backend targeted tests pass
- frontend targeted tests pass
- production build succeeds

- [ ] **Step 4: Manual runtime verification**

Run:

```bash
docker compose build backend frontend
docker compose up -d backend frontend
docker compose logs --tail=100 backend
```

Manual checks:

- mic control is inside the video area
- answer text is not rendered in the page
- speaking state appears after recording finishes
- first audio segment arrives before the full answer would previously have completed
- `Hoi tiep` and `Tiep tuc hoc` appear only after audio playback finishes

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: complete lesson voice tutor v2"
```

---

## Self-Review

### Spec coverage

- Compact floating mic control: covered by Task 4.
- Remove answer text from learner UI: covered by Task 5 and Task 6.
- Incremental backend response streaming: covered by Task 1, Task 2, and Task 3.
- Pause during assistant reply and explicit follow-up actions: covered by Task 4 and Task 5.
- Preserve current session model and provider stack: preserved across Tasks 1-3 without data model changes.

### Placeholder scan

- No `TODO`, `TBD`, or “similar to previous task” placeholders remain.
- Each task includes explicit files, tests, commands, and expected outcomes.

### Type consistency

- Streaming backend interface is consistently named `ILessonTutorResponseStreamService`.
- Segmenter interface is consistently named `ILessonTutorSegmenter`.
- Frontend event names align with the V2 spec:
  - `AssistantSpeechSegmentReady`
  - `AssistantSpeechCompleted`
  - `TutorFailed`

