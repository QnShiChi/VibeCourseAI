import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import RegisterPage from "./RegisterPage";
import { ThemeProvider } from "../theme/ThemeContext";

const { mockRegister, mockNavigate } = vi.hoisted(() => ({
  mockRegister: vi.fn(),
  mockNavigate: vi.fn()
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate
  };
});

vi.mock("../auth/useAuth", () => ({
  useAuth: () => ({
    register: mockRegister
  })
}));

afterEach(() => {
  mockRegister.mockReset();
  mockNavigate.mockReset();
  window.localStorage.clear();
});

describe("RegisterPage", () => {
  it("renders the redesigned registration layout with the facebook social action", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <RegisterPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: /tạo tài khoản mới/i })).toBeInTheDocument();
    expect(screen.getByText(/khởi tạo nội dung khóa học bằng trí tuệ nhân tạo/i)).toBeInTheDocument();
    expect(screen.getByLabelText("Họ và tên")).toBeInTheDocument();
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /đăng ký ngay/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /google/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /facebook/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /chuyển sang dark mode/i })).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "VibeCourseAI" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: /đăng nhập/i }).length).toBeGreaterThan(0);
  });

  it("applies the shared dark theme to the registration shell and keeps icon spacing local", () => {
    window.localStorage.setItem("app-theme", "dark");

    render(
      <MemoryRouter>
        <ThemeProvider>
          <RegisterPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByTestId("auth-shell")).toHaveAttribute("data-theme", "dark");
    expect(screen.getByLabelText("Họ và tên")).not.toHaveClass("ui-input");
  });

  it("redirects non-admin users to the homepage with the auth intro state after register succeeds", async () => {
    mockRegister.mockResolvedValueOnce({
      user: {
        role: "User"
      }
    });

    render(
      <MemoryRouter>
        <ThemeProvider>
          <RegisterPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    fireEvent.change(screen.getByLabelText("Họ và tên"), { target: { value: "Nguyen Van A" } });
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "user@example.com" } });
    fireEvent.change(screen.getByLabelText("Mật khẩu"), { target: { value: "secret123" } });
    fireEvent.click(screen.getByRole("button", { name: /đăng ký ngay/i }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith("/", {
        replace: true,
        state: {
          authIntro: {
            source: "register"
          }
        }
      });
    });
  });
});
