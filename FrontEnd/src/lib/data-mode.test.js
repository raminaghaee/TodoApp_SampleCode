import { afterEach, expect, test } from "bun:test";
import { isMockMode } from "./data-mode";
import { apiFetch } from "./api";
import { MOCK_ACCOUNTS, MOCK_PASSWORD, mockRequest } from "./mock-api";

const values = new Map();
const reads = [];
const originalFetch = globalThis.fetch;
Object.defineProperty(globalThis, "localStorage", { configurable: true, value: {
  getItem: (key) => { reads.push(key); return values.get(key) ?? null; },
  setItem: (key, value) => values.set(key, value),
  removeItem: (key) => values.delete(key),
} });
afterEach(() => { values.clear(); reads.length = 0; globalThis.fetch = originalFetch; });

test("mock mode requires explicit true and false disables it", () => {
  expect(isMockMode("true")).toBe(true);
  expect(isMockMode("false")).toBe(false);
  expect(isMockMode(undefined)).toBe(false);
  expect(isMockMode("TRUE")).toBe(false);
});

test("demo accounts sign in and read their own sample tasks", async () => {
  for (const account of MOCK_ACCOUNTS) {
    const response = await mockRequest("/auth/login", { method: "POST", body: { email: account.email, password: MOCK_PASSWORD } });
    expect(response.user.id).toBe(account.id);
    expect(response.user.role).toBe(account.role);
    localStorage.setItem("todo_demo_user", JSON.stringify(response.user));
    const tasks = await mockRequest("/tasks");
    expect(tasks.length).toBe(1);
    expect(tasks[0]?.created_by).toBe(account.id);
  }
});

test("real mode uses fetch and never reads demo storage", async () => {
  values.set("todo_demo_user", JSON.stringify(MOCK_ACCOUNTS[0]));
  let requested = "";
  globalThis.fetch = (async (input) => {
    requested = String(input);
    return new Response(JSON.stringify({ source: "backend" }));
  });
  const response = await apiFetch("/auth/me");
  expect(response.source).toBe("backend");
  expect(requested.endsWith("/api/v1/auth/me")).toBe(true);
  expect(reads.some((key) => key.startsWith("todo_demo"))).toBe(false);
});
test("data mode: saved choice wins, Lovable host defaults to demo, local defaults to API", async () => {
  const { resolveMockMode } = await import("./data-mode");
  expect(resolveMockMode({ saved: null, env: undefined, hostname: "id-preview--x.lovable.app" })).toBe(true);
  expect(resolveMockMode({ saved: null, env: undefined, hostname: "localhost" })).toBe(false);
  expect(resolveMockMode({ saved: "mock", env: undefined, hostname: "localhost" })).toBe(true);
  expect(resolveMockMode({ saved: "api", env: undefined, hostname: "x.lovable.app" })).toBe(false);
});
