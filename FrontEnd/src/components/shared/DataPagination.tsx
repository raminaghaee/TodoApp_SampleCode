import { ChevronRight, ChevronLeft } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

export type DataPaginationProps = {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  pageSizeOptions?: number[];
  compact?: boolean;
  className?: string;
};

/**
 * Shared pagination footer used by every paged data section so the
 * interaction pattern stays identical across the app.
 */
export function DataPagination({
  page,
  pageSize,
  total,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
  compact = false,
  className,
}: DataPaginationProps) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const current = Math.min(page, pageCount);
  const from = total === 0 ? 0 : (current - 1) * pageSize + 1;
  const to = Math.min(current * pageSize, total);

  if (total <= pageSize && !onPageSizeChange) return null;

  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3 text-sm",
        compact && "gap-2 text-xs",
        className,
      )}
    >
      <p className="text-muted-foreground">
        {from}–{to} از {total}
      </p>

      <div className="flex items-center gap-2">
        {onPageSizeChange && !compact && (
          <Select value={String(pageSize)} onValueChange={(v) => onPageSizeChange(Number(v))}>
            <SelectTrigger className="h-9 w-[92px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {pageSizeOptions.map((o) => (
                <SelectItem key={o} value={String(o)}>
                  {o}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        <Button
          variant="outline"
          size={compact ? "sm" : "icon"}
          disabled={current <= 1}
          onClick={() => onPageChange(current - 1)}
          aria-label="صفحه قبل"
        >
          <ChevronRight className="size-4" />
        </Button>
        <span className="text-muted-foreground">
          {current} / {pageCount}
        </span>
        <Button
          variant="outline"
          size={compact ? "sm" : "icon"}
          disabled={current >= pageCount}
          onClick={() => onPageChange(current + 1)}
          aria-label="صفحه بعد"
        >
          <ChevronLeft className="size-4" />
        </Button>
      </div>
    </div>
  );
}
