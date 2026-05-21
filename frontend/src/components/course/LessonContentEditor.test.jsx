import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonContentEditor from "./LessonContentEditor";

describe("LessonContentEditor", () => {
  it("submits edited lesson generated content", () => {
    const onChange = vi.fn();
    const onSave = vi.fn();

    render(
      <LessonContentEditor
        form={{
          teachingScript: "Script",
          slideOutlineJson: "{\"slides\":[]}",
          voiceoverPlanJson: "{\"tone\":\"clear\"}"
        }}
        onChange={onChange}
        onCancel={() => {}}
        onSave={onSave}
      />
    );

    fireEvent.change(screen.getByLabelText("Teaching script"), { target: { value: "Script moi" } });
    expect(onChange).toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: "Lưu nội dung AI" }));
    expect(onSave).toHaveBeenCalled();
  });
});
