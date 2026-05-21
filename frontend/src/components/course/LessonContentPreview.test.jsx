import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import LessonContentPreview from "./LessonContentPreview";

describe("LessonContentPreview", () => {
  it("renders slide cards instead of raw JSON", () => {
    render(
      <LessonContentPreview
        content={{
          teachingScript: "Script",
          slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
          voiceoverPlanJson: '{}'
        }}
      />
    );

    expect(screen.getByText("Slide 1")).toBeInTheDocument();
    expect(screen.getByText("Intro")).toBeInTheDocument();
    expect(screen.getByText("A")).toBeInTheDocument();
    expect(screen.queryByText('[{"slideNumber":1')).not.toBeInTheDocument();
  });

  it("shows fallback warning when slide json is invalid", () => {
    render(
      <LessonContentPreview
        content={{
          teachingScript: "Script",
          slideOutlineJson: '{bad json}',
          voiceoverPlanJson: '{}'
        }}
      />
    );

    expect(screen.getByText("Slide outline JSON hiện tại không hợp lệ.")).toBeInTheDocument();
  });
});
