/** Ordering and paging shared by the task list and by notification deep links. */

const FAR = "9999-12-31";

export type SortableTask = {
  id: number;
  list_id: number;
  title: string;
  due_date: string | null;
  created_at: string;
  is_important: boolean;
};

/** Single source of truth for task ordering, so a deep link lands on the right page. */
export function sortTasks<T extends SortableTask>(tasks: T[], sort: string): T[] {
  const arr = [...tasks];
  arr.sort((a, b) => {
    switch (sort) {
      case "created_asc":
        return a.created_at.localeCompare(b.created_at);
      case "title_asc":
        return a.title.localeCompare(b.title, "fa");
      case "title_desc":
        return b.title.localeCompare(a.title, "fa");
      case "due_asc":
        return (a.due_date ?? FAR).localeCompare(b.due_date ?? FAR);
      case "due_desc":
        return (b.due_date ?? "").localeCompare(a.due_date ?? "");
      case "important":
        return Number(b.is_important) - Number(a.is_important);
      default:
        return b.created_at.localeCompare(a.created_at);
    }
  });
  return arr;
}

/**
 * 1-based page that shows `task` once the list view of its own list is shown
 * with search and filters cleared.
 */
export function pageForTask<T extends SortableTask>(
  tasks: T[],
  task: T,
  sort: string,
  pageSize: number,
): number {
  const size = Math.max(1, pageSize);
  const inList = sortTasks(
    tasks.filter((t) => t.list_id === task.list_id),
    sort,
  );
  const index = inList.findIndex((t) => t.id === task.id);
  return index < 0 ? 1 : Math.floor(index / size) + 1;
}
