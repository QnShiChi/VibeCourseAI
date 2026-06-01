import { Route, Routes } from "react-router-dom";
import RequireAuth from "../auth/RequireAuth";
import AdminLayout from "../components/layout/AdminLayout";
import MainLayout from "../components/layout/MainLayout";
import AdminCoursesPage from "../pages/AdminCoursesPage";
import AdminCategoriesPage from "../pages/AdminCategoriesPage";
import AdminFinancePage from "../pages/AdminFinancePage";
import AdminProfilePage from "../pages/AdminProfilePage";
import AdminSettingsPage from "../pages/AdminSettingsPage";
import AdminUsersPage from "../pages/AdminUsersPage";
import ChangePasswordPage from "../pages/ChangePasswordPage";
import CoursesPage from "../pages/CoursesPage";
import CourseLearnPage from "../pages/CourseLearnPage";
import CourseStructurePage from "../pages/CourseStructurePage";
import DashboardPage from "../pages/DashboardPage";
import GenerationJobsPage from "../pages/GenerationJobsPage";
import HomePage from "../pages/HomePage";
import LoginPage from "../pages/LoginPage";
import ForgotPasswordPage from "../pages/ForgotPasswordPage";
import ResetPasswordPage from "../pages/ResetPasswordPage";
import ProfilePage from "../pages/ProfilePage";
import RegisterPage from "../pages/RegisterPage";
import SyllabusesPage from "../pages/SyllabusesPage";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route
        element={(
          <RequireAuth requiredRole="Admin">
            <AdminLayout />
          </RequireAuth>
        )}
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/admin/courses" element={<AdminCoursesPage />} />
        <Route path="/admin/categories" element={<AdminCategoriesPage />} />
        <Route path="/admin/users" element={<AdminUsersPage />} />
        <Route path="/admin/finance" element={<AdminFinancePage />} />
        <Route path="/admin/profile" element={<AdminProfilePage />} />
        <Route path="/admin/settings" element={<AdminSettingsPage />} />
        <Route path="/admin/syllabuses" element={<SyllabusesPage />} />
        <Route path="/admin/generation-jobs" element={<GenerationJobsPage />} />
        <Route path="/admin/courses/:courseId" element={<CourseStructurePage />} />
      </Route>
      <Route element={<MainLayout />}>
        <Route path="/" element={<HomePage />} />
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
        <Route
          path="/courses/:courseId/learn"
          element={
            <RequireAuth>
              <CourseLearnPage />
            </RequireAuth>
          }
        />
      </Route>
    </Routes>
  );
}
