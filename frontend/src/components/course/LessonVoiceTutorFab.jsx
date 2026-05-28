function MicIcon() {
  return (
    <svg aria-hidden="true" className="lesson-voice-fab__icon" viewBox="0 0 24 24">
      <path
        d="M12 15.25a3.25 3.25 0 0 0 3.25-3.25V7a3.25 3.25 0 0 0-6.5 0v5a3.25 3.25 0 0 0 3.25 3.25Z"
        fill="none"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
      <path
        d="M18 11.75a6 6 0 0 1-12 0M12 17.75v3.5M9.25 21.25h5.5"
        fill="none"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  );
}

export default function LessonVoiceTutorFab({
  state,
  errorMessage,
  onStartRecording,
  onStopRecording,
  onRequestFollowUp,
  onResumeLearning
}) {
  const isRecording = state === "recording";
  const isBusy = state === "uploading" || state === "thinking" || state === "speaking";
  const showDecision = state === "awaitingDecision";

  const label = isRecording
    ? "Đang nghe"
    : isBusy
      ? "Đang trả lời"
      : "Hỏi ngay";

  return (
    <div className={`lesson-voice-fab${isRecording ? " lesson-voice-fab--recording" : ""}`}>
      <div className="lesson-voice-fab__dock">
        <span className="lesson-voice-fab__label">{label}</span>
        <button
          type="button"
          className="lesson-voice-fab__button"
          aria-label={label}
          disabled={isBusy}
          onClick={isRecording ? onStopRecording : onStartRecording}
        >
          <MicIcon />
        </button>
      </div>

      {showDecision ? (
        <div className="lesson-voice-fab__actions">
          <button type="button" onClick={onRequestFollowUp}>
            Hỏi tiếp
          </button>
          <button type="button" onClick={onResumeLearning}>
            Tiếp tục học
          </button>
        </div>
      ) : null}

      {errorMessage ? <p className="lesson-voice-fab__error">{errorMessage}</p> : null}
    </div>
  );
}
