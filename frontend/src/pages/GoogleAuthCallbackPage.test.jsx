import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import GoogleAuthCallbackPage from "./GoogleAuthCallbackPage";

const { mockNavigate, mockCompleteGoogleLogin } = vi.hoisted(() => ({
  mockNavigate: vi.fn(),
  mockCompleteGoogleLogin: vi.fn()
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
    completeGoogleLogin: mockCompleteGoogleLogin
  })
}));

afterEach(() => {
  mockNavigate.mockReset();
  mockCompleteGoogleLogin.mockReset();
});

describe("GoogleAuthCallbackPage", () => {
  it("exchanges the google login token and redirects admins to the dashboard", async () => {
    mockCompleteGoogleLogin.mockResolvedValueOnce({
      user: {
        role: "Admin"
      }
    });

    render(
      <MemoryRouter initialEntries={["/auth/google/callback?exchangeToken=token-1"]}>
        <GoogleAuthCallbackPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(mockCompleteGoogleLogin).toHaveBeenCalledWith("token-1");
      expect(mockNavigate).toHaveBeenCalledWith("/dashboard", { replace: true });
    });
  });

  it("redirects back to login with a friendly oauth error message", async () => {
    render(
      <MemoryRouter initialEntries={["/auth/google/callback?error=account_locked"]}>
        <GoogleAuthCallbackPage />
      </MemoryRouter>
    );

    expect(screen.getByText("Tài khoản đã bị khóa.")).toBeInTheDocument();

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith("/login", {
        replace: true,
        state: { oauthError: "Tài khoản đã bị khóa." }
      });
    });
  });
});
