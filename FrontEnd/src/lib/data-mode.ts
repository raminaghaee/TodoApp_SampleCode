/** Explicit opt-in only; unknown or missing values always use the real API. */
export function isMockMode(value: unknown): boolean {
  return value === "true";
}

export const DATA_MODE_KEY = "todo_data_mode";
export type DataMode = "mock" | "api";

/** Lovable-hosted previews/sites have no reachable personal backend, so they default to demo data. */
export function isLovableHost(hostname: string): boolean {
  return /(^|\.)lovable(project)?\.(app|com|dev)$/.test(hostname);
}

/** Priority: user choice saved in the browser → env opt-in → Lovable host default. */
export function resolveMockMode(opts: {
  saved: string | null;
  env: unknown;
  hostname: string | null;
}): boolean {
  if (opts.saved === "mock") return true;
  if (opts.saved === "api") return false;
  if (isMockMode(opts.env)) return true;
  return opts.hostname ? isLovableHost(opts.hostname) : false;
}

function readSaved(): string | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage.getItem(DATA_MODE_KEY);
  } catch {
    return null;
  }
}

export const MOCK_MODE = resolveMockMode({
  saved: readSaved(),
  env: import.meta.env["VITE_USE_MOCK_DATA"],
  hostname: typeof window === "undefined" ? null : window.location.hostname,
});

/** Save the choice and reload so every module picks up the new mode. */
export function setDataMode(mode: DataMode) {
  window.localStorage.setItem(DATA_MODE_KEY, mode);
  window.location.reload();
}
