import { useState } from "react";
import { Loader2, Search, UserPlus, X } from "lucide-react";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  initials,
  searchProfiles,
  profileLabel,
  type TaskList,
  type User,
} from "@/lib/todo";

type Props = {
  open: boolean;
  list: TaskList | null;
  isOwner: boolean;
  profiles: User[];
  sharedUserIds: number[];
  onClose: () => void;
  onShare: (userId: number) => void;
  onUnshare: (userId: number) => void;
};

export function ShareDialog({
  open,
  list,
  isOwner,
  profiles,
  sharedUserIds,
  onClose,
  onShare,
  onUnshare,
}: Props) {
  const [query, setQuery] = useState("");
  // ✅ تغییر: استیت به آرایه تغییر کرد
  const [lookupResults, setLookupResults] = useState<User[]>([]);
  const [lookupState, setLookupState] = useState<"idle" | "loading" | "done">(
    "idle",
  );

  const runLookup = async () => {
    const email = query.trim().toLowerCase();
    setLookupState("loading");
    try {
      const results = await searchProfiles(email);
      // ✅ تغییر: همه نتایج بک‌اند را ذخیره می‌کنیم
      setLookupResults(results);
      setLookupState("done");
    } catch {
      setLookupResults([]);
      setLookupState("done");
    }
  };

  const shared = profiles.filter((p) => sharedUserIds.includes(p.id));

  // کاربرانی که از قبل در سیستم می‌شناسیم و با عبارت جستجو مطابقت دارند
  const localCandidates = profiles.filter(
    (p) =>
      p.id !== list?.owner_id &&
      !sharedUserIds.includes(p.id) &&
      (query.trim() === "" ||
        profileLabel(p).includes(query.trim()) ||
        (p.email ?? "").toLowerCase().includes(query.trim().toLowerCase())),
  );

  // ✅ ادغام نتایج جستجوی سرور و نتایج محلی، بدون تکرار
  const allCandidates = [
    ...lookupResults.filter(
      (r) => r.id !== list?.owner_id && !sharedUserIds.includes(r.id),
    ),
    ...localCandidates.filter(
      (c) => !lookupResults.some((r) => r.id === c.id), // جلوگیری از نمایش تکراری
    ),
  ];

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-start">
            اشتراک‌گذاری «{list?.name}»
          </DialogTitle>
          <DialogDescription className="text-start">
            کاربران انتخاب‌شده می‌توانند کارهای این لیست را ببینند، ویرایش کنند
            و کار جدید اضافه کنند.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div>
            <p className="mb-2 text-xs font-medium text-muted-foreground">
              اعضای فعلی
            </p>
            {shared.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                هنوز با کسی به اشتراک گذاشته نشده.
              </p>
            ) : (
              <ul className="space-y-2">
                {shared.map((p) => (
                  <li
                    key={p.id}
                    className="flex items-center gap-3 rounded-lg border border-border px-3 py-2"
                  >
                    <span className="flex size-7 items-center justify-center rounded-full bg-primary-soft text-xs font-bold text-accent-foreground">
                      {initials(p)}
                    </span>
                    <span className="min-w-0 flex-1 truncate text-sm">
                      {profileLabel(p)}
                    </span>
                    {isOwner && (
                      <button
                        onClick={() => onUnshare(p.id)}
                        aria-label="حذف دسترسی"
                      >
                        <X className="size-4 text-destructive" />
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>

          {isOwner && (
            <div>
              <p className="mb-2 text-xs font-medium text-muted-foreground">
                افزودن کاربر
              </p>
              <div className="flex gap-2">
                <Input
                  value={query}
                  onChange={(e) => {
                    setQuery(e.target.value);
                    setLookupResults([]); // پاک کردن نتایج قبلی هنگام تایپ
                    setLookupState("idle");
                  }}
                  placeholder="نام همکار یا ایمیل دقیق کاربر…"
                />
                <Button
                  variant="outline"
                  size="icon"
                  className="size-10 shrink-0"
                  onClick={runLookup}
                  disabled={lookupState === "loading" || !query.trim()}
                  aria-label="جستجوی کاربر"
                  title="جستجوی کاربر"
                >
                  {lookupState === "loading" ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Search className="size-4" />
                  )}
                </Button>
              </div>
              <ul className="mt-2 max-h-48 space-y-1 overflow-y-auto">
                {/* ✅ نمایش لیست ادغام شده */}
                {allCandidates.map((p) => (
                  <li key={p.id}>
                    <Button
                      variant="ghost"
                      className="w-full justify-start gap-2"
                      onClick={() => onShare(p.id)}
                    >
                      <UserPlus className="size-4" />
                      <span className="truncate">{profileLabel(p)}</span>
                      <span
                        className="ms-auto truncate text-xs text-muted-foreground"
                        dir="ltr"
                      >
                        {p.email}
                      </span>
                    </Button>
                  </li>
                ))}

                {allCandidates.length === 0 && lookupState === "done" && (
                  <li className="px-2 py-1 text-sm text-muted-foreground">
                    کاربری یافت نشد.
                  </li>
                )}
              </ul>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
