import { useRef, type ReactNode, type FormEvent } from "react";
import { Link } from "@tanstack/react-router";
import { Save, Loader2, type LucideIcon } from "lucide-react";
import { Toolbar } from "./Toolbar";
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbPage, BreadcrumbSeparator } from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "./PasswordInput";

export type AccountField = { key: string; label: string; type?: "text" | "email" | "password"; autoComplete?: string };

export function AccountForm({ title, icon, fields, values, onChange, onSubmit, loading, children, submitLabel = "ذخیره تغییرات", authenticated = false }: {
  title: string; icon: LucideIcon; fields: AccountField[]; values: Record<string, string>;
  onChange: (key: string, value: string) => void; onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  loading: boolean; children?: ReactNode; submitLabel?: string; authenticated?: boolean;
}) {
  const form = useRef<HTMLFormElement>(null);
  return <>
    <Breadcrumb className="mb-5"><BreadcrumbList>
      <BreadcrumbItem><BreadcrumbLink asChild><Link to={authenticated ? "/tasks" : "/auth"}>{authenticated ? "کارهای من" : "ورود"}</Link></BreadcrumbLink></BreadcrumbItem>
      <BreadcrumbSeparator /><BreadcrumbItem><BreadcrumbPage>{title}</BreadcrumbPage></BreadcrumbItem>
    </BreadcrumbList></Breadcrumb>
    <Toolbar title={title} icon={icon} actions={[{ key: "save", label: submitLabel, icon: Save, onClick: () => form.current?.requestSubmit(), disabled: loading, group: "custom" }]} />
    <form ref={form} onSubmit={onSubmit} className="mt-7 space-y-5">
      {fields.map(field => <div key={field.key} className="space-y-2">
        <Label htmlFor={field.key}>{field.label}</Label>
        {field.type === "password"
          ? <PasswordInput id={field.key} name={field.key} autoComplete={field.autoComplete} required disabled={loading} value={values[field.key] ?? ""} onChange={e => onChange(field.key, e.target.value)} />
          : <Input id={field.key} name={field.key} type={field.type ?? "text"} autoComplete={field.autoComplete} dir={field.type === "email" ? "ltr" : undefined} required disabled={loading} value={values[field.key] ?? ""} onChange={e => onChange(field.key, e.target.value)} />}
      </div>)}
      <Button type="submit" disabled={loading} className="w-full sm:w-auto">{loading ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}{submitLabel}</Button>
      {children}
    </form>
  </>;
}