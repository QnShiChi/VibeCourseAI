import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import AuthShell, { AuthField, MailIcon } from "../components/auth/AuthShell";
import Button from "../components/ui/Button";
import { useAuth } from "../auth/useAuth";
import styles from "../styles/AuthPage.module.css";

const authErrorAlertBoxStyle = {
  margin: 0,
  padding: "12px 16px",
  border: "1px solid var(--auth-error-border)",
  borderRadius: "16px",
  backgroundColor: "var(--auth-error-bg)"
};

const authSuccessAlertBoxStyle = {
  margin: 0,
  padding: "12px 16px",
  border: "1px solid var(--auth-success-border)",
  borderRadius: "16px",
  backgroundColor: "var(--auth-success-bg)"
};

const authErrorAlertTextStyle = {
  color: "var(--auth-error-text)",
  WebkitTextFillColor: "var(--auth-error-text)",
  opacity: 1
};

const authSuccessAlertTextStyle = {
  color: "var(--auth-success-text)",
  WebkitTextFillColor: "var(--auth-success-text)",
  opacity: 1
};

function getVietnameseErrorMessage(error) {
  return error?.response?.data?.message || "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
}

export default function ForgotPasswordPage() {
  const { forgotPassword } = useAuth();
  const [email, setEmail] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");
    setIsSubmitting(true);

    try {
      const response = await forgotPassword({ email });
      setSuccessMessage(response.message || "Hướng dẫn khôi phục mật khẩu đã được gửi đến hòm thư của bạn.");
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthShell
      alternateCta="Quay lại đăng nhập"
      alternateTo="/login"
      description="Nhập email của bạn để nhận hướng dẫn khôi phục mật khẩu."
      footerLabel="Chưa có tài khoản?"
      footerLinkLabel="Đăng ký ngay"
      footerLinkTo="/register"
      heading="Quên mật khẩu"
      showcaseAudience="+2.5k học viên đang trực tuyến"
      showcaseDescription="Hệ thống tối ưu hóa quy trình học tập và giảng dạy bằng trí tuệ nhân tạo thế hệ mới."
      showcaseEyebrow="Khôi phục quyền truy cập"
      showcaseMeta="Không gian học tập đang vận hành theo thời gian thực."
      showcaseTitle="Thiết kế tri thức cùng AI"
    >
      <form className={styles.formStack} onSubmit={handleSubmit}>
        <AuthField
          autoComplete="email"
          icon={<MailIcon />}
          id="email"
          label="Email"
          onChange={(event) => setEmail(event.target.value)}
          placeholder="example@vibecourse.ai"
          type="email"
          value={email}
        />

        {errorMessage ? (
          <div aria-live="polite" className={styles.authErrorAlert} role="alert" style={authErrorAlertBoxStyle}>
            <span className={styles.authErrorAlertText} style={authErrorAlertTextStyle}>
              {errorMessage}
            </span>
          </div>
        ) : null}

        {successMessage ? (
          <div aria-live="polite" className={styles.authErrorAlert} role="alert" style={authSuccessAlertBoxStyle}>
            <span className={styles.authErrorAlertText} style={authSuccessAlertTextStyle}>
              {successMessage}
            </span>
          </div>
        ) : null}

        <Button className={styles.submitButton} disabled={isSubmitting} type="submit">
          {isSubmitting ? "Đang gửi..." : "Gửi yêu cầu"}
          <span aria-hidden="true" className={styles.submitArrow}>
            →
          </span>
        </Button>
        
        <div style={{ textAlign: "center", marginTop: "1rem" }}>
          <Link to="/login" style={{ fontSize: "0.875rem", color: "var(--foreground)", textDecoration: "none" }}>
            Quay lại Đăng nhập
          </Link>
        </div>
      </form>
    </AuthShell>
  );
}
