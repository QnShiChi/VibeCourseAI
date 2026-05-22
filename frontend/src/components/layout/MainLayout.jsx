import { useEffect, useRef, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";
import vibecourseLogo from "../../assets/icons/vibecourse-logo.png";
import Button from "../ui/Button";
import Footer from "./Footer";

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
  const { pathname } = useLocation();
  const isAdmin = user?.role === "Admin";
  const getNavLinkClassName = ({ isActive }) => `app-nav__link${isActive ? " app-nav__link--active" : ""}`;
  const isDashboardSection = pathname === "/dashboard" || pathname.startsWith("/admin/");
  const isProfileSection = pathname === "/profile" || pathname === "/change-password";

  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="page-container app-header__inner">
          <Link aria-label="VibeCourseAI" className="app-brand" to="/">
            <img alt="VibeCourseAI" className="app-brand__logo" src={vibecourseLogo} />
          </Link>

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
                  <Button onClick={logout} variant="ghost">
                    Đăng xuất
                  </Button>
                </>
              ) : (
                <>
                  <NavLink className={getNavLinkClassName} to="/login">
                    Đăng nhập
                  </NavLink>
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
