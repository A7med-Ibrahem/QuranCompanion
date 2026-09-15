import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
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

// Offline cache - stores ONLY the non-sensitive public profile (name, wird id,
// ...). Access tokens live in memory and the refresh token only in an httpOnly
// cookie, so nothing here can be used to impersonate the user if compromised.
const USER_CACHE_KEY = "wird.user";

function loadCachedUser(): UserProfile | null {
  try {
    const raw = localStorage.getItem(USER_CACHE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as UserProfile;
    if (!parsed || typeof parsed.id !== "string" || typeof parsed.displayName !== "string") return null;
    return parsed;
  } catch {
    return null;
  }
}

function saveCachedUser(user: UserProfile | null) {
  try {
    if (user) localStorage.setItem(USER_CACHE_KEY, JSON.stringify(user));
    else localStorage.removeItem(USER_CACHE_KEY);
  } catch {
    // Storage can be unavailable (private mode/quota) - safe to ignore.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // Hydrate synchronously from the cache (NOT a token) so a PWA user who is
  // offline on page load stays "logged in" instead of bouncing to /login.
  const initialUser = useRef<UserProfile | null>(loadCachedUser()).current;
  const [user, setUser] = useState<UserProfile | null>(initialUser);
  // Only wait for the network check when there's nothing cached to show yet.
  const [isLoading, setIsLoading] = useState(() => initialUser === null);

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setUser(null);
    saveCachedUser(null);
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

      if (result.status === "ok") {
        setUser(result.auth.user);
        saveCachedUser(result.auth.user);
      } else if (result.status === "expired") {
        // The server explicitly rejected the refresh token - this is a real
        // logout, so drop everything (including the offline cache).
        clearSession();
      }
      // status === "offline": network/server trouble - keep the cached user
      // exactly as-is; they can keep reading offline (ProtectedRoute relies on
      // this: clearSession is NOT called here).

      setIsLoading(false);
    })();
  }, [clearSession]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login({ email, password });
    setAccessToken(result.accessToken);
    setUser(result.user);
    saveCachedUser(result.user);
  }, []);

  const loginWithGoogle = useCallback(async (idToken: string) => {
    const result = await authApi.googleLogin(idToken);
    setAccessToken(result.accessToken);
    setUser(result.user);
    saveCachedUser(result.user);
  }, []);

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const result = await authApi.register({ email, password, displayName });
    setAccessToken(result.accessToken);
    setUser(result.user);
    saveCachedUser(result.user);
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
    saveCachedUser(profile);
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