import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
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

afterEach(() => {
  vi.useRealTimers();
});

describe("MainLayout", () => {
  it("renders grouped navigation for an authenticated admin", () => {
    render(
      <MemoryRouter>
        <MainLayout />
      </MemoryRouter>
    );

    const banner = screen.getByRole("banner");
    const footer = screen.getByRole("contentinfo");
    const logoImages = screen.getAllByAltText("VibeCourseAI");

    expect(banner).toBeInTheDocument();
    expect(screen.getByRole("main")).toBeInTheDocument();
    expect(within(banner).getByRole("link", { name: "VibeCourseAI" })).toBeInTheDocument();
    expect(within(footer).getByRole("link", { name: "VibeCourseAI" })).toBeInTheDocument();
    expect(logoImages).toHaveLength(2);
    expect(within(banner).getByRole("link", { name: "Trang chủ" })).toBeInTheDocument();
    expect(within(banner).getByRole("link", { name: "Khóa học" })).toBeInTheDocument();
    expect(within(banner).getByRole("button", { name: "Dashboard" })).toBeInTheDocument();
    expect(within(banner).getByRole("button", { name: "Hồ sơ" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Tổng quan" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Thông tin hồ sơ" })).not.toBeInTheDocument();
    expect(screen.getByText("Quản trị viên hệ thống")).toBeInTheDocument();
    expect(screen.getByText("Đăng xuất")).toBeInTheDocument();
    expect(footer).toBeInTheDocument();
    expect(footer).toHaveTextContent("Trang chủ");
    expect(footer).toHaveTextContent("Khóa học");
  });

  it("shows dropdown links when hovering grouped navigation", () => {
    render(
      <MemoryRouter>
        <MainLayout />
      </MemoryRouter>
    );

    fireEvent.mouseEnter(screen.getByRole("button", { name: "Dashboard" }));
    expect(screen.getByRole("link", { name: "Tổng quan" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Đề cương" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Tiến trình" })).toBeInTheDocument();

    fireEvent.mouseEnter(screen.getByRole("button", { name: "Hồ sơ" }));
    expect(screen.getByRole("link", { name: "Thông tin hồ sơ" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Đổi mật khẩu" })).toBeInTheDocument();
  });

  it("keeps the dropdown open while moving from trigger into the menu", () => {
    vi.useFakeTimers();

    render(
      <MemoryRouter>
        <MainLayout />
      </MemoryRouter>
    );

    const trigger = screen.getByRole("button", { name: "Dashboard" });
    fireEvent.mouseEnter(trigger);

    const dropdownLink = screen.getByRole("link", { name: "Tổng quan" });
    const group = trigger.closest(".app-nav__group");

    expect(group).not.toBeNull();
    fireEvent.mouseLeave(group);
    fireEvent.mouseEnter(dropdownLink);

    act(() => {
      vi.advanceTimersByTime(100);
    });

    expect(screen.getByRole("link", { name: "Tổng quan" })).toBeInTheDocument();
  });
});
