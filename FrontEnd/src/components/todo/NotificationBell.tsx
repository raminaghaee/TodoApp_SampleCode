import { useState } from "react";
import { Link } from "@tanstack/react-router";
import { Bell, Check, CheckCheck, ChevronLeft, Clock, History, Trash2, UserCheck } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { confirmDelete } from "@/components/shared/ConfirmDialog";
import type { Notification } from "@/lib/notifications";

type Props = {
  /** Unread notifications only. */
  notifications: Notification[];
  onRead: (id: number) => void;
  onReadAll: () => void;
  onDelete: (id: number) => void;
  /** Jumps to the task a notification points at; the panel closes first. */
  onOpenTask?: (notification: Notification) => void;
};

export function NotificationBell({ notifications, onRead, onReadAll, onDelete, onOpenTask }: Props) {
  const [open, setOpen] = useState(false);
  const unread = notifications.filter((n) => !n.read_at);
  const count = unread.length;

  async function remove(n: Notification) {
    setOpen(false);
    if (await confirmDelete({ description: `اعلان «${n.title}» حذف شود؟`, hint: "این عمل قابل بازگشت نیست." })) onDelete(n.id);
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" size="icon" className="relative" aria-label="نوتیفیکیشن‌ها">
          <Bell className="size-4" />
          {count > 0 && (
            <span className="absolute -end-1 -top-1 flex size-4 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-destructive-foreground">
              {count > 9 ? "9+" : count}
            </span>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80 p-0">
        <div className="flex items-center justify-between border-b border-border px-4 py-2.5">
          <p className="text-sm font-medium">اعلان‌های خوانده‌نشده</p>
          {count > 0 && (
            <button onClick={onReadAll} className="flex items-center gap-1 text-xs text-primary hover:underline">
              <CheckCheck className="size-3.5" />
              خواندن همه
            </button>
          )}
        </div>
        <div className="max-h-[22rem] overflow-y-auto">
          {count === 0 ? (
            <p className="px-4 py-8 text-center text-sm text-muted-foreground">اعلان خوانده‌نشده‌ای وجود ندارد.</p>
          ) : (
            <ul className="divide-y divide-border">
              {unread.map((n) => {
                const Icon = n.type === "assigned" ? UserCheck : Clock;
                const canOpen = n.task_id != null && !!onOpenTask;
                return (
                  <li key={n.id} className="flex items-start gap-1 bg-primary/5 pe-2">
                    <button
                      onClick={() => {
                        if (canOpen) { setOpen(false); onOpenTask?.(n); } else onRead(n.id);
                      }}
                      aria-label={canOpen ? "رفتن به این کار" : undefined}
                      className="flex min-w-0 flex-1 items-start gap-3 px-4 py-3 text-start transition-colors hover:bg-muted/60"
                    >
                      <Icon className="mt-0.5 size-4 shrink-0 text-primary" />
                      <span className="min-w-0 flex-1">
                        <span className="block text-sm font-medium">{n.title}</span>
                        {n.body && <span className="block truncate text-xs text-muted-foreground">{n.body}</span>}
                        {canOpen && <span className="mt-1 block text-[11px] text-primary">کلیک کن تا به این کار بروی</span>}
                      </span>
                      {canOpen && <ChevronLeft className="mt-1 size-4 shrink-0 text-muted-foreground" />}
                    </button>
                    <div className="flex shrink-0 flex-col gap-1 py-2">
                      <Button variant="ghost" size="icon" className="size-7" title="خوانده شد" aria-label={`خواندن ${n.title}`} onClick={() => onRead(n.id)}>
                        <Check className="size-3.5 text-primary" />
                      </Button>
                      <Button variant="ghost" size="icon" className="size-7" title="حذف اعلان" aria-label={`حذف ${n.title}`} onClick={() => remove(n)}>
                        <Trash2 className="size-3.5 text-destructive" />
                      </Button>
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
        <div className="border-t border-border p-1.5">
          <Button asChild variant="ghost" size="sm" className="w-full gap-2" onClick={() => setOpen(false)}>
            <Link to="/profile" search={{ tab: "notifications" }}>
              <History className="size-4" />
              همه اعلان‌ها
            </Link>
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}
