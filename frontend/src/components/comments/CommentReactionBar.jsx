import { useMemo, useRef, useState } from "react";
import styles from "./LessonComments.module.css";
import { getSelectedReaction, getTopReactionSummary, REACTION_OPTIONS } from "./commentReactionState";

export default function CommentReactionBar({ commentId, onSelectReaction, reactions = [] }) {
  const [isPickerOpen, setIsPickerOpen] = useState(false);
  const closeTimerRef = useRef(null);
  const selectedReaction = useMemo(() => getSelectedReaction(reactions), [reactions]);
  const reactionSummary = useMemo(() => getTopReactionSummary(reactions), [reactions]);

  function openPicker() {
    if (closeTimerRef.current) {
      window.clearTimeout(closeTimerRef.current);
      closeTimerRef.current = null;
    }

    setIsPickerOpen(true);
  }

  function closePickerWithDelay() {
    closeTimerRef.current = window.setTimeout(() => {
      setIsPickerOpen(false);
    }, 90);
  }

  function closePickerNow() {
    if (closeTimerRef.current) {
      window.clearTimeout(closeTimerRef.current);
      closeTimerRef.current = null;
    }

    setIsPickerOpen(false);
  }

  function handlePrimaryClick() {
    if (isPickerOpen) {
      closePickerNow();
      return;
    }

    openPicker();
  }

  function handlePickerSelect(emoji) {
    const currentEmoji = selectedReaction?.emoji ?? null;
    const nextEmoji = currentEmoji === emoji ? null : emoji;
    onSelectReaction(commentId, currentEmoji, nextEmoji);
    closePickerNow();
  }

  return (
    <div className={styles.commentReactionShell}>
      <div
        className={styles.commentReactionPickerAnchor}
        onBlur={closePickerWithDelay}
        onFocus={openPicker}
        onMouseEnter={openPicker}
        onMouseLeave={closePickerWithDelay}
      >
        <button
          aria-expanded={isPickerOpen}
          aria-haspopup="true"
          className={selectedReaction ? styles.commentReactionPrimaryActive : styles.commentReactionPrimary}
          onClick={handlePrimaryClick}
          type="button"
        >
          <span className={styles.commentReactionPrimaryIcon}>{selectedReaction?.emoji ?? "👍"}</span>
          <span>{selectedReaction ? "Cảm xúc" : "Like"}</span>
        </button>

        <div className={`${styles.commentReactionPopup}${isPickerOpen ? ` ${styles.commentReactionPopupOpen}` : ""}`}>
          {REACTION_OPTIONS.map((emoji) => (
            <button
              aria-label={emoji}
              className={styles.commentReactionPopupButton}
              key={`${commentId}-${emoji}`}
              onClick={() => handlePickerSelect(emoji)}
              type="button"
            >
              {emoji}
            </button>
          ))}
        </div>
      </div>

      {reactionSummary.length ? (
        <div aria-label="Tóm tắt cảm xúc" className={styles.commentReactionSummary} role="list">
          {reactionSummary.map((reaction) => (
            <span
              aria-label={`${reaction.emoji} ${reaction.count}`}
              className={styles.commentReactionSummaryPill}
              key={`${commentId}-summary-${reaction.emoji}`}
              role="listitem"
            >
              <span>{reaction.emoji}</span>
              <strong>{reaction.count}</strong>
            </span>
          ))}
        </div>
      ) : null}
    </div>
  );
}
