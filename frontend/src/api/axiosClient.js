import axios from "axios";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "../auth/authStorage";

export const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api",
  headers: {
    "Content-Type": "application/json"
  }
});

let refreshPromise = null;

axiosClient.interceptors.request.use((config) => {
  const session = loadAuthSession();

  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }

  return config;
});

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const session = loadAuthSession();

    if (!session?.refreshToken || originalRequest?._retry || error?.response?.status !== 401) {
      throw error;
    }

    originalRequest._retry = true;

    if (!refreshPromise) {
      refreshPromise = axios
        .post(
          `${import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api"}/auth/refresh`,
          { refreshToken: session.refreshToken },
          { headers: { "Content-Type": "application/json" } }
        )
        .then((response) => {
          const nextSession = {
            accessToken: response.data.accessToken,
            refreshToken: response.data.refreshToken,
            user: response.data.user
          };

          saveAuthSession(nextSession);
          return nextSession;
        })
        .finally(() => {
          refreshPromise = null;
        });
    }

    try {
      const nextSession = await refreshPromise;
      originalRequest.headers.Authorization = `Bearer ${nextSession.accessToken}`;
      return axiosClient(originalRequest);
    } catch (refreshError) {
      clearAuthSession();
      window.alert("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
      window.location.href = "/login";
      throw refreshError;
    }
  }
);
