import { useEffect, useState } from "react";
import { TriangleAlert } from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { buttonVariants } from "@/components/ui/button";

type ConfirmOptions = {
  title?: string;
  description: string;
  confirmLabel?: string;
  /** Short warning line shown under the description, e.g. "این عمل قابل بازگشت نیست." */
  hint?: string;
};
type Pending = ConfirmOptions & { resolve: (ok: boolean) => void };

let show: ((p: Pending) => void) | null = null;

/** Shared delete/destructive confirmation. Resolves true only when the user confirms. */
export function confirmDelete(options: ConfirmOptions): Promise<boolean> {
  return new Promise((resolve) => {
    if (!show) return resolve(false);
    show({ ...options, resolve });
  });
}

/** Mounted once in the root layout. */
export function ConfirmDialogHost() {
  const [pending, setPending] = useState<Pending | null>(null);
  useEffect(() => {
    show = setPending;
    return () => { show = null; };
  }, []);

  const close = (ok: boolean) => {
    pending?.resolve(ok);
    setPending(null);
  };

  return (
    <AlertDialog open={!!pending} onOpenChange={(o) => !o && close(false)}>
      <AlertDialogContent className="max-w-md border-destructive/45 bg-card ring-1 ring-destructive/15">
        <AlertDialogHeader className="flex-row items-start gap-3 space-y-0 text-start">
          <span
            aria-hidden
            className="flex size-10 shrink-0 items-center justify-center rounded-full bg-destructive/10 text-destructive ring-1 ring-destructive/30"
          >
            <TriangleAlert className="size-5" />
          </span>
          <div className="min-w-0 flex-1 space-y-1.5 text-start">
            <AlertDialogTitle className="text-destructive">
              {pending?.title ?? "تأیید حذف"}
            </AlertDialogTitle>
            <AlertDialogDescription className="text-start text-foreground/80">
              {pending?.description}
            </AlertDialogDescription>
            {pending?.hint && (
              <p className="text-xs font-medium text-warning">{pending.hint}</p>
            )}
          </div>
        </AlertDialogHeader>
        <AlertDialogFooter className="gap-2">
          <AlertDialogCancel onClick={() => close(false)}>انصراف</AlertDialogCancel>
          <AlertDialogAction
            className={buttonVariants({ variant: "destructive" })}
            onClick={() => close(true)}
          >
            {pending?.confirmLabel ?? "حذف"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
