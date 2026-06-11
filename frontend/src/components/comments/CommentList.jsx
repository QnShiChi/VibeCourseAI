import { useState } from "react";
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
  const [expandedThreads, setExpandedThreads] = useState({});

  function toggleThread(threadId) {
    setExpandedThreads((current) => ({
      ...current,
      [threadId]: !current[threadId]
    }));
  }

  return (
    <div className={styles.commentList}>
      {comments.map((thread) => {
        const replyCount = thread.replies?.length ?? 0;
        const isExpanded = Boolean(expandedThreads[thread.comment.id]) || replyComposer?.threadId === thread.comment.id;

        return (
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

            {replyCount ? (
              <button
                className={styles.commentRepliesToggle}
                onClick={() => toggleThread(thread.comment.id)}
                type="button"
              >
                {isExpanded ? "Ẩn phản hồi" : `Xem ${replyCount} phản hồi`}
              </button>
            ) : null}

            {replyCount && isExpanded ? (
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
        );
      })}
    </div>
  );
}
