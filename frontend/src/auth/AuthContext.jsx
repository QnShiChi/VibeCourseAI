import { createContext, useContext, useEffect, useMemo, useState } from "react";
import * as authService from "./authService";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "./authStorage";

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
    const nextSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      user: response.user
    };

    saveAuthSession(nextSession);
    setSession(nextSession);
    setUser(response.user);
    return nextSession;
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

  const value = useMemo(
    () => ({
      session,
      user,
      isAuthenticated: Boolean(session?.accessToken && user),
      isBootstrapping,
      login,
      register,
      logout,
      changePassword
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
