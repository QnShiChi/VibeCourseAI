import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import { useTheme } from "../theme/ThemeContext";

function getBrowserInfo() {
  if (typeof navigator === "undefined") {
    return {
      browser: "Không xác định",
      platform: "Không xác định",
      language: "Không xác định"
    };
  }

  const userAgent = navigator.userAgent;
  let browser = "Trình duyệt hiện tại";

  if (userAgent.includes("Edg/")) {
    browser = "Microsoft Edge";
  } else if (userAgent.includes("Chrome/")) {
    browser = "Google Chrome";
  } else if (userAgent.includes("Firefox/")) {
    browser = "Mozilla Firefox";
  } else if (userAgent.includes("Safari/") && !userAgent.includes("Chrome/")) {
    browser = "Safari";
  }

  return {
    browser,
    platform: navigator.platform || "Không xác định",
    language: navigator.language || "Không xác định"
  };
}

export default function AdminSettingsPage() {
  const { user } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const browserInfo = getBrowserInfo();

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Cài đặt hệ thống</p>
          <h1>Hồ sơ & bảo mật quản trị</h1>
          <p className="admin-page__description">
            Quản lý nhận diện tài khoản admin, theme giao diện và các tác vụ bảo mật sẵn có trong hệ thống hiện tại.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button as={Link} to="/change-password">Đổi mật khẩu</Button>
          <Button onClick={toggleTheme} variant="ghost">
            Chuyển sang {theme === "dark" ? "light" : "dark"} mode
          </Button>
        </div>
      </div>

      <div className="admin-settings-grid">
        <Card className="admin-profile-hero" variant="shadowed">
          <div className="admin-profile-hero__identity">
            <div className="admin-avatar admin-avatar--large">
              {(user?.fullName || "AD")
                .split(/\s+/)
                .slice(0, 2)
                .map((part) => part[0] ?? "")
                .join("")
                .toUpperCase()}
            </div>
            <div>
              <p className="admin-page__eyebrow">Phiên hiện tại</p>
              <h2>{user?.fullName || "Admin User"}</h2>
              <span>{user?.email || "Không có email"}</span>
            </div>
          </div>

          <div className="admin-profile-hero__meta">
            <div>
              <span>Vai trò</span>
              <strong>{user?.role === "Admin" ? "Administrator" : user?.role || "User"}</strong>
            </div>
            <div>
              <span>Theme hiện tại</span>
              <strong>{theme === "dark" ? "Dark mode" : "Light mode"}</strong>
            </div>
            <div>
              <span>Trạng thái</span>
              <strong>Đã đăng nhập</strong>
            </div>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Môi trường</p>
          <h2>Thiết bị và trình duyệt</h2>
          <div className="admin-detail-list">
            <div><span>Trình duyệt</span><strong>{browserInfo.browser}</strong></div>
            <div><span>Nền tảng</span><strong>{browserInfo.platform}</strong></div>
            <div><span>Ngôn ngữ</span><strong>{browserInfo.language}</strong></div>
          </div>
        </Card>
      </div>

      <div className="admin-settings-grid admin-settings-grid--triple">
        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Tác vụ bảo mật</p>
          <h2>Công cụ khả dụng</h2>
          <div className="admin-action-list">
            <Link className="admin-action-list__item" to="/change-password">
              <strong>Đổi mật khẩu</strong>
              <span>Cập nhật mật khẩu quản trị ngay trong hệ thống.</span>
            </Link>
            <Link className="admin-action-list__item" to="/admin/profile">
              <strong>Xem Admin Profile</strong>
              <span>Kiểm tra hồ sơ quản trị và tiến độ điều hành nội dung.</span>
            </Link>
            <Link className="admin-action-list__item" to="/admin/generation-jobs">
              <strong>Theo dõi generate jobs</strong>
              <span>Giám sát các tác vụ AI đang chạy trong nền.</span>
            </Link>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Ngữ cảnh truy cập</p>
          <h2>Phiên đăng nhập</h2>
          <div className="admin-detail-list">
            <div><span>Quyền hiện tại</span><strong>{user?.role || "User"}</strong></div>
            <div><span>Theme</span><strong>{theme === "dark" ? "Dark" : "Light"}</strong></div>
            <div><span>Ngày truy cập</span><strong>{new Intl.DateTimeFormat("vi-VN", { dateStyle: "full", timeStyle: "short" }).format(new Date())}</strong></div>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Ghi chú</p>
          <h2>Phạm vi dữ liệu</h2>
          <p>
            Khu cài đặt này chỉ hiển thị những thông tin thật hệ thống đang có: tài khoản hiện tại, theme, browser và các luồng quản trị sẵn dùng.
          </p>
        </Card>
      </div>
    </Section>
  );
}
