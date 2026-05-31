import { Route, Routes } from "react-router-dom";
import RequireAuth from "../auth/RequireAuth";
import MainLayout from "../components/layout/MainLayout";
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
      <Route element={<MainLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route
          path="/dashboard"
          element={
            <RequireAuth requiredRole="Admin">
              <DashboardPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/syllabuses"
          element={
            <RequireAuth requiredRole="Admin">
              <SyllabusesPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/generation-jobs"
          element={
            <RequireAuth requiredRole="Admin">
              <GenerationJobsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/courses/:courseId"
          element={
            <RequireAuth requiredRole="Admin">
              <CourseStructurePage />
            </RequireAuth>
          }
        />
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
