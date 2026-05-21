import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonContentEditor from "./LessonContentEditor";

describe("LessonContentEditor", () => {
  it("edits generated lesson content with structured slide editor", () => {
    const onChange = vi.fn();
    const onSave = vi.fn();

    render(
      <LessonContentEditor
        form={{
          teachingScript: "Script",
          slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
          voiceoverPlanJson: '{"tone":"clear"}'
        }}
        onChange={onChange}
        onCancel={() => {}}
        onSave={onSave}
      />
    );

    fireEvent.change(screen.getByLabelText("Tiêu đề slide 1"), {
      target: { value: "Overview" }
    });

    expect(onChange).toHaveBeenCalledWith(
      "slideOutlineJson",
      '[{"slideNumber":1,"title":"Overview","bulletPoints":["A"],"speakerNotes":"N"}]'
    );

    fireEvent.click(screen.getByRole("button", { name: "Lưu nội dung AI" }));
    expect(onSave).toHaveBeenCalled();
  });
});
