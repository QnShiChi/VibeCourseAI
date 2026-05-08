import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./useAuth";

export default function RequireAuth({ children }) {
  const { isAuthenticated, isBootstrapping } = useAuth();
  const location = useLocation();

  if (isBootstrapping) {
    return <p>Đang kiểm tra phiên đăng nhập...</p>;
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname, message: "Bạn cần đăng nhập để tiếp tục." }}
      />
    );
  }

  return children;
}
