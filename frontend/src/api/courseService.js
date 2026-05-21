import { axiosClient } from "./axiosClient";

export async function getAdminCourses() {
  const { data } = await axiosClient.get("/courses/admin");
  return data;
}

export async function getPublishedCourses() {
  const { data } = await axiosClient.get("/courses/published");
  return data;
}

export async function publishCourse(courseId) {
  await axiosClient.put(`/courses/${courseId}/publish`);
}

export async function unpublishCourse(courseId) {
  await axiosClient.put(`/courses/${courseId}/unpublish`);
}

export async function getCourseLearnPayload(courseId) {
  const { data } = await axiosClient.get(`/courses/${courseId}/learn`);
  return data;
}
