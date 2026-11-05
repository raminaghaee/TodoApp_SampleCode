import { confirmDelete } from "@/components/shared/ConfirmDialog";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Menu,
  Plus,
  Share2,
  Sun,
  Star,
  CalendarDays,
  UserCheck,
  Inbox,
} from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Sheet, SheetContent } from "@/components/ui/sheet";
import { Toolbar, type FilterValues } from "@/components/shared/Toolbar";
import { DataPagination } from "@/components/shared/DataPagination";
import { ListDialog, type ListDialogValue } from "@/components/todo/ListDialog";
import { Sidebar, type ViewId } from "@/components/todo/Sidebar";
import { TaskRow } from "@/components/todo/TaskRow";
import { canEditTask } from "@/lib/todo";
import { pageForTask, sortTasks } from "@/lib/task-navigation";
import { TaskDetail } from "@/components/todo/TaskDetail";
import { ShareDialog } from "@/components/todo/ShareDialog";
import { NotificationBell } from "@/components/todo/NotificationBell";
import { getCurrentUser, logout } from "@/lib/auth";
import { errorMessage } from "@/lib/api-errors";
import { checkSuperAdmin } from "@/lib/admin";
import {
  fetchNotifications,
  deleteNotification,
  markAllNotificationsRead,
  markNotificationRead,
  type Notification,
} from "@/lib/notifications";
import {
  createList,
  createTask,
  deleteList,
  deleteTask,
  fetchLists,
  fetchProfiles,
  fetchShares,
  fetchTasks,
  shareList,
  renameList,
  unshareList,
  updateTask,
  type TodoItem,
  type TaskList,
} from "@/lib/todo";

export const Route = createFileRoute("/_authenticated/tasks")({
  head: () => ({
    meta: [
      { title: "کارهای من | Todo" },
      {
        name: "description",
        content: "لیست کارها، روز من، ارجاع و اشتراک‌گذاری تسک‌ها.",
      },
      { property: "og:title", content: "کارهای من | Todo" },
      {
        property: "og:description",
        content: "مدیریت کارهای شخصی و تیمی در Todo.",
      },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary" },
    ],
  }),
  validateSearch: (search: Record<string, unknown>): { task?: number | undefined } => {
    const task = Number(search["task"]);
    return Number.isInteger(task) && task > 0 ? { task } : {};
  },
  component: TasksPage,
});

const viewMeta: Record<string, { label: string; icon: typeof Sun }> = {
  myday: { label: "روز من", icon: Sun },
  important: { label: "مهم", icon: Star },
  planned: { label: "برنامه‌ریزی‌شده", icon: CalendarDays },
  assigned: { label: "ارجاع‌شده به من", icon: UserCheck },
  all: { label: "همه کارها", icon: Inbox },
};

function TasksPage() {
  const navigate = useNavigate({ from: Route.fullPath });
  const queryClient = useQueryClient();
  const [userId, setUserId] = useState<number | null>(null);
  const [userLabel, setUserLabel] = useState("کاربر");
  const [view, setView] = useState<ViewId>("myday");
  const [newTitle, setNewTitle] = useState("");
  const { task: openTaskId } = Route.useSearch();
  // A deep link reveals its task once; pendingReveal stops the page-reset
  // effect below from throwing away the page we just computed for it.
  const revealedTaskId = useRef<number | null>(null);
  const pendingReveal = useRef<number | null>(null);
  const [shareOpen, setShareOpen] = useState(false);
  const [mobileNav, setMobileNav] = useState(false);
  const [isSuperAdmin, setIsSuperAdmin] = useState(false);
  const [search, setSearch] = useState("");
  const [filters, setFilters] = useState<FilterValues>({});
  const [sort, setSort] = useState("created_desc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [listDialog, setListDialog] = useState<
    | { mode: "create" }
    | { mode: "edit"; id: number; initial: ListDialogValue }
    | null
  >(null);

  useEffect(() => {
    getCurrentUser().then(async (user) => {
      if (!user) return;
      setUserId(user.id);
      setUserLabel(
        (user.user_metadata?.["display_name"] as string | undefined) ??
          user.email?.split("@")[0] ??
          "کاربر",
      );
      setIsSuperAdmin(await checkSuperAdmin(user.id));
      queryClient.invalidateQueries({ queryKey: ["profiles"] });
    });
  }, [queryClient]);

  const listsQ = useQuery({
    queryKey: ["lists"],
    queryFn: fetchLists,
    enabled: !!userId,
  });
  const tasksQ = useQuery({
    queryKey: ["tasks"],
    queryFn: fetchTasks,
    enabled: !!userId,
  });
  const profilesQ = useQuery({
    queryKey: ["profiles"],
    queryFn: fetchProfiles,
    enabled: !!userId,
  });
  const sharesQ = useQuery({
    queryKey: ["shares"],
    queryFn: fetchShares,
    enabled: !!userId,
  });
  const notifsQ = useQuery({
    queryKey: ["notifications"],
    queryFn: fetchNotifications,
    enabled: !!userId,
    refetchInterval: 60_000,
  });

  const lists = listsQ.data ?? [];
  const tasks = tasksQ.data ?? [];
  const profiles = profilesQ.data ?? [];
  const shares = sharesQ.data ?? [];
  const notifications = notifsQ.data ?? [];

  // Due-date reminders (due_soon) are created server-side by the backend's
  // hourly BackgroundService — the client only displays them.

  // First-run: give every new user a default list, like Microsoft To Do's "Tasks".
  useEffect(() => {
    if (!userId || !listsQ.isSuccess) return;
    if ((listsQ.data ?? []).some((l) => l.owner_id === userId)) return;
    createList(userId, "کارها", "blue")
      .then(() => queryClient.invalidateQueries({ queryKey: ["lists"] }))
      .catch(() => undefined);
  }, [userId, listsQ.isSuccess, listsQ.data, queryClient]);

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["tasks"] });
    queryClient.invalidateQueries({ queryKey: ["lists"] });
    queryClient.invalidateQueries({ queryKey: ["shares"] });
    queryClient.invalidateQueries({ queryKey: ["notifications"] });
  };

  const isSmart = view in viewMeta;
  const activeList = lists.find((l) => l.id === view) ?? null;
  const defaultListId = activeList?.id ?? lists[0]?.id ?? null;

  const visibleTasks = useMemo(() => {
    const today = new Date().toISOString().slice(0, 10);
    let base: TodoItem[];
    if (view === "myday")
      base = tasks.filter((t) => t.is_my_day || t.due_date === today);
    else if (view === "important") base = tasks.filter((t) => t.is_important);
    else if (view === "planned") base = tasks.filter((t) => !!t.due_date);
    else if (view === "assigned")
      base = tasks.filter((t) => t.assigned_to === userId);
    else if (view === "all") base = tasks;
    else base = tasks.filter((t) => t.list_id === view);

    const q = search.trim().toLowerCase();
    const status = filters["status"] ?? "";
    const category = filters["category"] ?? "";
    const listFilter = filters["list"] ?? "";
    const from = filters["dueFrom"] ?? "";
    const to = filters["dueTo"] ?? "";

    return base.filter((t) => {
      if (q && !`${t.title} ${t.notes ?? ""}`.toLowerCase().includes(q))
        return false;
      if (status === "open" && t.is_completed) return false;
      if (status === "done" && !t.is_completed) return false;
      if (listFilter && t.list_id !== Number(listFilter)) return false;
      if (category === "important" && !t.is_important) return false;
      if (category === "myday" && !t.is_my_day) return false;
      if (category === "assigned" && t.assigned_to !== userId) return false;
      if (category === "unassigned" && t.assigned_to) return false;
      if (
        category === "overdue" &&
        !(t.due_date && t.due_date < today && !t.is_completed)
      )
        return false;
      if (category === "nodue" && t.due_date) return false;
      if (from && (!t.due_date || t.due_date < from)) return false;
      if (to && (!t.due_date || t.due_date > to)) return false;
      return true;
    });
  }, [tasks, view, userId, search, filters]);

  const sortedTasks = useMemo(
    () => sortTasks(visibleTasks, sort),
    [visibleTasks, sort],
  );

  const total = sortedTasks.length;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const currentPage = Math.min(page, pageCount);
  const pageTasks = sortedTasks.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize,
  );

  // Reset paging whenever the result set changes shape.
  useEffect(() => {
    if (pendingReveal.current != null) {
      pendingReveal.current = null;
      return;
    }
    setPage(1);
  }, [view, search, filters, sort, pageSize]);

  const open = pageTasks.filter((t) => !t.is_completed);
  const done = pageTasks.filter((t) => t.is_completed);

  const smartCounts = useMemo(() => {
    const today = new Date().toISOString().slice(0, 10);
    const notDone = tasks.filter((t) => !t.is_completed);
    return {
      myday: notDone.filter((t) => t.is_my_day || t.due_date === today).length,
      important: notDone.filter((t) => t.is_important).length,
      planned: notDone.filter((t) => !!t.due_date).length,
      assigned: notDone.filter((t) => t.assigned_to === userId).length,
      all: notDone.length,
    } as Record<string, number>;
  }, [tasks, userId]);

  const sharedListIds = useMemo(
    () => new Set(shares.map((s) => s.list_id)),
    [shares],
  );
  const listSharedUserIds = activeList
    ? shares.filter((s) => s.list_id === activeList.id).map((s) => s.user_id)
    : [];

  const openTask = tasks.find((t) => t.id === openTaskId) ?? null;
  const membersForTask = useMemo(() => {
    if (!openTask) return [];
    const list = lists.find((l) => l.id === openTask.list_id);
    const allowed = new Set<number>([
      ...(list ? [list.owner_id] : []),
      ...shares
        .filter((s) => s.list_id === openTask.list_id)
        .map((s) => s.user_id),
    ]);
    return profiles.filter((p) => allowed.has(p.id));
  }, [openTask, lists, shares, profiles]);

  function openTaskInUrl(id: number | null) {
    navigate({
      to: "/tasks",
      search: (prev) => ({ ...prev, task: id ?? undefined }),
    });
  }

  /** Shows the task's own list, free of search/filters, on the page holding it. */
  function revealTask(task: TodoItem) {
    revealedTaskId.current = task.id;
    pendingReveal.current = task.id;
    setView(task.list_id);
    setSearch("");
    setFilters({});
    setPage(pageForTask(tasks, task, sort, pageSize));
  }

  // A deep link (/tasks?task=7) or a notification click lands on the task itself.
  useEffect(() => {
    if (!openTaskId || !tasksQ.isSuccess || revealedTaskId.current === openTaskId)
      return;
    const target = tasks.find((t) => t.id === openTaskId);
    if (!target) {
      toast.error("این کار دیگر در دسترس نیست.");
      openTaskInUrl(null);
      return;
    }
    revealTask(target);
  }, [openTaskId, tasksQ.isSuccess, tasks, sort, pageSize]);

  function handleNotificationOpen(n: Notification) {
    if (!n.read_at) {
      markNotificationRead(n.id).catch(() => undefined);
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    }
    if (n.task_id == null) return;
    const target = tasks.find((t) => t.id === n.task_id);
    if (!target) {
      toast.error("این کار دیگر در دسترس نیست.");
      return;
    }
    revealTask(target);
    openTaskInUrl(n.task_id);
  }

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    const title = newTitle.trim();
    if (!title || !userId || !defaultListId) return;
    try {
      await createTask({
        listId: defaultListId,
        createdBy: userId,
        title,
        isMyDay: view === "myday",
        isImportant: view === "important",
        dueDate:
          view === "planned" ? new Date().toISOString().slice(0, 10) : null,
        assignedTo: view === "assigned" ? userId : null,
      });
      setNewTitle("");
      refresh();
    } catch (error) {
      toast.error(errorMessage(error, "افزودن کار انجام نشد."));
    }
  }

  async function patch(id: number, p: Partial<TodoItem>) {
    if (!mayEditTask(id)) return false;
    try {
      await updateTask(id, p);
      refresh();
      return true;
    } catch (err) {
      const msg =
        err instanceof Error ? err.message : "ذخیره تغییرات انجام نشد.";
      toast.error(msg);
      return false;
    }
  }

  function mayEditTask(id: number) {
    const currentTask = queryClient.getQueryData<TodoItem[]>(["tasks"])?.find((t) => t.id === id);
    if (!currentTask) return false;
    const currentList = queryClient.getQueryData<TaskList[]>(["lists"])?.find((l) => l.id === currentTask.list_id);
    return canEditTask(currentTask, userId, currentList);
  }

  async function submitListDialog(value: ListDialogValue) {
    if (!listDialog) return;
    try {
      if (listDialog.mode === "create") {
        if (!userId) return;
        const created = await createList(userId, value.name, value.color);
        queryClient.invalidateQueries({ queryKey: ["lists"] });
        if (created) setView(created.id);
      } else {
        await renameList(listDialog.id, value.name, value.color);
        queryClient.invalidateQueries({ queryKey: ["lists"] });
      }
      setListDialog(null);
    } catch {
      toast.error(
        listDialog.mode === "create"
          ? "ساخت لیست انجام نشد."
          : "ویرایش لیست انجام نشد.",
      );
    }
  }

  async function handleDeleteList(id: number) {
    if (!await confirmDelete({ description: "این لیست و همه کارهای آن حذف شود؟", hint: "این عمل قابل بازگشت نیست." })) return;
    try {
      await deleteList(id);
      if (view === id) setView("myday");
      refresh();
    } catch {
      toast.error("حذف لیست انجام نشد.");
    }
  }

  async function signOut() {
    await queryClient.cancelQueries();
    queryClient.clear();
    await logout();
    navigate({ to: "/auth", replace: true });
  }

  const HeaderIcon = isSmart ? viewMeta[view]?.icon : null;
  const title = isSmart
    ? (viewMeta[view]?.label ?? "کارها")
    : (activeList?.name ?? "کارها");
  const todayLabel = new Date().toLocaleDateString("fa-IR", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });

  const sidebar = (
    <Sidebar
      lists={lists}
      tasks={tasks}
      userId={userId ?? 0}
      view={view}
      onSelect={(v) => {
        setView(v);
        setMobileNav(false);
      }}
      onCreateList={() => setListDialog({ mode: "create" })}
      onDeleteList={handleDeleteList}
      onRenameList={(id, current) => {
        const l = lists.find((x) => x.id === id);
        setListDialog({
          mode: "edit",
          id,
          initial: { name: current, color: l?.color ?? "blue" },
        });
      }}
      onSignOut={signOut}
      userLabel={userLabel}
      sharedListIds={sharedListIds}
      smartCounts={smartCounts}
      isSuperAdmin={isSuperAdmin}
    />
  );

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      <div className="hidden w-72 shrink-0 lg:block">{sidebar}</div>

      <Sheet open={mobileNav} onOpenChange={setMobileNav}>
        <SheetContent side="right" className="w-72 p-0">
          {sidebar}
        </SheetContent>
      </Sheet>

      <main className="flex min-w-0 flex-1 flex-col">
        <header className="px-5 pb-4 pt-6 sm:px-8">
          <Toolbar
            title={title}
            description={view === "myday" ? todayLabel : `${total} کار`}
            {...(HeaderIcon ? { icon: HeaderIcon } : {})}
            search={{
              value: search,
              onChange: setSearch,
              placeholder: "جستجوی عنوان یا یادداشت…",
            }}
            sort={{
              value: sort,
              onChange: setSort,
              options: [
                { value: "created_desc", label: "جدیدترین" },
                { value: "created_asc", label: "قدیمی‌ترین" },
                { value: "due_asc", label: "سررسید نزدیک" },
                { value: "due_desc", label: "سررسید دور" },
                { value: "title_asc", label: "عنوان (الف-ی)" },
                { value: "title_desc", label: "عنوان (ی-الف)" },
                { value: "important", label: "مهم‌ها اول" },
              ],
            }}
            filters={[
              {
                key: "status",
                label: "وضعیت",
                type: "select",
                options: [
                  { value: "open", label: "باز" },
                  { value: "done", label: "انجام‌شده" },
                ],
              },
              {
                key: "category",
                label: "دسته‌بندی",
                type: "select",
                options: [
                  { value: "important", label: "مهم" },
                  { value: "myday", label: "روز من" },
                  { value: "assigned", label: "ارجاع‌شده به من" },
                  { value: "unassigned", label: "بدون مسئول" },
                  { value: "overdue", label: "سررسید گذشته" },
                  { value: "nodue", label: "بدون سررسید" },
                ],
              },
              {
                key: "list",
                label: "لیست",
                type: "select",
                options: lists.map((l) => ({
                  value: String(l.id),
                  label: l.name,
                })),
              },
              { key: "dueFrom", label: "سررسید از", type: "date" },
              { key: "dueTo", label: "سررسید تا", type: "date" },
            ]}
            filterValues={filters}
            onFilterChange={(k, v) => setFilters((f) => ({ ...f, [k]: v }))}
            onClearFilters={() => setFilters({})}
            leading={
              <Button
                variant="ghost"
                size="icon"
                className="lg:hidden"
                onClick={() => setMobileNav(true)}
                aria-label="منو"
              >
                <Menu className="size-5" />
              </Button>
            }
            extra={
              <>
                <NotificationBell
                  notifications={notifications}
                  onOpenTask={handleNotificationOpen}
                  onRead={async (id) => {
                    try { await markNotificationRead(id); } catch (e) { toast.error(errorMessage(e, "خواندن اعلان ناموفق بود.")); }
                    queryClient.invalidateQueries({ queryKey: ["notifications"] });
                  }}
                  onDelete={async (id) => {
                    try { await deleteNotification(id); toast.success("اعلان حذف شد."); } catch (e) { toast.error(errorMessage(e, "حذف اعلان ناموفق بود.")); }
                    queryClient.invalidateQueries({ queryKey: ["notifications"] });
                  }}
                  onReadAll={async () => {
                    if (!userId) return;
                    await markAllNotificationsRead(userId);
                    queryClient.invalidateQueries({
                      queryKey: ["notifications"],
                    });
                  }}
                />
                {activeList && (
                  <Button
                    variant="outline"
                    className="gap-2"
                    onClick={() => setShareOpen(true)}
                  >
                    <Share2 className="size-4" />
                    <span className="hidden sm:inline">اشتراک‌گذاری</span>
                  </Button>
                )}
              </>
            }
          />
        </header>

        <div className="px-5 sm:px-8">
          <form onSubmit={handleAdd} className="relative">
            <Plus className="pointer-events-none absolute end-4 top-1/2 size-4 -translate-y-1/2 text-primary" />
            <Input
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              placeholder="افزودن کار جدید…"
              className="h-12 rounded-xl border-border bg-card pe-11 shadow-panel"
              disabled={!defaultListId}
            />
          </form>
        </div>

        <div className="mt-5 min-h-0 flex-1 space-y-2 overflow-y-auto px-5 pb-10 sm:px-8">
          {open.length === 0 && done.length === 0 && (
            <div className="rounded-2xl border border-dashed border-border py-16 text-center">
              <p className="text-sm text-muted-foreground">
                هنوز کاری اینجا نیست. اولین کار را اضافه کن.
              </p>
            </div>
          )}

          {open.map((t) => (
            <TaskRow
              key={t.id}
              task={t}
              list={lists.find((l) => l.id === t.list_id)}
              assignee={profiles.find((p) => p.id === t.assigned_to)}
              showList={isSmart}
              selected={openTaskId === t.id}
              onToggleComplete={() =>
                patch(t.id, {
                  is_completed: true,
                  completed_at: new Date().toISOString(),
                })
              }
              onToggleImportant={() =>
                patch(t.id, { is_important: !t.is_important })
              }
              readOnly={!mayEditTask(t.id)}
              onOpen={() => openTaskInUrl(t.id)}
            />
          ))}

          {done.length > 0 && (
            <>
              <p className="pt-6 pb-1 text-xs font-medium text-muted-foreground">
                انجام‌شده ({done.length})
              </p>
              {done.map((t) => (
                <TaskRow
                  key={t.id}
                  task={t}
                  list={lists.find((l) => l.id === t.list_id)}
                  assignee={profiles.find((p) => p.id === t.assigned_to)}
                  showList={isSmart}
                  selected={openTaskId === t.id}
                  onToggleComplete={() =>
                    patch(t.id, { is_completed: false, completed_at: null })
                  }
                  onToggleImportant={() =>
                    patch(t.id, { is_important: !t.is_important })
                  }
                  readOnly={!mayEditTask(t.id)}
              onOpen={() => openTaskInUrl(t.id)}
                />
              ))}
            </>
          )}
        </div>

        <div className="px-5 pb-5 sm:px-8">
          <DataPagination
            page={currentPage}
            pageSize={pageSize}
            total={total}
            onPageChange={setPage}
            onPageSizeChange={setPageSize}
          />
        </div>
      </main>

      <TaskDetail
        task={openTask}
        lists={lists}
        members={membersForTask}
        userId={userId ?? 0}
        readOnly={!openTask || !mayEditTask(openTask.id)}
        onClose={() => openTaskInUrl(null)}
        onPatch={(p) => openTask ? patch(openTask.id, p) : Promise.resolve(false)}
        onDelete={async () => {
          if (!openTask || !mayEditTask(openTask.id)) return;
          if (!await confirmDelete({ description: `کار «${openTask.title}» حذف شود؟`, hint: "این عمل قابل بازگشت نیست." })) return;
          try {
            await deleteTask(openTask.id);
            openTaskInUrl(null);
            refresh();
          } catch (error) {
            toast.error(errorMessage(error, "حذف کار انجام نشد."));
          }
        }}
      />

      <ShareDialog
        open={shareOpen}
        list={activeList}
        isOwner={activeList?.owner_id === userId}
        profiles={profiles.filter((p) => p.id !== userId)}
        sharedUserIds={listSharedUserIds}
        onClose={() => setShareOpen(false)}
        onShare={async (uid) => {
          if (!activeList) throw new Error("لیست انتخاب نشده است.");
          await shareList(activeList.id, uid);
          queryClient.invalidateQueries({ queryKey: ["shares"] });
          queryClient.invalidateQueries({ queryKey: ["profiles"] });
        }}
        onUnshare={async (uid) => {
          if (!activeList) return;
          if (!await confirmDelete({ title: "حذف دسترسی", description: "دسترسی این کاربر به لیست حذف شود؟", confirmLabel: "حذف دسترسی", hint: "کاربر دیگر به این لیست دسترسی نخواهد داشت." })) return;
          try {
            await unshareList(activeList.id, uid);
            queryClient.invalidateQueries({ queryKey: ["shares"] });
          } catch {
            toast.error("حذف دسترسی انجام نشد.");
          }
        }}
      />

      <ListDialog
        open={!!listDialog}
        mode={listDialog?.mode ?? "create"}
        {...(listDialog?.mode === "edit"
          ? { initial: listDialog.initial }
          : {})}
        onClose={() => setListDialog(null)}
        onSubmit={submitListDialog}
      />
    </div>
  );
}
