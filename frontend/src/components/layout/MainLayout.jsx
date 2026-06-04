import { useEffect, useRef, useState } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";
import BrandLockup from "../brand/BrandLockup";
import { useTheme } from "../../theme/ThemeContext";
import Button from "../ui/Button";
import Footer from "./Footer";
import { recordWebActivity } from "../../utils/webActivity";

const AUTH_TRANSITION_IMAGE = "https://lh3.googleusercontent.com/aida-public/AB6AXuAYPOZVwCAf5b5EozQb5JWjUNvm6bVWnV-6O0buWpzmEKq8v1EJgSlpM-_ZjfYQlAyTDNTS2ayXXQRIJHQ25Gk-D1thv7ICBZf4Ox2MIw31gm0soIeIpEVO2UVL9njocBy0Z6mUAB1L2aJL6YvRc9OwARRo9QZd-uf7lGIO7Doda9d_ZBK5e1JHCA3MnR-4DV-eTjWFmbl15FsfdgWZMQkuTWqePJUcN_aZQ_52hDNT4bP8ce12GgC9kyrrcGYRaTOaL_1OGmmdVw";

function NavigationGroup({ items, isActive, label }) {
  const [isOpen, setIsOpen] = useState(false);
  const closeTimeoutRef = useRef(null);
  const getNavLinkClassName = ({ isActive: isChildActive }) =>
    `app-nav__dropdown-link${isChildActive ? " app-nav__dropdown-link--active" : ""}`;

  const clearCloseTimeout = () => {
    if (closeTimeoutRef.current) {
      window.clearTimeout(closeTimeoutRef.current);
      closeTimeoutRef.current = null;
    }
  };

  const openDropdown = () => {
    clearCloseTimeout();
    setIsOpen(true);
  };

  const scheduleCloseDropdown = () => {
    clearCloseTimeout();
    closeTimeoutRef.current = window.setTimeout(() => {
      setIsOpen(false);
      closeTimeoutRef.current = null;
    }, 120);
  };

  useEffect(() => () => clearCloseTimeout(), []);

  return (
    <div
      className="app-nav__group"
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) {
          scheduleCloseDropdown();
        }
      }}
      onMouseEnter={openDropdown}
      onMouseLeave={scheduleCloseDropdown}
    >
      <button
        aria-expanded={isOpen}
        className={`app-nav__group-trigger${isActive ? " app-nav__group-trigger--active" : ""}`}
        onFocus={openDropdown}
        type="button"
      >
        {label}
      </button>

      {isOpen ? (
        <div className="app-nav__dropdown" onFocus={openDropdown} onMouseEnter={openDropdown}>
          {items.map((item) => (
            <NavLink key={item.to} className={getNavLinkClassName} end={item.end} to={item.to}>
              {item.label}
            </NavLink>
          ))}
        </div>
      ) : null}
    </div>
  );
}

export default function MainLayout() {
  const { isAuthenticated, user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const location = useLocation();
  const navigate = useNavigate();
  const { pathname } = location;
  const isAdmin = user?.role === "Admin";
  const getNavLinkClassName = ({ isActive }) => `app-nav__link${isActive ? " app-nav__link--active" : ""}`;
  const isDashboardSection = pathname === "/dashboard" || pathname.startsWith("/admin/");
  const isProfileSection = pathname === "/profile" || pathname === "/change-password";
  const nextThemeLabel = theme === "light" ? "dark" : "light";
  const themeIcon = theme === "light" ? "☾" : "☀";
  const [authTransition, setAuthTransition] = useState(() =>
    location.state?.authIntro ? { isActive: false, mode: "reveal" } : null
  );
  const authIntroTimersRef = useRef({ close: null, open: null });

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

  useEffect(() => {
    if (!isAuthenticated) {
      return undefined;
    }

    const lastRecordedAtRef = { current: Date.now() };

    const flushActivity = () => {
      const now = Date.now();
      const isVisible = document.visibilityState === "visible";
      const isFocused = document.hasFocus();

      if (isVisible && isFocused) {
        const elapsedSeconds = (now - lastRecordedAtRef.current) / 1000;
        recordWebActivity(Math.min(elapsedSeconds, 30));
      }

      lastRecordedAtRef.current = now;
    };

    const syncTimestamp = () => {
      lastRecordedAtRef.current = Date.now();
    };

    const intervalId = window.setInterval(flushActivity, 15000);

    const handleVisibilityChange = () => {
      if (document.visibilityState === "hidden") {
        flushActivity();
        return;
      }

      syncTimestamp();
    };

    const handleWindowBlur = () => {
      flushActivity();
    };

    const handleWindowFocus = () => {
      syncTimestamp();
    };

    const handlePageHide = () => {
      flushActivity();
    };

    document.addEventListener("visibilitychange", handleVisibilityChange);
    window.addEventListener("blur", handleWindowBlur);
    window.addEventListener("focus", handleWindowFocus);
    window.addEventListener("pagehide", handlePageHide);

    return () => {
      flushActivity();
      window.clearInterval(intervalId);
      document.removeEventListener("visibilitychange", handleVisibilityChange);
      window.removeEventListener("blur", handleWindowBlur);
      window.removeEventListener("focus", handleWindowFocus);
      window.removeEventListener("pagehide", handlePageHide);
    };
  }, [isAuthenticated]);

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

      <header className="app-header">
        <div className="page-container app-header__inner">
          <BrandLockup className="app-brand" />

          <nav className="app-nav" aria-label="Điều hướng chính">
            <div className="app-nav__links">
              <NavLink className={getNavLinkClassName} end to="/">
                Trang chủ
              </NavLink>
              <NavLink className={getNavLinkClassName} to="/courses">
                Khóa học
              </NavLink>
              {isAdmin ? (
                <NavigationGroup
                  isActive={isDashboardSection}
                  label="Dashboard"
                  items={[
                    { end: true, label: "Tổng quan", to: "/dashboard" },
                    { label: "Đề cương", to: "/admin/syllabuses" },
                    { label: "Tiến trình", to: "/admin/generation-jobs" }
                  ]}
                />
              ) : null}
            </div>

            <div className="app-nav__actions">
              {isAuthenticated ? (
                <>
                  <span className="ui-badge">{user?.fullName}</span>
                  <NavigationGroup
                    isActive={isProfileSection}
                    label="Hồ sơ"
                    items={[
                      { label: "Thông tin hồ sơ", to: "/profile" },
                      { label: "Đổi mật khẩu", to: "/change-password" }
                    ]}
                  />
                  <button
                    aria-label={`Chuyển sang ${nextThemeLabel} mode`}
                    className="app-theme-toggle"
                    onClick={toggleTheme}
                    title={`Chuyển sang ${nextThemeLabel} mode`}
                    type="button"
                  >
                    <span aria-hidden="true" className="app-theme-toggle__icon">
                      {themeIcon}
                    </span>
                  </button>
                  <Button onClick={handleLogout} variant="ghost">
                    Đăng xuất
                  </Button>
                </>
              ) : (
                <>
                  <NavLink className={getNavLinkClassName} to="/login">
                    Đăng nhập
                  </NavLink>
                  <button
                    aria-label={`Chuyển sang ${nextThemeLabel} mode`}
                    className="app-theme-toggle"
                    onClick={toggleTheme}
                    title={`Chuyển sang ${nextThemeLabel} mode`}
                    type="button"
                  >
                    <span aria-hidden="true" className="app-theme-toggle__icon">
                      {themeIcon}
                    </span>
                  </button>
                  <Button as={Link} to="/register">
                    Đăng ký
                  </Button>
                </>
              )}
            </div>
          </nav>
        </div>
      </header>

      <main className="app-main">
        <div className="page-container app-main__inner">
          <Outlet />
        </div>
      </main>

      <Footer />
    </div>
  );
}
