import { request } from "./client";

export interface AuthResponse {
  token: string;
  userId: string;
  userName: string;
  displayName: string;
}

export function loginUser(
  email: string,
  password: string,
): Promise<AuthResponse> {
  return request("/auth/login", "Login failed", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export function registerUser(
  userName: string,
  email: string,
  displayName: string,
  password: string,
): Promise<AuthResponse> {
  return request("/auth/register", "Registration failed", {
    method: "POST",
    body: JSON.stringify({ userName, email, displayName, password }),
  });
}
