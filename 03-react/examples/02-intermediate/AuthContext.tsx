import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

export type User = {
  id: string;
  email: string;
  roles: string[];
};

type AuthContextValue = {
  user: User | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (role: string) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  const login = useCallback(async (email: string, password: string) => {
    const response = await fetch('/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    setUser((await response.json()) as User);
  }, []);

  const logout = useCallback(() => {
    setUser(null);
    void fetch('/api/logout', { method: 'POST' });
  }, []);

  const hasRole = useCallback(
    (role: string) => user?.roles.includes(role) ?? false,
    [user],
  );

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: user !== null,
      login,
      logout,
      hasRole,
    }),
    [hasRole, login, logout, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return context;
}
