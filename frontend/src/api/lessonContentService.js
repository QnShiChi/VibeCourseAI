import { axiosClient } from "./axiosClient";

export async function generateCourseLessonContent(courseId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/generate-lesson-content`);
  return data;
}

export async function regenerateLessonContent(courseId, lessonId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/lessons/${lessonId}/regenerate-lesson-content`);
  return data;
}

export async function getLessonGeneratedContent(lessonId) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/content`);
  return data;
}

export async function updateLessonGeneratedContent(lessonId, payload) {
  const { data } = await axiosClient.put(`/lessons/${lessonId}/content`, payload);
  return data;
}
