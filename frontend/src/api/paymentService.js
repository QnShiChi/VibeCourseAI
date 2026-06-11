import { axiosClient } from "./axiosClient";

export async function createCheckoutOrders(courseIds) {
  const { data } = await axiosClient.post("/checkout/orders", { courseIds });
  return data;
}

export async function getPaymentOrder(orderId) {
  const { data } = await axiosClient.get(`/payment-orders/${orderId}`);
  return data;
}
