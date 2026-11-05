import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { NotificationHistory } from "@/components/todo/NotificationHistory";
import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { UserRound, Mail, LockKeyhole } from "lucide-react";
import { toast } from "sonner";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { AccountForm, type AccountField } from "@/components/shared/AccountForm";
import { changePassword, matchingPasswords, requestEmailChange, updateDisplayName } from "@/lib/account";
import { errorMessage } from "@/lib/api-errors";
import { MOCK_MODE } from "@/lib/data-mode";

const TABS = ["name", "email", "password", "notifications"] as const;
type ProfileTab = (typeof TABS)[number];

export const Route = createFileRoute("/_authenticated/profile")({
  validateSearch: (search: Record<string, unknown>): { tab?: ProfileTab } =>
    TABS.includes(search["tab"] as ProfileTab) ? { tab: search["tab"] as ProfileTab } : {},
  head: () => ({ meta: [
    { title: "پروفایل من | Todo" }, { name: "description", content: "نام نمایشی، ایمیل و رمز حساب Todo خود را مدیریت کنید." },
    { property: "og:title", content: "پروفایل من | Todo" }, { property: "og:description", content: "مدیریت اطلاعات و امنیت حساب Todo." },
    { property: "og:type", content: "website" }, { name: "twitter:card", content: "summary" },
  ] }), component: ProfilePage,
});

const sections: Record<string, { title: string; icon: typeof UserRound; fields: AccountField[] }> = {
  name: { title: "نام نمایشی", icon: UserRound, fields: [{ key: "display_name", label: "نام نمایشی", autoComplete: "nickname" }] },
  email: { title: "تغییر ایمیل", icon: Mail, fields: [{ key: "email", label: "ایمیل جدید", type: "email", autoComplete: "email" }, { key: "current_password", label: "رمز فعلی", type: "password", autoComplete: "current-password" }] },
  password: { title: "تغییر رمز عبور", icon: LockKeyhole, fields: [{ key: "current_password", label: "رمز فعلی", type: "password", autoComplete: "current-password" }, { key: "password", label: "رمز جدید", type: "password", autoComplete: "new-password" }, { key: "confirmation", label: "تکرار رمز جدید", type: "password", autoComplete: "new-password" }] },
};

function ProfilePage() {
  const { user } = Route.useRouteContext();
  const queryClient = useQueryClient();
  const navigate = useNavigate({ from: "/profile" });
  const tab: ProfileTab = Route.useSearch().tab ?? "name";
  const [values, setValues] = useState<Record<string, string>>({ display_name: user.user_metadata.display_name ?? "", email: user.email ?? "" });
  const [loading, setLoading] = useState(false);
  const [notice, setNotice] = useState("");
  const [demoUrl, setDemoUrl] = useState("");
  const section = sections[tab] ?? sections["name"];
  if (!section) return null;
  return <main className="min-h-screen bg-background px-4 py-8 sm:px-8"><div className="mx-auto max-w-xl">
    <Tabs value={tab} onValueChange={value => { navigate({ search: { tab: value as ProfileTab }, replace: true }); setNotice(""); setDemoUrl(""); setValues(v => ({ ...v, current_password: "", password: "", confirmation: "" })); }}>
      <TabsList className="mb-5 grid w-full grid-cols-4"><TabsTrigger value="name">پروفایل</TabsTrigger><TabsTrigger value="email">ایمیل</TabsTrigger><TabsTrigger value="password">رمز عبور</TabsTrigger><TabsTrigger value="notifications">اعلان‌ها</TabsTrigger></TabsList>
    </Tabs>
    {tab === "notifications" ? <NotificationHistory /> :
    <AccountForm title={section.title} icon={section.icon} fields={section.fields} values={values} onChange={(key, value) => setValues(v => ({ ...v, [key]: value }))} loading={loading} authenticated onSubmit={async event => {
      event.preventDefault(); if (loading) return; setLoading(true); setNotice(""); setDemoUrl("");
      try {
        if (tab === "name") { await updateDisplayName(values["display_name"] ?? ""); await queryClient.invalidateQueries({ queryKey: ["profiles"] }); toast.success("نام نمایشی ذخیره شد."); }
        else if (tab === "email") { const result = await requestEmailChange(values["email"] ?? "", values["current_password"] ?? ""); setNotice(MOCK_MODE ? "تغییر ایمیل نمایشی در انتظار تأیید است؛ ایمیلی ارسال نمی‌شود." : "لینک تأیید به ایمیل جدید ارسال شد. تا تأیید، ایمیل قبلی فعال است."); if (MOCK_MODE) setDemoUrl(result.demo_url ?? ""); }
        else { matchingPasswords(values["password"] ?? "", values["confirmation"] ?? ""); await changePassword(values["current_password"] ?? "", values["password"] ?? ""); toast.success("رمز عبور تغییر کرد."); }
        setValues(v => ({ ...v, current_password: "", password: "", confirmation: "" }));
      } catch (error) { toast.error(errorMessage(error, "ذخیره تغییرات ناموفق بود.")); } finally { setLoading(false); }
    }}>
      {notice && <p role="status" className="text-sm leading-7 text-muted-foreground">{notice}</p>}
      {demoUrl && <Button asChild variant="outline"><a href={demoUrl}>تأیید ایمیل نمایشی</a></Button>}
      {tab === "password" && <Button asChild variant="link"><Link to="/forgot-password">رمز فعلی را فراموش کرده‌ام</Link></Button>}
    </AccountForm>}
  </div></main>;
}