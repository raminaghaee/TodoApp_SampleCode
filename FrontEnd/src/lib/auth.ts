/**
 * Authentication against the app's own backend (see BACKEND.md, section 4).
 * The backend sets HttpOnly auth cookies on login/register; the frontend never
 * stores the token. Session restore on startup goes through GET /auth/me.
 */
import { apiFetch, clearSession, getStoredUser, setSession, type StoredUser } from "@/lib/api";

/** Shape kept compatible with what the pages used to read from Supabase auth. */
export type AuthUser = {
  id: number;
  email: string | null;
  user_metadata: { display_name?: string | undefined };
};

type AuthResponse = {
  token: string;
  user: StoredUser;
};

function toAuthUser(u: StoredUser): AuthUser {
  return { id: u.id, email: u.email, user_metadata: { display_name: u.display_name ?? undefined } };
}

type AuthListener = (event: "SIGNED_IN" | "SIGNED_OUT" | "USER_UPDATED") => void;
const listeners = new Set<AuthListener>();

/** Subscribe to auth changes; returns an unsubscribe function. */
export function onAuthChange(cb: AuthListener): () => void {
  listeners.add(cb);
  return () => listeners.delete(cb);
}

function emit(event: "SIGNED_IN" | "SIGNED_OUT" | "USER_UPDATED") {
  listeners.forEach((cb) => cb(event));
}

export function notifyUserUpdated() {
  emit("USER_UPDATED");
}

export async function login(email: string, password: string): Promise<AuthUser> {
  // The response carries a token too, but we deliberately ignore it — the
  // HttpOnly cookie set by the backend is the session (XSS-safe).
  const res = await apiFetch<AuthResponse>("/auth/login", {
    method: "POST",
    body: { email, password },
  });
  setSession(res.user);
  emit("SIGNED_IN");
  return toAuthUser(res.user);
}

export async function register(
  email: string,
  password: string,
  displayName: string,
): Promise<{ user: AuthUser | null; needsConfirmation: boolean }> {
  const res = await apiFetch<AuthResponse>("/auth/register", {
    method: "POST",
    body: { email, password, display_name: displayName },
  });
  setSession(res.user);
  emit("SIGNED_IN");
  return { user: toAuthUser(res.user), needsConfirmation: false };
}

/**
 * Current user: asks the backend once (the HttpOnly cookie proves the session);
 * falls back to the cached profile when the request cannot complete.
 */
export async function getCurrentUser(): Promise<AuthUser | null> {
  const cached = getStoredUser();
  try {
    const me = await apiFetch<StoredUser>("/auth/me");
    setSession(me);
    return toAuthUser(me);
  } catch {
    return cached ? toAuthUser(cached) : null;
  }
}

export async function logout(): Promise<void> {
  try {
    await apiFetch("/auth/logout", { method: "POST" });
  } catch {
    /* best effort */
  }
  clearSession();
  emit("SIGNED_OUT");
}
