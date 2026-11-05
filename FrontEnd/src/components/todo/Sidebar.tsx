import { useMemo, useState } from "react";
import { Link } from "@tanstack/react-router";
import {
  CheckCircle2,
  Sun,
  Star,
  CalendarDays,
  Inbox,
  UserCheck,
  Plus,
  Users,
  LogOut,
  Trash2,
  Pencil,
  ShieldCheck,
  Search,
  ArrowUpDown,
  UserRound,
} from "lucide-react";


import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { DataPagination } from "@/components/shared/DataPagination";
import { colorClass, type TaskList, type TodoItem } from "@/lib/todo";
import { cn } from "@/lib/utils";

/** Smart views are string keys; a numeric value selects a TaskList by its id. */
export type ViewId = "myday" | "important" | "planned" | "assigned" | "all" | number;

const smartViews: { id: ViewId; label: string; icon: typeof Sun }[] = [
  { id: "myday", label: "روز من", icon: Sun },
  { id: "important", label: "مهم", icon: Star },
  { id: "planned", label: "برنامه‌ریزی‌شده", icon: CalendarDays },
  { id: "assigned", label: "ارجاع‌شده به من", icon: UserCheck },
  { id: "all", label: "همه کارها", icon: Inbox },
];

type Props = {
  lists: TaskList[];
  tasks: TodoItem[];
  userId: number;
  view: ViewId;
  onSelect: (v: ViewId) => void;
  onCreateList: () => void;
  onDeleteList: (id: number) => void;
  onRenameList: (id: number, current: string) => void;
  onSignOut: () => void;
  userLabel: string;
  sharedListIds: Set<number>;
  smartCounts: Record<string, number>;
  isSuperAdmin?: boolean;
};


export function Sidebar({
  lists,
  tasks,
  userId,
  view,
  onSelect,
  onCreateList,
  onDeleteList,
  onRenameList,
  onSignOut,
  userLabel,
  sharedListIds,
  smartCounts,
  isSuperAdmin = false,
}: Props) {
  const [listQuery, setListQuery] = useState("");
  const [listSort, setListSort] = useState("name_asc");
  const [listPage, setListPage] = useState(1);
  const listPageSize = 8;

  const openCounts = useMemo(() => {
    const map: Record<number, number> = {};
    for (const t of tasks) {
      if (t.is_completed) continue;
      map[t.list_id] = (map[t.list_id] ?? 0) + 1;
    }
    return map;
  }, [tasks]);

  const filteredLists = useMemo(() => {
    const q = listQuery.trim().toLowerCase();
    const arr = lists.filter((l) => !q || l.name.toLowerCase().includes(q));
    arr.sort((a, b) => {
      switch (listSort) {
        case "name_desc":
          return b.name.localeCompare(a.name, "fa");
        case "created_desc":
          return b.created_at.localeCompare(a.created_at);
        case "created_asc":
          return a.created_at.localeCompare(b.created_at);
        case "tasks_desc":
          return (openCounts[b.id] ?? 0) - (openCounts[a.id] ?? 0);
        default:
          return a.name.localeCompare(b.name, "fa");
      }
    });
    return arr;
  }, [lists, listQuery, listSort, openCounts]);

  const listPageCount = Math.max(1, Math.ceil(filteredLists.length / listPageSize));
  const currentListPage = Math.min(listPage, listPageCount);
  const pagedLists = filteredLists.slice(
    (currentListPage - 1) * listPageSize,
    currentListPage * listPageSize,
  );

  return (
    <aside className="flex h-full w-full flex-col border-e border-sidebar-border bg-sidebar">
      <div className="flex items-center justify-between px-5 py-5">
        <Link to="/" className="flex items-center gap-2">
          <span className="flex size-8 items-center justify-center rounded-lg bg-gradient-hero text-primary-foreground">
            <CheckCircle2 className="size-4" />
          </span>
          <span className="font-bold">Todo</span>
        </Link>
      </div>

      <nav className="space-y-1 px-3">
        {smartViews.map((v) => (
          <button
            key={v.id}
            onClick={() => onSelect(v.id)}
            className={cn(
              "flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
              view === v.id
                ? "bg-sidebar-accent font-medium text-sidebar-accent-foreground"
                : "text-sidebar-foreground hover:bg-sidebar-accent/60",
            )}
          >
            <v.icon className="size-4 text-primary" />
            <span className="flex-1 text-start">{v.label}</span>
            {smartCounts[v.id] ? (
              <span className="text-xs text-muted-foreground">{smartCounts[v.id]}</span>
            ) : null}
          </button>
        ))}
      </nav>

      <div className="mx-5 my-4 h-px bg-sidebar-border" />

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-2">
        <p className="px-3 pb-2 text-xs font-medium text-muted-foreground">لیست‌ها</p>

        <div className="mb-2 space-y-2 px-1">
          <div className="relative">
            <Search className="pointer-events-none absolute end-3 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={listQuery}
              onChange={(e) => {
                setListQuery(e.target.value);
                setListPage(1);
              }}
              placeholder="جستجوی لیست…"
              className="h-9 pe-8 text-sm"
            />
          </div>
          <Select
            value={listSort}
            onValueChange={(v) => {
              setListSort(v);
              setListPage(1);
            }}
          >
            <SelectTrigger className="h-9 gap-2 text-sm" aria-label="مرتب‌سازی لیست‌ها">
              <ArrowUpDown className="size-3.5 text-muted-foreground" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="name_asc">نام (الف-ی)</SelectItem>
              <SelectItem value="name_desc">نام (ی-الف)</SelectItem>
              <SelectItem value="created_desc">جدیدترین</SelectItem>
              <SelectItem value="created_asc">قدیمی‌ترین</SelectItem>
              <SelectItem value="tasks_desc">بیشترین کار باز</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1">
          {pagedLists.map((l) => {
            const count = openCounts[l.id] ?? 0;

            return (
              <div
                key={l.id}
                className={cn(
                  "group flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                  view === l.id
                    ? "bg-sidebar-accent font-medium text-sidebar-accent-foreground"
                    : "text-sidebar-foreground hover:bg-sidebar-accent/60",
                )}
              >
                <button onClick={() => onSelect(l.id)} className="flex flex-1 items-center gap-3">
                  <span className={cn("size-2.5 rounded-full", colorClass(l.color))} />
                  <span className="flex-1 truncate text-start">{l.name}</span>
                  {sharedListIds.has(l.id) && <Users className="size-3.5 text-muted-foreground" />}
                  {count ? <span className="text-xs text-muted-foreground">{count}</span> : null}
                </button>
                {l.owner_id === userId && (
                  <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                    <button
                      onClick={() => onRenameList(l.id, l.name)}
                      aria-label={`ویرایش لیست ${l.name}`}
                    >
                      <Pencil className="size-3.5 text-muted-foreground hover:text-primary" />
                    </button>
                    <button onClick={() => onDeleteList(l.id)} aria-label={`حذف لیست ${l.name}`}>
                      <Trash2 className="size-3.5 text-destructive" />
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {filteredLists.length === 0 && (
          <p className="px-3 py-4 text-xs text-muted-foreground">لیستی یافت نشد.</p>
        )}

        <DataPagination
          page={currentListPage}
          pageSize={listPageSize}
          total={filteredLists.length}
          onPageChange={setListPage}
          compact
          className="mt-2 px-1"
        />



        <Button variant="ghost" className="mt-3 w-full justify-start gap-2" onClick={onCreateList}>
          <Plus className="size-4" />
          لیست جدید
        </Button>
      </div>

      {isSuperAdmin && (
        <Link
          to="/admin"
          className="mx-3 mb-2 flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-sidebar-foreground transition-colors hover:bg-sidebar-accent/60"
        >
          <ShieldCheck className="size-4 text-primary" />
          <span className="flex-1 text-start">پنل سوپرادمین</span>
        </Link>
      )}



      <div className="flex items-center gap-3 border-t border-sidebar-border px-4 py-3">
        <span className="flex size-8 items-center justify-center rounded-full bg-primary text-xs font-bold text-primary-foreground">
          {userLabel.charAt(0).toUpperCase()}
        </span>
        <Link to="/profile" className="flex min-w-0 flex-1 items-center gap-2 text-sm" title="پروفایل من">
          <span className="truncate">{userLabel}</span><UserRound className="size-4 shrink-0 text-muted-foreground" />
        </Link>
        <button onClick={onSignOut} aria-label="خروج از حساب">
          <LogOut className="size-4 text-muted-foreground hover:text-foreground" />
        </button>
      </div>
    </aside>
  );
}
