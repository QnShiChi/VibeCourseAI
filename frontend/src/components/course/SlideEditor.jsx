import Button from "../ui/Button";
import FormField from "../ui/FormField";

export default function SlideEditor({ slides, onChange, validationError }) {
  function updateSlide(index, patch) {
    onChange(
      slides.map((slide, currentIndex) =>
        currentIndex === index ? { ...slide, ...patch } : slide
      )
    );
  }

  function moveSlide(index, direction) {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= slides.length) {
      return;
    }

    const nextSlides = [...slides];
    [nextSlides[index], nextSlides[targetIndex]] = [nextSlides[targetIndex], nextSlides[index]];
    onChange(nextSlides);
  }

  function addSlide() {
    onChange([
      ...slides,
      {
        slideNumber: slides.length + 1,
        title: "",
        bulletPoints: [""],
        speakerNotes: ""
      }
    ]);
  }

  function removeSlide(index) {
    onChange(slides.filter((_, currentIndex) => currentIndex !== index));
  }

  function addBulletPoint(index) {
    updateSlide(index, {
      bulletPoints: [...slides[index].bulletPoints, ""]
    });
  }

  function updateBulletPoint(index, bulletIndex, value) {
    updateSlide(index, {
      bulletPoints: slides[index].bulletPoints.map((point, currentIndex) =>
        currentIndex === bulletIndex ? value : point
      )
    });
  }

  function removeBulletPoint(index, bulletIndex) {
    updateSlide(index, {
      bulletPoints: slides[index].bulletPoints.filter((_, currentIndex) => currentIndex !== bulletIndex)
    });
  }

  return (
    <div className="slide-editor-stack">
      {slides.map((slide, index) => (
        <section className="surface-card slide-editor-card" key={`${slide.slideNumber}-${index}`}>
          <div className="slide-editor-actions">
            <h4>Slide {index + 1}</h4>
            <Button onClick={() => moveSlide(index, -1)} variant="ghost" disabled={index === 0}>
              Di chuyển lên slide {index + 1}
            </Button>
            <Button onClick={() => moveSlide(index, 1)} variant="ghost" disabled={index === slides.length - 1}>
              Di chuyển xuống slide {index + 1}
            </Button>
            <Button onClick={() => removeSlide(index)} variant="ghost">
              Xóa slide {index + 1}
            </Button>
          </div>

          <FormField id={`slide-title-${index}`} label={`Tiêu đề slide ${index + 1}`}>
            <input
              className="ui-input"
              id={`slide-title-${index}`}
              value={slide.title}
              onChange={(event) => updateSlide(index, { title: event.target.value })}
            />
          </FormField>

          <div className="slide-editor-bullets">
            {slide.bulletPoints.map((point, bulletIndex) => (
              <FormField
                id={`slide-bullet-${index}-${bulletIndex}`}
                key={`bullet-${index}-${bulletIndex}`}
                label={`Bullet point ${index + 1}-${bulletIndex + 1}`}
              >
                <div className="slide-editor-inline-row">
                  <input
                    className="ui-input"
                    id={`slide-bullet-${index}-${bulletIndex}`}
                    value={point}
                    onChange={(event) => updateBulletPoint(index, bulletIndex, event.target.value)}
                  />
                  <Button onClick={() => removeBulletPoint(index, bulletIndex)} variant="ghost">
                    Xóa bullet point {index + 1}-{bulletIndex + 1}
                  </Button>
                </div>
              </FormField>
            ))}
            <Button onClick={() => addBulletPoint(index)} variant="ghost">
              Thêm bullet point
            </Button>
          </div>

          <FormField id={`slide-notes-${index}`} label={`Speaker notes slide ${index + 1}`}>
            <textarea
              className="ui-input ui-textarea"
              id={`slide-notes-${index}`}
              rows="4"
              value={slide.speakerNotes}
              onChange={(event) => updateSlide(index, { speakerNotes: event.target.value })}
            />
          </FormField>
        </section>
      ))}

      {validationError ? <p className="lesson-card__error">{validationError}</p> : null}

      <Button onClick={addSlide} variant="ghost">
        Thêm slide
      </Button>
    </div>
  );
}
