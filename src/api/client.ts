export const BASE_URL =
  import.meta.env.VITE_API_URL ?? "http://localhost:5128/api";

export const API_ORIGIN = BASE_URL.replace(/\/api$/, "");

export function getAuthHeaders(): Record<string, string> {
  let token: string | null = null;
  const stored = localStorage.getItem("user");
  if (stored) {
    try {
      token = JSON.parse(stored).token ?? null;
    } catch {
      token = null;
    }
  }

  const headers: Record<string, string> = {};

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  return headers;
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
