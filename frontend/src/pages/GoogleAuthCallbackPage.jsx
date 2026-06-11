import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import Section from "../components/ui/Section";

function mapGoogleError(errorCode) {
  switch (errorCode) {
    case "google_state_invalid":
      return "Phiên đăng nhập Google không hợp lệ hoặc đã hết hạn.";
    case "google_email_unverified":
      return "Tài khoản Google chưa xác minh email.";
    case "account_locked":
      return "Tài khoản đã bị khóa.";
    default:
      return "Đăng nhập Google thất bại. Vui lòng thử lại.";
  }
}

export default function GoogleAuthCallbackPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { completeGoogleLogin } = useAuth();
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const exchangeToken = params.get("exchangeToken");
    const error = params.get("error");

    if (error) {
      const message = mapGoogleError(error);
      setErrorMessage(message);
      navigate("/login", { replace: true, state: { oauthError: message } });
      return;
    }

    if (!exchangeToken) {
      const message = "Thiếu thông tin đăng nhập Google.";
      setErrorMessage(message);
      navigate("/login", { replace: true, state: { oauthError: message } });
      return;
    }

    void completeGoogleLogin(exchangeToken)
      .then((session) => {
        navigate(session.user?.role === "Admin" ? "/dashboard" : "/", { replace: true });
      })
      .catch(() => {
        const message = "Đăng nhập Google thất bại. Vui lòng thử lại.";
        setErrorMessage(message);
        navigate("/login", { replace: true, state: { oauthError: message } });
      });
  }, [completeGoogleLogin, location.search, navigate]);

  return (
    <Section className="section-stack">
      <h1>Đang hoàn tất đăng nhập Google...</h1>
      <p>{errorMessage || "Vui lòng chờ trong giây lát."}</p>
    </Section>
  );
}
