import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterAll, beforeAll, describe, expect, it, vi } from "vitest";
import HomePage, { createHomepageParticles } from "./HomePage";
import { ThemeProvider } from "../theme/ThemeContext";

describe("HomePage", () => {
  const originalGetContext = HTMLCanvasElement.prototype.getContext;

  beforeAll(() => {
    HTMLCanvasElement.prototype.getContext = vi.fn((contextType) => {
      if (contextType !== "2d") {
        return null;
      }

      return {
        arc: vi.fn(),
        beginPath: vi.fn(),
        clearRect: vi.fn(),
        fill: vi.fn(),
        setTransform: vi.fn(),
        set fillStyle(_) {},
        set globalCompositeOperation(_) {}
      };
    });
  });

  afterAll(() => {
    HTMLCanvasElement.prototype.getContext = originalGetContext;
  });

  it("renders the refreshed hero and keeps the carousel directly below it", () => {
    const { container } = render(
      <MemoryRouter>
        <ThemeProvider>
          <HomePage />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByText(/Tạo khóa học AI-ready/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Bắt đầu miễn phí/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Xem khóa học/i })).toBeInTheDocument();
    expect(screen.getByAltText(/Minh họa giao diện dashboard khóa học AI của VibeCourseAI/i)).toBeInTheDocument();
    expect(screen.getByText(/Live orchestration/i)).toBeInTheDocument();
    expect(screen.getByText(/Syllabus intake/i)).toBeInTheDocument();
    expect(screen.getByText(/Video-ready output/i)).toBeInTheDocument();
    expect(screen.getByText(/Learner feedback/i)).toBeInTheDocument();

    const orderedSections = Array.from(container.querySelectorAll("section"));
    expect(orderedSections[0]).toHaveTextContent(/Tạo khóa học AI-ready/i);
    expect(within(orderedSections[1]).getByRole("region", { name: /showcase carousel/i })).toBeInTheDocument();
  });

  it("renders the tools grid, stats band, and final CTA sections", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <HomePage />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByText(/Công cụ cho toàn bộ pipeline khóa học/i)).toBeInTheDocument();
    expect(screen.getByText(/Import syllabus thông minh/i)).toBeInTheDocument();
    expect(screen.getByText(/Tăng tốc vận hành nội dung học tập/i)).toBeInTheDocument();
    expect(screen.getByText(/Sẵn sàng đưa khóa học lên production/i)).toBeInTheDocument();
    expect(screen.getAllByTestId("tool-card-icon")).toHaveLength(4);
    expect(document.querySelectorAll('[data-testid="tool-card-icon"] svg')).toHaveLength(4);
  });

  it("renders a canvas-based particle layer across the homepage background", () => {
    const { container } = render(
      <MemoryRouter>
        <ThemeProvider>
          <HomePage />
        </ThemeProvider>
      </MemoryRouter>
    );

    const homepage = container.firstChild;
    const particleCanvas = screen.getByTestId("homepage-particle-canvas");

    expect(homepage.firstChild).toBe(particleCanvas);
    expect(particleCanvas).toHaveStyle({
      position: "fixed"
    });
    expect(particleCanvas.tagName).toBe("CANVAS");
  });

  it("distributes particles across the full viewport without leaving dense grid cells empty", () => {
    const particles = createHomepageParticles(() => 0.5);
    const occupiedCells = new Set(
      particles.map((particle) => {
        const left = particle.originX;
        const top = particle.originY;
        return `${Math.floor(left / 4)}-${Math.floor(top / 5)}`;
      })
    );

    expect(particles).toHaveLength(500);
    expect(occupiedCells.size).toBe(500);
  });

  it("uses small particles with looping wander and fade-cycle data for the drifting effect", () => {
    const particles = createHomepageParticles(() => 0.5);
    const firstParticle = particles[0];

    expect(firstParticle.radius).toBeLessThanOrEqual(1.8);
    expect(firstParticle.opacity).toBeGreaterThanOrEqual(0.16);
    expect(firstParticle.lifeDuration).toBeGreaterThan(5);
    expect(firstParticle.fadeOffset).toBeGreaterThanOrEqual(0);
    expect(firstParticle.fadeOffset).toBeLessThanOrEqual(1);
    expect(firstParticle.drift).toBeGreaterThanOrEqual(8);
    expect(firstParticle.travelX).toBeGreaterThanOrEqual(-39);
    expect(firstParticle.travelX).toBeLessThanOrEqual(39);
    expect(firstParticle.travelY).toBeGreaterThanOrEqual(-39);
    expect(firstParticle.travelY).toBeLessThanOrEqual(39);
  });

  it("renders the hero theme toggle while still reading the shared app theme", () => {
    window.localStorage.setItem("app-theme", "dark");

    const { container } = render(
      <MemoryRouter>
        <ThemeProvider>
          <HomePage />
        </ThemeProvider>
      </MemoryRouter>
    );

    const homepage = container.firstChild;

    expect(homepage).toHaveAttribute("data-theme", "dark");
    expect(screen.getByRole("button", { name: /chuyển sang light mode/i })).toBeInTheDocument();
  });
});
