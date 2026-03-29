import { useEffect, useMemo, useState } from "react";
import { CalendarClock } from "lucide-react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { EmptyStateCard } from "../components/EmptyStateCard";
import { PageHeader } from "../components/PageHeader";
import { useAuthStore } from "../features/auth/authStore";
import type { RosterAssignment } from "../types/hrms";

function getLocalIsoDate(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    weekday: "short",
  });
}

function formatTime(value?: string | null) {
  if (!value) {
    return null;
  }

  const [hours, minutes] = value.split(":").map(Number);
  if (Number.isNaN(hours) || Number.isNaN(minutes)) {
    return value;
  }

  const date = new Date(2026, 0, 1, hours, minutes, 0, 0);
  return new Intl.DateTimeFormat("en-IN", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).format(date);
}

export function MyRosterPage() {
  const roles = useAuthStore((state) => state.roles);
  const employeeId = useAuthStore((state) => state.employeeId);
  const isEmployeeView = roles.includes("Employee") && Boolean(employeeId);
  const [rosters, setRosters] = useState<RosterAssignment[]>([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState<string | null>(null);

  const today = getLocalIsoDate();
  const todayRoster = useMemo(
    () => rosters.find((roster) => roster.workDate === today) ?? null,
    [rosters, today],
  );

  useEffect(() => {
    const loadRosters = async () => {
      if (!isEmployeeView) {
        setLoading(false);
        return;
      }

      setLoading(true);
      setMessage(null);

      try {
        const endDate = getLocalIsoDate(new Date(Date.now() + 29 * 24 * 60 * 60 * 1000));
        const response = await apiClient.get<RosterAssignment[]>(
          `/workforce/my-rosters?startDate=${today}&endDate=${endDate}`,
        );
        setRosters(response.data);
      } catch (error: any) {
        setMessage(error.response?.data?.message ?? "Could not load your shift roster.");
      } finally {
        setLoading(false);
      }
    };

    void loadRosters();
  }, [isEmployeeView, today]);

  if (!isEmployeeView) {
    return (
      <AnimatedPage>
        <PageHeader
          title="My Roster"
          subtitle="Upcoming shift assignments are available for employees with an active employee profile."
        />
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <PageHeader
        title="My Roster"
        subtitle="See today’s assignment and the next 30 days of shift planning without waiting on HR updates."
      />

      {message ? (
        <div className="soft-pop mt-6 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div>
      ) : null}

      <div className="mt-6 grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <div className="panel soft-pop space-y-4 p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Today</p>
          {todayRoster ? (
            <>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h2 className="font-display text-2xl text-ink">{todayRoster.shiftName}</h2>
                  <p className="mt-2 text-sm text-slate-600">
                    {todayRoster.shiftStartTimeLocal && todayRoster.shiftEndTimeLocal
                      ? `${formatTime(todayRoster.shiftStartTimeLocal)} to ${formatTime(todayRoster.shiftEndTimeLocal)}`
                      : "Shift timing is not available."}
                  </p>
                </div>
                {todayRoster.isRestDay ? (
                  <span className="badge bg-amber-50 text-amber-700">Rest Day</span>
                ) : (
                  <span className="badge bg-lagoon/10 text-lagoon">{todayRoster.shiftHours} hrs</span>
                )}
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                  Break: <span className="font-semibold text-ink">{todayRoster.breakMinutes} min</span>
                </div>
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                  Date: <span className="font-semibold text-ink">{formatDate(todayRoster.workDate)}</span>
                </div>
              </div>
              {todayRoster.notes ? (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
                  {todayRoster.notes}
                </div>
              ) : null}
            </>
          ) : (
            <EmptyStateCard
              title="No shift assigned for today"
              description="You do not have a roster entry for today yet. Contact HR if you expect to be scheduled."
            />
          )}
        </div>

        <div className="panel p-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Upcoming Schedule</p>
              <h2 className="mt-2 font-display text-2xl text-ink">Next 30 days</h2>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              {rosters.length} assignments loaded
            </div>
          </div>

          <div className="mt-5 space-y-3">
            {loading ? (
              <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">
                Loading your roster...
              </div>
            ) : null}

            {!loading && !rosters.length ? (
              <EmptyStateCard
                title="No upcoming shifts"
                description="Your upcoming roster is empty for the next 30 days."
                icon={<CalendarClock className="h-8 w-8 text-lagoon" strokeWidth={1.8} />}
              />
            ) : null}

            {!loading
              ? rosters.map((roster) => (
                  <div key={roster.id} className="soft-pop rounded-2xl border border-slate-100 bg-slate-50 p-4">
                    <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                      <div>
                        <div className="flex items-center gap-3">
                          <p className="font-semibold text-ink">{roster.shiftName}</p>
                          {roster.isRestDay ? (
                            <span className="badge bg-amber-50 text-amber-700">Rest Day</span>
                          ) : null}
                        </div>
                        <p className="mt-2 text-sm text-slate-600">{formatDate(roster.workDate)}</p>
                        <p className="mt-1 text-sm text-slate-600">
                          {roster.shiftStartTimeLocal && roster.shiftEndTimeLocal
                            ? `${formatTime(roster.shiftStartTimeLocal)} to ${formatTime(roster.shiftEndTimeLocal)}`
                            : "Timing unavailable"}
                        </p>
                        {roster.notes ? <p className="mt-2 text-sm text-slate-600">{roster.notes}</p> : null}
                      </div>
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="badge bg-lagoon/10 text-lagoon">{roster.shiftHours} hrs</span>
                        <span className="badge bg-slate-200 text-slate-700">{roster.breakMinutes} min break</span>
                        {roster.workDate === today ? (
                          <span className="badge bg-emerald-50 text-emerald-700">Today</span>
                        ) : null}
                      </div>
                    </div>
                  </div>
                ))
              : null}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
