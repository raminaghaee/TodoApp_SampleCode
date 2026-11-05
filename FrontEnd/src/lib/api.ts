/**
 * REST client for the app's own backend.
 *
 * The frontend talks to a self-hosted API (see BACKEND.md for the full contract).
 * Base URL comes from VITE_API_BASE_URL, e.g. http://localhost:5000 — the `/api`
 * versioned prefix `/api/v1` is added here, so set the variable to the server origin only.
 *
 * Authentication uses the backend's HttpOnly cookies (access_token/refresh_token):
 * every request goes out with `credentials: "include"` and no token is ever kept
 * in JavaScript-readable storage. Only the non-sensitive user profile is cached
 * in localStorage so the UI can render before /auth/me answers.
 */

import { MOCK_MODE } from "./data-mode";
import { backendErrorMessage } from "./api-errors";

const USER_KEY = MOCK_MODE ? "todo_demo_user" : "todo_user";

export const API_BASE_URL: string =
  (import.meta.env["VITE_API_BASE_URL"] as string | undefined) ??
  "http://localhost:5000";

/** One URL builder shared by JSON requests and file downloads. */
export function apiUrl(path: string): string {
  return `${API_BASE_URL.replace(/\/+$/, "")}/api/v1/${path.replace(/^\/+/, "")}`;
}

export type StoredUser = {
  id: number;
  email: string | null;
  display_name?: string | null;
  role?: string;
};

export function getStoredUser(): StoredUser | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  } catch {
    return null;
  }
}

export function setSession(user: StoredUser) {
  window.localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearSession() {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(USER_KEY);
}

type ApiOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  formData?: FormData;
};

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export async function apiFetch<T>(
  path: string,
  options: ApiOptions = {},
): Promise<T> {
  if (MOCK_MODE) {
    const { mockRequest } = await import("./mock-api");
    try {
      return await mockRequest<T>(path, options);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) clearSession();
      throw error;
    }
  }
  const { method = "GET", body, formData } = options;
  const headers = new Headers();
  if (body !== undefined) headers.set("Content-Type", "application/json");

  const res = await fetch(apiUrl(path), {
    method,
    headers,
    credentials: "include",
    body: formData ?? (body !== undefined ? JSON.stringify(body) : null),
  });

  if (!res.ok) {
    if (res.status === 401) clearSession();
    let message = `خطای سرور (${res.status})`;
    try {
      message = backendErrorMessage(await res.json(), message);
    } catch {
      /* keep default */
    }
    throw new ApiError(res.status, message);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
