import { expect, test } from "bun:test";
import { sortTasks, pageForTask } from "./task-navigation";

const task = (id, list_id, over = {}) => ({
  id,
  list_id,
  title: `کار ${id}`,
  due_date: null,
  created_at: `2026-01-${String(id).padStart(2, "0")}T00:00:00Z`,
  is_important: false,
  ...over,
});

test("due ascending keeps tasks without a due date last", () => {
  const rows = [task(1, 10), task(2, 10, { due_date: "2026-03-01" }), task(3, 10, { due_date: "2026-02-01" })];
  expect(sortTasks(rows, "due_asc").map((t) => t.id)).toEqual([3, 2, 1]);
});

test("default ordering is newest first", () => {
  const rows = [task(1, 10), task(2, 10)];
  expect(sortTasks(rows, "created_desc").map((t) => t.id)).toEqual([2, 1]);
});

test("page for a task counts only its own list and honors the sort", () => {
  const rows = [
    task(1, 10),
    task(2, 10),
    task(3, 10),
    task(4, 20),
    task(5, 10),
  ];
  // list 10 keeps tasks 1,2,3,5 newest-first: 5,3,2,1 -> task 1 sits on page 2 of size 3.
  expect(pageForTask(rows, task(1, 10), "created_desc", 3)).toBe(2);
  expect(pageForTask(rows, task(5, 10), "created_desc", 3)).toBe(1);
});

test("a task already visible on the first page maps to page one", () => {
  const rows = [task(1, 10), task(2, 10)];
  expect(pageForTask(rows, task(2, 10), "created_desc", 20)).toBe(1);
});

test("an unknown task falls back to the first page", () => {
  const rows = [task(1, 10)];
  expect(pageForTask(rows, task(99, 10), "created_desc", 20)).toBe(1);
});
