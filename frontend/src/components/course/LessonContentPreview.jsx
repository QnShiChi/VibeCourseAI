import { parseSlideOutlineJson } from "../../utils/slideOutline";
import { parseVoiceoverPlanJson } from "../../utils/voiceoverPlan";

export default function LessonContentPreview({ content }) {
  if (!content) {
    return null;
  }

  const { slides, error } = getSlidePreviewState(content.slideOutlineJson);
  const { voiceoverPlan, error: voiceoverError } = getVoiceoverPreviewState(content.voiceoverPlanJson);

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
        {voiceoverError ? (
          <p className="lesson-card__error">{voiceoverError}</p>
        ) : voiceoverPlan ? (
          <div className="voiceover-preview-grid">
            <article className="voiceover-preview-item">
              <strong>Thời lượng dự kiến</strong>
              <p>{voiceoverPlan.estimatedDurationMinutes} phút</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Giọng điệu</strong>
              <p>{voiceoverPlan.tone}</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Nhịp đọc</strong>
              <p>{voiceoverPlan.pacing}</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Đối tượng nghe</strong>
              <p>{voiceoverPlan.targetAudience}</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Lưu ý phát âm</strong>
              <p>{voiceoverPlan.pronunciationNotes}</p>
            </article>
          </div>
        ) : (
          <p>Chưa có voiceover plan.</p>
        )}
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

function getVoiceoverPreviewState(voiceoverPlanJson) {
  if (!voiceoverPlanJson?.trim()) {
    return { voiceoverPlan: null, error: "" };
  }

  try {
    return {
      voiceoverPlan: parseVoiceoverPlanJson(voiceoverPlanJson),
      error: ""
    };
  } catch {
    return {
      voiceoverPlan: null,
      error: "Voiceover plan JSON hiện tại không hợp lệ."
    };
  }
}
