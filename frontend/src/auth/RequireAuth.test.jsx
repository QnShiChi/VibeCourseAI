import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import RequireAuth from "./RequireAuth";

vi.mock("./useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: false,
    isBootstrapping: false
  })
}));

describe("RequireAuth", () => {
  it("chuyển người dùng chưa đăng nhập về trang đăng nhập", () => {
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
});
