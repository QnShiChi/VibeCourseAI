import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "./AuthContext";
import { saveAuthSession } from "./authStorage";

vi.mock("./authService", () => ({
  getCurrentUser: vi.fn().mockResolvedValue({
    id: "user-1",
    fullName: "Nguyễn Văn A",
    email: "vana@example.com",
    role: "User"
  })
}));

function Probe() {
  const { isAuthenticated, user, isBootstrapping } = useAuth();

  return (
    <div>
      <span>{isBootstrapping ? "Đang khởi tạo" : "Đã khởi tạo"}</span>
      <span>{isAuthenticated ? "Đã đăng nhập" : "Chưa đăng nhập"}</span>
      <span>{user?.fullName ?? "Không có người dùng"}</span>
    </div>
  );
}

describe("AuthContext", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("khôi phục phiên đăng nhập từ localStorage và gọi me", async () => {
    saveAuthSession({
      accessToken: "access-token",
      refreshToken: "refresh-token",
      user: {
        id: "user-1",
        fullName: "Nguyễn Văn A",
        email: "vana@example.com",
        role: "User"
      }
    });

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByText("Đã đăng nhập")).toBeInTheDocument();
      expect(screen.getByText("Nguyễn Văn A")).toBeInTheDocument();
    });
  });
});
