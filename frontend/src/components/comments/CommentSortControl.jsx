import styles from "./LessonComments.module.css";

export default function CommentSortControl({ onChange, sort }) {
  return (
    <label className={styles.commentSortControl}>
      <span className={styles.commentSortLabel}>Sắp xếp:</span>
      <select
        aria-label="Sắp xếp bình luận"
        className={styles.commentSortSelect}
        onChange={(event) => onChange(event.target.value)}
        value={sort}
      >
        <option value="newest">Mới nhất</option>
        <option value="oldest">Cũ nhất</option>
        <option value="featured">Nổi bật</option>
      </select>
    </label>
  );
}
