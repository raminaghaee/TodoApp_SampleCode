import { Users, ShieldCheck, LogIn } from "lucide-react";
import { Button } from "@/components/ui/button";
import { MOCK_ACCOUNTS, MOCK_PASSWORD } from "@/lib/mock-api";

export default function DemoLogin({ disabled, onLogin }: { disabled: boolean; onLogin: (email: string, password: string) => void }) {
  return (
    <section className="mt-6 border-t border-border pt-5">
      <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold"><Users className="size-4" />حساب‌های نمایشی</h2>
      <div className="space-y-2">
        {MOCK_ACCOUNTS.map((account) => (
          <Button key={account.id} variant="outline" className="h-auto w-full justify-between gap-3 py-3" disabled={disabled} onClick={() => onLogin(account.email, MOCK_PASSWORD)}>
            {account.role === "super_admin" ? <ShieldCheck className="size-4 shrink-0" /> : <Users className="size-4 shrink-0" />}
            <span className="min-w-0 flex-1 text-start"><span className="block">{account.display_name}</span><span dir="ltr" className="block break-all text-xs font-normal text-muted-foreground">{account.email}</span></span>
            <LogIn className="size-4 shrink-0" />
          </Button>
        ))}
      </div>
      <p className="mt-3 text-xs text-muted-foreground">رمز حساب‌ها: <span dir="ltr">{MOCK_PASSWORD}</span></p>
    </section>
  );
}