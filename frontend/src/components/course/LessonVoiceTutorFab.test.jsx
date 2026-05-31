import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonVoiceTutorFab from "./LessonVoiceTutorFab";

describe("LessonVoiceTutorFab", () => {
  it("shows compact idle label and mic action", () => {
    render(
      <LessonVoiceTutorFab
        state="idle"
        errorMessage=""
        onStartRecording={vi.fn()}
        onStopRecording={vi.fn()}
        onRequestFollowUp={vi.fn()}
        onResumeLearning={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: /hỏi ngay/i })).toBeInTheDocument();
    expect(screen.getByText("Hỏi ngay")).toBeInTheDocument();
  });

  it("shows follow-up actions after speech completes", () => {
    render(
      <LessonVoiceTutorFab
        state="awaitingDecision"
        errorMessage=""
        onStartRecording={vi.fn()}
        onStopRecording={vi.fn()}
        onRequestFollowUp={vi.fn()}
        onResumeLearning={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: "Hỏi tiếp" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tiếp tục học" })).toBeInTheDocument();
  });
});
