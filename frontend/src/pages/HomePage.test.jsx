import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import HomePage from "./HomePage";

describe("HomePage", () => {
  it("renders the redesigned homepage sections", () => {
    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText(/Showcase Carousel từ các khóa học nổi bật/i)).toBeInTheDocument();
    expect(screen.getByText(/Tạo khóa học từ syllabus trong 1 nút/i)).toBeInTheDocument();
    expect(screen.getByText(/AI tự động tạo Video \+ Narration/i)).toBeInTheDocument();
    expect(screen.getByText(/Sẵn sàng tạo khóa học AI-powered\?/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Đăng ký miễn phí/i })).toBeInTheDocument();
  });
});
