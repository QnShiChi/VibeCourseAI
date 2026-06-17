import { useEffect, useRef, useState } from "react";

export function useCarousel({ totalSlides, autoRotateMs = 5000 }) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const lastDirectionRef = useRef("none");

  function normalizeIndex(index) {
    if (totalSlides <= 0) {
      return 0;
    }

    return (index + totalSlides) % totalSlides;
  }

  function goToIndex(index) {
    setActiveIndex((current) => {
      const nextIndex = normalizeIndex(index);
      lastDirectionRef.current = nextIndex === current ? "none" : nextIndex > current ? "next" : "previous";
      return nextIndex;
    });
  }

  function goToNext() {
    lastDirectionRef.current = "next";
    setActiveIndex((current) => normalizeIndex(current + 1));
  }

  function goToPrevious() {
    lastDirectionRef.current = "previous";
    setActiveIndex((current) => normalizeIndex(current - 1));
  }

  useEffect(() => {
    if (!autoRotateMs || totalSlides <= 1 || isPaused) {
      return undefined;
    }

    const timerId = window.setInterval(() => {
      lastDirectionRef.current = "next";
      setActiveIndex((current) => normalizeIndex(current + 1));
    }, autoRotateMs);

    return () => window.clearInterval(timerId);
  }, [autoRotateMs, isPaused, totalSlides]);

  useEffect(() => {
    setActiveIndex((current) => normalizeIndex(current));
  }, [totalSlides]);

  return {
    activeIndex,
    goToIndex,
    goToNext,
    goToPrevious,
    lastDirection: lastDirectionRef.current,
    pause() {
      setIsPaused(true);
    },
    resume() {
      setIsPaused(false);
    }
  };
}
