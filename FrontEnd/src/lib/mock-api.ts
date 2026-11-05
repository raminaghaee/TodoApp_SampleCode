/** Browser-only demo store. Never imported by requests in real-backend mode. */
import { ApiError } from "./api";

export const MOCK_ACCOUNTS = [
  { id: 1, email: "admin@example.test", display_name: "مدیر نمایشی", role: "super_admin" },
  { id: 2, email: "sara@example.test", display_name: "سارا نمایشی", role: "user" },
  { id: 3, email: "reza@example.test", display_name: "رضا نمایشی", role: "user" },
];
export const MOCK_PASSWORD = "Demo123!";
const STORE_KEY = "todo_demo_data_v2";
const SESSION_KEY = "todo_demo_user";
type Row = Record<string, unknown> & { id: number };
type Store = Record<string, Row[]>;
const files = new Map<number, Blob>();
const now = () => new Date().toISOString();
let nextId = 1000;
const id = () => ++nextId;
const passwordHash = async (password: unknown) => Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(String(password)))), byte => byte.toString(16).padStart(2, "0")).join("");

/** Demo inbox: every seeded task gets an unread notification that points back at it. */
function demoNotifications(store: Store): Row[] {
  const owner = (listId: unknown) => store["lists"]?.find((l) => l.id === listId)?.["owner_id"];
  return (store["tasks"] ?? []).map((t, i) => ({
    id: 900 + i,
    user_id: t["assigned_to"] ?? owner(t["list_id"]) ?? 1,
    type: "assigned",
    title: `کار «${String(t["title"])}» برای شماست`,
    body: "برای رفتن به این کار روی اعلان کلیک کنید.",
    task_id: t.id,
    read_at: null,
    created_at: now(),
  }));
}

function seed(): Store {
  const lists = MOCK_ACCOUNTS.map((u) => ({ id: u.id * 10, owner_id: u.id, name: "کارهای نمایشی", color: "teal", created_at: now() }));
  const tasks = MOCK_ACCOUNTS.map((u) => ({ id: u.id * 100, list_id: u.id * 10, created_by: u.id, assigned_to: null, title: "بررسی برنامه هفتگی", notes: "یادداشت نمونه", is_completed: false, is_important: true, is_my_day: true, due_date: null, completed_at: null, created_at: now() }));
  return {
    users: MOCK_ACCOUNTS.map((u) => ({ ...u, is_active: true, created_at: now() })),
    lists,
    tasks,
    shares: [], subtasks: [], attachments: [],
    notifications: demoNotifications({ lists, tasks }),
  };
}

function load(): Store {
  try {
    const saved = localStorage.getItem(STORE_KEY);
    if (saved) {
      const store = JSON.parse(saved) as Store;
      // Demo stores created before notifications existed get the demo inbox.
      if (!Array.isArray(store["notifications"])) store["notifications"] = [];
      if (store["notifications"].length === 0) store["notifications"] = demoNotifications(store);
      return store;
    }
  } catch { /* Reset corrupt demo storage. */ }
  return seed();
}

export async function mockRequest<T>(path: string, options: { method?: string; body?: unknown; formData?: FormData } = {}): Promise<T> {
  const store = load();
  const method = options.method ?? "GET";
  const body = (options.body ?? {}) as Record<string, unknown>;
  const [route, query] = path.split("?");
  const parts = (route ?? "").split("/").filter(Boolean);
  const persist = () => localStorage.setItem(STORE_KEY, JSON.stringify(store));
  const result = (value: unknown) => { persist(); return value as T; };
  const credentials = store["credentials"] ?? (store["credentials"] = []);
  const tokens = store["account_tokens"] ?? (store["account_tokens"] = []);
  const validPassword = async (target: Row, password: unknown) => {
    const credential = credentials.find(c => c.id === target.id);
    return credential ? credential["hash"] === await passwordHash(password) : password === MOCK_PASSWORD;
  };
  const savePassword = async (target: Row, password: unknown) => {
    if (typeof password !== "string" || !password) throw new ApiError(400, "رمز جدید را وارد کنید.");
    const hash = await passwordHash(password);
    const credential = credentials.find(c => c.id === target.id);
    if (credential) credential["hash"] = hash;
    else credentials.push({ id: target.id, hash });
  };
  const newToken = (target: Row, purpose: string, redirect: unknown, extra = {}) => {
    // This is a local demo simulation only, never a real credential or email send.
    const token = crypto.randomUUID();
    tokens.push({ id: id(), token, user_id: target.id, purpose, expires_at: Date.now() + 30 * 60 * 1000, ...extra });
    return { demo_url: `${String(redirect).split("#")[0]}#type=recovery&token=${encodeURIComponent(token)}` };
  };

  if (route === "/auth/forgot-password" && method === "POST") {
    const target = store["users"]?.find(u => u["email"] === String(body["email"]).trim().toLowerCase());
    return result(target ? newToken(target, "reset", body["redirect_url"]) : {});
  }
  if ((route === "/auth/reset-password" || route === "/auth/confirm-email") && method === "POST") {
    const purpose = route === "/auth/reset-password" ? "reset" : "email";
    const token = tokens.find(t => t["token"] === body["token"] && t["purpose"] === purpose && Number(t["expires_at"]) > Date.now());
    const target = store["users"]?.find(u => u.id === token?.["user_id"]);
    if (!token || !target) throw new ApiError(400, "لینک نامعتبر یا منقضی شده است.");
    if (purpose === "reset") await savePassword(target, body["new_password"]);
    else {
      if (store["users"]?.some(u => u.id !== target.id && u["email"] === token["new_email"])) throw new ApiError(409, "این ایمیل قبلاً استفاده شده است.");
      target["email"] = token["new_email"];
    }
    store["account_tokens"] = tokens.filter(t => t["user_id"] !== target.id || t["purpose"] !== purpose);
    localStorage.removeItem(SESSION_KEY);
    return result(undefined);
  }

  if (route === "/auth/login") {
    const user = store["users"]?.find((u) => u["email"] === String(body["email"]).trim().toLowerCase());
    if (!user || !await validPassword(user, body["password"])) throw new ApiError(401, "ایمیل یا رمز عبور نمایشی اشتباه است.");
    if (!user["is_active"]) throw new ApiError(423, "حساب نمایشی غیرفعال است.");
    return result({ token: `demo:${user.id}`, user });
  }
  if (route === "/auth/register") throw new ApiError(400, "در حالت نمایشی از حساب‌های آماده استفاده کنید.");
  const session = localStorage.getItem(SESSION_KEY);
  const userId = session ? (JSON.parse(session) as Row).id : null;
  const user = store["users"]?.find((u) => u.id === userId);
  if (!user || !user["is_active"]) throw new ApiError(401, "ابتدا وارد حساب نمایشی شوید.");
  if (route === "/auth/me") return result(user);
  if (route === "/auth/logout") return result(undefined);

  if ((route === "/auth/change-password" || route === "/auth/change-email") && method === "POST") {
    if (!await validPassword(user, body["current_password"])) throw new ApiError(400, "رمز فعلی اشتباه است.");
    if (route === "/auth/change-password") { await savePassword(user, body["new_password"]); return result(undefined); }
    const email = String(body["new_email"]).trim().toLowerCase();
    if (!email.includes("@")) throw new ApiError(400, "ایمیل معتبر وارد کنید.");
    if (store["users"]?.some(u => u["email"] === email)) throw new ApiError(409, "این ایمیل قبلاً استفاده شده است.");
    return result(newToken(user, "email", body["redirect_url"], { new_email: email }));
  }

  const accessible = (listId: unknown) => store["lists"]?.some((l) => l.id === listId && (l["owner_id"] === user.id || store["shares"]?.some((s) => s["list_id"] === l.id && s["user_id"] === user.id)));
  const ownsTask = (taskId: unknown) => store["tasks"]?.some((t) => t.id === taskId && accessible(t["list_id"]));
  if (parts[0] === "admin") {
    if (user["role"] !== "super_admin") throw new ApiError(403, "دسترسی مجاز نیست.");
    if (method === "GET") return result(store["users"]);
    const target = store["users"]?.find((u) => u.id === Number(parts[2]));
    if (!target) throw new ApiError(404, "کاربر یافت نشد.");
    Object.assign(target, body);
    return result(target);
  }
  if (parts[0] === "profiles") {
    if (parts[1] === "me") { if (typeof body["display_name"] === "string") user["display_name"] = body["display_name"].trim(); return result(user); }
    if (parts[1] === "lookup") {
      const search = (new URLSearchParams(query).get("search") ?? "").trim().toLowerCase();
      return result((store["users"] ?? []).filter((u) => `${u["email"] ?? ""} ${u["display_name"] ?? ""}`.toLowerCase().includes(search)));
    }
    return result(store["users"]);
  }
  if (route === "/shares") return result(store["shares"]?.filter((s) => accessible(s["list_id"])));
  if (parts[0] === "lists" && parts[2] === "shares") {
    const list = store["lists"]?.find((l) => l.id === Number(parts[1]) && l["owner_id"] === user.id);
    if (!list) throw new ApiError(404, "لیست یافت نشد.");
    if (method === "POST") store["shares"]?.push({ id: id(), list_id: list.id, user_id: body["user_id"] });
    else store["shares"] = (store["shares"] ?? []).filter((s) => !(s["list_id"] === list.id && s["user_id"] === Number(parts[3])));
    return result(undefined);
  }

  let collection = parts[0] ?? "";
  let recordId = parts[1] !== undefined ? Number(parts[1]) : undefined;
  const taskId = collection === "tasks" && parts[2] ? Number(parts[1]) : undefined;
  if (taskId !== undefined) { collection = parts[2] ?? ""; recordId = undefined; }
  if (collection === "notifications" && method === "GET" && (parts[1] === "unread" || parts[1] === "all")) {
    const mine = (store["notifications"] ?? []).filter((n) => n["user_id"] === user.id && (parts[1] === "all" || !n["read_at"]));
    return result([...mine].sort((a, b) => String(b["created_at"]).localeCompare(String(a["created_at"]))));
  }
  if (collection === "notifications" && (parts[1] === "read-all" || parts[2] === "read")) {
    for (const n of store["notifications"] ?? []) if (n["user_id"] === user.id && (parts[1] === "read-all" || n.id === recordId)) n["read_at"] = now();
    return result(undefined);
  }
  const rows = store[collection];
  if (!rows) throw new ApiError(404, "عملیات نمایشی پشتیبانی نمی‌شود.");
  const visible = (r: Row) => collection === "lists" ? accessible(r.id) : collection === "tasks" ? accessible(r["list_id"]) : collection === "notifications" ? r["user_id"] === user.id : ownsTask(r["task_id"]);
  if (taskId !== undefined && !ownsTask(taskId)) throw new ApiError(404, "کار یافت نشد.");
  if (method === "GET") return result(recordId !== undefined ? rows.find((r) => r.id === recordId && visible(r)) : rows.filter((r) => visible(r) && (taskId === undefined || r["task_id"] === taskId)));
  if (method === "POST") {
    if (collection === "tasks" && !accessible(body["list_id"])) throw new ApiError(404, "لیست یافت نشد.");
    const record: Row = { id: id(), created_at: now(), ...body };
    if (collection === "lists") record["owner_id"] = user.id;
    if (collection === "tasks") Object.assign(record, { created_by: user.id, notes: null, is_completed: false, completed_at: null });
    if (collection === "notifications") Object.assign(record, { user_id: user.id, read_at: null });
    if (taskId !== undefined) Object.assign(record, { task_id: taskId, created_by: user.id, is_completed: false });
    if (collection === "attachments") {
      const file = options.formData?.get("file");
      if (!(file instanceof File)) throw new ApiError(400, "فایل انتخاب نشده است.");
      Object.assign(record, { file_name: file.name, mime_type: file.type, size_bytes: file.size, storage_path: String(record.id), uploaded_by: user.id });
      files.set(record.id, file);
    }
    rows.push(record);
    return result(record);
  }
  const record = rows.find((r) => r.id === recordId && visible(r));
  if (!record) throw new ApiError(404, "رکورد یافت نشد.");
  if (method === "DELETE") {
    store[collection] = rows.filter((r) => r.id !== recordId);
    if (collection === "lists") store["tasks"] = (store["tasks"] ?? []).filter((t) => t["list_id"] !== recordId);
    files.delete(record.id);
    return result(undefined);
  }
  // PATCH/PUT — translate the backend's clearing conventions like the real API does.
  const patch = { ...body };
  if (patch["clear_due_date"] === true) { delete patch["clear_due_date"]; patch["due_date"] = null; }
  if (patch["assigned_to"] === 0) patch["assigned_to"] = null;
  Object.assign(record, patch);
  return result(record);
}

export function mockAttachmentUrl(attachmentId: number): string {
  const file = files.get(attachmentId);
  if (!file) throw new Error("فایل نمایشی پس از بازخوانی صفحه در دسترس نیست؛ دوباره آن را اضافه کنید.");
  return URL.createObjectURL(file);
}
