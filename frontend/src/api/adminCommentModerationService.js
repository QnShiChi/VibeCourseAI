import { axiosClient } from "./axiosClient";

export async function getNegativeComments() {
  const { data } = await axiosClient.get("/admin/comments/negative");
  return data;
}

export async function deleteNegativeComment(commentId, lessonId) {
  await axiosClient.delete(`/admin/comments/${commentId}`, {
    params: { lessonId }
  });
}
