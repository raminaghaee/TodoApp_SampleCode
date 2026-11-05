/** Frontend permissions are a UI guard; the dedicated API remains authoritative. */
export function canEditTask(
  task: { assigned_to: number | null; created_by: number },
  userId: number | null | undefined,
  list: { owner_id: number } | null | undefined,
): boolean {
  if (userId == null || !list) return false;
  return list.owner_id === userId || task.assigned_to === userId ||
    (task.assigned_to == null && task.created_by === userId);
}

export function assertTaskWritable(readOnly: boolean): void {
  if (readOnly) throw new Error("شما اجازه تغییر این کار، زیرتسک‌ها و پیوست‌های آن را ندارید.");
}