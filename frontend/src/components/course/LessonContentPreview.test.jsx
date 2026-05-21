import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import LessonContentPreview from "./LessonContentPreview";

describe("LessonContentPreview", () => {
  it("renders slide cards and structured voiceover fields instead of raw JSON", () => {
    render(
      <LessonContentPreview
        content={{
          teachingScript: "Script",
          slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
          voiceoverPlanJson:
            '{"estimatedDurationMinutes":8,"tone":"Clear","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
        }}
      />
    );

    expect(screen.getByText("Slide 1")).toBeInTheDocument();
    expect(screen.getByText("Intro")).toBeInTheDocument();
    expect(screen.getByText("A")).toBeInTheDocument();
    expect(screen.getByText("Thời lượng dự kiến")).toBeInTheDocument();
    expect(screen.getByText("8 phút")).toBeInTheDocument();
    expect(screen.getByText("Giọng điệu")).toBeInTheDocument();
    expect(screen.getByText("Clear")).toBeInTheDocument();
    expect(screen.queryByText('[{"slideNumber":1')).not.toBeInTheDocument();
  });

  it("shows fallback warning when slide or voiceover json is invalid", () => {
    render(
      <LessonContentPreview
        content={{
          teachingScript: "Script",
          slideOutlineJson: '{bad json}',
          voiceoverPlanJson: '{bad json}'
        }}
      />
    );

    expect(screen.getByText("Slide outline JSON hiện tại không hợp lệ.")).toBeInTheDocument();
    expect(screen.getByText("Voiceover plan JSON hiện tại không hợp lệ.")).toBeInTheDocument();
  });
});
