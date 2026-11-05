import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Clock, Trash2, UserCheck } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { confirmDelete } from "@/components/shared/ConfirmDialog";
import { errorMessage } from "@/lib/api-errors";
import { deleteNotification, fetchAllNotifications, markNotificationRead } from "@/lib/notifications";
import { cn } from "@/lib/utils";

/** Full notification history (read + unread) from its own endpoint. */
export function NotificationHistory() {
  const queryClient = useQueryClient();
  const q = useQuery({ queryKey: ["notifications", "all"], queryFn: fetchAllNotifications });
  const items = q.data ?? [];
  const refresh = () => queryClient.invalidateQueries({ queryKey: ["notifications"] });

  async function run(action: () => Promise<void>, ok: string, fail: string) {
    try { await action(); if (ok) toast.success(ok); } catch (e) { toast.error(errorMessage(e, fail)); }
    refresh();
  }

  return (
    <section className="rounded-xl border border-border bg-card p-5">
      <h1 className="mb-1 text-lg font-semibold">همه اعلان‌ها</h1>
      <p className="mb-4 text-sm text-muted-foreground">اعلان‌های خوانده‌شده و خوانده‌نشده</p>
      {q.isLoading ? (
        <p className="py-8 text-center text-sm text-muted-foreground">در حال بارگذاری…</p>
      ) : q.isError ? (
        <p className="py-8 text-center text-sm text-destructive">{errorMessage(q.error, "دریافت اعلان‌ها ناموفق بود.")}</p>
      ) : items.length === 0 ? (
        <p className="py-8 text-center text-sm text-muted-foreground">اعلانی وجود ندارد.</p>
      ) : (
        <ul className="divide-y divide-border">
          {items.map((n) => {
            const Icon = n.type === "assigned" ? UserCheck : Clock;
            return (
              <li key={n.id} className={cn("flex items-start gap-3 py-3", !n.read_at && "font-medium")}>
                <Icon className="mt-0.5 size-4 shrink-0 text-primary" />
                <div className="min-w-0 flex-1">
                  <p className="text-sm">{n.title}</p>
                  {n.body && <p className="text-xs font-normal text-muted-foreground">{n.body}</p>}
                  <p className="mt-1 text-[11px] font-normal text-muted-foreground">
                    {new Date(n.created_at).toLocaleString("fa-IR")} · {n.read_at ? "خوانده‌شده" : "خوانده‌نشده"}
                  </p>
                </div>
                {!n.read_at && (
                  <Button variant="ghost" size="icon" className="size-8" title="خوانده شد" aria-label={`خواندن ${n.title}`}
                    onClick={() => run(() => markNotificationRead(n.id), "", "خواندن اعلان ناموفق بود.")}>
                    <Check className="size-4 text-primary" />
                  </Button>
                )}
                <Button variant="ghost" size="icon" className="size-8" title="حذف اعلان" aria-label={`حذف ${n.title}`}
                  onClick={async () => {
                    if (await confirmDelete({ description: `اعلان «${n.title}» حذف شود؟`, hint: "این عمل قابل بازگشت نیست." }))
                      run(() => deleteNotification(n.id), "اعلان حذف شد.", "حذف اعلان ناموفق بود.");
                  }}>
                  <Trash2 className="size-4 text-destructive" />
                </Button>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
