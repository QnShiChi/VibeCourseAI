import { Link, Outlet } from "react-router-dom";

const linkStyle = {
  color: "#f5f1e8",
  textDecoration: "none",
  fontWeight: 600
};

export default function MainLayout() {
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
        <div style={{ fontSize: 28, fontWeight: 700 }}>VibeCourseAI</div>
        <nav style={{ display: "flex", gap: 20 }}>
          <Link to="/dashboard" style={linkStyle}>
            Dashboard
          </Link>
          <Link to="/courses" style={linkStyle}>
            Courses
          </Link>
        </nav>
      </header>
      <main style={{ padding: 32 }}>
        <Outlet />
      </main>
    </div>
  );
}
