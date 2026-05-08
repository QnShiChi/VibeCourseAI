import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

function getVietnameseErrorMessage(error) {
  const message = error?.response?.data?.message;

  if (message?.includes("Email hoặc mật khẩu không đúng")) {
    return "Email hoặc mật khẩu không đúng.";
  }

  if (message?.includes("Tài khoản đã bị khóa")) {
    return "Tài khoản đã bị khóa.";
  }

  return "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
}

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");
    setIsSubmitting(true);

    try {
      await login(formData);
      navigate("/");
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section style={{ padding: 40, fontFamily: "Georgia, serif", maxWidth: 480 }}>
      <h1>Đăng nhập</h1>
      <p>Vui lòng nhập thông tin tài khoản để truy cập hệ thống học tập.</p>

      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 16, marginTop: 24 }}>
        <label htmlFor="email" style={{ display: "grid", gap: 8 }}>
          <span>Email</span>
          <input
            id="email"
            type="email"
            placeholder="Nhập email của bạn"
            value={formData.email}
            onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
          />
        </label>

        <label htmlFor="password" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu</span>
          <input
            id="password"
            type="password"
            placeholder="Nhập mật khẩu"
            value={formData.password}
            onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
          />
        </label>

        {errorMessage ? <p style={{ color: "#ffd6d6" }}>{errorMessage}</p> : null}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
        </button>
      </form>

      <p style={{ marginTop: 24 }}>
        Bạn chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link>
      </p>
    </section>
  );
}
