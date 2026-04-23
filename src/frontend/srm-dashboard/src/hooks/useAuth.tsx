import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import api from '../api/axios';
import { UserProfileDto } from '../types/api';

export interface LoginCredentials {
  email: string;
  password: string;
}

interface AuthContextType {
  user: UserProfileDto | null;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfileDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // 📊 Fonction utilitaire SRE pour émettre des logs JSON propres
  const emitLog = (level: "INFO" | "WARN" | "ERROR", event: string, details: any = {}) => {
    console.log(JSON.stringify({
      timestamp: new Date().toISOString(),
      level,
      event,
      service: "srm-frontend",
      ...details
    }));
  };

  const checkAuth = useCallback(async () => {
    emitLog("INFO", "auth_verification_started", { path: "/api/v1/profile/me" });
    setIsLoading(true);
    try {
      const response = await api.get<UserProfileDto>('/api/v1/profile/me');
      emitLog("INFO", "auth_verification_success", { userId: response.data.id, roles: response.data.roles });
      setUser(response.data);
    } catch (error: any) {
      // Pas besoin de logguer l'erreur Axios ici, l'interceptor s'en charge déjà !
      emitLog("WARN", "auth_session_inactive", { action: "clearing_user_state" });
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const login = async (credentials: LoginCredentials) => {
    emitLog("INFO", "login_attempt", { email: credentials.email });
    try {
      await api.post('/api/v1/auth/login', credentials);
      emitLog("INFO", "login_http_success", { action: "fetching_profile" });
      await checkAuth(); 
    } catch (error: any) {
      emitLog("ERROR", "login_failed", { email: credentials.email });
      throw error;
    }
  };

  const logout = async () => {
    emitLog("INFO", "logout_attempt");
    try {
      await api.post('/api/v1/auth/logout');
    } finally {
      setUser(null);
      emitLog("INFO", "logout_success");
    }
  };

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout, checkAuth }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};