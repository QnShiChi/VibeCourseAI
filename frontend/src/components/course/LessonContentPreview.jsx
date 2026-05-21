export default function LessonContentPreview({ content }) {
  if (!content) {
    return null;
  }

  return (
    <div className="lesson-generated-stack">
      <section className="surface-card lesson-generated-card">
        <h4>Script</h4>
        <pre className="text-preview text-preview--compact">{content.teachingScript || "Chưa có script."}</pre>
      </section>

      <section className="surface-card lesson-generated-card">
        <h4>Slides</h4>
        <pre className="text-preview text-preview--compact">{content.slideOutlineJson || "Chưa có slide outline."}</pre>
      </section>

      <section className="surface-card lesson-generated-card">
        <h4>Voiceover</h4>
        <pre className="text-preview text-preview--compact">{content.voiceoverPlanJson || "Chưa có voiceover plan."}</pre>
      </section>
    </div>
  );
}
