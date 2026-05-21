import { Link } from "react-router-dom";
import CarouselSection from "../components/sections/CarouselSection";
import FeatureSection from "../components/sections/FeatureSection";
import StatsSection from "../components/sections/StatsSection";
import Button from "../components/ui/Button";
import { homeCarouselItems } from "../data/homeCarousel";
import styles from "../styles/HomePage.module.css";

const featureSections = [
  {
    title: "Tạo khóa học từ syllabus trong 1 nút",
    description:
      "Upload đề cương, cấu trúc lại course skeleton và đẩy toàn bộ pipeline sang một flow gọn để đội ngũ giáo dục không còn bị mắc ở bước chuẩn bị thủ công.",
    bullets: [
      "Import syllabus và content seed có cấu trúc",
      "Biến đề cương thành module, lesson và flow sản xuất rõ ràng",
      "Giảm độ rời rạc giữa học thuật và vận hành nội dung"
    ],
    cta: { as: Link, to: "/admin/syllabuses", label: "Bắt đầu tải đề cương" },
    tone: "saffron",
    visual: (
      <div className={styles.visualStack}>
        <div className={styles.visualMiniCard}>
          <strong>Upload zone</strong>
          <span>PDF, DOCX hoặc cấu trúc đề cương chuẩn hóa</span>
        </div>
        <div className={styles.visualMiniCard}>
          <strong>Course skeleton</strong>
          <span>Module, lesson và content seed được sinh ngay trong cùng giao diện.</span>
        </div>
        <div className={styles.visualMiniCard}>
          <strong>Ready for AI generation</strong>
          <span>Không còn phải copy-paste nhiều công cụ rời rạc.</span>
        </div>
      </div>
    )
  },
  {
    title: "AI tự động tạo Video + Narration",
    description:
      "Kết nối script, slides, voiceover, audio và video theo đúng nhịp bài giảng. VibeCourseAI giữ toàn bộ workflow này trong một trục điều phối rõ ràng.",
    bullets: [
      "Sinh lesson script và slide narrative đồng bộ",
      "TTS và render video bám theo slide thay vì đọc trôi tự do",
      "Sẵn sàng cho learner video delivery"
    ],
    cta: { as: Link, to: "/courses", label: "Xem demo", variant: "ghost" },
    tone: "highlight",
    layout: "content-right",
    visual: (
      <div className={styles.visualProcess}>
        <span className={styles.visualProcessHighlight}>Tiết kiệm 90% thời gian sản xuất video</span>
        <div className={styles.visualProcessStep}><strong>1. Script</strong><span>Lesson content được AI tạo có cấu trúc.</span></div>
        <div className={styles.visualProcessStep}><strong>2. Narration</strong><span>Voiceover bám sát slide và lesson cadence.</span></div>
        <div className={styles.visualProcessStep}><strong>3. Video</strong><span>Render ảnh slide + audio thành video learner-ready.</span></div>
      </div>
    )
  },
  {
    title: "Trải nghiệm học tập hoàn hảo",
    description:
      "Không chỉ dừng ở generate nội dung. Learner nhận được một hành trình học tập sáng sủa hơn, có nhịp, có tiến trình và sẵn sàng để hoàn thành khóa học.",
    bullets: [
      "Video lesson và progress tracking rõ ràng",
      "Course navigation gọn, bám đúng module và lesson",
      "Sẵn sàng mở rộng sang bookmarks, notes và learning habits"
    ],
    cta: { as: Link, to: "/courses", label: "Đồng ý làm học viên", variant: "ghost" },
    tone: "saffron",
    visual: (
      <div className={styles.visualLearner}>
        <div className={styles.visualLearnerShell}>
          <div className={styles.visualLearnerRow}><strong>Current lesson</strong><span>Video playback with a clean stage</span></div>
          <div className={styles.visualLearnerRow}><strong>Progress rhythm</strong><span>Module-by-module completion signals</span></div>
          <div className={styles.visualLearnerRow}><strong>Calm focus</strong><span>Less clutter, more learning momentum</span></div>
        </div>
      </div>
    )
  }
];

const stats = [
  { label: "Khóa học AI-ready", value: 24, suffix: "+", description: "Pipeline course có thể được dựng thành video lesson.", tone: "mint" },
  { label: "Video generated", value: 180, suffix: "+", description: "Lesson video được render từ slide và narration.", tone: "saffron" },
  { label: "Lesson workflows", value: 96, suffix: "%", description: "Tác vụ lesson có thể được theo dõi trong cùng một hệ thống.", tone: "lavender" },
  { label: "Learner momentum", value: 12, suffix: "x", description: "Cảm giác học tập rõ hơn so với flow nội dung rời rạc.", tone: "pink" }
];

export default function HomePage() {
  return (
    <section className={styles.homepage}>
      <CarouselSection className={styles.homeSectionBlock} items={homeCarouselItems} />

      {featureSections.map((section) => (
        <FeatureSection
          key={section.title}
          bullets={section.bullets}
          className={styles.homeSectionBlock}
          cta={section.cta}
          description={section.description}
          layout={section.layout}
          title={section.title}
          tone={section.tone}
          visual={section.visual}
        />
      ))}

      <StatsSection className={styles.homeSectionBlock} items={stats} />

      <section className={`${styles.bottomCta} ${styles.homeSectionBlock}`.trim()}>
        <span className="ui-badge">Launch your next course</span>
        <h2>Sẵn sàng tạo khóa học AI-powered?</h2>
        <p>Tạo ra một workflow hiện đại cho online learning: sáng hơn, nhanh hơn và có video delivery thật sự usable.</p>

        <div className={styles.bottomCtaActions}>
          <Button as={Link} to="/register">Đăng ký miễn phí</Button>
          <Button as="a" href="mailto:hello@vibecourse.ai" variant="ghost">Liên hệ</Button>
        </div>
      </section>
    </section>
  );
}
