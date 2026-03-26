import { FormEvent, useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import type { AttendanceRecord, PagedResult } from "../types/hrms";

export function AttendancePage() {
  const [logs, setLogs] = useState<PagedResult<AttendanceRecord> | null>(null);
  const [note, setNote] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [working, setWorking] = useState(false);

  const loadLogs = async () => {
    const response = await apiClient.get<PagedResult<AttendanceRecord>>("/attendance/logs?pageNumber=1&pageSize=15");
    setLogs(response.data);
  };

  useEffect(() => {
    loadLogs();
  }, []);

  const submitAction = async (path: string, event: FormEvent) => {
    event.preventDefault();
    setWorking(true);
    setMessage(null);

    try {
      await apiClient.post(path, { notes: note || null });
      setMessage(path.includes("check-in") ? "Check-in recorded." : "Check-out recorded.");
      setNote("");
      await loadLogs();
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Attendance action failed.");
    } finally {
      setWorking(false);
    }
  };

  const onCheckOut = async () => {
    setWorking(true);
    setMessage(null);

    try {
      await apiClient.post("/attendance/check-out", { notes: note || null });
      setMessage("Check-out recorded.");
      setNote("");
      await loadLogs();
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Attendance action failed.");
    } finally {
      setWorking(false);
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Attendance Workspace"
        subtitle="Mark daily presence, capture notes, and review the latest attendance trail with late and half-day visibility."
      />

      <div className="grid gap-6 xl:grid-cols-[0.8fr_1.2fr]">
        <form className="panel space-y-4 p-6" onSubmit={(event) => submitAction("/attendance/check-in", event)}>
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Daily Actions</p>
          <h2 className="font-display text-2xl text-ink">Mark attendance</h2>
          <textarea className="input min-h-28 transition-all duration-300 focus:-translate-y-0.5" placeholder="Optional note for today's attendance..." value={note} onChange={(event) => setNote(event.target.value)} />

          {message ? <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}

          <div className="flex flex-col gap-3 sm:flex-row">
            <button type="submit" className={`btn-primary ${working ? "pulse-glow" : ""}`} disabled={working}>Check In</button>
            <button type="button" className="btn-secondary" onClick={onCheckOut}>
              Check Out
            </button>
          </div>
        </form>

        <div className="panel p-6">
          <h2 className="font-display text-2xl text-ink">Attendance Logs</h2>
          <div className="mt-5 space-y-3">
            {logs?.items.map((record, index) => (
              <div
                key={record.id}
                className="soft-pop rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1"
                style={{ animationDelay: `${index * 35}ms` }}
              >
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="font-semibold text-ink">{record.workDate}</p>
                    <p className="mt-1 text-sm text-slate-600">
                      In: {new Date(record.checkInUtc).toLocaleString()} {record.checkOutUtc ? `• Out: ${new Date(record.checkOutUtc).toLocaleString()}` : ""}
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="badge bg-amber-50 text-amber-700">{record.status}</span>
                    <span className="text-sm font-semibold text-ink">{record.workedHours} hrs</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
