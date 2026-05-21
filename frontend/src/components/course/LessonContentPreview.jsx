import { parseSlideOutlineJson } from "../../utils/slideOutline";

export default function LessonContentPreview({ content }) {
  if (!content) {
    return null;
  }

  const { slides, error } = getSlidePreviewState(content.slideOutlineJson);

  return (
    <div className="lesson-generated-stack">
      <section className="surface-card lesson-generated-card">
        <h4>Script</h4>
        <pre className="text-preview text-preview--compact">{content.teachingScript || "Chưa có script."}</pre>
      </section>

      <section className="surface-card lesson-generated-card">
        <h4>Slides</h4>
        {error ? (
          <p className="lesson-card__error">{error}</p>
        ) : slides.length ? (
          <div className="slide-preview-stack">
            {slides.map((slide) => (
              <article className="slide-preview-card" key={slide.slideNumber}>
                <strong>Slide {slide.slideNumber}</strong>
                <h5>{slide.title}</h5>
                <ul>
                  {slide.bulletPoints.map((point, index) => (
                    <li key={`${slide.slideNumber}-${index}`}>{point}</li>
                  ))}
                </ul>
                <p>{slide.speakerNotes}</p>
              </article>
            ))}
          </div>
        ) : (
          <p>Chưa có slide outline.</p>
        )}
      </section>

      <section className="surface-card lesson-generated-card">
        <h4>Voiceover</h4>
        <pre className="text-preview text-preview--compact">{content.voiceoverPlanJson || "Chưa có voiceover plan."}</pre>
      </section>
    </div>
  );
}

function getSlidePreviewState(slideOutlineJson) {
  if (!slideOutlineJson?.trim()) {
    return { slides: [], error: "" };
  }

  try {
    return {
      slides: parseSlideOutlineJson(slideOutlineJson),
      error: ""
    };
  } catch {
    return {
      slides: [],
      error: "Slide outline JSON hiện tại không hợp lệ."
    };
  }
}
