import { axiosClient } from "./axiosClient";

export async function getModerationComments({ sentiment, authorName } = {}) {
  const { data } = await axiosClient.get("/admin/comments", {
    params: {
      sentiment,
      authorName: authorName?.trim() || undefined
    }
  });
  return data;
}

export async function deleteModerationComment(commentId, lessonId) {
  await axiosClient.delete(`/admin/comments/${commentId}`, {
    params: { lessonId }
  });
}

export async function pinComment(commentId) {
  await axiosClient.patch(`/admin/comments/${commentId}/pin`);
}

export async function getPositiveCourseHighlights() {
  const { data } = await axiosClient.get("/admin/comments/positive-courses");
  return data;
}
