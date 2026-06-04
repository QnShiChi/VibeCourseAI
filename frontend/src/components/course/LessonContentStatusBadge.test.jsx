import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import LessonContentStatusBadge from "./LessonContentStatusBadge";

describe("LessonContentStatusBadge", () => {
  it("renders a contextual label and description for completed content", () => {
    render(<LessonContentStatusBadge type="content" status="Completed" />);

    const badge = screen.getByText("Nội dung: Đã xong");
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveAttribute("title", "Nội dung bài học đã generate xong.");
  });

  it("renders a contextual label and description for not generated video", () => {
    render(<LessonContentStatusBadge type="video" status="NotGenerated" />);

    const badge = screen.getByText("Video: Chưa tạo");
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveAttribute("title", "Video bài học chưa được generate.");
  });

  it("renders a contextual label and description for failed audio", () => {
    render(<LessonContentStatusBadge type="audio" status="Failed" />);

    const badge = screen.getByText("Audio: Lỗi");
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveAttribute("title", "Audio bài học generate lỗi. Cần chạy lại.");
  });
});
