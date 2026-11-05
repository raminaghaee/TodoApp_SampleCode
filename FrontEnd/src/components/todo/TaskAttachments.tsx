import { confirmDelete } from "@/components/shared/ConfirmDialog";
import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, Loader2, Paperclip, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { errorMessage } from "@/lib/api-errors";
import { assertTaskWritable } from "@/lib/task-access";
import {
  deleteAttachment,
  fetchAttachments,
  formatBytes,
  getAttachmentUrl,
  uploadAttachment,
  type TaskAttachment,
} from "@/lib/task-details";

type Props = { taskId: number; userId: number; readOnly?: boolean };

const MAX_BYTES = 20 * 1024 * 1024;

export function TaskAttachments({ taskId, userId, readOnly = false }: Props) {
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);

  const q = useQuery({
    queryKey: ["attachments", taskId],
    queryFn: () => fetchAttachments(taskId),
  });
  const files = q.data ?? [];
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["attachments", taskId] });

  const remove = useMutation({
    mutationFn: async (a: TaskAttachment) => {
      assertTaskWritable(readOnly);
      return deleteAttachment(a);
    },
    onSuccess: invalidate,
    onError: (error) => toast.error(errorMessage(error, "حذف پیوست انجام نشد.")),
  });

  async function onPick(list: FileList | null) {
    if (readOnly || !list?.length) return;
    setBusy(true);
    try {
      for (const file of Array.from(list)) {
        if (file.size > MAX_BYTES) {
          toast.error(`«${file.name}» بزرگ‌تر از ۲۰ مگابایت است.`);
          continue;
        }
        await uploadAttachment(taskId, userId, file);
      }
      invalidate();
    } catch (error) {
      toast.error(errorMessage(error, "بارگذاری فایل انجام نشد."));
    } finally {
      invalidate();
      setBusy(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  }

  async function openFile(a: TaskAttachment) {
    try {
      const url = await getAttachmentUrl(a.id);
      window.open(url, "_blank", "noopener");
    } catch (error) {
      toast.error(errorMessage(error, "دریافت فایل انجام نشد."));
    }
  }

  return (
    <div className="space-y-3">
      <Label className="flex items-center gap-2">
        <Paperclip className="size-4" />
        پیوست‌ها
        {files.length > 0 && (
          <span className="text-xs font-normal text-muted-foreground">{files.length} فایل</span>
        )}
      </Label>

      <div className="space-y-1">
        {files.map((a) => (
          <div
            key={a.id}
            className="group flex items-center gap-3 rounded-lg border border-border bg-card px-3 py-2"
          >
            <Paperclip className="size-4 shrink-0 text-muted-foreground" />
            <button onClick={() => openFile(a)} className="min-w-0 flex-1 text-start">
              <span className="block truncate text-sm">{a.file_name}</span>
              <span className="text-xs text-muted-foreground">{formatBytes(a.size_bytes)}</span>
            </button>
            <button onClick={() => openFile(a)} aria-label={`دانلود ${a.file_name}`}>
              <Download className="size-3.5 text-muted-foreground hover:text-primary" />
            </button>
            {!readOnly && <button
              onClick={async () => { if (await confirmDelete({ description: `فایل «${a.file_name}» حذف شود؟`, hint: "این عمل قابل بازگشت نیست." })) remove.mutate(a); }}
              aria-label={`حذف ${a.file_name}`}
              className="opacity-0 transition-opacity group-hover:opacity-100"
            >
              <Trash2 className="size-3.5 text-destructive" />
            </button>}
          </div>
        ))}
      </div>

      {!readOnly && <>
      <input
        ref={inputRef}
        type="file"
        multiple
        className="hidden"
        onChange={(e) => onPick(e.target.files)}
      />
      <Button
        variant="outline"
        className="w-full gap-2"
        disabled={busy}
        onClick={() => inputRef.current?.click()}
      >
        {busy ? <Loader2 className="size-4 animate-spin" /> : <Upload className="size-4" />}
        افزودن فایل
      </Button>
      <p className="text-xs text-muted-foreground">حداکثر حجم هر فایل ۲۰ مگابایت.</p>
      </>}
    </div>
  );
}
