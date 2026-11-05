import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { Mail } from "lucide-react";
import { toast } from "sonner";
import { AccountForm } from "@/components/shared/AccountForm";
import { Button } from "@/components/ui/button";
import { forgotPassword } from "@/lib/account";
import { errorMessage } from "@/lib/api-errors";
import { MOCK_MODE } from "@/lib/data-mode";

export const Route = createFileRoute("/forgot-password")({
  head: () => ({ meta: [
    { title: "فراموشی رمز عبور | Todo" }, { name: "description", content: "درخواست بازیابی رمز عبور حساب Todo با ایمیل." },
    { property: "og:title", content: "فراموشی رمز عبور | Todo" }, { property: "og:description", content: "بازیابی دسترسی به حساب Todo." },
    { property: "og:type", content: "website" }, { name: "twitter:card", content: "summary" },
  ] }), component: ForgotPasswordPage,
});

function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);
  const [demoUrl, setDemoUrl] = useState("");
  const [mock, setMock] = useState(false);
  useEffect(() => setMock(MOCK_MODE), []);
  return <main className="min-h-screen bg-background px-4 py-12"><div className="mx-auto max-w-md">
    <AccountForm title="فراموشی رمز عبور" icon={Mail} fields={sent ? [] : [{ key: "email", label: "ایمیل حساب", type: "email", autoComplete: "email" }]} values={{ email }} onChange={(_, value) => { setEmail(value); setSent(false); }} loading={loading} submitLabel={sent ? "ارسال دوباره لینک" : "ارسال لینک بازیابی"} onSubmit={async event => {
      event.preventDefault(); if (loading) return; setLoading(true); setDemoUrl("");
      try { const result = await forgotPassword(email); setSent(true); if (MOCK_MODE) setDemoUrl(result.demo_url ?? ""); }
      catch (error) { toast.error(errorMessage(error, "درخواست بازیابی ناموفق بود.")); } finally { setLoading(false); }
    }}>
      {sent && <p role="status" className="text-sm leading-7 text-muted-foreground">{mock ? "در حالت نمایشی ایمیلی ارسال نمی‌شود؛ برای حساب نمایشی موجود لینک آزمایشی آماده است." : "اگر حسابی با این ایمیل وجود داشته باشد، لینک بازیابی ارسال می‌شود."}</p>}
      {demoUrl && <Button asChild variant="outline"><a href={demoUrl}>بازنشانی رمز نمایشی</a></Button>}
    </AccountForm>
  </div></main>;
}