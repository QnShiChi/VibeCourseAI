import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useLessonVoiceTutor } from "./useLessonVoiceTutor";

const mockCreateLessonVoiceSession = vi.fn();
const mockCloseLessonVoiceSession = vi.fn();
const mockCreateLessonVoiceTutorConnection = vi.fn();

vi.mock("../api/lessonVoiceTutorService", () => ({
  createLessonVoiceSession: (...args) => mockCreateLessonVoiceSession(...args),
  closeLessonVoiceSession: (...args) => mockCloseLessonVoiceSession(...args),
  createLessonVoiceTutorConnection: (...args) => mockCreateLessonVoiceTutorConnection(...args)
}));

describe("useLessonVoiceTutor", () => {
  beforeEach(() => {
    mockCreateLessonVoiceSession.mockReset();
    mockCloseLessonVoiceSession.mockReset();
    mockCreateLessonVoiceTutorConnection.mockReset();
  });

  it("does not expose transcript or answer text state", () => {
    const { result } = renderHook(() =>
      useLessonVoiceTutor({
        lessonId: "lesson-id",
        enabled: false,
        onPauseVideo: () => {},
        onResumeVideo: () => {}
      })
    );

    expect(result.current.transcriptText).toBeUndefined();
    expect(result.current.answerText).toBeUndefined();
    expect(result.current.state).toBe("idle");
  });

  it("starts recording immediately when requesting a follow-up", async () => {
    const stopTrack = vi.fn();
    const stream = { getTracks: () => [{ stop: stopTrack }] };

    mockCreateLessonVoiceSession.mockResolvedValue({ sessionId: "session-1" });
    Object.defineProperty(global.navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue(stream)
      }
    });

    class FakeMediaRecorder {
      constructor() {
        this.state = "inactive";
        this.ondataavailable = null;
        this.onstop = null;
      }

      start() {
        this.state = "recording";
      }

      stop() {
        this.state = "inactive";
      }
    }

    global.MediaRecorder = FakeMediaRecorder;

    const { result } = renderHook(() =>
      useLessonVoiceTutor({
        lessonId: "lesson-id",
        enabled: false,
        onPauseVideo: () => {},
        onResumeVideo: () => {}
      })
    );

    await act(async () => {
      await result.current.requestFollowUp(12);
    });

    expect(global.navigator.mediaDevices.getUserMedia).toHaveBeenCalledWith({ audio: true });
    expect(result.current.state).toBe("recording");
  });
});
