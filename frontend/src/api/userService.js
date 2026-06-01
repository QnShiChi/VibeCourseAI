import { axiosClient } from "./axiosClient";

export async function getUsers() {
  const { data } = await axiosClient.get("/users");
  return data;
}

export async function updateUserActive(userId, isActive) {
  await axiosClient.patch(`/users/${userId}/active`, { isActive });
}
