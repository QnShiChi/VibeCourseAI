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

export async function generateCourseLessonAudio(courseId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/generate-lesson-audio`);
  return data;
}

export async function generateLessonAudio(courseId, lessonId) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/generate-audio`, null, {
    params: { courseId }
  });
  return data;
}

export async function regenerateLessonAudio(courseId, lessonId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/lessons/${lessonId}/regenerate-lesson-audio`);
  return data;
}

export async function getLessonAudio(lessonId) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/audio`);
  return data;
}

export async function generateCourseLessonVideo(courseId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/generate-lesson-video`);
  return data;
}

export async function generateLessonVideo(courseId, lessonId) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/generate-video`, null, {
    params: { courseId }
  });
  return data;
}

export async function regenerateLessonVideo(courseId, lessonId) {
  const { data } = await axiosClient.post(`/courses/${courseId}/lessons/${lessonId}/regenerate-lesson-video`);
  return data;
}

export async function getLessonVideo(lessonId) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/video`);
  return data;
}
