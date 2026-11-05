import { test, expect, beforeEach } from "bun:test";
import { mockRequest, MOCK_PASSWORD } from "./mock-api";
import { matchingPasswords } from "./account";

const values = new Map();
Object.defineProperty(globalThis, "localStorage", {
  configurable: true,
  value: {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: (key) => values.delete(key),
  },
});
const post = (path, body) => mockRequest(path, { method: "POST", body });
beforeEach(async () => {
  values.clear();
  const { user } = await post("/auth/login", {
    email: "sara@example.test",
    password: MOCK_PASSWORD,
  });
  localStorage.setItem("todo_demo_user", JSON.stringify(user));
});

test("profile changes display name but does not change email directly", async () => {
  const user = await mockRequest("/profiles/me", {
    method: "PUT",
    body: { display_name: "سارا جدید", email: "bypass@example.test" },
  });
  expect(user.display_name).toBe("سارا جدید");
  expect(user.email).toBe("sara@example.test");
  expect((await mockRequest("/auth/me")).display_name).toBe("سارا جدید");
});

test("password change requires current password and new password works", async () => {
  await expect(
    post("/auth/change-password", { current_password: "wrong", new_password: "Changed123!" }),
  ).rejects.toThrow("رمز فعلی اشتباه است.");
  await post("/auth/change-password", {
    current_password: MOCK_PASSWORD,
    new_password: "Changed123!",
  });
  await expect(
    post("/auth/login", { email: "sara@example.test", password: MOCK_PASSWORD }),
  ).rejects.toThrow();
  expect(
    (await post("/auth/login", { email: "sara@example.test", password: "Changed123!" })).user.id,
  ).toBe(2);
});

test("email remains unchanged until confirmation and new email can sign in", async () => {
  const result = await post("/auth/change-email", {
    new_email: "new@example.test",
    current_password: MOCK_PASSWORD,
    redirect_url: "http://localhost/confirm-email",
  });
  expect((await mockRequest("/auth/me")).email).toBe("sara@example.test");
  const token = new URLSearchParams(new URL(result.demo_url).hash.slice(1)).get("token");
  await post("/auth/confirm-email", { token });
  await expect(post("/auth/confirm-email", { token })).rejects.toThrow(
    "لینک نامعتبر یا منقضی شده است.",
  );
  expect(
    (await post("/auth/login", { email: "new@example.test", password: MOCK_PASSWORD })).user.email,
  ).toBe("new@example.test");
});

test("forgotten password can reset signed out without current password", async () => {
  localStorage.removeItem("todo_demo_user");
  const result = await post("/auth/forgot-password", {
    email: "sara@example.test",
    redirect_url: "http://localhost/reset-password",
  });
  const token = new URLSearchParams(new URL(result.demo_url).hash.slice(1)).get("token");
  await post("/auth/reset-password", { token, new_password: "Recovered123!" });
  expect(localStorage.getItem("todo_demo_user")).toBeNull();
  expect(
    (await post("/auth/login", { email: "sara@example.test", password: "Recovered123!" })).user.id,
  ).toBe(2);
});

test("password confirmation must match", () => {
  expect(() => matchingPasswords("One123!", "Other123!")).toThrow(
    "رمز جدید و تکرار آن یکسان نیستند.",
  );
});

test("recovery rejects expired token", async () => {
  const result = await post("/auth/forgot-password", {
    email: "sara@example.test",
    redirect_url: "http://localhost/reset-password",
  });
  const token = new URLSearchParams(new URL(result.demo_url).hash.slice(1)).get("token");
  const store = JSON.parse(localStorage.getItem("todo_demo_data_v2"));
  store.account_tokens[0].expires_at = 0;
  localStorage.setItem("todo_demo_data_v2", JSON.stringify(store));
  await expect(
    post("/auth/reset-password", { token, new_password: "Recovered123!" }),
  ).rejects.toThrow();
});
