import { Link } from "react-router-dom";
import CarouselSection from "../components/sections/CarouselSection";
import Button from "../components/ui/Button";
import heroImage from "../assets/images/hero/hero-image-1.png";
import { homeCarouselItems } from "../data/homeCarousel";
import styles from "../styles/HomePage.module.css";

const toolCards = [
  {
    title: "Import syllabus thông minh",
    description:
      "Đưa đề cương vào hệ thống, chuẩn hóa cấu trúc môn học và mở đường cho toàn bộ pipeline tạo course video.",
    detail: "PDF, DOCX hoặc nội dung đề cương đã chuẩn hóa.",
    tone: "warm",
    size: "large"
  },
  {
    title: "AI video workflow",
    description:
      "Đi từ lesson script, slide outline, voiceover đến video-ready flow trong cùng một nhịp vận hành.",
    detail: "Giữ toàn bộ tiến trình generate trong một bề mặt rõ ràng.",
    tone: "mint",
    size: "tall"
  },
  {
    title: "Theo dõi tiến trình rõ ràng",
    description:
      "Giúp admin và learner nhìn được lesson status, publish rhythm và tiến độ học tập theo thời gian thực tế.",
    detail: "Ít phải nhảy giữa các màn hình rời rạc.",
    tone: "lavender",
    size: "compact"
  },
  {
    title: "Vận hành khóa học tập trung",
    description:
      "Quản lý import, generate, publish và course delivery trong một bề mặt điều phối nhất quán cho đội ngũ nội dung.",
    detail: "Từ chuẩn bị học liệu đến lúc learner bắt đầu học.",
    tone: "neutral",
    size: "wide"
  }
];

const stats = [
  { value: "24+", label: "Course pipelines" },
  { value: "180+", label: "Lesson videos" },
  { value: "96%", label: "Workflow visibility" },
  { value: "12x", label: "Learning momentum" }
];

export default function HomePage() {
  return (
    <div className={styles.homepage}>
      <section className={styles.heroSection}>
        <div className={styles.heroBody}>
          <span className={`${styles.heroEyebrow} ui-badge`.trim()}>AI course operating system</span>
          <div className={styles.heroCopy}>
            <h1>Tạo khóa học AI-ready từ syllabus đến video bài giảng.</h1>
            <p>
              VibeCourseAI giúp đội ngũ giáo dục biến đề cương thành course structure, lesson content,
              narration và learner-ready experience trong một flow vận hành sáng sủa hơn.
            </p>
          </div>

          <div className={styles.heroActions}>
            <Button as={Link} to="/register">Bắt đầu miễn phí</Button>
            <Button as={Link} to="/courses" variant="ghost">Xem khóa học</Button>
          </div>

          <div className={styles.heroMeta}>
            <span>Import syllabus có cấu trúc</span>
            <span>Generate lesson và video workflow</span>
            <span>Phát hành course cho learner</span>
          </div>
        </div>

        <div className={styles.heroMediaWrap}>
          <div className={styles.heroMediaAccentTop} aria-hidden="true" />
          <div className={styles.heroMediaAccentBottom} aria-hidden="true" />
          <div className={styles.heroMediaFrame}>
            <div className={styles.heroMediaImageSurface}>
              <img
                alt="Minh họa giao diện dashboard khóa học AI của VibeCourseAI"
                className={styles.heroMediaImage}
                src={heroImage}
              />
            </div>
          </div>
        </div>
      </section>

      <section className={styles.carouselBlock}>
        <CarouselSection items={homeCarouselItems} />
      </section>

      <section className={styles.toolsSection}>
        <div className={styles.sectionHeading}>
          <span className="ui-badge">Workflow layers</span>
          <h2>Công cụ cho toàn bộ pipeline khóa học</h2>
          <p>
            Từ nhập đề cương đến phát hành bài học, homepage mới tập trung vào các khối giá trị cốt lõi
            thay vì trải dài thành nhiều section marketing rời nhau.
          </p>
        </div>

        <div className={styles.toolsGrid}>
          {toolCards.map((card) => (
            <article
              key={card.title}
              className={`${styles.toolCard} ${styles[`toolCard--${card.tone}`]} ${styles[`toolCard--${card.size}`]}`}
            >
              <div className={styles.toolCardIcon} aria-hidden="true">
                <span />
              </div>
              <div className={styles.toolCardCopy}>
                <h3>{card.title}</h3>
                <p>{card.description}</p>
              </div>
              <div className={styles.toolCardFoot}>
                <span>{card.detail}</span>
                <span className={styles.toolCardPulse} aria-hidden="true" />
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.statsBand}>
        <div className={styles.statsBandInner}>
          <div className={styles.statsIntro}>
            <span className="ui-badge">Platform signal</span>
            <h2>Tăng tốc vận hành nội dung học tập</h2>
            <p>
              Thân trang mới nhấn mạnh cách VibeCourseAI gom course structure, generation workflow và
              learner delivery vào cùng một nhịp điều phối dễ nhìn hơn.
            </p>
          </div>

          <div className={styles.statsGrid}>
            {stats.map((item) => (
              <article key={item.label} className={styles.statsCard}>
                <strong>{item.value}</strong>
                <span>{item.label}</span>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className={styles.ctaSection}>
        <div className={styles.ctaPanel}>
          <span className="ui-badge">Ready to launch</span>
          <h2>Sẵn sàng đưa khóa học lên production?</h2>
          <p>
            Bắt đầu với flow AI-powered cho đề cương, lesson và video bài giảng. Bạn có thể thay toàn bộ
            placeholder media bằng ảnh thật ở bước tiếp theo.
          </p>

          <div className={styles.ctaActions}>
            <Button as={Link} to="/register">Tạo tài khoản</Button>
            <Button as={Link} to="/courses" variant="ghost">Xem thư viện khóa học</Button>
          </div>
        </div>
      </section>
    </div>
  );
}
