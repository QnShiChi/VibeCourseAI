import { useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import CarouselSection from "../components/sections/CarouselSection";
import Button from "../components/ui/Button";
import heroImage from "../assets/images/hero/hero-image-1.png";
import { homeCarouselItems } from "../data/homeCarousel";
import { useTheme } from "../theme/ThemeContext";
import styles from "../styles/HomePage.module.css";

function SyllabusImportIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <path d="M7 4.75h7l3.25 3.25v10.25A1.75 1.75 0 0 1 15.5 20h-8A1.75 1.75 0 0 1 5.75 18.25V6.5A1.75 1.75 0 0 1 7.5 4.75Z" stroke="currentColor" strokeLinejoin="round" strokeWidth="1.7" />
      <path d="M14 4.75v3.5h3.25" stroke="currentColor" strokeLinejoin="round" strokeWidth="1.7" />
      <path d="M8.5 12h6.5" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
      <path d="m12 9.5 2.5 2.5-2.5 2.5" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" />
    </svg>
  );
}

function VideoWorkflowIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <rect height="11" rx="2.2" stroke="currentColor" strokeWidth="1.7" width="15" x="4.5" y="6.5" />
      <path d="m10 9.75 4.5 2.25L10 14.25v-4.5Z" fill="currentColor" />
      <path d="M7 19.5h10" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
    </svg>
  );
}

function ProgressTrackingIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <path d="M5.75 18.25h12.5" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
      <path d="M8 16.5V11" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
      <path d="M12 16.5V7.75" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
      <path d="M16 16.5v-4.25" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
      <path d="m7.25 9.25 4-3.25 2.75 1.75 3-2.25" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" />
    </svg>
  );
}

function OperationsHubIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <rect height="4.75" rx="1.3" stroke="currentColor" strokeWidth="1.7" width="4.75" x="4.5" y="4.5" />
      <rect height="4.75" rx="1.3" stroke="currentColor" strokeWidth="1.7" width="4.75" x="14.75" y="4.5" />
      <rect height="4.75" rx="1.3" stroke="currentColor" strokeWidth="1.7" width="4.75" x="4.5" y="14.75" />
      <rect height="4.75" rx="1.3" stroke="currentColor" strokeWidth="1.7" width="4.75" x="14.75" y="14.75" />
      <path d="M9.25 6.9h2.75v2.7" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" />
      <path d="M14.75 17.1H12v-2.7" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" />
      <path d="M12 9.6v4.8" stroke="currentColor" strokeLinecap="round" strokeWidth="1.7" />
    </svg>
  );
}

const toolCards = [
  {
    title: "Import syllabus thông minh",
    description:
      "Đưa đề cương vào hệ thống, chuẩn hóa cấu trúc môn học và mở đường cho toàn bộ pipeline tạo course video.",
    detail: "PDF, DOCX hoặc nội dung đề cương đã chuẩn hóa.",
    icon: SyllabusImportIcon,
    tone: "warm",
    size: "large"
  },
  {
    title: "AI video workflow",
    description:
      "Đi từ lesson script, slide outline, voiceover đến video-ready flow trong cùng một nhịp vận hành.",
    detail: "Giữ toàn bộ tiến trình generate trong một bề mặt rõ ràng.",
    icon: VideoWorkflowIcon,
    tone: "mint",
    size: "tall"
  },
  {
    title: "Theo dõi tiến trình rõ ràng",
    description:
      "Giúp admin và learner nhìn được lesson status, publish rhythm và tiến độ học tập theo thời gian thực tế.",
    detail: "Ít phải nhảy giữa các màn hình rời rạc.",
    icon: ProgressTrackingIcon,
    tone: "lavender",
    size: "compact"
  },
  {
    title: "Vận hành khóa học tập trung",
    description:
      "Quản lý import, generate, publish và course delivery trong một bề mặt điều phối nhất quán cho đội ngũ nội dung.",
    detail: "Từ chuẩn bị học liệu đến lúc learner bắt đầu học.",
    icon: OperationsHubIcon,
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

const liveSystemCards = [
  {
    title: "Live orchestration",
    description: "Theo dõi import, generate và publish trong cùng một flow điều phối.",
    tone: "spotlight",
    position: "topLeft"
  },
  {
    title: "Syllabus intake",
    description: "Chuẩn hóa đề cương thành module và lesson có thể vận hành ngay.",
    tone: "warm",
    position: "topRight"
  },
  {
    title: "Video-ready output",
    description: "Đi từ script, voiceover đến lesson video trong nhịp generate rõ ràng.",
    tone: "mint",
    position: "bottomRight"
  },
  {
    title: "Learner feedback",
    description: "Quản lý bình luận, phản hồi và moderation trực tiếp trên từng lesson.",
    tone: "neutral",
    position: "bottomLeft"
  }
];

export function createHomepageParticles(random = Math.random) {
  const columns = 25;
  const rows = 20;
  const cellWidth = 100 / columns;
  const cellHeight = 100 / rows;

  return Array.from({ length: columns * rows }, (_, index) => {
    const column = index % columns;
    const row = Math.floor(index / columns);
    const originX = (column * cellWidth) + (random() * cellWidth);
    const originY = (row * cellHeight) + (random() * cellHeight);
    const radius = 0.55 + (random() * 1.2);
    const opacity = 0.16 + (random() * 0.42);
    const drift = 8 + (random() * 24);
    const twinkle = 0.16 + (random() * 0.36);
    const phase = random() * Math.PI * 2;
    const lifeDuration = 5.8 + (random() * 8.2);
    const fadeOffset = random();
    const swayX = 0.35 + (random() * 1.1);
    const swayY = 0.42 + (random() * 1.25);
    const loopX = 0.22 + (random() * 0.6);
    const loopY = 0.2 + (random() * 0.58);
    const travelX = (random() - 0.5) * 78;
    const travelY = (random() - 0.5) * 78;
    const swirl = 0.6 + (random() * 1.6);

    return {
      id: `p-${index + 1}`,
      originX,
      originY,
      radius,
      opacity,
      drift,
      twinkle,
      phase,
      lifeDuration,
      fadeOffset,
      swayX,
      swayY,
      loopX,
      loopY,
      travelX,
      travelY,
      swirl
    };
  });
}

const homepageParticles = createHomepageParticles();

export default function HomePage() {
  const { theme, toggleTheme } = useTheme();
  const particleCanvasRef = useRef(null);

  useEffect(() => {
    const canvas = particleCanvasRef.current;

    if (!canvas) {
      return undefined;
    }

    let context = null;

    try {
      context = canvas.getContext("2d");
    } catch {
      return undefined;
    }

    if (!context) {
      return undefined;
    }

    let animationFrameId = 0;
    let viewportWidth = window.innerWidth;
    let viewportHeight = window.innerHeight;

    function resizeCanvas() {
      const dpr = Math.min(window.devicePixelRatio || 1, 1.5);
      viewportWidth = window.innerWidth;
      viewportHeight = window.innerHeight;
      canvas.width = Math.round(viewportWidth * dpr);
      canvas.height = Math.round(viewportHeight * dpr);
      canvas.style.width = `${viewportWidth}px`;
      canvas.style.height = `${viewportHeight}px`;
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
    }

    function renderFrame(timestamp) {
      const elapsedSeconds = timestamp / 1000;
      context.clearRect(0, 0, viewportWidth, viewportHeight);
      context.globalCompositeOperation = "source-over";

      for (const particle of homepageParticles) {
        const cycleProgress = ((elapsedSeconds / particle.lifeDuration) + particle.fadeOffset) % 1;
        const fadeEnvelope =
          cycleProgress < 0.18
            ? cycleProgress / 0.18
            : cycleProgress > 0.72
              ? Math.max(0, (1 - cycleProgress) / 0.28)
              : 1;
        const travelProgress = (cycleProgress * 2) - 1;
        const wave = (elapsedSeconds * particle.twinkle) + particle.phase;
        const baseX = (particle.originX / 100) * viewportWidth;
        const baseY = (particle.originY / 100) * viewportHeight;
        const orbitX =
          (Math.sin(wave * particle.swayX) * particle.drift) +
          (Math.cos(wave * particle.loopX * particle.swirl) * (particle.drift * 0.55));
        const orbitY =
          (Math.cos(wave * particle.swayY) * particle.drift) +
          (Math.sin(wave * particle.loopY * particle.swirl) * (particle.drift * 0.52));
        const x = baseX + orbitX + (particle.travelX * travelProgress);
        const y = baseY + orbitY + (particle.travelY * travelProgress);
        const opacity = Math.max(0, particle.opacity * fadeEnvelope);

        if (opacity < 0.015) {
          continue;
        }

        context.fillStyle = `rgba(183, 245, 105, ${Math.min(0.82, opacity)})`;
        context.beginPath();
        context.arc(x, y, particle.radius, 0, Math.PI * 2);
        context.fill();
      }

      animationFrameId = window.requestAnimationFrame(renderFrame);
    }

    resizeCanvas();
    animationFrameId = window.requestAnimationFrame(renderFrame);
    window.addEventListener("resize", resizeCanvas);

    return () => {
      window.removeEventListener("resize", resizeCanvas);
      window.cancelAnimationFrame(animationFrameId);
    };
  }, []);

  return (
    <div className={styles.homepage} data-theme={theme}>
      <canvas
        ref={particleCanvasRef}
        aria-hidden="true"
        className={styles.homepageParticleField}
        data-testid="homepage-particle-canvas"
        style={{ inset: 0, position: "fixed" }}
      />
      <section className={styles.heroSection}>
        <div className={styles.heroBody}>
          <div className={styles.heroTopbar}>
            <span className={`${styles.heroEyebrow} ui-badge`.trim()}>
              <span className={styles.heroEyebrowText}>AI course operating system</span>
            </span>
            <button
              aria-label={`Chuyển sang ${theme === "light" ? "dark" : "light"} mode`}
              className={styles.themeToggle}
              onClick={toggleTheme}
              type="button"
            >
              <span aria-hidden="true" className={styles.themeToggleDot} />
              <span className={styles.themeToggleText}>{theme === "light" ? "Dark mode" : "Light mode"}</span>
            </button>
          </div>
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

          <div className={styles.heroSignals} aria-label="Tín hiệu hệ thống đang hoạt động">
            <article className={styles.heroSignalCard}>
              <span className={styles.heroSignalLabel}>Realtime pipeline</span>
              <strong>Import to publish</strong>
              <p>Luồng generate được theo dõi xuyên suốt trên cùng một bề mặt vận hành.</p>
            </article>
            <article className={styles.heroSignalCard}>
              <span className={styles.heroSignalLabel}>AI readiness</span>
              <strong>Lesson, voice, video</strong>
              <p>Biến đề cương thành đầu ra học tập có thể phát hành mà không tách rời workflow.</p>
            </article>
          </div>
        </div>

        <div className={styles.heroMediaWrap}>
          <div className={styles.heroMediaAccentTop} aria-hidden="true" />
          <div className={styles.heroMediaAccentBottom} aria-hidden="true" />
          <div className={styles.heroLiveCards} aria-label="Live system highlights">
            {liveSystemCards.map((card) => (
              <article
                key={card.title}
                className={`${styles.heroLiveCard} ${styles[`heroLiveCard--${card.tone}`]} ${styles[`heroLiveCard--${card.position}`]}`}
              >
                <span className={styles.heroLiveCardSignal} aria-hidden="true" />
                <div className={styles.heroLiveCardCopy}>
                  <strong>{card.title}</strong>
                  <p>{card.description}</p>
                </div>
              </article>
            ))}
          </div>
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
              <div className={styles.toolCardIcon} aria-hidden="true" data-testid="tool-card-icon">
                <card.icon />
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
