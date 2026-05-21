import FormField from "../ui/FormField";

export default function VoiceoverEditor({ voiceoverPlan, onChange, validationError }) {
  function updateField(field, value) {
    onChange({
      ...voiceoverPlan,
      [field]: value
    });
  }

  return (
    <div className="voiceover-editor-card">
      <FormField id="voiceover-duration" label="Thời lượng dự kiến (phút)">
        <input
          className="ui-input"
          id="voiceover-duration"
          min="1"
          type="number"
          value={voiceoverPlan.estimatedDurationMinutes}
          onChange={(event) => updateField("estimatedDurationMinutes", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-tone" label="Giọng điệu">
        <input
          className="ui-input"
          id="voiceover-tone"
          value={voiceoverPlan.tone}
          onChange={(event) => updateField("tone", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-pacing" label="Nhịp đọc">
        <textarea
          className="ui-input ui-textarea"
          id="voiceover-pacing"
          rows="3"
          value={voiceoverPlan.pacing}
          onChange={(event) => updateField("pacing", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-target" label="Đối tượng nghe">
        <textarea
          className="ui-input ui-textarea"
          id="voiceover-target"
          rows="3"
          value={voiceoverPlan.targetAudience}
          onChange={(event) => updateField("targetAudience", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-pronunciation" label="Lưu ý phát âm">
        <textarea
          className="ui-input ui-textarea"
          id="voiceover-pronunciation"
          rows="3"
          value={voiceoverPlan.pronunciationNotes}
          onChange={(event) => updateField("pronunciationNotes", event.target.value)}
        />
      </FormField>

      {validationError ? <p className="lesson-card__error">{validationError}</p> : null}
    </div>
  );
}
