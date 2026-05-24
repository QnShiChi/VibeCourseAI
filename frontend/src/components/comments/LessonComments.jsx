import { useEffect, useState } from "react";
import {
  addLessonCommentReaction,
  createLessonComment,
  createLessonReply,
  deleteLessonComment,
  getLessonComments,
  hideLessonComment,
  removeLessonCommentReaction,
  unhideLessonComment
} from "../../api/commentService";
import CommentComposer from "./CommentComposer";
import CommentList from "./CommentList";
import CommentSortControl from "./CommentSortControl";
import { applyReactionUpdateToThreads } from "./commentReactionState";
import styles from "./LessonComments.module.css";

const DEFAULT_VISIBLE_COMMENTS = 4;

export default function LessonComments({ isAdmin = false, lessonId }) {
  const [comments, setComments] = useState([]);
  const [sort, setSort] = useState("newest");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmittingReply, setIsSubmittingReply] = useState(false);
  const [replyComposer, setReplyComposer] = useState(null);
  const [showAllComments, setShowAllComments] = useState(false);

  useEffect(() => {
    if (!lessonId) {
      return;
    }

    setShowAllComments(false);
    loadComments({ nextSort: sort, nextPage: 1, append: false });
  }, [lessonId, sort]);

  async function loadComments({ nextSort, nextPage, append }) {
    setIsLoading(!append);
    setErrorMessage("");

    try {
      const data = await getLessonComments(lessonId, { sort: nextSort, page: nextPage, pageSize: 10 });
      setComments((current) => (append ? [...current, ...data.items] : data.items));
      setPage(data.page);
      setHasMore(data.hasMore);
    } catch {
      setErrorMessage("Không thể tải bình luận của video này.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleSubmitComment(content) {
    setIsSubmitting(true);
    setErrorMessage("");
    try {
      await createLessonComment(lessonId, content);
      await loadComments({ nextSort: sort, nextPage: 1, append: false });
    } catch (error) {
      setErrorMessage(error?.response?.data?.message || "Không thể gửi bình luận.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleSubmitReply(threadId, content, replyToUserId) {
    setIsSubmittingReply(true);
    setErrorMessage("");
    try {
      await createLessonReply(lessonId, threadId, {
        content,
        replyToUserId: replyToUserId || null
      });
      setReplyComposer(null);
      await loadComments({ nextSort: sort, nextPage: 1, append: false });
    } catch (error) {
      setErrorMessage(error?.response?.data?.message || "Không thể gửi reply.");
    } finally {
      setIsSubmittingReply(false);
    }
  }

  async function handleSelectReaction(commentId, currentEmoji, nextEmoji) {
    const previousComments = comments;
    setComments((current) => applyReactionUpdateToThreads(current, commentId, nextEmoji));

    try {
      if (currentEmoji && currentEmoji !== nextEmoji) {
        await removeLessonCommentReaction(lessonId, commentId, currentEmoji);
      }

      if (nextEmoji) {
        await addLessonCommentReaction(lessonId, commentId, nextEmoji);
      }
    } catch (error) {
      setComments(previousComments);
      setErrorMessage(error?.response?.data?.message || "Không thể cập nhật reaction.");
      await loadComments({ nextSort: sort, nextPage: 1, append: false });
    }
  }

  async function handleDelete(commentId) {
    try {
      await deleteLessonComment(lessonId, commentId);
      await loadComments({ nextSort: sort, nextPage: 1, append: false });
    } catch (error) {
      setErrorMessage(error?.response?.data?.message || "Không thể xóa bình luận.");
    }
  }

  async function handleHide(commentId) {
    try {
      await hideLessonComment(commentId, lessonId);
      await loadComments({ nextSort: sort, nextPage: 1, append: false });
    } catch (error) {
      setErrorMessage(error?.response?.data?.message || "Không thể ẩn bình luận.");
    }
  }

  async function handleUnhide(commentId) {
    try {
      await unhideLessonComment(commentId, lessonId);
      await loadComments({ nextSort: sort, nextPage: 1, append: false });
    } catch (error) {
      setErrorMessage(error?.response?.data?.message || "Không thể bỏ ẩn bình luận.");
    }
  }

  function handleStartReply(comment) {
    const threadId = comment.replyToUserId ? comments.find((item) => item.replies?.some((reply) => reply.id === comment.id))?.comment?.id || comment.id : comment.id;
    setReplyComposer({
      commentId: comment.id,
      threadId,
      replyToUserId: comment.userId,
      initialValue: `@${comment.authorName} `,
      onCancel: () => setReplyComposer(null)
    });
  }

  const visibleComments = showAllComments ? comments : comments.slice(0, DEFAULT_VISIBLE_COMMENTS);
  const canToggleVisibleComments = comments.length > DEFAULT_VISIBLE_COMMENTS;

  return (
    <section className={styles.commentsSection}>
      <div className={styles.commentsHeader}>
        <div>
          <h2>Bình luận</h2>
          <p>Trao đổi trực tiếp ngay dưới lesson video đang học.</p>
        </div>

        <CommentSortControl onChange={setSort} sort={sort} />
      </div>

      <CommentComposer
        isSubmitting={isSubmitting}
        onSubmit={handleSubmitComment}
        placeholder="Viết bình luận cho bài học này..."
      />

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      {isLoading ? (
        <p>Đang tải bình luận...</p>
      ) : comments.length ? (
        <>
          <CommentList
            comments={visibleComments}
            isAdmin={isAdmin}
            isSubmittingReply={isSubmittingReply}
            onDelete={handleDelete}
            onHide={handleHide}
            onReply={handleSubmitReply}
            onStartReply={handleStartReply}
            onToggleReaction={handleSelectReaction}
            onUnhide={handleUnhide}
            replyComposer={replyComposer}
          />

          {canToggleVisibleComments ? (
            <div className={styles.commentsFooter}>
              <button
                className={styles.commentGhostButton}
                onClick={() => setShowAllComments((current) => !current)}
                type="button"
              >
                {showAllComments ? "Ẩn bớt" : "Xem thêm bình luận"}
              </button>
            </div>
          ) : null}

          {hasMore ? (
            <div className={styles.commentsFooter}>
              <button
                className={styles.commentGhostButton}
                onClick={() => loadComments({ nextSort: sort, nextPage: page + 1, append: true })}
                type="button"
              >
                Load more
              </button>
            </div>
          ) : null}
        </>
      ) : (
        <div className={styles.commentsEmptyState}>
          <strong>Chưa có bình luận nào.</strong>
          <span>Hãy bắt đầu cuộc thảo luận cho bài học này.</span>
        </div>
      )}
    </section>
  );
}
