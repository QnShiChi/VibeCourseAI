import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";
import { useTheme } from "../../theme/ThemeContext";

const AUTH_TRANSITION_IMAGE = "https://lh3.googleusercontent.com/aida-public/AB6AXuAYPOZVwCAf5b5EozQb5JWjUNvm6bVWnV-6O0buWpzmEKq8v1EJgSlpM-_ZjfYQlAyTDNTS2ayXXQRIJHQ25Gk-D1thv7ICBZf4Ox2MIw31gm0soIeIpEVO2UVL9njocBy0Z6mUAB1L2aJL6YvRc9OwARRo9QZd-uf7lGIO7Doda9d_ZBK5e1JHCA3MnR-4DV-eTjWFmbl15FsfdgWZMQkuTWqePJUcN_aZQ_52hDNT4bP8ce12GgC9kyrrcGYRaTOaL_1OGmmdVw";

function getInitials(name = "") {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "AD";
  }

  return parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

function AdminNavItem({ to, label, icon, end = false }) {
  return (
    <NavLink className={({ isActive }) => `admin-shell__nav-link${isActive ? " admin-shell__nav-link--active" : ""}`} end={end} to={to}>
      <span aria-hidden="true" className="admin-shell__nav-icon">{icon}</span>
      <span>{label}</span>
    </NavLink>
  );
}

export default function AdminLayout() {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const location = useLocation();
  const navigate = useNavigate();
  const initials = getInitials(user?.fullName);
  const isDark = theme === "dark";
  const [authTransition, setAuthTransition] = useState(() =>
    location.state?.authIntro ? { isActive: false, mode: "reveal" } : null
  );
  const authIntroTimersRef = useRef({ close: null, open: null });
  const placeholderByRoute = location.pathname.startsWith("/admin/users")
    ? "Tìm kiếm người dùng..."
    : location.pathname.startsWith("/admin/courses")
      ? "Tìm kiếm khóa học..."
      : location.pathname.startsWith("/admin/comment-moderation")
        ? "Tìm kiếm moderation queue..."
      : location.pathname.startsWith("/admin/payments")
        ? "Tìm kiếm hóa đơn..."
      : location.pathname.startsWith("/admin/categories")
        ? "Tìm kiếm danh mục..."
        : location.pathname.startsWith("/admin/settings")
        ? "Tìm kiếm cài đặt..."
        : "Tìm kiếm hệ thống...";

  const clearAuthIntroTimers = () => {
    if (authIntroTimersRef.current.open) {
      window.clearTimeout(authIntroTimersRef.current.open);
      authIntroTimersRef.current.open = null;
    }

    if (authIntroTimersRef.current.close) {
      window.clearTimeout(authIntroTimersRef.current.close);
      authIntroTimersRef.current.close = null;
    }
  };

  useEffect(() => {
    if (!location.state?.authIntro) {
      return undefined;
    }

    clearAuthIntroTimers();
    setAuthTransition({ isActive: false, mode: "reveal" });
    navigate(location.pathname, { replace: true, state: null });

    authIntroTimersRef.current.open = window.setTimeout(() => {
      setAuthTransition({ isActive: true, mode: "reveal" });
    }, 20);
    authIntroTimersRef.current.close = window.setTimeout(() => {
      setAuthTransition(null);
      authIntroTimersRef.current.close = null;
      authIntroTimersRef.current.open = null;
    }, 1180);

    return undefined;
  }, [location.pathname, location.state, navigate]);

  useEffect(() => () => clearAuthIntroTimers(), []);

  function handleLogout() {
    clearAuthIntroTimers();
    setAuthTransition({ isActive: false, mode: "conceal" });

    authIntroTimersRef.current.open = window.setTimeout(() => {
      setAuthTransition({ isActive: true, mode: "conceal" });
    }, 20);

    authIntroTimersRef.current.close = window.setTimeout(() => {
      authIntroTimersRef.current.close = null;
      authIntroTimersRef.current.open = null;
      setAuthTransition(null);
      navigate("/login", { replace: true });
      void logout();
    }, 1040);
  }

  return (
    <div className="page-shell" data-theme={theme}>
      {authTransition ? (
        <div
          aria-hidden="true"
          className={`auth-transition auth-transition--${authTransition.mode}${authTransition.isActive ? " auth-transition--active" : ""}`}
          data-testid="auth-transition"
        >
          <div className="auth-transition__panel auth-transition__panel--left">
            <div
              className="auth-transition__image"
              style={{ backgroundImage: `url(${AUTH_TRANSITION_IMAGE})` }}
            />
            <div className="auth-transition__overlay" />
          </div>
          <div className="auth-transition__seam" />
          <div className="auth-transition__panel auth-transition__panel--right">
            <div className="auth-transition__surface" />
          </div>
        </div>
      ) : null}

      <div className="admin-shell">
        <aside className="admin-shell__sidebar">
          <div className="admin-shell__brand">
            <strong>VibeCourseAI</strong>
            <span>Hệ thống Quản trị</span>
          </div>

          <nav className="admin-shell__nav" aria-label="Điều hướng quản trị">
            <AdminNavItem end icon="◫" label="Dashboard" to="/dashboard" />
            <AdminNavItem icon="◍" label="Khu học tập" to="/" />
            <AdminNavItem icon="▤" label="Quản lý khóa học" to="/admin/courses" />
            <AdminNavItem icon="◈" label="Quản lý danh mục" to="/admin/categories" />
            <AdminNavItem icon="◌" label="Quản lý người dùng" to="/admin/users" />
            <AdminNavItem icon="⚑" label="Điều phối bình luận" to="/admin/comment-moderation" />
            <AdminNavItem icon="◨" label="Quản lý hóa đơn" to="/admin/payments" />
            <AdminNavItem icon="◧" label="Báo cáo hệ thống" to="/admin/finance" />
            <AdminNavItem icon="◎" label="Admin Profile" to="/admin/profile" />
            <AdminNavItem icon="✦" label="Cài đặt hệ thống" to="/admin/settings" />
          </nav>

          <div className="admin-shell__sidebar-footer">
            <NavLink className="admin-shell__cta" to="/admin/syllabuses">
              <span aria-hidden="true">+</span>
              <span>Tạo khóa học mới</span>
            </NavLink>

            <div className="admin-shell__support-links">
              <NavLink className="admin-shell__support-link" to="/admin/generation-jobs">Tiến trình generate</NavLink>
              <button className="admin-shell__support-link" onClick={handleLogout} type="button">Đăng xuất</button>
            </div>

            <div className="admin-shell__identity">
              <div className="admin-shell__identity-avatar">{initials}</div>
              <div>
                <strong>{user?.fullName || "Admin User"}</strong>
                <span>{user?.role === "Admin" ? "Administrator" : user?.role || "User"}</span>
              </div>
            </div>
          </div>
        </aside>

        <div className="admin-shell__main">
          <header className="admin-shell__header">
            <label className="admin-shell__search">
              <span aria-hidden="true">⌕</span>
              <input placeholder={placeholderByRoute} type="text" />
            </label>

            <div className="admin-shell__header-actions">
              <button aria-label="Chuyển theme" className="admin-shell__icon-button" onClick={toggleTheme} type="button">
                {isDark ? "☀" : "☾"}
              </button>
              <div className="admin-shell__header-profile">
                <div className="admin-shell__header-avatar">{initials}</div>
                <div>
                  <strong>{user?.fullName || "Admin"}</strong>
                  <span>{user?.role === "Admin" ? "Super Admin" : user?.role || "Người dùng"}</span>
                </div>
              </div>
            </div>
          </header>

          <main className="admin-shell__content">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
}
