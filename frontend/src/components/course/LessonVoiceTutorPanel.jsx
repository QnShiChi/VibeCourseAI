export default function LessonVoiceTutorPanel({
  state,
  transcriptText,
  answerText,
  errorMessage,
  onStartRecording,
  onStopRecording,
  onFollowUp,
  onResume
}) {
  return (
    <section className="voice-tutor-panel" aria-label="Tro giang giong noi">
      <p className="voice-tutor-panel__eyebrow">Tro giang giong noi</p>
      <p className="voice-tutor-panel__status">
        {state === "idle" ? "Bạn có thể hỏi ngay trong lúc học." : null}
        {state === "recording" ? "Dang nghe cau hoi..." : null}
        {state === "uploading" ? "Dang gui audio..." : null}
        {state === "thinking" ? "Dang suy luan..." : null}
        {state === "speaking" ? "Dang tra loi bang giong noi..." : null}
        {state === "awaitingDecision" ? "Da tra loi xong." : null}
        {state === "error" ? errorMessage || "Co loi xay ra voi tro giang giong noi." : null}
      </p>

      {transcriptText ? (
        <div className="voice-tutor-panel__block">
          <span className="voice-tutor-panel__label">Bạn hỏi</span>
          <p className="voice-tutor-panel__transcript">{transcriptText}</p>
        </div>
      ) : null}

      {answerText ? (
        <div className="voice-tutor-panel__block">
          <span className="voice-tutor-panel__label">Trợ giảng trả lời</span>
          <p className="voice-tutor-panel__answer">{answerText}</p>
        </div>
      ) : null}

      {state === "idle" ? (
        <button className="voice-tutor-panel__primary" type="button" onClick={onStartRecording}>
          Hoi bang giong noi
        </button>
      ) : null}

      {state === "recording" ? (
        <button className="voice-tutor-panel__primary" type="button" onClick={onStopRecording}>
          Ket thuc ghi am
        </button>
      ) : null}

      {state === "awaitingDecision" ? (
        <div className="voice-tutor-panel__actions">
          <button className="voice-tutor-panel__secondary" type="button" onClick={onFollowUp}>
            Hoi tiep
          </button>
          <button className="voice-tutor-panel__primary" type="button" onClick={onResume}>
            Tiep tuc hoc
          </button>
        </div>
      ) : null}
    </section>
  );
}
