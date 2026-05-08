import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const { changePassword } = useAuth();
  const [formData, setFormData] = useState({ currentPassword: "", newPassword: "" });
  const [message, setMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  async function handleSubmit(event) {
    event.preventDefault();
    setMessage("");
    setErrorMessage("");

    try {
      await changePassword(formData);
      setMessage("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
      navigate("/login");
    } catch {
      setErrorMessage("Không thể đổi mật khẩu. Vui lòng kiểm tra lại thông tin.");
    }
  }

  return (
    <section style={{ maxWidth: 480 }}>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Đổi mật khẩu</h1>
      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 16 }}>
        <label htmlFor="currentPassword" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu hiện tại</span>
          <input
            id="currentPassword"
            type="password"
            value={formData.currentPassword}
            onChange={(event) => setFormData((current) => ({ ...current, currentPassword: event.target.value }))}
          />
        </label>

        <label htmlFor="newPassword" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu mới</span>
          <input
            id="newPassword"
            type="password"
            value={formData.newPassword}
            onChange={(event) => setFormData((current) => ({ ...current, newPassword: event.target.value }))}
          />
        </label>

        {message ? <p>{message}</p> : null}
        {errorMessage ? <p style={{ color: "#ffd6d6" }}>{errorMessage}</p> : null}

        <button type="submit">Cập nhật mật khẩu</button>
      </form>
    </section>
  );
}
