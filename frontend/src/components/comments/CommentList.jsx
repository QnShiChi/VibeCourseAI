import CommentItem from "./CommentItem";
import styles from "./LessonComments.module.css";

export default function CommentList({
  comments,
  isAdmin,
  isSubmittingReply,
  onDelete,
  onHide,
  onReply,
  onStartReply,
  onToggleReaction,
  onUnhide,
  replyComposer
}) {
  return (
    <div className={styles.commentList}>
      {comments.map((thread) => (
        <div className={styles.commentThread} key={thread.comment.id}>
          <CommentItem
            comment={thread.comment}
            isAdmin={isAdmin}
            isSubmittingReply={isSubmittingReply && replyComposer?.threadId === thread.comment.id}
            onDelete={onDelete}
            onHide={onHide}
            onReply={(content) => onReply(thread.comment.id, content, replyComposer?.replyToUserId)}
            onStartReply={onStartReply}
            onToggleReaction={onToggleReaction}
            onUnhide={onUnhide}
            replyComposer={replyComposer?.threadId === thread.comment.id ? replyComposer : null}
          />

          {thread.replies?.length ? (
            <div className={styles.commentReplies}>
              {thread.replies.map((reply) => (
                <CommentItem
                  comment={reply}
                  isAdmin={isAdmin}
                  isReply
                  key={reply.id}
                  onDelete={onDelete}
                  onHide={onHide}
                  onReply={(content) => onReply(thread.comment.id, content, reply.userId)}
                  onStartReply={onStartReply}
                  onToggleReaction={onToggleReaction}
                  onUnhide={onUnhide}
                />
              ))}
            </div>
          ) : null}
        </div>
      ))}
    </div>
  );
}
