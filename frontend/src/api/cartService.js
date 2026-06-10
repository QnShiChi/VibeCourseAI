import { axiosClient } from "./axiosClient";

export async function getCart(guestCartToken) {
  const { data } = await axiosClient.get("/cart", {
    params: guestCartToken ? { guestCartToken } : {}
  });
  return data;
}

export async function addCartItem(payload) {
  const { data } = await axiosClient.post("/cart/items", payload);
  return data;
}

export async function removeCartItem(courseId, guestCartToken) {
  const { data } = await axiosClient.delete(`/cart/items/${courseId}`, {
    params: guestCartToken ? { guestCartToken } : {}
  });
  return data;
}

export async function mergeGuestCart(guestCartToken) {
  const { data } = await axiosClient.post("/cart/merge", { guestCartToken });
  return data;
}
