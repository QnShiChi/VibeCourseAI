import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import CarouselSection from "./CarouselSection";

const items = [
  {
    id: "1",
    image: "/a.jpg",
    alt: "A",
    title: "Khóa học A",
    caption: "Caption A",
    tag: "Tag A",
    accentPill: { label: "CTA", tone: "system-green" }
  },
  { id: "2", image: "/b.jpg", alt: "B", title: "Khóa học B", caption: "Caption B", tag: "Tag B" }
];

describe("CarouselSection", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders active carousel content and next controls", () => {
    render(<CarouselSection items={items} />);

    expect(screen.getByText("Khóa học A")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /slide tiếp theo/i }));
    expect(screen.getByText("Khóa học B")).toBeInTheDocument();
  });

  it("supports keyboard navigation through arrow keys", () => {
    render(<CarouselSection items={items} />);

    const region = screen.getByRole("region", { name: /showcase carousel/i });
    region.focus();
    fireEvent.keyDown(region, { key: "ArrowRight" });
    expect(screen.getByText("Khóa học B")).toBeInTheDocument();
  });

  it("renders accent pill only for slides configured with one", () => {
    render(<CarouselSection items={items} />);

    expect(screen.getByLabelText("CTA")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /slide tiếp theo/i }));
    expect(screen.queryByLabelText("CTA")).not.toBeInTheDocument();
  });

  it("animates the green bubble only after the active slide changes", () => {
    render(<CarouselSection items={items} />);

    const firstDot = screen.getByRole("button", { name: /đi tới slide 1/i });
    const secondDot = screen.getByRole("button", { name: /đi tới slide 2/i });

    expect(firstDot).not.toHaveAttribute("data-transfer-state");
    expect(secondDot).not.toHaveAttribute("data-transfer-state");

    fireEvent.click(screen.getByRole("button", { name: /slide tiếp theo/i }));

    expect(secondDot).toHaveAttribute("data-transfer-state", "arriving");
    expect(secondDot).toHaveStyle({
      "--carousel-enter-offset": "-22px"
    });
  });

  it("keeps a real transition phase, then settles the bubble without replaying back", () => {
    render(<CarouselSection items={items} />);

    fireEvent.click(screen.getByRole("button", { name: /slide tiếp theo/i }));

    expect(screen.getByTestId("carousel-outgoing-image")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /đi tới slide 2/i })).toHaveAttribute("data-transfer-state", "arriving");

    act(() => {
      vi.advanceTimersByTime(820);
    });

    expect(screen.queryByTestId("carousel-outgoing-image")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /đi tới slide 2/i })).not.toHaveAttribute("data-transfer-state");
  });

  it("reverses the arrival offset when moving to the previous slide", () => {
    render(<CarouselSection items={items} />);

    fireEvent.click(screen.getByRole("button", { name: /slide tiếp theo/i }));
    fireEvent.click(screen.getByRole("button", { name: /slide trước/i }));

    expect(screen.getByRole("button", { name: /đi tới slide 1/i })).toHaveStyle({
      "--carousel-enter-offset": "22px"
    });
  });
});
