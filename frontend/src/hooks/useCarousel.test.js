import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useCarousel } from "./useCarousel";

describe("useCarousel", () => {
  it("cycles through slides and wraps around", () => {
    const { result } = renderHook(() => useCarousel({ totalSlides: 3, autoRotateMs: 0 }));

    expect(result.current.activeIndex).toBe(0);

    act(() => result.current.goToNext());
    expect(result.current.activeIndex).toBe(1);

    act(() => result.current.goToPrevious());
    expect(result.current.activeIndex).toBe(0);

    act(() => result.current.goToPrevious());
    expect(result.current.activeIndex).toBe(2);
  });
});
