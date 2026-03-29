import { AlertCircle, CheckCircle2, Info, X } from "lucide-react";

type ToastTone = "success" | "error" | "info";

interface ToastBannerProps {
  title: string;
  message: string;
  tone?: ToastTone;
  onDismiss?: () => void;
}

const toneStyles: Record<ToastTone, { container: string; icon: typeof CheckCircle2 }> = {
  success: {
    container: "border-emerald-200 bg-emerald-50/95 text-emerald-900 shadow-[0_18px_40px_-24px_rgba(5,150,105,0.45)]",
    icon: CheckCircle2,
  },
  error: {
    container: "border-rose-200 bg-rose-50/95 text-rose-900 shadow-[0_18px_40px_-24px_rgba(225,29,72,0.45)]",
    icon: AlertCircle,
  },
  info: {
    container: "border-lagoon/25 bg-white/95 text-ink shadow-panel",
    icon: Info,
  },
};

export function ToastBanner({ title, message, tone = "info", onDismiss }: ToastBannerProps) {
  const Icon = toneStyles[tone].icon;

  return (
    <div
      aria-live="polite"
      className={`soft-pop flex items-start gap-3 rounded-3xl border px-4 py-3 backdrop-blur ${toneStyles[tone].container}`}
    >
      <span className="mt-0.5 rounded-2xl bg-white/70 p-2">
        <Icon className="h-4 w-4" strokeWidth={2.2} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold">{title}</p>
        <p className="mt-1 text-sm opacity-90">{message}</p>
      </div>
      {onDismiss ? (
        <button
          type="button"
          aria-label="Dismiss notification"
          className="rounded-full p-1 opacity-70 transition hover:bg-white/70 hover:opacity-100"
          onClick={onDismiss}
        >
          <X className="h-4 w-4" />
        </button>
      ) : null}
    </div>
  );
}
