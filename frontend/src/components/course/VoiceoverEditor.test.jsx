import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import VoiceoverEditor from "./VoiceoverEditor";

describe("VoiceoverEditor", () => {
  it("updates structured voiceover fields", () => {
    const onChange = vi.fn();

    render(
      <VoiceoverEditor
        voiceoverPlan={{
          estimatedDurationMinutes: 8,
          tone: "Clear",
          pacing: "Moderate",
          targetAudience: "Students",
          pronunciationNotes: "OOP"
        }}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.change(screen.getByLabelText("Giọng điệu"), {
      target: { value: "Warm" }
    });

    expect(onChange).toHaveBeenCalledWith({
      estimatedDurationMinutes: 8,
      tone: "Warm",
      pacing: "Moderate",
      targetAudience: "Students",
      pronunciationNotes: "OOP"
    });
  });
});
