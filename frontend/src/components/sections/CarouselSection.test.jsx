import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import CarouselSection from "./CarouselSection";

const items = [
  { id: "1", image: "/a.jpg", alt: "A", title: "Khóa học A", caption: "Caption A", tag: "Tag A" },
  { id: "2", image: "/b.jpg", alt: "B", title: "Khóa học B", caption: "Caption B", tag: "Tag B" }
];

describe("CarouselSection", () => {
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
});
