import { Link } from "react-router-dom";
import BrandLockup from "../brand/BrandLockup";

export default function Footer() {
  return (
    <footer className="site-footer" role="contentinfo">
      <div className="page-container site-footer__inner">
        <div className="site-footer__grid">
          <div className="site-footer__brand">
            <BrandLockup className="site-footer__brand-link" />
            <p>Tạo, quản lý và học khóa học video với AI trong một trải nghiệm sáng, rõ và sống động.</p>
          </div>

          <div className="site-footer__column">
            <h2>Khám phá</h2>
            <Link to="/">Trang chủ</Link>
            <Link to="/courses">Khóa học</Link>
          </div>

          <div className="site-footer__column">
            <h2>Nền tảng</h2>
            <span>Tạo khóa học</span>
            <span>AI video workflow</span>
            <span>Lesson narration</span>
          </div>

          <div className="site-footer__column">
            <h2>Liên hệ</h2>
            <span>hello@vibecourse.ai</span>
            <span>Built for modern learning teams</span>
          </div>
        </div>

        <div className="site-footer__meta">© 2026 VibeCourseAI. AI-powered online learning.</div>
      </div>
    </footer>
  );
}
