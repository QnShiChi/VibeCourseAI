import { Route, Routes } from "react-router-dom";
import RequireAuth from "../auth/RequireAuth";
import MainLayout from "../components/layout/MainLayout";
import ChangePasswordPage from "../pages/ChangePasswordPage";
import CoursesPage from "../pages/CoursesPage";
import DashboardPage from "../pages/DashboardPage";
import HomePage from "../pages/HomePage";
import LoginPage from "../pages/LoginPage";
import ProfilePage from "../pages/ProfilePage";
import RegisterPage from "../pages/RegisterPage";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<MainLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route
          path="/profile"
          element={
            <RequireAuth>
              <ProfilePage />
            </RequireAuth>
          }
        />
        <Route
          path="/change-password"
          element={
            <RequireAuth>
              <ChangePasswordPage />
            </RequireAuth>
          }
        />
        <Route
          path="/courses"
          element={
            <RequireAuth>
              <CoursesPage />
            </RequireAuth>
          }
        />
      </Route>
    </Routes>
  );
}
