import { useMemo } from "react";
import { useCarousel } from "../../hooks/useCarousel";
import styles from "../../styles/HomePage.module.css";

export default function CarouselSection({ items = [] }) {
  const slides = useMemo(() => items, [items]);
  const { activeIndex, goToIndex, goToNext, goToPrevious, pause, resume } = useCarousel({
    totalSlides: slides.length,
    autoRotateMs: 5000
  });

  if (!slides.length) {
    return null;
  }

  const activeSlide = slides[activeIndex];

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
      className={styles.carouselSection}
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
        <img alt={activeSlide.alt} className={styles.carouselImage} src={activeSlide.image} />
        <div className={styles.carouselGlow} aria-hidden="true" />

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
        {slides.map((item, index) => (
          <button
            key={item.id}
            aria-label={`Đi tới slide ${index + 1}`}
            aria-pressed={index === activeIndex}
            className={`${styles.carouselDot}${index === activeIndex ? ` ${styles.carouselDotActive}` : ""}`}
            onClick={() => goToIndex(index)}
            type="button"
          />
        ))}
      </div>
    </section>
  );
}
