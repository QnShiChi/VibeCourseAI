import { axiosClient } from "./axiosClient";

export async function getGenerationJobs() {
  const { data } = await axiosClient.get("/generation-jobs");
  return data;
}

export async function getGenerationJobDetail(id) {
  const { data } = await axiosClient.get(`/generation-jobs/${id}`);
  return data;
}

export async function cancelGenerationJob(id) {
  const { data } = await axiosClient.post(`/generation-jobs/${id}/cancel`);
  return data;
}
