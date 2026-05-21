import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import MainLayout from "./MainLayout";

vi.mock("../../auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: {
      fullName: "Quản trị viên hệ thống",
      role: "Admin"
    },
    logout: vi.fn()
  })
}));

describe("MainLayout", () => {
  it("renders the shared navigation shell for an authenticated admin", () => {
    render(
      <MemoryRouter>
        <MainLayout />
      </MemoryRouter>
    );

    expect(screen.getByText("VibeCourseAI")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Trang chủ" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Khóa học" })).toBeInTheDocument();
    expect(screen.getByText("Dashboard")).toBeInTheDocument();
    expect(screen.getByText("Quản trị viên hệ thống")).toBeInTheDocument();
    expect(screen.getByText("Đăng xuất")).toBeInTheDocument();
  });
});
