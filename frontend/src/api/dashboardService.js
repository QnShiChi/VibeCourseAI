import { axiosClient } from "./axiosClient";

export const getDashboardStats = async () => {
  const response = await axiosClient.get("/dashboard/stats");
  return response.data;
};

export const getDashboardPaymentOverview = async () => {
  const response = await axiosClient.get("/dashboard/payment-overview");
  return response.data;
};
