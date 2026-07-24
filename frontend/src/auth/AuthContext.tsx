import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { posApi } from '../api/pos';
import type { Role } from '../api/types';

type Session = { email: string; role: Role } | null;
type AuthContextValue = { session: Session; login: (email: string, password: string) => Promise<void>; logout: () => void };
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readSession(): Session {
  const token = localStorage.getItem('pos.accessToken');
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    const role = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? 'Cashier';
    return { email: payload.email ?? payload.unique_name ?? 'Team member', role };
  } catch { return null; }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>(readSession);
  const value = useMemo(() => ({
    session,
    login: async (email: string, password: string) => { const tokens = await posApi.login(email, password); localStorage.setItem('pos.accessToken', tokens.accessToken); setSession(readSession()); },
    logout: () => { localStorage.removeItem('pos.accessToken'); setSession(null); }
  }), [session]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
export const useAuth = () => { const context = useContext(AuthContext); if (!context) throw new Error('useAuth must be inside AuthProvider'); return context; };
