import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
import { useAuth } from "../auth/useAuth";

function getVietnameseErrorMessage(error) {
  const responseData = error?.response?.data;
  const message = typeof responseData === "string" ? responseData : responseData?.message;

  if (message?.includes("Email đã tồn tại")) {
    return "Email đã tồn tại.";
  }

  if (error?.response?.status === 400 && message) {
    return message;
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
    <section className="auth-page">
      <Card className="auth-card" variant="shadowed">
        <PageHeader
          eyebrow="Tai khoan"
          title="Đăng ký"
          description="Tạo tài khoản mới để bắt đầu học và theo dõi các khóa học trên hệ thống."
        />

        <form className="auth-form" onSubmit={handleSubmit}>
          <FormField id="fullName" label="Họ và tên">
            <input
              className="ui-input"
              id="fullName"
              type="text"
              placeholder="Nhập họ và tên"
              value={formData.fullName}
              onChange={(event) => setFormData((current) => ({ ...current, fullName: event.target.value }))}
            />
          </FormField>

          <FormField id="register-email" label="Email">
            <input
              className="ui-input"
              id="register-email"
              type="email"
              placeholder="Nhập email của bạn"
              value={formData.email}
              onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
            />
          </FormField>

          <FormField id="register-password" label="Mật khẩu">
            <input
              className="ui-input"
              id="register-password"
              type="password"
              placeholder="Tạo mật khẩu"
              value={formData.password}
              onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
            />
          </FormField>

          {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

          <Button disabled={isSubmitting} type="submit">
            {isSubmitting ? "Đang tạo tài khoản..." : "Tạo tài khoản"}
          </Button>
        </form>

        <p className="auth-footer">
          Bạn đã có tài khoản? <Link to="/login">Đăng nhập</Link>
        </p>
      </Card>
    </section>
  );
}
