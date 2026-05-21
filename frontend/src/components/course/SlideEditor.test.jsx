import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import SlideEditor from "./SlideEditor";

describe("SlideEditor", () => {
  it("renders slides and updates title", () => {
    const onChange = vi.fn();

    render(
      <SlideEditor
        slides={[{ slideNumber: 1, title: "Intro", bulletPoints: ["A"], speakerNotes: "N" }]}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.change(screen.getByLabelText("Tiêu đề slide 1"), {
      target: { value: "Overview" }
    });

    expect(onChange).toHaveBeenCalledWith([
      { slideNumber: 1, title: "Overview", bulletPoints: ["A"], speakerNotes: "N" }
    ]);
  });

  it("adds and removes bullet points", () => {
    const onChange = vi.fn();

    render(
      <SlideEditor
        slides={[{ slideNumber: 1, title: "Intro", bulletPoints: ["A"], speakerNotes: "N" }]}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Thêm bullet point" }));
    fireEvent.click(screen.getByRole("button", { name: "Xóa bullet point 1-1" }));

    expect(onChange).toHaveBeenNthCalledWith(1, [
      { slideNumber: 1, title: "Intro", bulletPoints: ["A", ""], speakerNotes: "N" }
    ]);
    expect(onChange).toHaveBeenNthCalledWith(2, [
      { slideNumber: 1, title: "Intro", bulletPoints: [], speakerNotes: "N" }
    ]);
  });

  it("adds, removes, and reorders slides", () => {
    const onChange = vi.fn();

    render(
      <SlideEditor
        slides={[
          { slideNumber: 1, title: "One", bulletPoints: ["A"], speakerNotes: "N1" },
          { slideNumber: 2, title: "Two", bulletPoints: ["B"], speakerNotes: "N2" }
        ]}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Di chuyển xuống slide 1" }));
    fireEvent.click(screen.getByRole("button", { name: "Thêm slide" }));
    fireEvent.click(screen.getByRole("button", { name: "Xóa slide 2" }));

    expect(onChange).toHaveBeenNthCalledWith(1, [
      { slideNumber: 2, title: "Two", bulletPoints: ["B"], speakerNotes: "N2" },
      { slideNumber: 1, title: "One", bulletPoints: ["A"], speakerNotes: "N1" }
    ]);
    expect(onChange).toHaveBeenNthCalledWith(2, [
      { slideNumber: 1, title: "One", bulletPoints: ["A"], speakerNotes: "N1" },
      { slideNumber: 2, title: "Two", bulletPoints: ["B"], speakerNotes: "N2" },
      { slideNumber: 3, title: "", bulletPoints: [""], speakerNotes: "" }
    ]);
    expect(onChange).toHaveBeenNthCalledWith(3, [
      { slideNumber: 1, title: "One", bulletPoints: ["A"], speakerNotes: "N1" }
    ]);
  });
});
