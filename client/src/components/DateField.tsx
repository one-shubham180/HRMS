import { ChevronLeft, ChevronRight, CalendarDays } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";

interface DateFieldProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
}

const weekdayLabels = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
const monthFormatter = new Intl.DateTimeFormat("en-IN", { month: "long", year: "numeric" });
const displayFormatter = new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" });

function parseIsoDate(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return null;
  }

  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(year, month - 1, day, 12, 0, 0, 0);
  return Number.isNaN(date.getTime()) ? null : date;
}

function toIsoDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function sameDay(left: Date | null, right: Date) {
  return !!left &&
    left.getFullYear() === right.getFullYear() &&
    left.getMonth() === right.getMonth() &&
    left.getDate() === right.getDate();
}

export function DateField({ value, onChange, placeholder = "Select date", className = "" }: DateFieldProps) {
  const [open, setOpen] = useState(false);
  const selectedDate = useMemo(() => parseIsoDate(value), [value]);
  const [visibleMonth, setVisibleMonth] = useState<Date>(() => startOfMonth(selectedDate ?? new Date()));
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (selectedDate) {
      setVisibleMonth(startOfMonth(selectedDate));
    }
  }, [selectedDate]);

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

  const calendarDays = useMemo(() => {
    const monthStart = startOfMonth(visibleMonth);
    const daysInMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + 1, 0).getDate();
    const leadingEmpty = monthStart.getDay();
    const cells: Array<Date | null> = [];

    for (let index = 0; index < leadingEmpty; index += 1) {
      cells.push(null);
    }

    for (let day = 1; day <= daysInMonth; day += 1) {
      cells.push(new Date(visibleMonth.getFullYear(), visibleMonth.getMonth(), day, 12, 0, 0, 0));
    }

    return cells;
  }, [visibleMonth]);

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
          <CalendarDays className="h-4 w-4 shrink-0" />
        </button>
      </div>

      {selectedDate ? <p className="mt-1 pl-1 text-xs text-slate-500">{displayFormatter.format(selectedDate)}</p> : null}

      {open ? (
        <div className="absolute z-[100] mt-2 w-[19rem] max-w-full rounded-3xl border border-slate-200 bg-white p-4 shadow-[0_18px_50px_-28px_rgba(15,23,42,0.35)]">
          <div className="flex items-center justify-between">
            <button
              type="button"
              className="rounded-full p-2 text-slate-500 transition hover:bg-slate-100 hover:text-ink"
              onClick={() => setVisibleMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <p className="text-sm font-semibold text-ink">{monthFormatter.format(visibleMonth)}</p>
            <button
              type="button"
              className="rounded-full p-2 text-slate-500 transition hover:bg-slate-100 hover:text-ink"
              onClick={() => setVisibleMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>

          <div className="mt-4 grid grid-cols-7 gap-1 text-center text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">
            {weekdayLabels.map((label) => (
              <span key={label} className="py-2">{label}</span>
            ))}
          </div>

          <div className="grid grid-cols-7 gap-1">
            {calendarDays.map((date, index) => (
              date ? (
                <button
                  key={toIsoDate(date)}
                  type="button"
                  className={`rounded-2xl py-2 text-sm transition ${
                    sameDay(selectedDate, date)
                      ? "bg-ink text-white"
                      : "text-slate-700 hover:bg-slate-100"
                  }`}
                  onClick={() => {
                    onChange(toIsoDate(date));
                    setOpen(false);
                  }}
                >
                  {date.getDate()}
                </button>
              ) : (
                <span key={`empty-${index}`} className="py-2" />
              )
            ))}
          </div>

          <div className="mt-4 flex items-center justify-between gap-3">
            <button
              type="button"
              className="text-sm font-semibold text-lagoon transition hover:text-ink"
              onClick={() => {
                const today = new Date();
                onChange(toIsoDate(today));
                setVisibleMonth(startOfMonth(today));
                setOpen(false);
              }}
            >
              Use today
            </button>
            <p className="text-xs text-slate-500">Stored as YYYY-MM-DD</p>
          </div>
        </div>
      ) : null}
    </div>
  );
}
