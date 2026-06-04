import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import LoginPage from "./LoginPage";
import { ThemeProvider } from "../theme/ThemeContext";

const { mockLogin, mockNavigate } = vi.hoisted(() => ({
  mockLogin: vi.fn(),
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
    login: mockLogin
  })
}));

afterEach(() => {
  mockLogin.mockReset();
  mockNavigate.mockReset();
  window.localStorage.clear();
});

describe("LoginPage", () => {
  it("renders the redesigned split auth layout with social actions", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <LoginPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: /tài khoản \/ đăng nhập/i })).toBeInTheDocument();
    expect(screen.getByText(/thiết kế tri thức cùng ai/i)).toBeInTheDocument();
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Đăng nhập" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /google/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /facebook/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /chuyển sang dark mode/i })).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "VibeCourseAI" }).length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: /đăng ký ngay/i })).toBeInTheDocument();
  });

  it("keeps the auth shell and inputs on the shared dark theme without mixing light styles", () => {
    window.localStorage.setItem("app-theme", "dark");

    render(
      <MemoryRouter>
        <ThemeProvider>
          <LoginPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(screen.getByTestId("auth-shell")).toHaveAttribute("data-theme", "dark");
    expect(screen.getByLabelText("Email")).not.toHaveClass("ui-input");
  });

  it("disables spellcheck and auto correction on auth inputs to avoid browser text artifacts", () => {
    render(
      <MemoryRouter>
        <ThemeProvider>
          <LoginPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText("Email");
    const passwordInput = screen.getByLabelText("Mật khẩu");

    expect(emailInput).toHaveAttribute("spellcheck", "false");
    expect(emailInput).toHaveAttribute("autocapitalize", "none");
    expect(emailInput).toHaveAttribute("autocorrect", "off");
    expect(passwordInput).toHaveAttribute("spellcheck", "false");
    expect(passwordInput).toHaveAttribute("autocapitalize", "none");
    expect(passwordInput).toHaveAttribute("autocorrect", "off");
  });

  it("redirects admin users to the dashboard with the auth intro state after login succeeds", async () => {
    mockLogin.mockResolvedValueOnce({
      user: {
        role: "Admin"
      }
    });

    render(
      <MemoryRouter>
        <ThemeProvider>
          <LoginPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "admin@vibecourse.local" } });
    fireEvent.change(screen.getByLabelText("Mật khẩu"), { target: { value: "secret123" } });
    fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith("/dashboard", {
        replace: true,
        state: {
          authIntro: {
            source: "login"
          }
        }
      });
    });
  });

  it("forces readable error colors when login fails in dark mode", async () => {
    window.localStorage.setItem("app-theme", "dark");
    mockLogin.mockRejectedValueOnce({
      response: {
        data: {
          message: "Email hoặc mật khẩu không đúng."
        }
      }
    });

    render(
      <MemoryRouter>
        <ThemeProvider>
          <LoginPage />
        </ThemeProvider>
      </MemoryRouter>
    );

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "user@vibecourse.local" } });
    fireEvent.change(screen.getByLabelText("Mật khẩu"), { target: { value: "wrong-password" } });
    fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));

    const alertText = await screen.findByText("Email hoặc mật khẩu không đúng.");
    expect(alertText.className).toContain("authErrorAlertText");
    expect(alertText).toHaveStyle({
      color: "var(--auth-error-text)"
    });
    expect(alertText.style.webkitTextFillColor).toBe("var(--auth-error-text)");
    expect(alertText.closest('[role="alert"]')).toHaveStyle({
      backgroundColor: "var(--auth-error-bg)",
      border: "1px solid var(--auth-error-border)"
    });
  });
});
