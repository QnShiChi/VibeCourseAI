import { useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
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
    <section className="auth-page">
      <Card className="auth-card" variant="shadowed">
        <PageHeader
          eyebrow="Bao mat"
          title="Đổi mật khẩu"
          description="Cập nhật mật khẩu mới để bảo vệ tài khoản và tiếp tục sử dụng hệ thống an toàn hơn."
        />

        <form className="auth-form" onSubmit={handleSubmit}>
          <FormField id="currentPassword" label="Mật khẩu hiện tại">
            <input
              className="ui-input"
              id="currentPassword"
              type="password"
              value={formData.currentPassword}
              onChange={(event) => setFormData((current) => ({ ...current, currentPassword: event.target.value }))}
            />
          </FormField>

          <FormField id="newPassword" label="Mật khẩu mới">
            <input
              className="ui-input"
              id="newPassword"
              type="password"
              value={formData.newPassword}
              onChange={(event) => setFormData((current) => ({ ...current, newPassword: event.target.value }))}
            />
          </FormField>

          {message ? <p className="ui-alert ui-alert--success">{message}</p> : null}
          {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

          <Button type="submit">Cập nhật mật khẩu</Button>
        </form>
      </Card>
    </section>
  );
}
