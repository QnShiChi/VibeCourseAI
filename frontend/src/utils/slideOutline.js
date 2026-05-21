export function parseSlideOutlineJson(value) {
  if (!value?.trim()) {
    return [];
  }

  const parsed = JSON.parse(value);
  if (!Array.isArray(parsed)) {
    throw new Error("Slide outline phải là một mảng slide.");
  }

  return parsed.map((slide, index) => ({
    slideNumber: Number(slide.slideNumber ?? slide.SlideNumber ?? index + 1),
    title: String(slide.title ?? slide.Title ?? ""),
    bulletPoints: Array.isArray(slide.bulletPoints)
      ? slide.bulletPoints.map(String)
      : Array.isArray(slide.BulletPoints)
        ? slide.BulletPoints.map(String)
        : [],
    speakerNotes: String(slide.speakerNotes ?? slide.SpeakerNotes ?? "")
  }));
}

export function normalizeSlides(slides) {
  return slides.map((slide, index) => ({
    slideNumber: index + 1,
    title: slide.title.trim(),
    bulletPoints: slide.bulletPoints.map((item) => item.trim()).filter(Boolean),
    speakerNotes: slide.speakerNotes.trim()
  }));
}

export function validateSlides(slides) {
  if (!slides.length) {
    throw new Error("Lesson phải có ít nhất một slide.");
  }

  for (const slide of normalizeSlides(slides)) {
    if (!slide.title) {
      throw new Error("Tiêu đề slide là bắt buộc.");
    }

    if (!slide.bulletPoints.length) {
      throw new Error("Mỗi slide phải có ít nhất một bullet point.");
    }

    if (!slide.speakerNotes) {
      throw new Error("Speaker notes là bắt buộc.");
    }
  }
}

export function serializeSlideOutline(slides) {
  validateSlides(slides);
  return JSON.stringify(normalizeSlides(slides));
}
