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

export async function updateCourseCategory(courseId, category) {
  const { data } = await axiosClient.put(`/courses/${courseId}/category`, { category });
  return data;
}

export async function uploadCourseThumbnail(courseId, file) {
  const formData = new FormData();
  formData.append("file", file);

  const { data } = await axiosClient.post(`/courses/${courseId}/thumbnail`, formData, {
    headers: {
      "Content-Type": "multipart/form-data"
    }
  });
  return data;
}
