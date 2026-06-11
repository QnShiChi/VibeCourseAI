import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import AdminLayout from "./AdminLayout";
import { ThemeProvider } from "../../theme/ThemeContext";

const { mockLogout } = vi.hoisted(() => ({
  mockLogout: vi.fn()
}));

vi.mock("../../auth/useAuth", () => ({
  useAuth: () => ({
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

function renderAdminLayout(initialEntry = "/dashboard") {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <ThemeProvider>
        <Routes>
          <Route element={<AdminLayout />} path="/dashboard">
            <Route element={<div>Dashboard</div>} index />
          </Route>
          <Route element={<AdminLayout />} path="/admin/users">
            <Route element={<div>Users</div>} index />
          </Route>
          <Route element={<div>Login</div>} path="/login" />
        </Routes>
      </ThemeProvider>
    </MemoryRouter>
  );
}

describe("AdminLayout", () => {
  it("renders and dismisses the auth success intro overlay after login navigation", () => {
    vi.useFakeTimers();

    renderAdminLayout({
      pathname: "/dashboard",
      state: {
        authIntro: {
          source: "login"
        }
      }
    });

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

  it("plays the closing auth transition before completing admin logout", () => {
    vi.useFakeTimers();

    renderAdminLayout("/dashboard");

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
    expect(screen.getByText("Login")).toBeInTheDocument();
  });
});
