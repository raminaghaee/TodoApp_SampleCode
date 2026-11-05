import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRight, RefreshCw, ShieldCheck, Users } from "lucide-react";
import { toast } from "sonner";

import { Toolbar, type FilterValues } from "@/components/shared/Toolbar";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { getCurrentUser } from "@/lib/auth";
import {
  ROLE_ACCESS,
  ROLE_LABELS,
  checkSuperAdmin,
  fetchManagedUsers,
  setUserActive,
  setUserRole,
  type AppRole,
} from "@/lib/admin";

export const Route = createFileRoute("/_authenticated/admin")({
  head: () => ({
    meta: [
      { title: "پنل سوپرادمین | Todo" },
      { name: "description", content: "مدیریت کاربران، نقش‌ها و وضعیت دسترسی‌ها در Todo." },
      { property: "og:title", content: "پنل سوپرادمین | Todo" },
      { property: "og:description", content: "فعال/غیرفعال کردن کاربران و تنظیم نقش‌ها." },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary" },
    ],
  }),
  component: AdminPage,
});

function AdminPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [userId, setUserId] = useState<number | null>(null);
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null);
  const [search, setSearch] = useState("");
  const [filters, setFilters] = useState<FilterValues>({});

  useEffect(() => {
    getCurrentUser().then(async (user) => {
      if (!user) return;
      setUserId(user.id);
      setIsAdmin(await checkSuperAdmin(user.id));
    });
  }, []);

  const usersQ = useQuery({
    queryKey: ["managed-users"],
    queryFn: fetchManagedUsers,
    enabled: isAdmin === true,
  });

  const users = usersQ.data ?? [];

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase();
    return users.filter((u) => {
      if (q && !`${u.display_name ?? ""} ${u.email ?? ""}`.toLowerCase().includes(q)) return false;
      if (filters["role"] && u.role !== filters["role"]) return false;
      if (filters["status"]) {
        const active = filters["status"] === "active";
        if (u.is_active !== active) return false;
      }
      return true;
    });
  }, [users, search, filters]);

  async function toggleActive(id: number, next: boolean) {
    try {
      await setUserActive(id, next);
      queryClient.invalidateQueries({ queryKey: ["managed-users"] });
      toast.success(next ? "کاربر فعال شد." : "کاربر غیرفعال شد.");
    } catch {
      toast.error("تغییر وضعیت کاربر انجام نشد.");
    }
  }

  async function changeRole(id: number, role: AppRole) {
    try {
      await setUserRole(id, role);
      queryClient.invalidateQueries({ queryKey: ["managed-users"] });
      toast.success("نقش کاربر به‌روزرسانی شد.");
    } catch {
      toast.error("تغییر نقش انجام نشد.");
    }
  }

  if (isAdmin === false) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-8 text-center">
        <ShieldCheck className="size-10 text-muted-foreground" />
        <h1 className="text-xl font-bold">دسترسی محدود</h1>
        <p className="text-sm text-muted-foreground">این بخش فقط برای سوپرادمین قابل مشاهده است.</p>
        <Button onClick={() => navigate({ to: "/tasks" })} className="gap-2">
          <ArrowRight className="size-4" />
          بازگشت به کارها
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-6xl space-y-5 px-5 py-6 sm:px-8">
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink href="/tasks">کارها</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>پنل سوپرادمین</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>

        <Toolbar
          title="مدیریت کاربران"
          description={`${visible.length} کاربر از ${users.length} کاربر`}
          icon={Users}
          search={{ value: search, onChange: setSearch, placeholder: "جستجوی نام یا ایمیل…" }}
          filters={[
            {
              key: "role",
              label: "نقش",
              type: "select",
              options: [
                { value: "super_admin", label: ROLE_LABELS.super_admin },
                { value: "user", label: ROLE_LABELS.user },
              ],
            },
            {
              key: "status",
              label: "وضعیت",
              type: "select",
              options: [
                { value: "active", label: "فعال" },
                { value: "inactive", label: "غیرفعال" },
              ],
            },
          ]}
          filterValues={filters}
          onFilterChange={(k, v) => setFilters((f) => ({ ...f, [k]: v }))}
          onClearFilters={() => setFilters({})}
          actions={[
            {
              key: "refresh",
              label: "بازخوانی",
              icon: RefreshCw,
              group: "general",
              onClick: () => queryClient.invalidateQueries({ queryKey: ["managed-users"] }),
            },
          ]}
          leading={
            <Button
              variant="ghost"
              size="icon"
              aria-label="بازگشت"
              onClick={() => navigate({ to: "/tasks" })}
            >
              <ArrowRight className="size-5" />
            </Button>
          }
        />

        <div className="overflow-hidden rounded-2xl border border-border bg-card shadow-panel">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="text-start">کاربر</TableHead>
                <TableHead className="text-start">ایمیل</TableHead>
                <TableHead className="text-start">نقش</TableHead>
                <TableHead className="text-start">وضعیت</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visible.map((u) => (
                <TableRow key={u.id}>
                  <TableCell className="font-medium">
                    {u.display_name || "بدون نام"}
                    {u.id === userId && (
                      <span className="ms-2 text-xs text-muted-foreground">(شما)</span>
                    )}
                  </TableCell>
                  <TableCell className="text-muted-foreground">{u.email ?? "—"}</TableCell>
                  <TableCell>
                    <Select
                      value={u.role}
                      onValueChange={(v) => changeRole(u.id, v as AppRole)}
                      disabled={u.id === userId}
                    >
                      <SelectTrigger className="h-9 w-36">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="super_admin">{ROLE_LABELS.super_admin}</SelectItem>
                        <SelectItem value="user">{ROLE_LABELS.user}</SelectItem>
                      </SelectContent>
                    </Select>
                  </TableCell>
                  <TableCell className="w-32 whitespace-nowrap">
                    <div className="flex min-w-24 items-center gap-3">
                      <Switch
                        checked={u.is_active}
                        onCheckedChange={(v) => toggleActive(u.id, v)}
                        disabled={u.id === userId}
                        aria-label={`وضعیت کاربر ${u.display_name || u.email || "بدون نام"}`}
                      />
                      <span className="min-w-12 text-sm text-muted-foreground">
                        {u.is_active ? "فعال" : "غیرفعال"}
                      </span>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
              {visible.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} className="py-10 text-center text-sm text-muted-foreground">
                    کاربری یافت نشد.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {(["super_admin", "user"] as AppRole[]).map((role) => (
            <div key={role} className="rounded-2xl border border-border bg-card p-5 shadow-panel">
              <h2 className="flex items-center gap-2 font-bold">
                <ShieldCheck className="size-4 text-primary" />
                دسترسی‌های {ROLE_LABELS[role]}
              </h2>
              <ul className="mt-3 space-y-1.5 text-sm text-muted-foreground">
                {ROLE_ACCESS[role].map((a) => (
                  <li key={a}>• {a}</li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
