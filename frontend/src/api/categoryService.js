import { axiosClient } from "./axiosClient";

export async function getVisibleCategories() {
  const { data } = await axiosClient.get("/categories");
  return data;
}

export async function getAdminCategories(params = {}) {
  const { data } = await axiosClient.get("/admin/categories", { params });
  return data;
}

export async function createCategory(payload) {
  const { data } = await axiosClient.post("/admin/categories", payload);
  return data;
}

export async function updateCategory(id, payload) {
  const { data } = await axiosClient.put(`/admin/categories/${id}`, payload);
  return data;
}

export async function deleteCategory(id) {
  await axiosClient.delete(`/admin/categories/${id}`);
}

export async function reorderCategories(categoryIds) {
  await axiosClient.patch("/admin/categories/reorder", { categoryIds });
}
