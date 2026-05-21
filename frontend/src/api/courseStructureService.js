import { axiosClient } from "./axiosClient";

export async function getCourseStructure(courseId) {
  const { data } = await axiosClient.get(`/courses/${courseId}/structure`);
  return data;
}

export async function updateModule(moduleId, payload) {
  const { data } = await axiosClient.put(`/modules/${moduleId}`, payload);
  return data;
}

export async function updateLesson(lessonId, payload) {
  const { data } = await axiosClient.put(`/lessons/${lessonId}`, payload);
  return data;
}
