import { afterEach, expect, test } from "bun:test";
import { ApiError } from "./api";
import { backendErrorMessage, errorMessage } from "./api-errors";
import { updateTask, deleteTask } from "./todo";
import { createSubtask, updateSubtask, deleteSubtask, uploadAttachment, deleteAttachment } from "./task-details";

const originalFetch = globalThis.fetch;
afterEach(() => { globalThis.fetch = originalFetch; });
const denied = "فقط کاربری که تسک به او ارجاع شده اجازه تغییر دارد.";

test("task edits/deletion preserve backend assignment-denial messages", async () => {
  globalThis.fetch = async () => Response.json({ title: denied }, { status: 403 });
  for (const operation of [() => updateTask(3, { title: "تغییر" }), () => deleteTask(3)]) {
    try { await operation(); throw new Error("Expected denial"); }
    catch (error) { expect(error).toBeInstanceOf(ApiError); expect(error.status).toBe(403); expect(errorMessage(error, "fallback")).toBe(denied); }
  }
});

test("subtask mutations preserve parent-task assignment denial", async () => {
  globalThis.fetch = async () => Response.json({ detail: denied }, { status: 403 });
  for (const operation of [() => createSubtask({ taskId: 3, createdBy: 2, title: "مرحله", position: 0 }), () => updateSubtask(4, { title: "ویرایش" }), () => updateSubtask(4, { is_completed: true }), () => deleteSubtask(4)]) {
    await expect(operation()).rejects.toThrow(denied);
  }
});

test("attachment mutations preserve parent-task assignment denial", async () => {
  globalThis.fetch = async () => Response.json({ message: denied }, { status: 403 });
  await expect(uploadAttachment(3, 2, new File(["test"], "test.txt"))).rejects.toThrow(denied);
  await expect(deleteAttachment({ id: 4 })).rejects.toThrow(denied);
});

test("subtask title editing uses existing PATCH API without changing completion", async () => {
  let request;
  globalThis.fetch = async (url, options) => { request = { url: String(url), ...options }; return new Response(null, { status: 204 }); };
  await updateSubtask(42, { title: "عنوان اصلاح‌شده" });
  expect(request.url.endsWith("/api/v1/subtasks/42")).toBe(true);
  expect(request.method).toBe("PATCH");
  expect(JSON.parse(request.body)).toEqual({ title: "عنوان اصلاح‌شده" });
  expect(request.credentials).toBe("include");
});

test("empty validation errors safely fall back", () => {
  expect(backendErrorMessage({ errors: {} }, "fallback")).toBe("fallback");
  expect(backendErrorMessage({ errors: { title: ["عنوان الزامی است."] } }, "fallback")).toBe("عنوان الزامی است.");
});