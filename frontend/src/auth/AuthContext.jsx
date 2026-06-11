import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { mergeGuestCart } from "../api/cartService";
import * as authService from "./authService";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "./authStorage";
import { clearGuestCartToken, getGuestCartToken } from "../utils/cartStorage";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => loadAuthSession());
  const [user, setUser] = useState(() => loadAuthSession()?.user ?? null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);

  useEffect(() => {
    async function bootstrap() {
      if (!session?.accessToken || !session?.refreshToken) {
        setIsBootstrapping(false);
        return;
      }

      try {
        const currentUser = await authService.getCurrentUser();
        const nextSession = { ...session, user: currentUser };
        saveAuthSession(nextSession);
        setSession(nextSession);
        setUser(currentUser);
      } catch {
        clearAuthSession();
        setSession(null);
        setUser(null);
      } finally {
        setIsBootstrapping(false);
      }
    }

    bootstrap();
  }, []);

  async function handleAuthSuccess(response) {
    const guestCartToken = getGuestCartToken();

    const nextSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      user: response.user
    };

    saveAuthSession(nextSession);
    setSession(nextSession);
    setUser(response.user);

    if (guestCartToken) {
      try {
        await mergeGuestCart(guestCartToken);
        clearGuestCartToken();
      } catch {
        // Ignore cart merge failure to avoid blocking auth success.
      }
    }

    return nextSession;
  }

  async function completeGoogleLogin(exchangeToken) {
    const response = await authService.exchangeGoogleLogin(exchangeToken);
    return handleAuthSuccess(response);
  }

  async function login(formData) {
    const response = await authService.login(formData);
    return handleAuthSuccess(response);
  }

  async function register(formData) {
    const response = await authService.register(formData);
    return handleAuthSuccess(response);
  }

  async function logout() {
    try {
      if (session?.refreshToken) {
        await authService.logout(session.refreshToken);
      }
    } finally {
      clearAuthSession();
      setSession(null);
      setUser(null);
    }
  }

  async function changePassword(payload) {
    await authService.changePassword(payload);
    await logout();
  }

  async function forgotPassword(payload) {
    return await authService.forgotPassword(payload);
  }

  async function resetPassword(payload) {
    return await authService.resetPassword(payload);
  }

  const value = useMemo(
    () => ({
      session,
      user,
      isAuthenticated: Boolean(session?.accessToken && user),
      isBootstrapping,
      login,
      register,
      completeGoogleLogin,
      logout,
      changePassword,
      forgotPassword,
      resetPassword
    }),
    [session, user, isBootstrapping]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
