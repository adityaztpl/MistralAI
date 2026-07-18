import {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { apiClient, setAccessTokenProvider } from "./apiClient";

type AuthUser = {
  id: string;
  email: string;
  roles: string[];
};

type AuthState = {
  accessToken: string | null;
  expiresAt: string | null;
  user: AuthUser | null;
};

type LoginRequest = {
  email: string;
  password: string;
};

type AuthContextValue = AuthState & {
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
};

const storageKey = import.meta.env.VITE_AUTH_STORAGE_KEY ?? "product-catalog-auth";

const anonymousState: AuthState = {
  accessToken: null,
  expiresAt: null,
  user: null,
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => loadStoredAuth());

  const logout = useCallback(() => {
    localStorage.removeItem(storageKey);
    setState(anonymousState);
  }, []);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await apiClient.post<AuthState>("/api/v1/auth/login", request, { skipAuth: true });
    localStorage.setItem(storageKey, JSON.stringify(response));
    setState(response);
  }, []);

  useEffect(() => {
    setAccessTokenProvider(() => state.accessToken);
    return () => setAccessTokenProvider(() => null);
  }, [state.accessToken]);

  useEffect(() => {
    const onExpired = () => logout();
    window.addEventListener("auth:expired", onExpired);
    return () => window.removeEventListener("auth:expired", onExpired);
  }, [logout]);

  const value = useMemo<AuthContextValue>(
    () => ({
      ...state,
      isAuthenticated: Boolean(state.accessToken),
      login,
      logout,
    }),
    [login, logout, state]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used inside AuthProvider");
  }
  return value;
}

function loadStoredAuth(): AuthState {
  const raw = localStorage.getItem(storageKey);
  if (!raw) return anonymousState;

  try {
    const parsed = JSON.parse(raw) as AuthState;
    if (parsed.expiresAt && new Date(parsed.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(storageKey);
      return anonymousState;
    }
    return parsed;
  } catch {
    localStorage.removeItem(storageKey);
    return anonymousState;
  }
}

export function getStoredAccessToken(): string | null {
  return loadStoredAuth().accessToken;
}
