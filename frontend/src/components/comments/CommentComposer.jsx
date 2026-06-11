import { useEffect, useState } from "react";
import styles from "./LessonComments.module.css";

export default function CommentComposer({
  avatarLabel,
  autoFocus = false,
  initialValue = "",
  isSubmitting = false,
  onCancel,
  onSubmit,
  placeholder = "Viết bình luận của bạn...",
  submitLabel = "Gửi"
}) {
  const [value, setValue] = useState(initialValue);

  useEffect(() => {
    setValue(initialValue);
  }, [initialValue]);

  async function handleSubmit(event) {
    event.preventDefault();
    const trimmed = value.trim();
    if (!trimmed || isSubmitting) {
      return;
    }

    await onSubmit(trimmed);
    if (!initialValue) {
      setValue("");
    }
  }

  return (
    <form className={styles.commentComposer} onSubmit={handleSubmit}>
      <div className={`${styles.commentComposerRow}${!avatarLabel ? ` ${styles.commentComposerRowFull}` : ""}`}>
        {avatarLabel ? (
          <div className={styles.commentComposerAvatar} aria-hidden="true">
            {avatarLabel}
          </div>
        ) : null}

        <textarea
          autoFocus={autoFocus}
          className={styles.commentComposerInput}
          onChange={(event) => setValue(event.target.value)}
          placeholder={placeholder}
          rows={3}
          value={value}
        />
      </div>

      <div className={styles.commentComposerActions}>
        {onCancel ? (
          <button className={styles.commentGhostButton} onClick={onCancel} type="button">
            Hủy
          </button>
        ) : null}
        <button className={styles.commentPrimaryButton} disabled={!value.trim() || isSubmitting} type="submit">
          {isSubmitting ? "Đang gửi..." : submitLabel}
        </button>
      </div>
    </form>
  );
}
