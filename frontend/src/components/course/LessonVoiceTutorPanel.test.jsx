import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonVoiceTutorPanel from "./LessonVoiceTutorPanel";

describe("LessonVoiceTutorPanel", () => {
  it("shows follow-up and resume actions after an answer completes", () => {
    render(
      <LessonVoiceTutorPanel
        state="awaitingDecision"
        transcriptText="Tri tue nhan tao la gi?"
        answerText="AI la he thong mo phong tri tue cua con nguoi."
        errorMessage=""
        onStartRecording={vi.fn()}
        onStopRecording={vi.fn()}
        onFollowUp={vi.fn()}
        onResume={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: "Hoi tiep" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tiep tuc hoc" })).toBeInTheDocument();
  });
});
