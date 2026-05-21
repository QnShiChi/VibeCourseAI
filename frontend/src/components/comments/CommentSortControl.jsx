import styles from "./LessonComments.module.css";

export default function CommentSortControl({ onChange, sort }) {
  return (
    <div aria-label="Sắp xếp bình luận" className={styles.commentSortControl} role="group">
      <button
        aria-pressed={sort === "newest"}
        className={sort === "newest" ? styles.commentSortButtonActive : styles.commentSortButton}
        onClick={() => onChange("newest")}
        type="button"
      >
        Mới nhất
      </button>
      <button
        aria-pressed={sort === "featured"}
        className={sort === "featured" ? styles.commentSortButtonActive : styles.commentSortButton}
        onClick={() => onChange("featured")}
        type="button"
      >
        Nổi bật
      </button>
    </div>
  );
}
