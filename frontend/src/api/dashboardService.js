import { axiosClient } from "./axiosClient";

export const getDashboardStats = async () => {
  const response = await axiosClient.get("/dashboard/stats");
  return response.data;
};
