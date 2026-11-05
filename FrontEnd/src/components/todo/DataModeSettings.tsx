import { useEffect, useState } from "react";
import { Settings } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { MOCK_MODE, setDataMode, type DataMode } from "@/lib/data-mode";

export function DataModeSettings() {
  const [open, setOpen] = useState(false);
  const [current, setCurrent] = useState<DataMode | null>(null);
  useEffect(() => setCurrent(MOCK_MODE ? "mock" : "api"), []);

  const options: { value: DataMode; label: string; text: string }[] = [
    { value: "mock", label: "داده نمایشی", text: "بدون نیاز به سرور، با حساب‌های نمایشی وارد شوید." },
    { value: "api", label: "سرور بک‌اند", text: "اطلاعات از سرور شخصی شما خوانده می‌شود." },
  ];

  return (
    <>
      <Button variant="ghost" size="icon" aria-label="تنظیمات" onClick={() => setOpen(true)}>
        <Settings className="size-5" />
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>منبع اطلاعات</DialogTitle>
            <DialogDescription>انتخاب کنید برنامه از کجا اطلاعات بخواند.</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            {options.map((o) => (
              <Button
                key={o.value}
                variant={current === o.value ? "default" : "outline"}
                className="h-auto w-full flex-col items-start gap-1 py-3 text-start"
                onClick={() => (current === o.value ? setOpen(false) : setDataMode(o.value))}
              >
                <span>{o.label}</span>
                <span className="text-xs font-normal opacity-80">{o.text}</span>
              </Button>
            ))}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
