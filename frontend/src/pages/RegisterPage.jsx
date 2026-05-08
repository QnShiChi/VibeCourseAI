import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

function getVietnameseErrorMessage(error) {
  const message = error?.response?.data?.message;

  if (message?.includes("Email đã tồn tại")) {
    return "Email đã tồn tại.";
  }

  return "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
}

export default function RegisterPage() {
  const navigate = useNavigate();
  const { register } = useAuth();
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: ""
  });
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");

    if (!formData.fullName || !formData.email || !formData.password) {
      setErrorMessage("Vui lòng nhập đầy đủ thông tin.");
      return;
    }

    setIsSubmitting(true);

    try {
      await register(formData);
      navigate("/");
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section style={{ padding: 40, fontFamily: "Georgia, serif", maxWidth: 480 }}>
      <h1>Đăng ký</h1>
      <p>Tạo tài khoản mới để bắt đầu học các khóa học trên hệ thống.</p>

      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 16, marginTop: 24 }}>
        <label htmlFor="fullName" style={{ display: "grid", gap: 8 }}>
          <span>Họ và tên</span>
          <input
            id="fullName"
            type="text"
            placeholder="Nhập họ và tên"
            value={formData.fullName}
            onChange={(event) => setFormData((current) => ({ ...current, fullName: event.target.value }))}
          />
        </label>

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
            placeholder="Tạo mật khẩu"
            value={formData.password}
            onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
          />
        </label>

        {errorMessage ? <p style={{ color: "#ffd6d6" }}>{errorMessage}</p> : null}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang tạo tài khoản..." : "Tạo tài khoản"}
        </button>
      </form>

      <p style={{ marginTop: 24 }}>
        Bạn đã có tài khoản? <Link to="/login">Đăng nhập</Link>
      </p>
    </section>
  );
}
