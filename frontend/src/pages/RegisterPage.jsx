import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import AuthShell, { AuthField, LockIcon, MailIcon, PersonIcon } from "../components/auth/AuthShell";
import Button from "../components/ui/Button";
import { useAuth } from "../auth/useAuth";
import styles from "../styles/AuthPage.module.css";

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
  const location = useLocation();
  const { register } = useAuth();
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: ""
  });
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [acceptTerms, setAcceptTerms] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");

    if (!formData.fullName || !formData.email || !formData.password) {
      setErrorMessage("Vui lòng nhập đầy đủ thông tin.");
      return;
    }

    setIsSubmitting(true);

    try {
      const nextSession = await register(formData);
      const nextPath = location.state?.from?.pathname ?? (nextSession?.user?.role === "Admin" ? "/dashboard" : "/");
      navigate(nextPath, {
        replace: true,
        state: {
          authIntro: {
            source: "register"
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
      alternateCta="Đăng nhập"
      alternateTo="/login"
      description="Gia nhập cộng đồng giáo dục AI lớn nhất Việt Nam với flow tạo khóa học rõ ràng hơn."
      footerLabel="Đã có tài khoản?"
      footerLinkLabel="Đăng nhập"
      footerLinkTo="/login"
      heading="Tạo tài khoản mới"
      showcaseAudience="1,200+ nhà sáng tạo"
      showcaseDescription="Hệ thống tự động hóa quy trình sản xuất bài giảng, giúp bạn tiết kiệm thời gian thiết kế giáo án và tập trung vào tri thức."
      showcaseEyebrow="Bắt đầu ngay"
      showcaseMeta="Đã tham gia cùng hệ sinh thái học tập của VibeCourseAI."
      showcaseTitle="Khởi tạo nội dung khóa học bằng Trí tuệ nhân tạo"
    >
      <form className={styles.formStack} onSubmit={handleSubmit}>
        <AuthField
          autoComplete="name"
          icon={<PersonIcon />}
          id="fullName"
          label="Họ và tên"
          onChange={(event) => setFormData((current) => ({ ...current, fullName: event.target.value }))}
          placeholder="Nguyễn Văn A"
          type="text"
          value={formData.fullName}
        />

        <AuthField
          autoComplete="email"
          icon={<MailIcon />}
          id="register-email"
          label="Email"
          onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
          placeholder="email@example.com"
          type="email"
          value={formData.email}
        />

        <AuthField
          autoComplete="new-password"
          helper="Tối thiểu 8 ký tự, bao gồm chữ cái và số."
          icon={<LockIcon />}
          id="register-password"
          label="Mật khẩu"
          onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
          placeholder="••••••••"
          type="password"
          value={formData.password}
        />

        <label className={styles.checkboxRow} htmlFor="accept-terms">
          <input
            checked={acceptTerms}
            id="accept-terms"
            onChange={(event) => setAcceptTerms(event.target.checked)}
            type="checkbox"
          />
          <span className={styles.checkboxLabel}>
            Tôi đồng ý với <Link to="/register">Điều khoản</Link> và <Link to="/register">Chính sách bảo mật</Link>.
          </span>
        </label>

        {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

        <Button className={styles.submitButton} disabled={isSubmitting} type="submit">
          {isSubmitting ? "Đang tạo tài khoản..." : "Đăng ký ngay"}
          <span aria-hidden="true" className={styles.submitArrow}>
            →
          </span>
        </Button>
      </form>
    </AuthShell>
  );
}
