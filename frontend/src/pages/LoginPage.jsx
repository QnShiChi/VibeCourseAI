import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import AuthShell, { AuthField, LockIcon, MailIcon } from "../components/auth/AuthShell";
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

const authErrorAlertTextStyle = {
  color: "var(--auth-error-text)",
  WebkitTextFillColor: "var(--auth-error-text)",
  opacity: 1
};

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
  const [keepSignedIn, setKeepSignedIn] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");
    setIsSubmitting(true);

    try {
      const nextSession = await login(formData);
      const nextPath = nextSession?.user?.role === "Admin" ? "/dashboard" : "/";
      navigate(nextPath, {
        replace: true,
        state: {
          authIntro: {
            source: "login"
          }
        }
      });
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthShell
      alternateCta="Dùng thử miễn phí"
      alternateTo="/register"
      description="Đăng nhập để quay lại flow học tập và vận hành khóa học AI-ready của bạn."
      footerLabel="Chưa có tài khoản?"
      footerLinkLabel="Đăng ký ngay"
      footerLinkTo="/register"
      heading="Tài khoản / Đăng nhập"
      showcaseAudience="+2.5k học viên đang trực tuyến"
      showcaseDescription="Hệ thống tối ưu hóa quy trình học tập và giảng dạy bằng trí tuệ nhân tạo thế hệ mới."
      showcaseEyebrow="Chào mừng trở lại"
      showcaseMeta="Không gian học tập đang vận hành theo thời gian thực."
      showcaseTitle="Thiết kế tri thức cùng AI"
    >
      <form className={styles.formStack} onSubmit={handleSubmit}>
        <AuthField
          autoComplete="email"
          icon={<MailIcon />}
          id="email"
          label="Email"
          onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
          placeholder="example@vibecourse.ai"
          type="email"
          value={formData.email}
        />

        <AuthField
          autoComplete="current-password"
          icon={<LockIcon />}
          id="password"
          label="Mật khẩu"
          onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
          placeholder="••••••••"
          trailingAction={
            <Link className={styles.fieldAction} to="/forgot-password">
              Quên mật khẩu?
            </Link>
          }
          type="password"
          value={formData.password}
        />

        <label className={styles.checkboxRow} htmlFor="keep-signed-in">
          <input
            checked={keepSignedIn}
            id="keep-signed-in"
            onChange={(event) => setKeepSignedIn(event.target.checked)}
            type="checkbox"
          />
          <span className={styles.checkboxLabel}>Duy trì đăng nhập</span>
        </label>

        {errorMessage ? (
          <div
            aria-live="polite"
            className={styles.authErrorAlert}
            role="alert"
            style={authErrorAlertBoxStyle}
          >
            <span className={styles.authErrorAlertText} style={authErrorAlertTextStyle}>
              {errorMessage}
            </span>
          </div>
        ) : null}

        <Button className={styles.submitButton} disabled={isSubmitting} type="submit">
          {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
          <span aria-hidden="true" className={styles.submitArrow}>
            →
          </span>
        </Button>
      </form>
    </AuthShell>
  );
}
