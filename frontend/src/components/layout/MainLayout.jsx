import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";

const linkStyle = {
  color: "#f5f1e8",
  textDecoration: "none",
  fontWeight: 600
};

export default function MainLayout() {
  const { isAuthenticated, user, logout } = useAuth();
  const isAdmin = user?.role === "Admin";

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "radial-gradient(circle at top, #244b5a 0%, #16313b 40%, #0d1f26 100%)",
        color: "#f5f1e8",
        fontFamily: "Georgia, serif"
      }}
    >
      <header
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "20px 32px",
          borderBottom: "1px solid rgba(245, 241, 232, 0.2)"
        }}
      >
        <Link to="/" style={{ ...linkStyle, fontSize: 28 }}>
          VibeCourseAI
        </Link>
        <nav style={{ display: "flex", gap: 20, alignItems: "center" }}>
          <Link to="/" style={linkStyle}>
            Trang chủ
          </Link>
          <Link to="/courses" style={linkStyle}>
            Khóa học
          </Link>
          {isAuthenticated ? (
            <>
              {isAdmin ? (
                <Link to="/dashboard" style={linkStyle}>
                  Dashboard
                </Link>
              ) : null}
              <span>{user?.fullName}</span>
              <Link to="/profile" style={linkStyle}>
                Hồ sơ
              </Link>
              <Link to="/change-password" style={linkStyle}>
                Đổi mật khẩu
              </Link>
              <button
                type="button"
                onClick={logout}
                style={{ background: "transparent", border: "none", color: "#f5f1e8", fontWeight: 600 }}
              >
                Đăng xuất
              </button>
            </>
          ) : (
            <>
              <Link to="/login" style={linkStyle}>
                Đăng nhập
              </Link>
              <Link to="/register" style={linkStyle}>
                Đăng ký
              </Link>
            </>
          )}
        </nav>
      </header>
      <main style={{ padding: 32 }}>
        <Outlet />
      </main>
    </div>
  );
}
