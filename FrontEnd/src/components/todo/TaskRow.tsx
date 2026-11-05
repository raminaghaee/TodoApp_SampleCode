import { Star, Sun, CalendarDays, UserCheck } from "lucide-react";

import { Checkbox } from "@/components/ui/checkbox";
import { cn } from "@/lib/utils";
import { initials, profileLabel, type TaskList, type User, type TodoItem } from "@/lib/todo";

type Props = {
  task: TodoItem;
  list?: TaskList | undefined;
  assignee?: User | undefined;
  showList: boolean;
  selected: boolean;
  onToggleComplete: () => void;
  onToggleImportant: () => void;
  onOpen: () => void;
  readOnly?: boolean;
};

export function TaskRow({
  task,
  list,
  assignee,
  showList,
  selected,
  onToggleComplete,
  onToggleImportant,
  onOpen,
  readOnly = false,
}: Props) {
  return (
    <div
      className={cn(
        "flex items-center gap-3 rounded-xl border bg-card px-4 py-3 shadow-panel transition-colors",
        selected ? "border-primary" : "border-border hover:border-primary/40",
      )}
    >
      <Checkbox
        checked={task.is_completed}
        onCheckedChange={onToggleComplete}
        disabled={readOnly}
        aria-label="انجام شد"
        className="size-5 rounded-full"
      />
      <button onClick={onOpen} className="min-w-0 flex-1 text-start">
        <span
          className={cn(
            "block truncate text-sm",
            task.is_completed && "text-muted-foreground line-through",
          )}
        >
          {task.title}
        </span>
        <span className="mt-1 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
          {showList && list && <span>{list.name}</span>}
          {task.is_my_day && (
            <span className="flex items-center gap-1">
              <Sun className="size-3" /> روز من
            </span>
          )}
          {task.due_date && (
            <span className="flex items-center gap-1">
              <CalendarDays className="size-3" />
              {new Date(task.due_date).toLocaleDateString("fa-IR")}
            </span>
          )}
          {assignee && (
            <span className="flex items-center gap-1">
              <UserCheck className="size-3" />
              {profileLabel(assignee)}
            </span>
          )}
        </span>
      </button>

      {assignee && (
        <span className="flex size-7 items-center justify-center rounded-full bg-primary-soft text-xs font-bold text-accent-foreground">
          {initials(assignee)}
        </span>
      )}

      <button onClick={onToggleImportant} aria-label="علامت مهم" disabled={readOnly} className="disabled:opacity-40">
        <Star
          className={cn(
            "size-4",
            task.is_important ? "fill-accent-amber text-accent-amber" : "text-muted-foreground",
          )}
        />
      </button>
    </div>
  );
}
