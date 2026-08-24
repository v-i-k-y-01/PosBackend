import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { posApi } from '../api/pos';
import type { Role } from '../api/types';

/**
 * Represents the authenticated user session context.
 */
type Session = {
  email: string;
  role: Role;
} | null;

/**
 * The structure of the authentication context value.
 */
type AuthContextValue = {
  session: Session;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Extracts and decodes user session details from the stored JWT access token payload.
 * @returns The parsed session containing user email and role, or null if token is missing/invalid.
 */
function readSessionFromToken(): Session {
  const token = localStorage.getItem('pos.accessToken');
  if (!token) {
    return null;
  }

  try {
    // Decode base64 URL payload from the JWT.
    const tokenPayloadBase64 = token.split('.')[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const decodedPayload = JSON.parse(atob(tokenPayloadBase64));

    // Resolve claims by matching typical standard formats.
    const role = decodedPayload.role 
      ?? decodedPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] 
      ?? 'Cashier';

    const email = decodedPayload.email 
      ?? decodedPayload.unique_name 
      ?? 'Team member';

    return { email, role };
  } catch {
    return null;
  }
}

/**
 * Provider component that exposes authentication state and login/logout functions to child elements.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>(readSessionFromToken);

  const authContextValue = useMemo(() => ({
    session,
    login: async (email: string, password: string) => {
      const tokens = await posApi.login(email, password);
      localStorage.setItem('pos.accessToken', tokens.accessToken);
      setSession(readSessionFromToken());
    },
    logout: () => {
      localStorage.removeItem('pos.accessToken');
      setSession(null);
    }
  }), [session]);

  return (
    <AuthContext.Provider value={authContextValue}>
      {children}
    </AuthContext.Provider>
  );
}

/**
 * Hook to consume the authentication context state.
 * Must be executed within an <AuthProvider> wrapper.
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be inside AuthProvider');
  }
  return context;
};
