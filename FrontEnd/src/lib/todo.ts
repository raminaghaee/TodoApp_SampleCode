import { apiFetch } from "@/lib/api";

/** Entity names and fields mirror the backend contract (see BACKEND.md). */
export type User = {
  id: number;
  email: string | null;
  display_name: string | null;
};

export type TaskList = {
  id: number;
  owner_id: number;
  name: string;
  color: string;
  created_at: string;
};

export type ListShare = {
  id: number;
  list_id: number;
  user_id: number;
};

export type TodoItem = {
  id: number;
  list_id: number;
  created_by: number;
  assigned_to: number | null;
  title: string;
  notes: string | null;
  is_completed: boolean;
  is_important: boolean;
  is_my_day: boolean;
  due_date: string | null;
  completed_at: string | null;
  created_at: string;
};

export const LIST_COLORS: { key: string; label: string; className: string }[] = [
  { key: "blue", label: "آبی", className: "bg-primary" },
  { key: "teal", label: "فیروزه‌ای", className: "bg-accent-teal" },
  { key: "violet", label: "بنفش", className: "bg-accent-violet" },
  { key: "amber", label: "کهربایی", className: "bg-accent-amber" },
  { key: "rose", label: "سرخابی", className: "bg-accent-rose" },
];

export function colorClass(color: string) {
  return LIST_COLORS.find((c) => c.key === color)?.className ?? "bg-primary";
}

export async function fetchProfiles(): Promise<User[]> {
  return apiFetch<User[]>("/profiles");
}

/** Backend lookup returns a ProfileDto array, searched by name or email. */
export async function searchProfiles(search: string): Promise<User[]> {
  return apiFetch<User[]>(`/profiles/lookup?email=${encodeURIComponent(search.trim())}`);
}

export async function fetchLists(): Promise<TaskList[]> {
  return apiFetch<TaskList[]>("/lists");
}

export async function fetchTasks(): Promise<TodoItem[]> {
  return apiFetch<TodoItem[]>("/tasks");
}

export async function fetchShares(): Promise<ListShare[]> {
  return apiFetch<ListShare[]>("/shares");
}

export async function createList(ownerId: number, name: string, color: string) {
  return apiFetch<TaskList>("/lists", { method: "POST", body: { name, color } });
}

export async function renameList(id: number, name: string, color?: string) {
  await apiFetch(`/lists/${id}`, {
    method: "PATCH",
    body: { name, ...(color ? { color } : {}) },
  });
}

export async function deleteList(id: number) {
  await apiFetch(`/lists/${id}`, { method: "DELETE" });
}

export async function createTask(input: {
  listId: number;
  createdBy: number;
  title: string;
  isMyDay?: boolean;
  isImportant?: boolean;
  dueDate?: string | null;
  assignedTo?: number | null;
}) {
  await apiFetch("/tasks", {
    method: "POST",
    body: {
      list_id: input.listId,
      title: input.title,
      is_my_day: input.isMyDay ?? false,
      is_important: input.isImportant ?? false,
      due_date: input.dueDate ?? null,
      assigned_to: input.assignedTo ?? null,
    },
  });
}

/**
 * PATCH /api/v1/tasks/:id rules (backend contract):
 * - absent field = no change
 * - clearing due_date → { clear_due_date: true } (null means "no change")
 * - clearing notes → { notes: "" }
 * - removing assignee → { assigned_to: 0 }
 */
export async function updateTask(id: number, patch: Partial<TodoItem>) {
  const body: Record<string, unknown> = { ...patch };
  if ("due_date" in patch && patch.due_date === null) {
    delete body["due_date"];
    body["clear_due_date"] = true;
  }
  if ("notes" in patch && patch.notes === null) {
    body["notes"] = "";
  }
  if ("assigned_to" in patch && patch.assigned_to === null) {
    body["assigned_to"] = 0;
  }
  await apiFetch(`/tasks/${id}`, { method: "PATCH", body });
}

export async function deleteTask(id: number) {
  await apiFetch(`/tasks/${id}`, { method: "DELETE" });
}

export async function shareList(listId: number, userId: number) {
  await apiFetch(`/lists/${listId}/shares`, { method: "POST", body: { user_id: userId } });
}

export async function unshareList(listId: number, userId: number) {
  await apiFetch(`/lists/${listId}/shares/${userId}`, { method: "DELETE" });
}

export function profileLabel(p?: User | null) {
  if (!p) return "بدون مسئول";
  return p.display_name || p.email || "کاربر";
}

export function initials(p?: User | null) {
  const label = profileLabel(p);
  return label.trim().charAt(0).toUpperCase();
}

export { canEditTask } from "./task-access";
