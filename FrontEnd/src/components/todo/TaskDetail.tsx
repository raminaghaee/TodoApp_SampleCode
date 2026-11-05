import { Sun, Star, Trash2, CalendarDays } from "lucide-react";

import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { profileLabel, type TaskList, type User, type TodoItem } from "@/lib/todo";
import { TaskSubtasks } from "@/components/todo/TaskSubtasks";
import { TaskAttachments } from "@/components/todo/TaskAttachments";

const UNASSIGNED = "__none__";

type Props = {
  task: TodoItem | null;
  lists: TaskList[];
  members: User[];
  userId: number;
  onClose: () => void;
  onPatch: (patch: Partial<TodoItem>) => Promise<boolean>;
  onDelete: () => void;
  readOnly?: boolean;
};

export function TaskDetail({ task, lists, members, userId, onClose, onPatch, onDelete, readOnly = false }: Props) {
  const patch = (value: Partial<TodoItem>) => readOnly ? Promise.resolve(false) : onPatch(value);
  return (
    <Sheet open={!!task} onOpenChange={(o) => !o && onClose()}>
      <SheetContent side="left" className="w-full gap-0 overflow-y-auto sm:max-w-md">
        {task && (
          <>
            <SheetHeader>
              <SheetTitle className="text-start">جزئیات کار</SheetTitle>
            </SheetHeader>

            <div className="space-y-5 px-4 pb-6">
              {readOnly && (
                <p className="rounded-lg border border-border bg-muted px-3 py-2 text-xs text-muted-foreground">
                  فقط مالک لیست، مسئول کار یا سازندهٔ کارِ بدون مسئول اجازهٔ تغییر دارند.
                </p>
              )}
              <fieldset disabled={readOnly} className="space-y-5 disabled:opacity-70">
              <div className="space-y-2">
                <Label htmlFor="title">عنوان</Label>
                <Input
                  id="title"
                  defaultValue={task.title}
                  key={`${task.id}-title`}
                  onBlur={async (e) => {
                    if (readOnly) return;
                    const input = e.currentTarget;
                    const v = input.value.trim();
                    if (!v || (v !== task.title && !await patch({ title: v }))) input.value = task.title;
                  }}
                />
              </div>

              <div className="flex gap-2">
                <Button
                  variant={task.is_my_day ? "default" : "outline"}
                  size="sm"
                  className="flex-1 gap-2"
                  onClick={() => patch({ is_my_day: !task.is_my_day })}
                >
                  <Sun className="size-4" />
                  روز من
                </Button>
                <Button
                  variant={task.is_important ? "default" : "outline"}
                  size="sm"
                  className="flex-1 gap-2"
                  onClick={() => patch({ is_important: !task.is_important })}
                >
                  <Star className="size-4" />
                  مهم
                </Button>
              </div>

              <div className="space-y-2">
                <Label htmlFor="list">لیست</Label>
                <Select
                  disabled={readOnly}
                  value={String(task.list_id)}
                  onValueChange={(v) => patch({ list_id: Number(v) })}
                >
                  <SelectTrigger id="list">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {lists.map((l) => (
                      <SelectItem key={l.id} value={String(l.id)}>
                        {l.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="assignee">ارجاع به</Label>
                <Select
                  disabled={readOnly}
                  value={task.assigned_to !== null ? String(task.assigned_to) : UNASSIGNED}
                  onValueChange={(v) => patch({ assigned_to: v === UNASSIGNED ? null : Number(v) })}
                >
                  <SelectTrigger id="assignee">
                    <SelectValue placeholder="بدون مسئول" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={UNASSIGNED}>بدون مسئول</SelectItem>
                    {members.map((m) => (
                      <SelectItem key={m.id} value={String(m.id)}>
                        {profileLabel(m)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground">
                  فقط کاربرانی که لیست با آن‌ها به اشتراک گذاشته شده در این فهرست دیده می‌شوند.
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="due" className="flex items-center gap-2">
                  <CalendarDays className="size-4" />
                  سررسید
                </Label>
                <Input
                  id="due"
                  type="date"
                  dir="ltr"
                  value={task.due_date ?? ""}
                  onChange={(e) => patch({ due_date: e.target.value || null })}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="notes">یادداشت</Label>
                <Textarea
                  id="notes"
                  rows={5}
                  key={`${task.id}-notes`}
                  defaultValue={task.notes ?? ""}
                  onBlur={async (e) => {
                    if (readOnly) return;
                    const input = e.currentTarget;
                    const v = input.value;
                    if (v !== (task.notes ?? "") && !await patch({ notes: v || null })) input.value = task.notes ?? "";
                  }}
                  placeholder="جزئیات، لینک‌ها یا مراحل انجام کار…"
                />
              </div>

              </fieldset>
              <div className="h-px bg-border" />
              <TaskSubtasks key={task.id} taskId={task.id} userId={userId} readOnly={readOnly} />

              <div className="h-px bg-border" />
              <TaskAttachments taskId={task.id} userId={userId} readOnly={readOnly} />

              <div className="h-px bg-border" />



              {!readOnly && (
                <Button variant="ghost" className="w-full gap-2 text-destructive" onClick={() => { if (!readOnly) onDelete(); }}>
                  <Trash2 className="size-4" />
                  حذف کار
                </Button>
              )}
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}
