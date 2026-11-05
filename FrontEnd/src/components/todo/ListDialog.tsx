import { useEffect, useState } from "react";
import { Check, ListPlus, Pencil } from "lucide-react";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { LIST_COLORS } from "@/lib/todo";
import { cn } from "@/lib/utils";

export type ListDialogValue = { name: string; color: string };

type Props = {
  open: boolean;
  mode: "create" | "edit";
  initial?: ListDialogValue;
  onClose: () => void;
  onSubmit: (value: ListDialogValue) => Promise<void> | void;
};

/** Lookup dialog for creating or renaming a task list (name + color). */
export function ListDialog({ open, mode, initial, onClose, onSubmit }: Props) {
  const [name, setName] = useState(initial?.name ?? "");
  const [color, setColor] = useState(initial?.color ?? "blue");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setName(initial?.name ?? "");
      setColor(initial?.color ?? "blue");
      setSaving(false);
    }
  }, [open, initial?.name, initial?.color]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed || saving) return;
    setSaving(true);
    try {
      await onSubmit({ name: trimmed, color });
    } finally {
      setSaving(false);
    }
  }

  const Icon = mode === "create" ? ListPlus : Pencil;

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-start">
            <Icon className="size-5 text-primary" />
            {mode === "create" ? "لیست کار جدید" : "ویرایش لیست"}
          </DialogTitle>
          <DialogDescription className="text-start">
            نام لیست و رنگ آن را انتخاب کنید.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={submit} className="space-y-5">
          <div className="space-y-2">
            <Label htmlFor="list-name">نام لیست</Label>
            <Input
              id="list-name"
              autoFocus
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="مثلاً پروژه فروش"
              maxLength={80}
            />
          </div>

          <div className="space-y-2">
            <Label>رنگ</Label>
            <div className="flex flex-wrap gap-2">
              {LIST_COLORS.map((c) => (
                <button
                  key={c.key}
                  type="button"
                  onClick={() => setColor(c.key)}
                  aria-label={c.label}
                  title={c.label}
                  className={cn(
                    "flex size-9 items-center justify-center rounded-full border-2 transition-colors",
                    color === c.key ? "border-primary" : "border-transparent",
                  )}
                >
                  <span
                    className={cn(
                      "flex size-6 items-center justify-center rounded-full",
                      c.className,
                    )}
                  >
                    {color === c.key && <Check className="size-3.5 text-primary-foreground" />}
                  </span>
                </button>
              ))}
            </div>
          </div>

          <DialogFooter className="gap-2 sm:justify-start">
            <Button type="submit" disabled={!name.trim() || saving} className="gap-2">
              <Check className="size-4" />
              {mode === "create" ? "ایجاد لیست" : "ذخیره"}
            </Button>
            <Button type="button" variant="ghost" onClick={onClose}>
              انصراف
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
