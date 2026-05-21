import Button from "../ui/Button";
import FormField from "../ui/FormField";

export default function LessonContentEditor({ form, onChange, onSave, onCancel }) {
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
      <FormField id="lesson-generated-slides" label="Slide outline JSON">
        <textarea
          className="ui-input ui-textarea"
          id="lesson-generated-slides"
          rows="8"
          value={form.slideOutlineJson}
          onChange={(event) => onChange("slideOutlineJson", event.target.value)}
        />
      </FormField>
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
