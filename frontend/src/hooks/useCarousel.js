import { useEffect, useState } from "react";

export function useCarousel({ totalSlides, autoRotateMs = 5000 }) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [isPaused, setIsPaused] = useState(false);

  function normalizeIndex(index) {
    if (totalSlides <= 0) {
      return 0;
    }

    return (index + totalSlides) % totalSlides;
  }

  function goToIndex(index) {
    setActiveIndex(normalizeIndex(index));
  }

  function goToNext() {
    setActiveIndex((current) => normalizeIndex(current + 1));
  }

  function goToPrevious() {
    setActiveIndex((current) => normalizeIndex(current - 1));
  }

  useEffect(() => {
    if (!autoRotateMs || totalSlides <= 1 || isPaused) {
      return undefined;
    }

    const timerId = window.setInterval(() => {
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
    pause() {
      setIsPaused(true);
    },
    resume() {
      setIsPaused(false);
    }
  };
}
