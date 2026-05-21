import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
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
    <section className="auth-page">
      <Card className="auth-card" variant="shadowed">
        <PageHeader
          eyebrow="Tai khoan"
          title="Đăng nhập"
          description="Vui lòng nhập thông tin tài khoản để truy cập hệ thống học tập."
        />

        <form className="auth-form" onSubmit={handleSubmit}>
          <FormField id="email" label="Email">
            <input
              className="ui-input"
              id="email"
              type="email"
              placeholder="Nhập email của bạn"
              value={formData.email}
              onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
            />
          </FormField>

          <FormField id="password" label="Mật khẩu">
            <input
              className="ui-input"
              id="password"
              type="password"
              placeholder="Nhập mật khẩu"
              value={formData.password}
              onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
            />
          </FormField>

          {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

          <Button disabled={isSubmitting} type="submit">
            {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
          </Button>
        </form>

        <p className="auth-footer">
          Bạn chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link>
        </p>
      </Card>
    </section>
  );
}
