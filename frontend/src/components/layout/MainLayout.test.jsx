import { render, screen, within } from "@testing-library/react";
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

    const banner = screen.getByRole("banner");
    const footer = screen.getByRole("contentinfo");

    expect(banner).toBeInTheDocument();
    expect(screen.getByRole("main")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "VibeCourseAI" })).toBeInTheDocument();
    expect(within(banner).getByRole("link", { name: "Trang chủ" })).toBeInTheDocument();
    expect(within(banner).getByRole("link", { name: "Khóa học" })).toBeInTheDocument();
    expect(screen.getByText("Dashboard")).toBeInTheDocument();
    expect(screen.getByText("Quản trị viên hệ thống")).toBeInTheDocument();
    expect(screen.getByText("Đăng xuất")).toBeInTheDocument();
    expect(footer).toBeInTheDocument();
    expect(footer).toHaveTextContent("VibeCourseAI");
    expect(footer).toHaveTextContent("Trang chủ");
    expect(footer).toHaveTextContent("Khóa học");
  });
});
