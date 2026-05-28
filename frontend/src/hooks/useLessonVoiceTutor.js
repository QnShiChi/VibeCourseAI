import { useEffect, useRef, useState } from "react";
import {
  closeLessonVoiceSession,
  createLessonVoiceSession,
  createLessonVoiceTutorConnection
} from "../api/lessonVoiceTutorService";

function playQueuedAudio(queueRef, isPlayingRef, onQueueEmpty) {
  if (isPlayingRef.current || queueRef.current.length === 0 || typeof Audio === "undefined") {
    if (!isPlayingRef.current && queueRef.current.length === 0 && typeof onQueueEmpty === "function") {
      onQueueEmpty();
    }
    return;
  }

  const next = queueRef.current.shift();
  if (!next?.audioUrl) {
    playQueuedAudio(queueRef, isPlayingRef, onQueueEmpty);
    return;
  }

  const audio = new Audio(next.audioUrl);
  isPlayingRef.current = true;
  audio.onended = () => {
    isPlayingRef.current = false;
    playQueuedAudio(queueRef, isPlayingRef, onQueueEmpty);
  };
  audio.onerror = () => {
    isPlayingRef.current = false;
    playQueuedAudio(queueRef, isPlayingRef, onQueueEmpty);
  };
  void audio.play().catch(() => {
    isPlayingRef.current = false;
    if (typeof onQueueEmpty === "function" && queueRef.current.length === 0) {
      onQueueEmpty();
    }
  });
}

export function useLessonVoiceTutor({ lessonId, enabled, onPauseVideo, onResumeVideo }) {
  const [state, setState] = useState("idle");
  const [session, setSession] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");
  const connectionRef = useRef(null);
  const mediaRecorderRef = useRef(null);
  const mediaChunksRef = useRef([]);
  const queueRef = useRef([]);
  const isPlayingRef = useRef(false);
  const assistantCompletedRef = useRef(false);
  const receivedAudioUrlsRef = useRef([]);

  async function cleanupAssistantAudio() {
    const urls = receivedAudioUrlsRef.current.filter(Boolean);
    receivedAudioUrlsRef.current = [];

    if (urls.length === 0) {
      return;
    }

    try {
      await connectionRef.current?.invoke("CleanupAssistantAudio", urls);
    } catch (error) {
      console.error("Lesson voice tutor cleanup failed:", error);
    }
  }

  useEffect(() => {
    if (!enabled || !lessonId) {
      return undefined;
    }

    const connection = createLessonVoiceTutorConnection();
    connectionRef.current = connection;

    connection.on("TranscriptionStarted", () => {
      setState("uploading");
    });

    connection.on("TranscriptionCompleted", () => {
      setState("thinking");
    });

    connection.on("AssistantSpeechSegmentReady", (sequenceIndex, audioUrl, durationSeconds) => {
      queueRef.current.push({ sequenceIndex, audioUrl, durationSeconds });
      if (audioUrl) {
        receivedAudioUrlsRef.current.push(audioUrl);
      }
      queueRef.current.sort((left, right) => left.sequenceIndex - right.sequenceIndex);
      setState("speaking");
      playQueuedAudio(queueRef, isPlayingRef, async () => {
        if (assistantCompletedRef.current && queueRef.current.length === 0) {
          await cleanupAssistantAudio();
          setState("awaitingDecision");
        }
      });
    });

    connection.on("AssistantSpeechCompleted", () => {
      assistantCompletedRef.current = true;
      if (!isPlayingRef.current && queueRef.current.length === 0) {
        setState("awaitingDecision");
      }
    });

    connection.on("AwaitingFollowUpDecision", () => {
      if (!isPlayingRef.current && queueRef.current.length === 0) {
        setState("awaitingDecision");
      }
    });

    connection.on("TutorFailed", (message) => {
      setErrorMessage(message || "Tro giang hien chua the tra loi.");
      setState("error");
    });

    connection.start().catch((error) => {
      console.error("Lesson voice tutor connection failed:", error);
      const detail = error instanceof Error ? error.message : String(error ?? "");
      setErrorMessage(detail ? `Không thể kết nối trợ giảng giọng nói. ${detail}` : "Không thể kết nối trợ giảng giọng nói.");
      setState("error");
    });

    return () => {
      void cleanupAssistantAudio();
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
      assistantCompletedRef.current = false;
      queueRef.current = [];
      receivedAudioUrlsRef.current = [];
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

  async function requestFollowUp(playbackTimeSeconds) {
    await cleanupAssistantAudio();
    assistantCompletedRef.current = false;
    queueRef.current = [];
    setState("idle");
    await startRecording(playbackTimeSeconds);
  }

  async function resumeLearning() {
    await cleanupAssistantAudio();
    setState("idle");
    onResumeVideo();
  }

  return {
    state,
    errorMessage,
    startRecording,
    stopRecording,
    requestFollowUp,
    resumeLearning
  };
}
