import { axiosClient } from "./axiosClient";

export async function importSyllabus(formData) {
  const { data } = await axiosClient.post("/syllabuses/import", formData, {
    headers: { "Content-Type": "multipart/form-data" }
  });
  return data;
}

export async function getSyllabuses() {
  const { data } = await axiosClient.get("/syllabuses");
  return data;
}

export async function getSyllabusDetail(id) {
  const { data } = await axiosClient.get(`/syllabuses/${id}`);
  return data;
}

export async function generateSyllabusCourse(id) {
  const { data } = await axiosClient.post(`/syllabuses/${id}/generate`);
  return data;
}

export async function deleteSyllabus(id) {
  await axiosClient.delete(`/syllabuses/${id}`);
}
