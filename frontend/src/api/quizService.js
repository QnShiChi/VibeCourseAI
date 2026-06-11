import { axiosClient } from "./axiosClient";

export async function getLessonQuiz(lessonId) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/quiz`);
  return data;
}

export async function getFinalQuiz(courseId) {
  const { data } = await axiosClient.get(`/courses/${courseId}/final-quiz`);
  return data;
}

export async function startQuizAttempt(quizId) {
  const { data } = await axiosClient.post(`/quizzes/${quizId}/attempts`);
  return data;
}

export async function submitQuizAttempt(quizId, attemptId, payload) {
  const { data } = await axiosClient.post(`/quizzes/${quizId}/attempts/${attemptId}/submit`, payload);
  return data;
}

export async function getQuizAttemptHistory(quizId) {
  const { data } = await axiosClient.get(`/quizzes/${quizId}/attempts`);
  return data;
}
