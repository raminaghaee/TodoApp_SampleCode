import { expect, test } from "bun:test";
import { canEditTask, assertTaskWritable } from "./task-access";

const list = { owner_id: 1 };
const assigned = { created_by: 2, assigned_to: 3 };
const unassigned = { created_by: 2, assigned_to: null };

test("list owner can edit a task assigned to someone else", () => {
  expect(canEditTask(assigned, 1, list)).toBe(true);
});
test("task assignee can edit", () => {
  expect(canEditTask(assigned, 3, list)).toBe(true);
});
test("creator can edit only when task is unassigned", () => {
  expect(canEditTask(unassigned, 2, list)).toBe(true);
  expect(canEditTask(assigned, 2, list)).toBe(false);
});
test("other shared members cannot edit assigned or unassigned tasks", () => {
  expect(canEditTask(assigned, 4, list)).toBe(false);
  expect(canEditTask(unassigned, 3, list)).toBe(false);
});
test("missing user or list context denies editing", () => {
  expect(canEditTask(unassigned, null, list)).toBe(false);
  expect(canEditTask(unassigned, 2, undefined)).toBe(false);
});
test("unauthorized mutation guard blocks callbacks before any API operation", async () => {
  let calls = 0;
  const mutate = async (readOnly) => {
    assertTaskWritable(readOnly);
    calls++;
  };
  await expect(mutate(true)).rejects.toThrow();
  expect(calls).toBe(0);
  await mutate(false);
  expect(calls).toBe(1);
});