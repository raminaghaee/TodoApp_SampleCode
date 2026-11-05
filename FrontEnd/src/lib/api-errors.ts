/** Preserve the dedicated backend's ProblemDetails/validation message. */
export function backendErrorMessage(data: unknown, fallback: string): string {
  if (typeof data === "string") return data.trim() || fallback;
  if (!data || typeof data !== "object") return fallback;
  const value = data as Record<string, unknown>;
  for (const key of ["detail", "title", "message"]) {
    const message = value[key];
    if (typeof message === "string" && message.trim()) return message;
  }
  const errors = value["errors"];
  if (errors && typeof errors === "object") {
    for (const entry of Object.values(errors)) {
      const messages = Array.isArray(entry) ? entry : [entry];
      for (const message of messages) {
        if (typeof message === "string" && message.trim()) return message;
      }
    }
  }
  return fallback;
}

export function errorMessage(error: unknown, fallback: string): string {
  return error instanceof Error && error.message.trim() ? error.message : fallback;
}