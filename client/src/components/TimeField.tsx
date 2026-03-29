import { Clock3 } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";

interface TimeFieldProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
}

const timeOptions = Array.from({ length: 48 }, (_, index) => {
  const hours = String(Math.floor(index / 2)).padStart(2, "0");
  const minutes = index % 2 === 0 ? "00" : "30";
  return `${hours}:${minutes}`;
});

function isValidTime(value: string) {
  if (!/^\d{2}:\d{2}$/.test(value)) {
    return false;
  }

  const [hours, minutes] = value.split(":").map(Number);
  return hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59;
}

function formatDisplayTime(value: string) {
  if (!isValidTime(value)) {
    return null;
  }

  const [hours, minutes] = value.split(":").map(Number);
  const date = new Date(2026, 0, 1, hours, minutes, 0, 0);
  return new Intl.DateTimeFormat("en-IN", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).format(date);
}

export function TimeField({ value, onChange, placeholder = "HH:MM", className = "" }: TimeFieldProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const displayValue = useMemo(() => formatDisplayTime(value), [value]);

  useEffect(() => {
    const handlePointerDown = (event: MouseEvent | TouchEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("touchstart", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("touchstart", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, []);

  return (
    <div ref={containerRef} className={`relative ${open ? "z-[90]" : "z-0"} ${className}`}>
      <div className="input flex items-center gap-3 pr-2">
        <input
          className="min-w-0 flex-1 border-0 bg-transparent p-0 text-sm text-slate-800 outline-none placeholder:text-slate-400"
          placeholder={placeholder}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
        <button
          type="button"
          className="rounded-full p-2 text-slate-500 transition hover:bg-slate-100 hover:text-ink"
          onClick={() => setOpen((current) => !current)}
        >
          <Clock3 className="h-4 w-4 shrink-0" />
        </button>
      </div>

      {displayValue ? <p className="mt-1 pl-1 text-xs text-slate-500">{displayValue}</p> : null}

      {open ? (
        <div className="absolute z-[100] mt-2 w-full min-w-[14rem] rounded-3xl border border-slate-200 bg-white p-3 shadow-[0_18px_50px_-28px_rgba(15,23,42,0.35)]">
          <div className="grid max-h-72 grid-cols-2 gap-2 overflow-y-auto pr-1">
            {timeOptions.map((timeOption) => (
              <button
                key={timeOption}
                type="button"
                className={`rounded-2xl px-3 py-2 text-left text-sm transition ${
                  timeOption === value
                    ? "bg-ink text-white"
                    : "text-slate-700 hover:bg-slate-100"
                }`}
                onClick={() => {
                  onChange(timeOption);
                  setOpen(false);
                }}
              >
                {timeOption}
              </button>
            ))}
          </div>

          <div className="mt-3 flex items-center justify-between gap-3">
            <button
              type="button"
              className="text-sm font-semibold text-lagoon transition hover:text-ink"
              onClick={() => {
                onChange("09:00");
                setOpen(false);
              }}
            >
              Use 09:00
            </button>
            <p className="text-xs text-slate-500">Type or pick a time</p>
          </div>
        </div>
      ) : null}
    </div>
  );
}
