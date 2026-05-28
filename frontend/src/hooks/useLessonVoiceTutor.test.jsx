import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useLessonVoiceTutor } from "./useLessonVoiceTutor";

vi.mock("../api/lessonVoiceTutorService", () => ({
  createLessonVoiceSession: vi.fn(),
  closeLessonVoiceSession: vi.fn(),
  createLessonVoiceTutorConnection: vi.fn()
}));

describe("useLessonVoiceTutor", () => {
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
});
