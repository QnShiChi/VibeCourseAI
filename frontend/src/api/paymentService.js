import { axiosClient } from "./axiosClient";

export async function createCheckoutOrders(courseIds) {
  const { data } = await axiosClient.post("/checkout/orders", { courseIds });
  return data;
}

export async function getPaymentOrder(orderId) {
  const { data } = await axiosClient.get(`/payment-orders/${orderId}`);
  return data;
}

export async function cancelPaymentOrder(orderId) {
  const { data } = await axiosClient.post(`/payment-orders/${orderId}/cancel`);
  return data;
}

export async function getPurchaseHistory() {
  const { data } = await axiosClient.get("/payment-orders");
  return data;
}

export async function getAdminPaymentOrders(params = {}) {
  const { data } = await axiosClient.get("/admin/payment-orders", { params });
  return data;
}

export async function getAdminPaymentOrderDetail(paymentOrderId) {
  const { data } = await axiosClient.get(`/admin/payment-orders/${paymentOrderId}`);
  return data;
}
