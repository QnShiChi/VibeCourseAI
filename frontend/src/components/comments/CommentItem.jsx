import CommentComposer from "./CommentComposer";
import CommentReactionBar from "./CommentReactionBar";
import styles from "./LessonComments.module.css";

export default function CommentItem({
  comment,
  isAdmin,
  isReply = false,
  isSubmittingReply = false,
  onDelete,
  onHide,
  onReply,
  onStartReply,
  onToggleReaction,
  onUnhide,
  replyComposer
}) {
  return (
    <article className={`${styles.commentItem}${isReply ? ` ${styles.commentItemReply}` : ""}`}>
      <div className={styles.commentAvatar} aria-hidden="true">
        {(comment.authorName || "U").slice(0, 1).toUpperCase()}
      </div>

      <div className={`${styles.commentBody}${isReply ? ` ${styles.commentBodyReply}` : ""}`}>
        <div className={styles.commentMeta}>
          <strong>{comment.authorName}</strong>
          {isAdmin && comment.sentiment ? (
            <span
              className={styles.sentimentBadge}
              title={`Phân tích bởi PhoBERT: ${comment.sentiment}`}
            >
              {comment.sentiment === "positive" ? "😊 Tích cực" : comment.sentiment === "negative" ? "😡 Tiêu cực" : "😐 Bình thường"}
            </span>
          ) : null}
          {comment.authorRole ? <span className={styles.commentRoleBadge}>{comment.authorRole}</span> : null}
          <span>{new Date(comment.createdAt).toLocaleString("vi-VN")}</span>
        </div>

        <p className={styles.commentContent}>
          {comment.replyToUserName && !comment.isDeleted && !comment.isHidden ? (
            <strong>@{comment.replyToUserName} </strong>
          ) : null}
          {comment.content}
        </p>

        <div className={styles.commentActionRow}>
          <div className={styles.commentActions}>
            {!comment.isDeleted ? (
              <button className={styles.commentActionButton} onClick={() => onStartReply(comment)} type="button">
                Reply
              </button>
            ) : null}
            {comment.canDelete ? (
              <button className={styles.commentActionButton} onClick={() => onDelete(comment.id)} type="button">
                Xóa
              </button>
            ) : null}
            {isAdmin && !comment.isDeleted ? (
              comment.isHidden ? (
                <button className={styles.commentActionButton} onClick={() => onUnhide(comment.id)} type="button">
                  Bỏ ẩn bình luận
                </button>
              ) : (
                <button className={styles.commentActionButton} onClick={() => onHide(comment.id)} type="button">
                  Ẩn bình luận
                </button>
              )
            ) : null}
          </div>

          <CommentReactionBar
            commentId={comment.id}
            onSelectReaction={onToggleReaction}
            reactions={comment.reactions}
          />
        </div>

        {replyComposer ? (
          <CommentComposer
            autoFocus
            initialValue={replyComposer.initialValue}
            isSubmitting={isSubmittingReply}
            onCancel={replyComposer.onCancel}
            onSubmit={onReply}
            placeholder="Trả lời bình luận này..."
            submitLabel="Gửi reply"
          />
        ) : null}
      </div>
    </article>
  );
}
