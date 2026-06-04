import { useState, useEffect } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import AuthShell, { AuthField, LockIcon } from "../components/auth/AuthShell";
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
  border: "1px solid #c3e6cb",
  borderRadius: "16px",
  backgroundColor: "#d4edda"
};

const authErrorAlertTextStyle = {
  color: "var(--auth-error-text)",
  WebkitTextFillColor: "var(--auth-error-text)",
  opacity: 1
};

const authSuccessAlertTextStyle = {
  color: "#155724",
  WebkitTextFillColor: "#155724",
  opacity: 1
};

function getVietnameseErrorMessage(error) {
  return error?.response?.data?.message || "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
}

export default function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { resetPassword } = useAuth();
  
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!token || !email) {
      setErrorMessage("Link khôi phục không hợp lệ hoặc đã hết hạn.");
    }
  }, [token, email]);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");

    if (newPassword !== confirmPassword) {
      setErrorMessage("Mật khẩu xác nhận không khớp.");
      return;
    }

    if (newPassword.length < 6) {
      setErrorMessage("Mật khẩu phải có ít nhất 6 ký tự.");
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await resetPassword({ email, token, newPassword });
      setSuccessMessage(response.message || "Mật khẩu đã được đặt lại thành công.");
      setTimeout(() => {
        navigate("/login", { replace: true });
      }, 3000);
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthShell
      alternateCta="Về trang chủ"
      alternateTo="/"
      description="Nhập mật khẩu mới của bạn bên dưới."
      footerLabel="Chưa có tài khoản?"
      footerLinkLabel="Đăng ký ngay"
      footerLinkTo="/register"
      heading="Đặt lại mật khẩu"
      showcaseAudience="+2.5k học viên đang trực tuyến"
      showcaseDescription="Hệ thống tối ưu hóa quy trình học tập và giảng dạy bằng trí tuệ nhân tạo thế hệ mới."
      showcaseEyebrow="Khôi phục quyền truy cập"
      showcaseMeta="Không gian học tập đang vận hành theo thời gian thực."
      showcaseTitle="Thiết kế tri thức cùng AI"
    >
      <form className={styles.formStack} onSubmit={handleSubmit}>
        <AuthField
          autoComplete="new-password"
          icon={<LockIcon />}
          id="newPassword"
          label="Mật khẩu mới"
          onChange={(event) => setNewPassword(event.target.value)}
          placeholder="••••••••"
          type="password"
          value={newPassword}
          disabled={!token || !email || successMessage !== ""}
        />

        <AuthField
          autoComplete="new-password"
          icon={<LockIcon />}
          id="confirmPassword"
          label="Xác nhận mật khẩu"
          onChange={(event) => setConfirmPassword(event.target.value)}
          placeholder="••••••••"
          type="password"
          value={confirmPassword}
          disabled={!token || !email || successMessage !== ""}
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

        <Button 
          className={styles.submitButton} 
          disabled={isSubmitting || !token || !email || successMessage !== ""} 
          type="submit"
        >
          {isSubmitting ? "Đang xử lý..." : "Đặt lại mật khẩu"}
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
