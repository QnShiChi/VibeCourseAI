import { beforeEach, describe, expect, it } from "vitest";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "./authStorage";

describe("authStorage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("lưu và đọc lại phiên đăng nhập từ localStorage", () => {
    const session = {
      accessToken: "access-token",
      refreshToken: "refresh-token",
      user: {
        id: "user-1",
        fullName: "Nguyễn Văn A",
        email: "vana@example.com",
        role: "User"
      }
    };

    saveAuthSession(session);

    expect(loadAuthSession()).toEqual(session);
  });

  it("xóa phiên đăng nhập khỏi localStorage", () => {
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

    clearAuthSession();

    expect(loadAuthSession()).toBeNull();
  });
});
