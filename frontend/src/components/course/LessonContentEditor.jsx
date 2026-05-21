import { useEffect, useState } from "react";
import { parseSlideOutlineJson, serializeSlideOutline } from "../../utils/slideOutline";
import Button from "../ui/Button";
import FormField from "../ui/FormField";
import SlideEditor from "./SlideEditor";

export default function LessonContentEditor({ form, onChange, onSave, onCancel }) {
  const [slideError, setSlideError] = useState("");
  const [slides, setSlides] = useState(() => safeParseSlides(form.slideOutlineJson).slides);

  useEffect(() => {
    const parsed = safeParseSlides(form.slideOutlineJson);
    setSlides(parsed.slides);
    setSlideError(parsed.error);
  }, [form.slideOutlineJson]);

  function handleSlidesChange(nextSlides) {
    setSlides(nextSlides);

    try {
      const serialized = serializeSlideOutline(nextSlides);
      setSlideError("");
      onChange("slideOutlineJson", serialized);
    } catch (error) {
      setSlideError(error.message);
    }
  }

  return (
    <div className="inline-edit-card">
      <FormField id="lesson-generated-script" label="Teaching script">
        <textarea
          className="ui-input ui-textarea"
          id="lesson-generated-script"
          rows="8"
          value={form.teachingScript}
          onChange={(event) => onChange("teachingScript", event.target.value)}
        />
      </FormField>

      <div className="form-field">
        <span className="form-field__label">Slides</span>
        <SlideEditor slides={slides} onChange={handleSlidesChange} validationError={slideError} />
      </div>

      <FormField id="lesson-generated-voiceover" label="Voiceover plan JSON">
        <textarea
          className="ui-input ui-textarea"
          id="lesson-generated-voiceover"
          rows="6"
          value={form.voiceoverPlanJson}
          onChange={(event) => onChange("voiceoverPlanJson", event.target.value)}
        />
      </FormField>
      <div className="quick-actions">
        <Button onClick={onSave}>Lưu nội dung AI</Button>
        <Button onClick={onCancel} variant="ghost">Hủy</Button>
      </div>
    </div>
  );
}

function safeParseSlides(slideOutlineJson) {
  try {
    const slides = parseSlideOutlineJson(slideOutlineJson);
    return {
      slides: slides.length
        ? slides
        : [{ slideNumber: 1, title: "", bulletPoints: [""], speakerNotes: "" }],
      error: ""
    };
  } catch (error) {
    return {
      slides: [{ slideNumber: 1, title: "", bulletPoints: [""], speakerNotes: "" }],
      error: error.message
    };
  }
}
