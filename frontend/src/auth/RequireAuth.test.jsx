import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import RequireAuth from "./RequireAuth";

const mockedUseAuth = vi.fn();

vi.mock("./useAuth", () => ({
  useAuth: () => mockedUseAuth()
}));

describe("RequireAuth", () => {
  it("chuyển người dùng chưa đăng nhập về trang đăng nhập", () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: false,
      isBootstrapping: false,
      user: null
    });

    render(
      <MemoryRouter initialEntries={["/courses"]}>
        <Routes>
          <Route path="/login" element={<div>Trang đăng nhập</div>} />
          <Route
            path="/courses"
            element={
              <RequireAuth>
                <div>Trang khóa học</div>
              </RequireAuth>
            }
          />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText("Trang đăng nhập")).toBeInTheDocument();
  });

  it("chuyển user thường về trang chủ khi route yêu cầu role Admin", () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      isBootstrapping: false,
      user: { role: "User" }
    });

    render(
      <MemoryRouter initialEntries={["/admin/syllabuses"]}>
        <Routes>
          <Route path="/" element={<div>Trang chủ</div>} />
          <Route
            path="/admin/syllabuses"
            element={
              <RequireAuth requiredRole="Admin">
                <div>Đề cương admin</div>
              </RequireAuth>
            }
          />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText("Trang chủ")).toBeInTheDocument();
  });
});
