import { apiFetch, apiUrl } from "@/lib/api";
import { MOCK_MODE } from "@/lib/data-mode";

export type Subtask = {
  id: number;
  task_id: number;
  title: string;
  is_completed: boolean;
  position: number;
  created_by: number;
  created_at: string;
};

export type TaskAttachment = {
  id: number;
  task_id: number;
  storage_path: string;
  file_name: string;
  mime_type: string | null;
  size_bytes: number | null;
  uploaded_by: number;
  created_at: string;
};

export async function fetchSubtasks(taskId: number): Promise<Subtask[]> {
  return apiFetch<Subtask[]>(`/tasks/${taskId}/subtasks`);
}

export async function createSubtask(input: {
  taskId: number;
  createdBy: number;
  title: string;
  position: number;
}) {
  await apiFetch(`/tasks/${input.taskId}/subtasks`, {
    method: "POST",
    body: { title: input.title, position: input.position },
  });
}

export async function updateSubtask(id: number, patch: Partial<Subtask>) {
  await apiFetch(`/subtasks/${id}`, { method: "PATCH", body: patch });
}

export async function deleteSubtask(id: number) {
  await apiFetch(`/subtasks/${id}`, { method: "DELETE" });
}

export async function fetchAttachments(taskId: number): Promise<TaskAttachment[]> {
  return apiFetch<TaskAttachment[]>(`/tasks/${taskId}/attachments`);
}

export async function uploadAttachment(taskId: number, userId: number, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  await apiFetch(`/tasks/${taskId}/attachments`, { method: "POST", formData });
}

export async function deleteAttachment(att: TaskAttachment) {
  await apiFetch(`/attachments/${att.id}`, { method: "DELETE" });
}

/**
 * Download URL for an attachment. The backend endpoint streams the file when the
 * caller is authorized, so we fetch it (with the HttpOnly auth cookie) and hand
 * back a short-lived object URL.
 */
export async function getAttachmentUrl(attachmentId: number) {
  if (MOCK_MODE) {
    const { mockAttachmentUrl } = await import("@/lib/mock-api");
    return mockAttachmentUrl(attachmentId);
  }
  const res = await fetch(apiUrl(`/attachments/${attachmentId}/download`), {
    credentials: "include",
  });
  if (!res.ok) throw new Error(`دانلود پیوست ناموفق بود (${res.status})`);
  const contentType = res.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    const data = (await res.json()) as { url?: string };
    if (data.url) return data.url;
  }
  const blob = await res.blob();
  return URL.createObjectURL(blob);
}

export function formatBytes(bytes?: number | null) {
  if (!bytes) return "";
  const units = ["B", "KB", "MB", "GB"];
  let v = bytes;
  let i = 0;
  while (v >= 1024 && i < units.length - 1) {
    v /= 1024;
    i += 1;
  }
  return `${v.toFixed(v >= 10 || i === 0 ? 0 : 1)} ${units[i]}`;
}
