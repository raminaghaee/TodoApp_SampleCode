import { createFileRoute, Link } from "@tanstack/react-router";
import { CheckCircle2, Users, Sun, Star, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DataModeSettings } from "@/components/todo/DataModeSettings";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "Todo — کارها را ساده مدیریت کن" },
      {
        name: "description",
        content:
          "لیست کارهای شخصی و تیمی: روز من، کارهای مهم، ارجاع تسک به همکاران و اشتراک‌گذاری لیست‌ها.",
      },
      { property: "og:title", content: "Todo — کارها را ساده مدیریت کن" },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary" },
      {
        property: "og:description",
        content: "لیست کارهای شخصی و تیمی با ارجاع تسک و اشتراک‌گذاری بین کاربران.",
      },
    ],
  }),
  component: Landing,
});

const features = [
  {
    icon: Sun,
    title: "روز من",
    text: "هر روز صبح کارهای مهم امروز را انتخاب کن و تمرکزت را از دست نده.",
  },
  {
    icon: Users,
    title: "ارجاع و اشتراک",
    text: "لیست‌ها را با همکارانت به اشتراک بگذار و تسک‌ها را به آن‌ها ارجاع بده.",
  },
  {
    icon: Star,
    title: "کارهای مهم",
    text: "با یک ستاره، تسک‌های حیاتی را جدا کن و همیشه در دسترس داشته باش.",
  },
];

function Landing() {
  return (
    <main className="min-h-screen bg-background">
      <header className="mx-auto flex max-w-6xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-2">
          <span className="flex size-9 items-center justify-center rounded-xl bg-gradient-hero text-primary-foreground">
            <CheckCircle2 className="size-5" />
          </span>
          <span className="text-lg font-bold">Todo</span>
        </div>
        <div className="flex items-center gap-1">
          <DataModeSettings />
          <Button asChild variant="ghost">
            <Link to="/auth">ورود / ثبت‌نام</Link>
          </Button>
        </div>
      </header>

      <section className="mx-auto grid max-w-6xl items-center gap-12 px-6 pb-16 pt-10 lg:grid-cols-2">
        <div>
          <p className="mb-4 inline-flex rounded-full bg-primary-soft px-3 py-1 text-xs font-medium text-accent-foreground">
            مدیریت کار، ساده و تیمی
          </p>
          <h1 className="text-balance-fa text-4xl font-bold leading-tight sm:text-5xl">
            کارهایت را بنویس، انجام بده، با تیم پیش برو
          </h1>
          <p className="mt-5 max-w-xl text-base leading-8 text-muted-foreground">
            لیست‌های شخصی بساز، برای هر کار یادداشت و سررسید تعیین کن، و کارها را بین اعضای تیم
            تقسیم کن. همه‌چیز در یک محیط تمیز و سریع.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Button asChild size="lg">
              <Link to="/auth">
                شروع کن
                <ArrowLeft className="size-4" />
              </Link>
            </Button>
            <Button asChild size="lg" variant="outline">
              <Link to="/auth">
                حساب دارم
              </Link>
            </Button>
          </div>
        </div>

        <div className="rounded-3xl bg-gradient-hero p-1 shadow-float">
          <div className="rounded-[calc(1.5rem-2px)] bg-card p-6">
            <p className="text-sm font-semibold text-muted-foreground">روز من</p>
            <ul className="mt-4 space-y-3">
              {[
                "آماده‌سازی گزارش هفتگی",
                "بازبینی طراحی صفحه ورود",
                "تماس با تیم پشتیبانی",
                "ارجاع تسک انتشار نسخه به علی",
              ].map((t, i) => (
                <li
                  key={t}
                  className="flex items-center gap-3 rounded-xl border border-border bg-background px-4 py-3 text-sm"
                >
                  <span
                    className={`size-5 shrink-0 rounded-full border-2 ${
                      i === 0 ? "border-primary bg-primary" : "border-muted-foreground/40"
                    }`}
                  />
                  <span className={i === 0 ? "text-muted-foreground line-through" : ""}>{t}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <section className="mx-auto grid max-w-6xl gap-5 px-6 pb-24 md:grid-cols-3">
        {features.map((f) => (
          <div key={f.title} className="rounded-2xl border border-border bg-card p-6 shadow-panel">
            <span className="flex size-10 items-center justify-center rounded-xl bg-primary-soft text-accent-foreground">
              <f.icon className="size-5" />
            </span>
            <h2 className="mt-4 text-lg font-semibold">{f.title}</h2>
            <p className="mt-2 text-sm leading-7 text-muted-foreground">{f.text}</p>
          </div>
        ))}
      </section>
    </main>
  );
}
