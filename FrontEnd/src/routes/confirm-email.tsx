import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { MailCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { confirmEmail } from "@/lib/account";
import { clearSession } from "@/lib/api";
import { errorMessage } from "@/lib/api-errors";

export const Route = createFileRoute("/confirm-email")({
  head: () => ({ meta: [
    { title: "تأیید ایمیل جدید | Todo" }, { name: "description", content: "تأیید تغییر ایمیل حساب Todo." },
    { property: "og:title", content: "تأیید ایمیل جدید | Todo" }, { property: "og:description", content: "تکمیل تغییر ایمیل حساب Todo." },
    { property: "og:type", content: "website" }, { name: "twitter:card", content: "summary" },
  ] }), component: ConfirmEmailPage,
});

function ConfirmEmailPage() {
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [done, setDone] = useState(false);
  const [message, setMessage] = useState("");
  useEffect(() => { setToken(new URLSearchParams(window.location.hash.slice(1)).get("token") ?? new URLSearchParams(window.location.search).get("token") ?? ""); window.history.replaceState(null, "", window.location.pathname); }, []);
  return <main className="min-h-screen bg-background px-4 py-12"><div className="mx-auto max-w-md space-y-6">
    <MailCheck className="size-10 text-primary" /><h1 className="text-2xl font-bold">تأیید ایمیل جدید</h1>
    <p role="status" className="text-sm leading-7 text-muted-foreground">{done ? "ایمیل جدید تأیید شد؛ با ایمیل جدید وارد شوید." : message || (token === null ? "در حال بررسی لینک…" : !token ? "لینک تأیید معتبر نیست." : "تغییر ایمیل حساب را تأیید می‌کنید؟")}</p>
    {token && !done && <Button disabled={loading} onClick={async () => { if (loading) return; setLoading(true); setMessage(""); try { await confirmEmail(token); clearSession(); setDone(true); } catch (error) { setMessage(errorMessage(error, "تأیید ایمیل ناموفق بود.")); } finally { setLoading(false); } }}>{loading ? "در حال تأیید…" : "تأیید تغییر ایمیل"}</Button>}
    <Button asChild variant="outline"><Link to="/auth">بازگشت به ورود</Link></Button>
  </div></main>;
}