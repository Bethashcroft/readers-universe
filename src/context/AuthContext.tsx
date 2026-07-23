import { useState } from "react";
import type { AuthResponse } from "../api/auth";
import { loginUser, registerUser } from "../api/auth";
import { AuthContext } from "./useAuth";

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthResponse | null>(() => {
    const stored = localStorage.getItem("user");
    if (!stored) return null;
    try {
      const parsed = JSON.parse(stored);
      return parsed.userId ? parsed : null;
    } catch {
      return null;
    }
  });

  const login = async (email: string, password: string) => {
    const data = await loginUser(email, password);
    setUser(data);
    localStorage.setItem("user", JSON.stringify(data));
  };

  const register = async (
    userName: string,
    email: string,
    displayName: string,
    password: string,
  ) => {
    const data = await registerUser(userName, email, displayName, password);
    setUser(data);
    localStorage.setItem("user", JSON.stringify(data));
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem("user");
  };

  const updateUser = (changes: Partial<AuthResponse>) => {
    setUser((prev) => {
      if (!prev) return prev;
      const next = { ...prev, ...changes };
      localStorage.setItem("user", JSON.stringify(next));
      return next;
    });
  };

  return (
    <AuthContext.Provider
      value={{ user, login, register, logout, updateUser }}
    >
      {children}
    </AuthContext.Provider>
  );
}
