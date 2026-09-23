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

export interface GoogleSignUpDetails {
  suggestedUserName: string;
  displayName: string;
  email: string;
}

export interface GoogleSignInResponse {
  auth: AuthResponse | null;
  signUp: GoogleSignUpDetails | null;
}

export function googleSignIn(idToken: string): Promise<GoogleSignInResponse> {
  return request("/auth/google", "Google sign-in failed", {
    method: "POST",
    body: JSON.stringify({ idToken }),
  });
}

export function googleRegister(
  idToken: string,
  userName: string,
  displayName: string,
): Promise<AuthResponse> {
  return request("/auth/google/register", "Couldn't create your account", {
    method: "POST",
    body: JSON.stringify({ idToken, userName, displayName }),
  });
}
