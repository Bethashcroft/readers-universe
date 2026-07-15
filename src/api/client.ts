export const BASE_URL =
  import.meta.env.VITE_API_URL ?? "http://localhost:5128/api";

export const API_ORIGIN = BASE_URL.replace(/\/api$/, "");

export function getToken(): string | null {
  const stored = localStorage.getItem("user");
  if (!stored) return null;
  try {
    return JSON.parse(stored).token ?? null;
  } catch {
    return null;
  }
}

export function getAuthHeaders(): Record<string, string> {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export function getHeaders(): Record<string, string> {
  return {
    "Content-Type": "application/json",
    ...getAuthHeaders(),
  };
}

export async function parseError(
  response: Response,
  fallback: string,
): Promise<string> {
  const isAuthEndpoint =
    response.url.endsWith("/auth/login") ||
    response.url.endsWith("/auth/register");

  if (
    response.status === 401 &&
    !isAuthEndpoint &&
    localStorage.getItem("user")
  ) {
    localStorage.removeItem("user");
    window.location.href = "/login";
  }

  const data = await response.json().catch(() => null);
  if (!data) return fallback;
  if (typeof data.message === "string") return data.message;
  if (Array.isArray(data) && data[0]?.description) return data[0].description;
  return fallback;
}

async function checkedFetch(
  path: string,
  fallback: string,
  options: RequestInit,
): Promise<Response> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: getHeaders(),
    ...options,
  });

  if (!response.ok) {
    throw new Error(await parseError(response, fallback));
  }

  return response;
}

export async function request<T>(
  path: string,
  fallback: string,
  options: RequestInit = {},
): Promise<T> {
  const response = await checkedFetch(path, fallback, options);
  return response.json();
}

export async function requestVoid(
  path: string,
  fallback: string,
  options: RequestInit = {},
): Promise<void> {
  await checkedFetch(path, fallback, options);
}
