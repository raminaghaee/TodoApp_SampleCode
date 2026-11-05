import { confirmDelete } from "@/components/shared/ConfirmDialog";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, ListChecks, Pencil, Plus, Trash2, X } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";
import { errorMessage } from "@/lib/api-errors";
import { assertTaskWritable } from "@/lib/task-access";
import {
  createSubtask,
  deleteSubtask,
  fetchSubtasks,
  updateSubtask,
} from "@/lib/task-details";

type Props = { taskId: number; userId: number; readOnly?: boolean };

export function TaskSubtasks({ taskId, userId, readOnly = false }: Props) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editTitle, setEditTitle] = useState("");

  const q = useQuery({
    queryKey: ["subtasks", taskId],
    queryFn: () => fetchSubtasks(taskId),
  });
  const subtasks = q.data ?? [];
  const doneCount = subtasks.filter((s) => s.is_completed).length;

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["subtasks", taskId] });

  const add = useMutation({
    mutationFn: async (t: string) => {
      assertTaskWritable(readOnly);
      return createSubtask({ taskId, createdBy: userId, title: t, position: subtasks.length });
    },
    onSuccess: () => {
      setTitle("");
      invalidate();
    },
    onError: (error) => toast.error(errorMessage(error, "افزودن زیرتسک انجام نشد.")),
  });

  const toggle = useMutation({
    mutationFn: async (v: { id: number; is_completed: boolean }) => {
      assertTaskWritable(readOnly);
      return updateSubtask(v.id, { is_completed: v.is_completed });
    },
    onSuccess: invalidate,
    onError: (error) => toast.error(errorMessage(error, "ذخیره زیرتسک انجام نشد.")),
  });

  const remove = useMutation({
    mutationFn: async (id: number) => {
      assertTaskWritable(readOnly);
      return deleteSubtask(id);
    },
    onSuccess: invalidate,
    onError: (error) => toast.error(errorMessage(error, "حذف زیرتسک انجام نشد.")),
  });

  const edit = useMutation({
    mutationFn: async (input: { id: number; title: string }) => {
      assertTaskWritable(readOnly);
      return updateSubtask(input.id, { title: input.title });
    },
    onSuccess: () => {
      setEditingId(null);
      invalidate();
    },
    onError: (error) => toast.error(errorMessage(error, "ویرایش زیرتسک انجام نشد.")),
  });

  return (
    <div className="space-y-3">
      <Label className="flex items-center gap-2">
        <ListChecks className="size-4" />
        زیرتسک‌ها
        {subtasks.length > 0 && (
          <span className="text-xs font-normal text-muted-foreground">
            {doneCount} از {subtasks.length}
          </span>
        )}
      </Label>

      {subtasks.length > 0 && (
        <Progress value={(doneCount / subtasks.length) * 100} className="h-1.5" />
      )}

      <div className="space-y-1">
        {subtasks.map((s) => (
          <div
            key={s.id}
            className="group flex items-center gap-3 rounded-lg border border-border bg-card px-3 py-2"
          >
            <Checkbox
              checked={s.is_completed}
              disabled={readOnly || toggle.isPending || edit.isPending || remove.isPending}
              onCheckedChange={(v) => toggle.mutate({ id: s.id, is_completed: !!v })}
              aria-label="انجام شد"
              className="size-4 rounded-full"
            />
            {!readOnly && editingId === s.id ? (
              <form
                className="flex min-w-0 flex-1 items-center gap-1"
                onSubmit={(event) => {
                  event.preventDefault();
                  const value = editTitle.trim();
                  if (!readOnly && value && !edit.isPending) edit.mutate({ id: s.id, title: value });
                }}
              >
                <Input
                  autoFocus
                  aria-label="عنوان زیرتسک"
                  value={editTitle}
                  onChange={(event) => setEditTitle(event.target.value)}
                  disabled={edit.isPending}
                  className="h-8 min-w-0 flex-1"
                />
                <Button type="submit" variant="ghost" size="icon" className="size-8 shrink-0" aria-label="ذخیره زیرتسک" title="ذخیره زیرتسک" disabled={!editTitle.trim() || edit.isPending}>
                  <Check className="size-4" />
                </Button>
                <Button type="button" variant="ghost" size="icon" className="size-8 shrink-0" aria-label="لغو ویرایش زیرتسک" title="لغو ویرایش زیرتسک" disabled={edit.isPending} onClick={() => setEditingId(null)}>
                  <X className="size-4" />
                </Button>
              </form>
            ) : <span
              className={cn(
                "min-w-0 flex-1 truncate text-sm",
                s.is_completed && "text-muted-foreground line-through",
              )}
            >
              {s.title}
            </span>}
            {!readOnly && editingId !== s.id && <Button
              type="button"
              variant="ghost"
              size="icon"
              className="size-8 shrink-0"
              aria-label={`ویرایش ${s.title}`}
              title="ویرایش زیرتسک"
              disabled={edit.isPending || remove.isPending}
              onClick={() => { setEditingId(s.id); setEditTitle(s.title); }}
            >
              <Pencil className="size-3.5" />
            </Button>}
            {!readOnly && <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={async () => { if (await confirmDelete({ description: `زیرتسک «${s.title}» حذف شود؟`, hint: "این عمل قابل بازگشت نیست." })) remove.mutate(s.id); }}
              aria-label={`حذف ${s.title}`}
              title="حذف زیرتسک"
              disabled={remove.isPending || edit.isPending}
              className="size-8 shrink-0"
            >
              <Trash2 className="size-3.5 text-destructive" />
            </Button>}
          </div>
        ))}
      </div>

      {!readOnly && <form
        onSubmit={(e) => {
          e.preventDefault();
          const t = title.trim();
            if (!readOnly && t && !add.isPending) add.mutate(t);
        }}
        className="flex gap-2"
      >
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="افزودن مرحله…"
          className="h-9"
        />
        <Button type="submit" size="icon" variant="outline" disabled={!title.trim() || add.isPending} aria-label="افزودن مرحله">
          <Plus className="size-4" />
        </Button>
      </form>}
    </div>
  );
}
