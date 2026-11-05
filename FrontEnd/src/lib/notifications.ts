import { apiFetch } from "@/lib/api";

export type Notification = {
  id: number;
  user_id: number;
  type: string;
  title: string;
  body: string | null;
  task_id: number | null;
  read_at: string | null;
  created_at: string;
};

/** Unread notifications only — feeds the bell popover. */
export async function fetchNotifications(): Promise<Notification[]> {
  return apiFetch<Notification[]>("/notifications/unread");
}

/** Full history (read and unread) — feeds the profile notifications tab. */
export async function fetchAllNotifications(): Promise<Notification[]> {
  return apiFetch<Notification[]>("/notifications/all");
}

export async function markNotificationRead(id: number) {
  await apiFetch(`/notifications/${id}/read`, { method: "PATCH" });
}

export async function markAllNotificationsRead(_userId?: number) {
  await apiFetch("/notifications/read-all", { method: "POST" });
}

export async function deleteNotification(id: number) {
  await apiFetch(`/notifications/${id}`, { method: "DELETE" });
}

/** Days remaining until a due date (0 = today, negative = overdue). */
export function daysUntil(due: string) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const target = new Date(`${due}T00:00:00`);
  return Math.round((target.getTime() - today.getTime()) / 86_400_000);
}
