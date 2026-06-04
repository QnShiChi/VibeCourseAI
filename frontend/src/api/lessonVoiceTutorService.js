import * as signalR from "@microsoft/signalr";
import { loadAuthSession } from "../auth/authStorage";
import { axiosClient } from "./axiosClient";

export async function createLessonVoiceSession(lessonId) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/voice-sessions`);
  return data;
}

export async function closeLessonVoiceSession(sessionId) {
  await axiosClient.post(`/voice-sessions/${sessionId}/close`);
}

export function createLessonVoiceTutorConnection() {
  const session = loadAuthSession();
  const baseUrl = (import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api").replace(/\/api$/, "");

  return new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/lesson-voice-tutor`, {
      accessTokenFactory: () => session?.accessToken ?? "",
      withCredentials: false
    })
    .withAutomaticReconnect()
    .build();
}
