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

  const value = useMemo(
    () => ({
      session,
      user,
      isAuthenticated: Boolean(session?.accessToken && user),
      isBootstrapping
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
