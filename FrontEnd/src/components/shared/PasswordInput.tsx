import { useState, type ComponentProps } from "react";
import { Eye, EyeOff } from "lucide-react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

type PasswordInputProps = Omit<ComponentProps<typeof Input>, "type">;

/**
 * Shared password field with a show/hide control.
 * Used by every form that asks for a password so the toggle looks and behaves
 * the same everywhere (sign in, sign up, profile, reset password).
 */
export function PasswordInput({ className, disabled, ...props }: PasswordInputProps) {
  const [visible, setVisible] = useState(false);
  const label = visible ? "پنهان کردن رمز عبور" : "نمایش رمز عبور";
  return (
    // Credentials are always typed left-to-right, and some containers force a
    // direction of their own (Radix tabs do), so this wrapper owns it. That way
    // the control at the inline end and the field padding always line up.
    <div className="relative" dir="ltr">
      <Input
        type={visible ? "text" : "password"}
        disabled={disabled}
        className={cn("pe-9", className)}
        {...props}
      />
      <button
        type="button"
        onClick={() => setVisible(value => !value)}
        disabled={disabled}
        aria-pressed={visible}
        aria-label={label}
        title={label}
        className="absolute inset-y-0 end-0 flex w-9 items-center justify-center rounded-e-md text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
      >
        {visible ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
      </button>
    </div>
  );
}
