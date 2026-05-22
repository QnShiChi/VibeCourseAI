import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import HomePage from "./HomePage";

describe("HomePage", () => {
  it("renders the refreshed hero and keeps the carousel directly below it", () => {
    const { container } = render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText(/Tạo khóa học AI-ready/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Bắt đầu miễn phí/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Xem khóa học/i })).toBeInTheDocument();
    expect(screen.getByAltText(/Minh họa giao diện dashboard khóa học AI của VibeCourseAI/i)).toBeInTheDocument();

    const orderedSections = Array.from(container.querySelectorAll("section"));
    expect(orderedSections[0]).toHaveTextContent(/Tạo khóa học AI-ready/i);
    expect(within(orderedSections[1]).getByRole("region", { name: /showcase carousel/i })).toBeInTheDocument();
  });

  it("renders the tools grid, stats band, and final CTA sections", () => {
    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText(/Công cụ cho toàn bộ pipeline khóa học/i)).toBeInTheDocument();
    expect(screen.getByText(/Import syllabus thông minh/i)).toBeInTheDocument();
    expect(screen.getByText(/Tăng tốc vận hành nội dung học tập/i)).toBeInTheDocument();
    expect(screen.getByText(/Sẵn sàng đưa khóa học lên production/i)).toBeInTheDocument();
  });
});
