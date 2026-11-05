import { apiFetch, setSession, type StoredUser } from "./api";
import { notifyUserUpdated } from "./auth";

export type AccountNotice = { message?: string; demo_url?: string };

export async function updateDisplayName(displayName: string) {
  const user = await apiFetch<StoredUser>("/profiles/me", { method: "PUT", body: { display_name: displayName.trim() } });
  setSession(user);
  notifyUserUpdated();
  return user;
}

export function requestEmailChange(email: string, currentPassword: string) {
  return apiFetch<AccountNotice>("/auth/change-email", { method: "POST", body: {
    new_email: email.trim(), current_password: currentPassword,
    redirect_url: `${window.location.origin}/confirm-email`,
  } });
}

export function changePassword(currentPassword: string, password: string) {
  return apiFetch<void>("/auth/change-password", { method: "POST", body: { current_password: currentPassword, new_password: password } });
}

export function forgotPassword(email: string) {
  return apiFetch<AccountNotice>("/auth/forgot-password", { method: "POST", body: {
    email: email.trim(), redirect_url: `${window.location.origin}/reset-password`,
  } });
}

export function resetPassword(token: string, password: string) {
  return apiFetch<void>("/auth/reset-password", { method: "POST", body: { token, new_password: password } });
}

export async function confirmEmail(token: string) {
  await apiFetch<void>("/auth/confirm-email", { method: "POST", body: { token } });
}

export function matchingPasswords(password: string, confirmation: string) {
  if (password !== confirmation) throw new Error("رمز جدید و تکرار آن یکسان نیستند.");
}