import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { lazy, Suspense, useEffect, useState } from "react";
import { CheckCircle2, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { PasswordInput } from "@/components/shared/PasswordInput";
import { getCurrentUser, login, register } from "@/lib/auth";
import { MOCK_MODE } from "@/lib/data-mode";

const DemoLogin = lazy(() => import("@/components/todo/DemoLogin"));

export const Route = createFileRoute("/auth")({
  head: () => ({
    meta: [
      { title: "ورود به Todo" },
      { name: "description", content: "وارد حساب Todo خود شوید یا حساب جدید بسازید." },
      { property: "og:title", content: "ورود به Todo" },
      { property: "og:description", content: "ورود و ثبت‌نام در اپلیکیشن مدیریت کارها." },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary" },
    ],
  }),
  component: AuthPage,
});

function AuthPage() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [name, setName] = useState("");
  const [sentConfirm, setSentConfirm] = useState(false);
  // Mode depends on browser storage/hostname; read after hydration to avoid SSR mismatch.
  const [mockMode, setMockMode] = useState(false);
  useEffect(() => setMockMode(MOCK_MODE), []);

  useEffect(() => {
    getCurrentUser().then((user) => {
      if (user) navigate({ to: "/tasks", replace: true });
    });
  }, [navigate]);

  async function signIn(e: React.FormEvent) {
    e.preventDefault();
    await signInWithCredentials(email, password);
  }

  async function signInWithCredentials(loginEmail: string, loginPassword: string) {
    setLoading(true);
    try {
      await login(loginEmail, loginPassword);
      navigate({ to: "/tasks", replace: true });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "ورود ناموفق بود.");
    } finally {
      setLoading(false);
    }
  }

  async function signUp(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      const result = await register(email, password, name || email.split("@")[0] || "کاربر");
      if (result.needsConfirmation) {
        setSentConfirm(true);
        toast.success("ایمیل تأیید ارسال شد. صندوق ورودی خود را بررسی کنید.");
        return;
      }
      navigate({ to: "/tasks", replace: true });
    } catch (err) {
      toast.error(`ثبت‌نام ناموفق بود: ${err instanceof Error ? err.message : "خطای ناشناخته"}`);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4 py-12">
      <div className="w-full max-w-md">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <span className="flex size-12 items-center justify-center rounded-2xl bg-gradient-hero text-primary-foreground">
            <CheckCircle2 className="size-6" />
          </span>
          <h1 className="text-2xl font-bold">به Todo خوش آمدید</h1>
          <p className="text-sm text-muted-foreground">
            کارهای شخصی و تیمی‌ات را در یک جا مدیریت کن.
          </p>
        </div>

        <div className="rounded-2xl border border-border bg-card p-6 shadow-panel">
          <Tabs defaultValue="signin">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="signin">ورود</TabsTrigger>
              <TabsTrigger value="signup" disabled={mockMode}>ثبت‌نام</TabsTrigger>
            </TabsList>

            <TabsContent value="signin" className="mt-6">
              <form onSubmit={signIn} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="email">ایمیل</Label>
                  <Input
                    id="email"
                    type="email"
                    dir="ltr"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="you@example.com"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="password">رمز عبور</Label>
                  <PasswordInput id="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
                </div>
                <Button type="submit" className="w-full" disabled={loading}>
                  {loading && <Loader2 className="size-4 animate-spin" />}
                  ورود
                </Button>
                <Button asChild variant="link" className="w-full"><Link to="/forgot-password">رمز عبور را فراموش کرده‌ام</Link></Button>
              </form>
            </TabsContent>

            <TabsContent value="signup" className="mt-6">
              {sentConfirm ? (
                <p className="rounded-xl bg-primary-soft p-4 text-sm leading-7 text-accent-foreground">
                  لینک تأیید به ایمیل شما ارسال شد. پس از تأیید، وارد حساب خود شوید.
                </p>
              ) : (
                <form onSubmit={signUp} className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="name">نام نمایشی</Label>
                    <Input
                      id="name"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      placeholder="مثلاً سارا رضایی"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="email-up">ایمیل</Label>
                    <Input
                      id="email-up"
                      type="email"
                      dir="ltr"
                      required
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="you@example.com"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="password-up">رمز عبور</Label>
                    <PasswordInput id="password-up" required minLength={6} value={password} onChange={(e) => setPassword(e.target.value)} />
                  </div>
                  <Button type="submit" className="w-full" disabled={loading}>
                    {loading && <Loader2 className="size-4 animate-spin" />}
                    ساخت حساب
                  </Button>
                </form>
              )}
            </TabsContent>
          </Tabs>
          {mockMode && <Suspense fallback={null}><DemoLogin disabled={loading} onLogin={signInWithCredentials} /></Suspense>}
        </div>
      </div>
    </main>
  );
}
