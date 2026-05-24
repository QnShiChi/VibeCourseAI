import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import MainLayout from "./MainLayout";
import { ThemeProvider } from "../../theme/ThemeContext";

const { mockLogout } = vi.hoisted(() => ({
  mockLogout: vi.fn()
}));

vi.mock("../../auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: {
      fullName: "Quản trị viên hệ thống",
      role: "Admin"
    },
    logout: mockLogout
  })
}));

afterEach(() => {
  vi.useRealTimers();
  mockLogout.mockReset();
});

describe("MainLayout", () => {
  it("renders grouped navigation for an authenticated admin", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <MainLayout />
        </ThemeProvider>
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
    expect(screen.getByRole("button", { name: /chuyển sang dark mode/i })).toBeInTheDocument();
    expect(footer).toBeInTheDocument();
    expect(footer).toHaveTextContent("Trang chủ");
    expect(footer).toHaveTextContent("Khóa học");
  });

  it("shows dropdown links when hovering grouped navigation", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <MainLayout />
        </ThemeProvider>
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
        <ThemeProvider>
          <MainLayout />
        </ThemeProvider>
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

  it("toggles the global theme from the header and persists it on the page shell", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <MainLayout />
        </ThemeProvider>
      </MemoryRouter>
    );

    const shell = document.querySelector(".page-shell");
    const toggle = screen.getByRole("button", { name: /chuyển sang dark mode/i });

    expect(shell).toHaveAttribute("data-theme", "light");

    fireEvent.click(toggle);

    expect(shell).toHaveAttribute("data-theme", "dark");
    expect(screen.getByRole("button", { name: /chuyển sang light mode/i })).toBeInTheDocument();
    expect(window.localStorage.getItem("app-theme")).toBe("dark");
  });

  it("renders and dismisses the auth success intro overlay after navigation from auth pages", () => {
    vi.useFakeTimers();

    render(
      <MemoryRouter
        initialEntries={[
          {
            pathname: "/",
            state: {
              authIntro: {
                source: "login"
              }
            }
          }
        ]}
      >
        <ThemeProvider>
          <MainLayout />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByTestId("auth-transition")).toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(40);
    });

    expect(screen.getByTestId("auth-transition")).toHaveClass("auth-transition--reveal");
    expect(screen.getByTestId("auth-transition")).toHaveClass("auth-transition--active");

    act(() => {
      vi.advanceTimersByTime(1200);
    });

    expect(screen.queryByTestId("auth-transition")).not.toBeInTheDocument();
  });

  it("plays the closing auth transition before completing logout", () => {
    vi.useFakeTimers();

    render(
      <MemoryRouter>
        <ThemeProvider>
          <MainLayout />
        </ThemeProvider>
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole("button", { name: "Đăng xuất" }));

    expect(screen.getByTestId("auth-transition")).toHaveClass("auth-transition--conceal");

    act(() => {
      vi.advanceTimersByTime(40);
    });

    expect(screen.getByTestId("auth-transition")).toHaveClass("auth-transition--active");
    expect(mockLogout).not.toHaveBeenCalled();

    act(() => {
      vi.advanceTimersByTime(1100);
    });

    expect(mockLogout).toHaveBeenCalledTimes(1);
  });
});
