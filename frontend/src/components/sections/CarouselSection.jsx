import { useEffect, useMemo, useRef, useState } from "react";
import { useCarousel } from "../../hooks/useCarousel";
import styles from "../../styles/HomePage.module.css";

export default function CarouselSection({ items = [], className = "" }) {
  const transitionDurationMs = 820;
  const slides = useMemo(() => items, [items]);
  const { activeIndex, goToIndex, goToNext, goToPrevious, lastDirection, pause, resume } = useCarousel({
    totalSlides: slides.length,
    autoRotateMs: 5000
  });
  const previousActiveIndexRef = useRef(0);
  const [transitionState, setTransitionState] = useState(null);

  useEffect(() => {
    const previousActiveIndex = previousActiveIndexRef.current;

    if (previousActiveIndex === activeIndex) {
      return undefined;
    }

    const nextTransitionState = {
      fromIndex: previousActiveIndex,
      toIndex: activeIndex,
      direction: lastDirection === "previous" ? "previous" : "next"
    };

    previousActiveIndexRef.current = activeIndex;
    setTransitionState(nextTransitionState);

    const timerId = window.setTimeout(() => {
      setTransitionState((current) => (
        current?.fromIndex === nextTransitionState.fromIndex && current?.toIndex === nextTransitionState.toIndex
          ? null
          : current
      ));
    }, transitionDurationMs);

    return () => window.clearTimeout(timerId);
  }, [activeIndex, lastDirection]);

  if (!slides.length) {
    return null;
  }

  const activeSlide = slides[activeIndex];
  const isTransitioning = transitionState?.toIndex === activeIndex;
  const enterOffset = !isTransitioning
    ? 0
    : transitionState.direction === "previous"
      ? 22
      : -22;
  const outgoingSlide = isTransitioning ? slides[transitionState.fromIndex] : null;

  function handleKeyDown(event) {
    if (event.key === "ArrowRight") {
      goToNext();
    }

    if (event.key === "ArrowLeft") {
      goToPrevious();
    }
  }

  return (
    <section
      aria-label="Showcase Carousel từ các khóa học nổi bật"
      className={`${styles.carouselSection} ${className}`.trim()}
      data-reveal="true"
      onKeyDown={handleKeyDown}
      role="region"
      tabIndex={0}
    >
      <div className={styles.carouselHeader}>
        <span className="ui-badge">Showcase</span>
        <div>
          <h2>Showcase Carousel từ các khóa học nổi bật</h2>
          <p>Các visual nổi bật giúp nhìn ngay tinh thần AI, video và trải nghiệm học tập của VibeCourseAI.</p>
        </div>
      </div>

      <div className={styles.carouselFrame} onMouseEnter={pause} onMouseLeave={resume}>
        <div className={styles.carouselMediaStack}>
          {outgoingSlide ? (
            <img
              alt={outgoingSlide.alt}
              className={`${styles.carouselImage} ${styles.carouselImageOutgoing}`.trim()}
              data-testid="carousel-outgoing-image"
              src={outgoingSlide.image}
            />
          ) : null}
          <img
            alt={activeSlide.alt}
            className={`${styles.carouselImage} ${isTransitioning ? styles.carouselImageIncoming : ""}`.trim()}
            src={activeSlide.image}
          />
        </div>
        <div className={styles.carouselGlow} aria-hidden="true" />
        {activeSlide.accentPill ? (
          <span
            aria-label={activeSlide.accentPill.label}
            className={styles.carouselAccentPill}
          />
        ) : null}

        <div className={styles.carouselOverlay}>
          <span className="ui-badge">{activeSlide.tag}</span>
          <h3>{activeSlide.title}</h3>
          <p>{activeSlide.caption}</p>
        </div>

        <div className={styles.carouselControls}>
          <button aria-label="Slide trước" className={styles.carouselControl} onClick={goToPrevious} type="button">
            ←
          </button>
          <button aria-label="Slide tiếp theo" className={styles.carouselControl} onClick={goToNext} type="button">
            →
          </button>
        </div>
      </div>

      <div className={styles.carouselDots}>
        {slides.map((item, index) => {
          const isActive = index === activeIndex;

          return (
            <button
              key={item.id}
              aria-label={`Đi tới slide ${index + 1}`}
              aria-pressed={isActive}
              className={`${styles.carouselDot}${isActive ? ` ${styles.carouselDotActive}` : ""}`}
              data-transfer-state={isActive && isTransitioning ? "arriving" : undefined}
              style={isActive ? { "--carousel-enter-offset": `${enterOffset}px` } : undefined}
              onClick={() => goToIndex(index)}
              type="button"
            />
          );
        })}
      </div>
    </section>
  );
}
