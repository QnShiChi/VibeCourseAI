import { axiosClient } from "./axiosClient";

export async function getLessonComments(lessonId, { sort = "newest", page = 1, pageSize = 10 } = {}) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/comments`, {
    params: { sort, page, pageSize }
  });
  return data;
}

export async function createLessonComment(lessonId, content) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/comments`, { content });
  return data;
}

export async function createLessonReply(lessonId, commentId, payload) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/comments/${commentId}/replies`, payload);
  return data;
}

export async function addLessonCommentReaction(lessonId, commentId, emoji) {
  await axiosClient.post(`/lessons/${lessonId}/comments/${commentId}/reactions`, { emoji });
}

export async function removeLessonCommentReaction(lessonId, commentId, emoji) {
  await axiosClient.delete(`/lessons/${lessonId}/comments/${commentId}/reactions/${encodeURIComponent(emoji)}`);
}

export async function deleteLessonComment(lessonId, commentId) {
  await axiosClient.delete(`/lessons/${lessonId}/comments/${commentId}`);
}

export async function hideLessonComment(commentId, lessonId) {
  await axiosClient.patch(`/admin/comments/${commentId}/hide`, null, {
    params: { lessonId }
  });
}

export async function unhideLessonComment(commentId, lessonId) {
  await axiosClient.patch(`/admin/comments/${commentId}/unhide`, null, {
    params: { lessonId }
  });
}
