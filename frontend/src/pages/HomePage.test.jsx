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

  it("renders the homepage wrapper without generic section layout classes", () => {
    const { container } = render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    const rootSection = container.querySelector("section");
    expect(rootSection).not.toBeNull();
    expect(rootSection.className).not.toContain("section-stack");
    expect(rootSection.className).not.toContain("section ");
    expect(rootSection.className).not.toBe("section");
  });

  it("applies the dedicated landing-page margin class to homepage sections", () => {
    const { container } = render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    const spacedSections = Array.from(container.querySelectorAll("section")).filter((section) =>
      section.className.includes("homeSectionBlock")
    );

    expect(spacedSections).toHaveLength(6);
  });
});
