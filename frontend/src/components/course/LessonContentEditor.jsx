import { useEffect, useState } from "react";
import { parseSlideOutlineJson, serializeSlideOutline } from "../../utils/slideOutline";
import { parseVoiceoverPlanJson, serializeVoiceoverPlan } from "../../utils/voiceoverPlan";
import Button from "../ui/Button";
import FormField from "../ui/FormField";
import SlideEditor from "./SlideEditor";
import VoiceoverEditor from "./VoiceoverEditor";

export default function LessonContentEditor({ form, onChange, onSave, onCancel }) {
  const [slideError, setSlideError] = useState("");
  const [voiceoverError, setVoiceoverError] = useState("");
  const [slides, setSlides] = useState(() => safeParseSlides(form.slideOutlineJson).slides);
  const [voiceoverPlan, setVoiceoverPlan] = useState(
    () => safeParseVoiceover(form.voiceoverPlanJson).voiceoverPlan
  );

  useEffect(() => {
    const parsedSlides = safeParseSlides(form.slideOutlineJson);
    setSlides(parsedSlides.slides);
    setSlideError(parsedSlides.error);

    const parsedVoiceover = safeParseVoiceover(form.voiceoverPlanJson);
    setVoiceoverPlan(parsedVoiceover.voiceoverPlan);
    setVoiceoverError(parsedVoiceover.error);
  }, [form.slideOutlineJson, form.voiceoverPlanJson]);

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

  function handleVoiceoverChange(nextPlan) {
    setVoiceoverPlan(nextPlan);

    try {
      const serialized = serializeVoiceoverPlan(nextPlan);
      setVoiceoverError("");
      onChange("voiceoverPlanJson", serialized);
    } catch (error) {
      setVoiceoverError(error.message);
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

      <div className="form-field">
        <span className="form-field__label">Voiceover</span>
        <VoiceoverEditor
          voiceoverPlan={voiceoverPlan}
          onChange={handleVoiceoverChange}
          validationError={voiceoverError}
        />
      </div>
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

function safeParseVoiceover(voiceoverPlanJson) {
  try {
    const voiceoverPlan = parseVoiceoverPlanJson(voiceoverPlanJson);
    return {
      voiceoverPlan: voiceoverPlan ?? createDefaultVoiceoverPlan(),
      error: ""
    };
  } catch (error) {
    return {
      voiceoverPlan: createDefaultVoiceoverPlan(),
      error: error.message
    };
  }
}

function createDefaultVoiceoverPlan() {
  return {
    estimatedDurationMinutes: 1,
    tone: "",
    pacing: "",
    targetAudience: "",
    pronunciationNotes: ""
  };
}
