import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { authApi } from "@/api/authApi";
import { setAccessToken, setUnauthorizedHandler, silentRefresh } from "@/api/client";
import type { UserProfile } from "@/types/auth";

interface AuthContextValue {
  user: UserProfile | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  loginWithGoogle: (idToken: string) => Promise<void>;
  register: (email: string, password: string, displayName: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setUser(null);
  }, []);

  // On first load, try to silently refresh using the httpOnly cookie -
  // this is what keeps the user logged in across page reloads/app restarts.
  // Goes through the shared silentRefresh() (not a direct authApi.refresh()
  // call) so that if this effect somehow fires more than once - e.g. React's
  // StrictMode double-invoking it in development - both calls share a single
  // in-flight request instead of racing each other against our rotating
  // refresh tokens (see the comment on silentRefresh for why that race used
  // to log people out on reload).
  useEffect(() => {
    setUnauthorizedHandler(clearSession);
    (async () => {
      const result = await silentRefresh();
      if (result) {
        setUser(result.user);
      } else {
        clearSession();
      }
      setIsLoading(false);
    })();
  }, [clearSession]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login({ email, password });
    setAccessToken(result.accessToken);
    setUser(result.user);
  }, []);

  const loginWithGoogle = useCallback(async (idToken: string) => {
    const result = await authApi.googleLogin(idToken);
    setAccessToken(result.accessToken);
    setUser(result.user);
  }, []);

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const result = await authApi.register({ email, password, displayName });
    setAccessToken(result.accessToken);
    setUser(result.user);
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      clearSession();
    }
  }, [clearSession]);

  const refreshProfile = useCallback(async () => {
    const profile = await authApi.me();
    setUser(profile);
  }, []);

  const value = useMemo(
    () => ({ user, isLoading, login, loginWithGoogle, register, logout, refreshProfile }),
    [user, isLoading, login, loginWithGoogle, register, logout, refreshProfile]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
