import { Link } from "react-router-dom";
import Button from "../ui/Button";
import styles from "../../styles/HomePage.module.css";

const pipelineChips = ["Syllabus", "Lesson Script", "Video Ready"];

export default function HeroSection() {
  return (
    <section className={styles.heroSection}>
      <div className={styles.heroContent}>
        <span className="ui-badge">AI + Video Learning</span>
        <h1>Tạo khóa học video với AI trong một nền tảng gọn và sáng.</h1>
        <p>
          Từ syllabus đến lesson video, VibeCourseAI gom toàn bộ flow tạo khóa học, quản trị và delivery vào một trải nghiệm rõ ràng hơn.
        </p>

        <div className={styles.heroActions}>
          <Button as={Link} to="/register">Bắt đầu ngay</Button>
          <Button as={Link} to="/courses" variant="ghost">Xem khóa học</Button>
        </div>

        <div className={styles.heroSignalRow}>
          <span>⚡ Tự động hóa course production</span>
          <span>🎬 Lesson video sẵn sàng cho learner</span>
        </div>
      </div>

      <div className={styles.heroVisual}>
        <div className={styles.heroVisualCard}>
          <span className="ui-badge">AI Course Pipeline</span>
          <h2>Từ đề cương đến video bài giảng</h2>
          <p>Chỉ giữ 3 điểm mạnh nhất để nhìn phát hiểu ngay sản phẩm làm gì.</p>

          <div className={styles.heroVisualGrid}>
            <div className={styles.heroVisualPanel}>
              <strong>Upload syllabus</strong>
              <span>Nhập đề cương để mở đầu pipeline.</span>
            </div>
            <div className={styles.heroVisualPanel}>
              <strong>Batch generation</strong>
              <span>AI tạo content, audio và video theo nhịp rõ ràng.</span>
            </div>
            <div className={styles.heroVisualPanel}>
              <strong>Learner delivery</strong>
              <span>Xuất ra lesson video sẵn sàng cho người học.</span>
            </div>
          </div>
        </div>

        {pipelineChips.map((chip, index) => (
          <span
            key={chip}
            className={styles.heroChip}
            style={{ "--chip-delay": `${index * 0.35}s` }}
          >
            {chip}
          </span>
        ))}
      </div>
    </section>
  );
}
