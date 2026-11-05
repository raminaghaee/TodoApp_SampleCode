import { apiFetch, getStoredUser } from "@/lib/api";

export type AppRole = "super_admin" | "user";

export type UserRoleRow = {
  id: number;
  user_id: number;
  role: AppRole;
};

export type ManagedUser = {
  id: number;
  email: string | null;
  display_name: string | null;
  is_active: boolean;
  created_at: string;
  role: AppRole;
};

export async function checkSuperAdmin(userId: number): Promise<boolean> {
  try {
    const me = await apiFetch<{ id: number; role?: AppRole }>("/auth/me");
    return me.role === "super_admin";
  } catch {
    return getStoredUser()?.role === "super_admin";
  }
}

export async function fetchManagedUsers(): Promise<ManagedUser[]> {
  return apiFetch<ManagedUser[]>("/admin/users");
}

export async function setUserActive(userId: number, isActive: boolean) {
  await apiFetch(`/admin/users/${userId}`, { method: "PATCH", body: { is_active: isActive } });
}

export async function setUserRole(userId: number, role: AppRole) {
  await apiFetch(`/admin/users/${userId}/role`, { method: "PUT", body: { role } });
}

export const ROLE_LABELS: Record<AppRole, string> = {
  super_admin: "سوپرادمین",
  user: "کاربر",
};

/** Human readable description of what each role can reach in the app. */
export const ROLE_ACCESS: Record<AppRole, string[]> = {
  super_admin: [
    "مدیریت کاربران (فعال/غیرفعال)",
    "تنظیم نقش کاربران",
    "مشاهده همه پروفایل‌ها",
    "همه امکانات کاربر عادی",
  ],
  user: [
    "ساخت و مدیریت لیست‌های خود",
    "ساخت، ویرایش و حذف کارها",
    "ارجاع و اشتراک‌گذاری لیست‌ها",
    "دریافت نوتیفیکیشن",
  ],
};
