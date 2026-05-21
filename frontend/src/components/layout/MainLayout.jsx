import { Link, NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";
import Button from "../ui/Button";

export default function MainLayout() {
  const { isAuthenticated, user, logout } = useAuth();
  const isAdmin = user?.role === "Admin";
  const getNavLinkClassName = ({ isActive }) => `app-nav__link${isActive ? " app-nav__link--active" : ""}`;

  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="page-container app-header__inner">
          <Link className="app-brand" to="/">
            <span aria-hidden="true" className="app-brand__mark">
              VC
            </span>
            <span>VibeCourseAI</span>
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
                <>
                  <NavLink className={getNavLinkClassName} to="/dashboard">
                    Dashboard
                  </NavLink>
                  <NavLink className={getNavLinkClassName} to="/admin/syllabuses">
                    Đề cương
                  </NavLink>
                  <NavLink className={getNavLinkClassName} to="/admin/generation-jobs">
                    Tiến trình
                  </NavLink>
                </>
              ) : null}
            </div>

            <div className="app-nav__actions">
              {isAuthenticated ? (
                <>
                  <span className="ui-badge">{user?.fullName}</span>
                  <NavLink className={getNavLinkClassName} to="/profile">
                    Hồ sơ
                  </NavLink>
                  <NavLink className={getNavLinkClassName} to="/change-password">
                    Đổi mật khẩu
                  </NavLink>
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

      <main className="page-container app-main">
        <Outlet />
      </main>
    </div>
  );
}
