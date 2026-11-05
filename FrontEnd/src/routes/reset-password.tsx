import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { LockKeyhole } from "lucide-react";
import { toast } from "sonner";
import { AccountForm } from "@/components/shared/AccountForm";
import { Button } from "@/components/ui/button";
import { matchingPasswords, resetPassword } from "@/lib/account";
import { errorMessage } from "@/lib/api-errors";
import { clearSession } from "@/lib/api";

export const Route = createFileRoute("/reset-password")({
  head: () => ({ meta: [
    { title: "بازنشانی رمز عبور | Todo" }, { name: "description", content: "تعیین رمز جدید برای حساب Todo با لینک بازیابی." },
    { property: "og:title", content: "بازنشانی رمز عبور | Todo" }, { property: "og:description", content: "تعیین رمز جدید حساب Todo." },
    { property: "og:type", content: "website" }, { name: "twitter:card", content: "summary" },
  ] }), component: ResetPasswordPage,
});

function ResetPasswordPage() {
  const [token, setToken] = useState<string | null>(null);
  const [values, setValues] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [done, setDone] = useState(false);
  useEffect(() => { setToken(new URLSearchParams(window.location.hash.slice(1)).get("token") ?? new URLSearchParams(window.location.search).get("token") ?? ""); window.history.replaceState(null, "", window.location.pathname); }, []);
  return <main className="min-h-screen bg-background px-4 py-12"><div className="mx-auto max-w-md">
    {token && !done ? <AccountForm title="بازنشانی رمز عبور" icon={LockKeyhole} fields={[{ key: "password", label: "رمز جدید", type: "password", autoComplete: "new-password" }, { key: "confirmation", label: "تکرار رمز جدید", type: "password", autoComplete: "new-password" }]} values={values} onChange={(key, value) => setValues(v => ({ ...v, [key]: value }))} loading={loading} submitLabel="ذخیره رمز جدید" onSubmit={async event => {
      event.preventDefault(); if (loading) return; setLoading(true);
      try { matchingPasswords(values["password"] ?? "", values["confirmation"] ?? ""); await resetPassword(token, values["password"] ?? ""); clearSession(); setValues({}); setDone(true); }
      catch (error) { toast.error(errorMessage(error, "بازنشانی رمز ناموفق بود.")); } finally { setLoading(false); }
    }} /> : <div className="space-y-5"><h1 className="text-2xl font-bold">بازنشانی رمز عبور</h1><p role="status" className="text-sm text-muted-foreground">{done ? "رمز جدید ذخیره شد؛ دوباره وارد حساب شوید." : token === null ? "در حال بررسی لینک…" : "لینک بازیابی معتبر نیست. لینک جدید درخواست کنید."}</p><Button asChild><Link to={done ? "/auth" : "/forgot-password"}>{done ? "ورود به حساب" : "درخواست لینک جدید"}</Link></Button></div>}
  </div></main>;
}