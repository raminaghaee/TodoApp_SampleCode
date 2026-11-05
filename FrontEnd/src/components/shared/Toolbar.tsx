import { useState, type ReactNode } from "react";
import { Eye, Search, X, SlidersHorizontal, ArrowUpDown } from "lucide-react";
import type { LucideIcon } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

export type ToolbarAction = {
  key: string;
  label: string;
  icon: LucideIcon;
  onClick: () => void;
  variant?: "default" | "outline" | "ghost" | "destructive" | "secondary";
  disabled?: boolean;
  group?: "general" | "common" | "custom" | "print";
};

export type FilterField =
  | { key: string; label: string; type: "select"; options: { value: string; label: string }[] }
  | { key: string; label: string; type: "date" | "text" };

export type FilterValues = Record<string, string>;

export type SortOption = { value: string; label: string };

export type ToolbarProps = {
  title: string;
  description?: string;
  icon?: LucideIcon;
  actions?: ToolbarAction[];
  extra?: ReactNode;
  search?: { value: string; onChange: (v: string) => void; placeholder?: string };
  sort?: { value: string; onChange: (v: string) => void; options: SortOption[] };
  filters?: FilterField[];
  filterValues?: FilterValues;
  onFilterChange?: (key: string, value: string) => void;
  onClearFilters?: () => void;
  leading?: ReactNode;
};

const ALL = "__all__";

/**
 * Shared, configuration-driven page toolbar: title block, grouped actions,
 * a collapsible search panel and a collapsible advanced-filter panel.
 * Reused by every page so the interaction pattern stays identical app-wide.
 */
export function Toolbar({
  title,
  description,
  icon: Icon,
  actions = [],
  extra,
  search,
  sort,
  filters = [],
  filterValues = {},
  onFilterChange,
  onClearFilters,
  leading,
}: ToolbarProps) {
  const [showLabels, setShowLabels] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);

  const activeFilterCount = filters.filter((f) => {
    const v = filterValues[f.key];
    return !!v && v !== ALL;
  }).length;

  const groups: ToolbarAction["group"][] = ["general", "common", "custom", "print"];
  const grouped = groups
    .map((g) => actions.filter((a) => (a.group ?? "custom") === g))
    .filter((g) => g.length > 0);

  return (
    <div className="space-y-3">
      <div className="flex items-start gap-3">
        {leading}
        <div className="min-w-0 flex-1">
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            {Icon && <Icon className="size-6 text-primary" />}
            <span className="truncate">{title}</span>
          </h1>
          {description && <p className="mt-1 text-sm text-muted-foreground">{description}</p>}
        </div>

        <div className="flex flex-wrap items-center justify-end gap-1.5">
          {extra}
          <Button
            variant="ghost"
            size={showLabels ? "default" : "icon"}
            onClick={() => setShowLabels((s) => !s)}
            aria-label="نمایش برچسب دکمه‌ها"
            className="gap-2"
          >
            <Eye className="size-4" />
            {showLabels && <span>برچسب‌ها</span>}
          </Button>

          {grouped.map((group, i) => (
            <div key={i} className="flex items-center gap-1.5">
              {i > 0 && <span className="mx-1 h-6 w-px bg-border" />}
              {group.map((a) => (
                <Button
                  key={a.key}
                  variant={a.variant ?? "ghost"}
                  size={showLabels ? "default" : "icon"}
                  onClick={a.onClick}
                  disabled={a.disabled}
                  aria-label={a.label}
                  title={a.label}
                  className="gap-2"
                >
                  <a.icon className="size-4" />
                  {showLabels && <span>{a.label}</span>}
                </Button>
              ))}
            </div>
          ))}

          {sort && (
            <Select value={sort.value} onValueChange={sort.onChange}>
              <SelectTrigger className="h-9 w-[160px] gap-2" aria-label="مرتب‌سازی">
                <ArrowUpDown className="size-4 text-muted-foreground" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {sort.options.map((o) => (
                  <SelectItem key={o.value} value={o.value}>
                    {o.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}

          {filters.length > 0 && (
            <Button
              variant={activeFilterCount ? "default" : "ghost"}
              size={showLabels ? "default" : "icon"}
              onClick={() => setFiltersOpen((s) => !s)}
              aria-label="فیلترها"
              title="فیلترها"
              className="gap-2"
            >
              <SlidersHorizontal className="size-4" />
              {showLabels && <span>فیلترها</span>}
              {activeFilterCount ? (
                <span className="rounded-full bg-background/20 px-1.5 text-xs">
                  {activeFilterCount}
                </span>
              ) : null}
            </Button>
          )}

          {search && (
            <Button
              variant={searchOpen ? "default" : "ghost"}
              size={showLabels ? "default" : "icon"}
              onClick={() => setSearchOpen((s) => !s)}
              aria-label="جستجو"
              title="جستجو"
              className="gap-2"
            >
              <Search className="size-4" />
              {showLabels && <span>جستجو</span>}
            </Button>
          )}
        </div>
      </div>

      {search && searchOpen && (
        <div className="relative rounded-xl border border-border bg-card p-3 shadow-panel">
          <Search className="pointer-events-none absolute end-6 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            autoFocus
            value={search.value}
            onChange={(e) => search.onChange(e.target.value)}
            placeholder={search.placeholder ?? "جستجو…"}
            className="h-10 border-0 bg-transparent pe-9 shadow-none focus-visible:ring-0"
          />
        </div>
      )}

      {filters.length > 0 && filtersOpen && (
        <div className="rounded-xl border border-border bg-card p-4 shadow-panel">
          <div className={cn("grid gap-3", "sm:grid-cols-2 lg:grid-cols-4")}>
            {filters.map((f) => (
              <div key={f.key} className="space-y-1.5">
                <Label className="text-xs text-muted-foreground">{f.label}</Label>
                {f.type === "select" ? (
                  <Select
                    value={filterValues[f.key] || ALL}
                    onValueChange={(v) => onFilterChange?.(f.key, v === ALL ? "" : v)}
                  >
                    <SelectTrigger className="h-10">
                      <SelectValue placeholder="همه" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={ALL}>همه</SelectItem>
                      {f.options.map((o) => (
                        <SelectItem key={o.value} value={o.value}>
                          {o.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <Input
                    type={f.type === "date" ? "date" : "text"}
                    value={filterValues[f.key] ?? ""}
                    onChange={(e) => onFilterChange?.(f.key, e.target.value)}
                    className="h-10"
                  />
                )}
              </div>
            ))}
          </div>
          <div className="mt-3 flex justify-end">
            <Button variant="ghost" size="sm" className="gap-2" onClick={onClearFilters}>
              <X className="size-4" />
              پاک کردن فیلترها
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
