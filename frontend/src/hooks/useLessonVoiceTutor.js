import { useEffect, useRef, useState } from "react";
import {
  closeLessonVoiceSession,
  createLessonVoiceSession,
  createLessonVoiceTutorConnection
} from "../api/lessonVoiceTutorService";

function playQueuedAudio(queueRef, isPlayingRef) {
  if (isPlayingRef.current || queueRef.current.length === 0 || typeof Audio === "undefined") {
    return;
  }

  const next = queueRef.current.shift();
  if (!next?.audioUrl) {
    playQueuedAudio(queueRef, isPlayingRef);
    return;
  }

  const audio = new Audio(next.audioUrl);
  isPlayingRef.current = true;
  audio.onended = () => {
    isPlayingRef.current = false;
    playQueuedAudio(queueRef, isPlayingRef);
  };
  audio.onerror = () => {
    isPlayingRef.current = false;
    playQueuedAudio(queueRef, isPlayingRef);
  };
  void audio.play().catch(() => {
    isPlayingRef.current = false;
  });
}

export function useLessonVoiceTutor({ lessonId, enabled, onPauseVideo, onResumeVideo }) {
  const [state, setState] = useState("idle");
  const [session, setSession] = useState(null);
  const [transcriptText, setTranscriptText] = useState("");
  const [answerText, setAnswerText] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const connectionRef = useRef(null);
  const mediaRecorderRef = useRef(null);
  const mediaChunksRef = useRef([]);
  const queueRef = useRef([]);
  const isPlayingRef = useRef(false);

  useEffect(() => {
    if (!enabled || !lessonId) {
      return undefined;
    }

    const connection = createLessonVoiceTutorConnection();
    connectionRef.current = connection;

    connection.on("TranscriptionStarted", () => {
      setState("uploading");
    });

    connection.on("TranscriptionCompleted", (text) => {
      setTranscriptText(text);
      setState("thinking");
    });

    connection.on("AnswerCompleted", (text) => {
      setAnswerText(text);
    });

    connection.on("AnswerAudioSegment", (sequenceIndex, text, audioUrl, durationSeconds) => {
      queueRef.current.push({ sequenceIndex, text, audioUrl, durationSeconds });
      queueRef.current.sort((left, right) => left.sequenceIndex - right.sequenceIndex);
      setState("speaking");
      playQueuedAudio(queueRef, isPlayingRef);
    });

    connection.on("AwaitingFollowUpDecision", () => {
      setState("awaitingDecision");
    });

    connection.start().catch((error) => {
      console.error("Lesson voice tutor connection failed:", error);
      const detail = error instanceof Error ? error.message : String(error ?? "");
      setErrorMessage(detail ? `Không thể kết nối trợ giảng giọng nói. ${detail}` : "Không thể kết nối trợ giảng giọng nói.");
      setState("error");
    });

    return () => {
      if (session?.sessionId) {
        void closeLessonVoiceSession(session.sessionId).catch(() => {});
      }
      void connection.stop();
    };
  }, [enabled, lessonId]);

  async function startRecording(playbackTimeSeconds) {
    try {
      setErrorMessage("");
      const currentSession = session ?? await createLessonVoiceSession(lessonId);
      setSession(currentSession);
      setTranscriptText("");
      setAnswerText("");
      queueRef.current = [];
      onPauseVideo(playbackTimeSeconds);

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream, { mimeType: "audio/webm" });
      mediaRecorderRef.current = recorder;
      mediaChunksRef.current = [];

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          mediaChunksRef.current.push(event.data);
        }
      };

      recorder.onstop = async () => {
        try {
          setState("uploading");
          const blob = new Blob(mediaChunksRef.current, { type: "audio/webm" });
          const buffer = await blob.arrayBuffer();
          const payload = Array.from(new Uint8Array(buffer));
          await connectionRef.current.invoke(
            "CompleteTurn",
            currentSession.sessionId,
            playbackTimeSeconds,
            payload
          );
        } catch (error) {
          console.error("Lesson voice tutor invocation failed:", error);
          const detail = error instanceof Error ? error.message : String(error ?? "");
          setErrorMessage(detail ? `Không thể gửi câu hỏi giọng nói. ${detail}` : "Không thể gửi câu hỏi giọng nói.");
          setState("error");
        } finally {
          stream.getTracks().forEach((track) => track.stop());
        }
      };

      recorder.start();
      setState("recording");
    } catch (error) {
      console.error("Lesson voice tutor recording failed:", error);
      const detail = error instanceof Error ? error.message : String(error ?? "");
      setErrorMessage(detail ? `Không thể bắt đầu ghi âm. ${detail}` : "Không thể bắt đầu ghi âm.");
      setState("error");
    }
  }

  function stopRecording() {
    if (!mediaRecorderRef.current || mediaRecorderRef.current.state === "inactive") {
      return;
    }

    mediaRecorderRef.current.stop();
  }

  function requestFollowUp() {
    setTranscriptText("");
    setAnswerText("");
    queueRef.current = [];
    setState("idle");
  }

  function resumeLearning() {
    setState("idle");
    onResumeVideo();
  }

  return {
    state,
    transcriptText,
    answerText,
    errorMessage,
    startRecording,
    stopRecording,
    requestFollowUp,
    resumeLearning
  };
}
