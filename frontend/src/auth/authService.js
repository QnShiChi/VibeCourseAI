import { axiosClient } from "../api/axiosClient";

export async function login(payload) {
  const { data } = await axiosClient.post("/auth/login", payload);
  return data;
}

export async function register(payload) {
  const { data } = await axiosClient.post("/auth/register", payload);
  return data;
}

export async function refresh(refreshToken) {
  const { data } = await axiosClient.post("/auth/refresh", { refreshToken });
  return data;
}

export async function logout(refreshToken) {
  await axiosClient.post("/auth/logout", { refreshToken });
}

export async function getCurrentUser() {
  const { data } = await axiosClient.get("/auth/me");
  return data;
}

export async function changePassword(payload) {
  await axiosClient.post("/auth/change-password", payload);
}
