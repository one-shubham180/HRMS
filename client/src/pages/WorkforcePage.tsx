import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { DateField } from "../components/DateField";
import { PageHeader } from "../components/PageHeader";
import { SelectField } from "../components/SelectField";
import { TimeField } from "../components/TimeField";
import { useAuthStore } from "../features/auth/authStore";
import type { Employee, HolidayCalendar, PagedResult, RosterAssignment, ShiftDefinition } from "../types/hrms";

const today = new Date().toISOString().slice(0, 10);

const defaultShiftForm = {
  name: "",
  code: "",
  startTimeLocal: "09:00",
  endTimeLocal: "18:00",
  standardHours: 8,
  breakMinutes: 60,
  minimumOvertimeMinutes: 30,
};

const defaultCalendarForm = {
  name: "",
  code: "",
  isDefault: false,
};

const defaultHolidayForm = {
  holidayCalendarId: "",
  date: today,
  name: "",
  isOptional: false,
};

const defaultRosterForm = {
  employeeId: "",
  shiftDefinitionId: "",
  workDate: today,
  isRestDay: false,
  notes: "",
};

function formatDate(value: string) {
  return new Date(value).toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

function getApiErrorMessage(error: any, fallbackMessage: string) {
  if (!error.response) {
    return "Saving failed. Could not reach the server. Please try again.";
  }

  if (error.response?.data?.errors) {
    return `Saving failed. ${Object.values(error.response.data.errors).flat().join(" ")}`;
  }

  return error.response?.data?.message ?? fallbackMessage;
}

function calculateShiftHours(startTimeLocal: string, endTimeLocal: string, breakMinutes: number) {
  if (!/^\d{2}:\d{2}$/.test(startTimeLocal) || !/^\d{2}:\d{2}$/.test(endTimeLocal)) {
    return 0;
  }

  const [startHour, startMinute] = startTimeLocal.split(":").map(Number);
  const [endHour, endMinute] = endTimeLocal.split(":").map(Number);

  let startTotalMinutes = startHour * 60 + startMinute;
  let endTotalMinutes = endHour * 60 + endMinute;
  if (endTotalMinutes <= startTotalMinutes) {
    endTotalMinutes += 24 * 60;
  }

  const effectiveMinutes = Math.max(endTotalMinutes - startTotalMinutes - Math.max(breakMinutes, 0), 0);
  return Math.round((effectiveMinutes / 60) * 100) / 100;
}

export function WorkforcePage() {
  const roles = useAuthStore((state) => state.roles);
  const isManagerView = roles.includes("Admin") || roles.includes("HR");

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [shifts, setShifts] = useState<ShiftDefinition[]>([]);
  const [calendars, setCalendars] = useState<HolidayCalendar[]>([]);
  const [rosters, setRosters] = useState<RosterAssignment[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingSection, setSavingSection] = useState<"shift" | "calendar" | "holiday" | "roster" | null>(null);

  const [shiftForm, setShiftForm] = useState(defaultShiftForm);
  const [calendarForm, setCalendarForm] = useState(defaultCalendarForm);
  const [holidayForm, setHolidayForm] = useState(defaultHolidayForm);
  const [rosterForm, setRosterForm] = useState(defaultRosterForm);
  const calculatedStandardHours = useMemo(
    () => calculateShiftHours(shiftForm.startTimeLocal, shiftForm.endTimeLocal, shiftForm.breakMinutes),
    [shiftForm.startTimeLocal, shiftForm.endTimeLocal, shiftForm.breakMinutes],
  );

  const loadWorkforce = async () => {
    setLoading(true);

    try {
      const [employeeResponse, shiftResponse, calendarResponse, rosterResponse] = await Promise.all([
        apiClient.get<PagedResult<Employee>>("/employees?pageNumber=1&pageSize=100&sortBy=name"),
        apiClient.get<ShiftDefinition[]>("/workforce/shifts"),
        apiClient.get<HolidayCalendar[]>("/workforce/holiday-calendars"),
        apiClient.get<RosterAssignment[]>("/workforce/rosters"),
      ]);

      const employeeItems = employeeResponse.data.items;
      const shiftItems = shiftResponse.data;
      const calendarItems = calendarResponse.data;

      setEmployees(employeeItems);
      setShifts(shiftItems);
      setCalendars(calendarItems);
      setRosters(rosterResponse.data);

      setHolidayForm((current) => ({
        ...current,
        holidayCalendarId: current.holidayCalendarId || calendarItems[0]?.id || "",
      }));

      setRosterForm((current) => ({
        ...current,
        employeeId: current.employeeId || employeeItems[0]?.id || "",
        shiftDefinitionId: current.shiftDefinitionId || shiftItems[0]?.id || "",
      }));
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not load workforce setup data.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isManagerView) {
      setLoading(false);
      return;
    }

    void loadWorkforce();
  }, [isManagerView]);

  const onCreateShift = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingSection("shift");
    setMessage(null);

    try {
      await apiClient.post("/workforce/shifts", { ...shiftForm, standardHours: calculatedStandardHours });
      setShiftForm(defaultShiftForm);
      await loadWorkforce();
      setMessage(`Shift definition created. Scheduled hours were calculated automatically as ${calculatedStandardHours} hrs.`);
    } catch (error: any) {
      setMessage(getApiErrorMessage(error, "Saving failed. Could not create shift definition. Please try again."));
    } finally {
      setSavingSection(null);
    }
  };

  const onCreateCalendar = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingSection("calendar");
    setMessage(null);

    try {
      await apiClient.post("/workforce/holiday-calendars", calendarForm);
      setCalendarForm(defaultCalendarForm);
      await loadWorkforce();
      setMessage("Holiday calendar created.");
    } catch (error: any) {
      setMessage(getApiErrorMessage(error, "Saving failed. Could not create holiday calendar. Please try again."));
    } finally {
      setSavingSection(null);
    }
  };

  const onAddHoliday = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingSection("holiday");
    setMessage(null);

    try {
      await apiClient.post("/workforce/holiday-dates", holidayForm);
      setHolidayForm((current) => ({ ...current, date: today, name: "", isOptional: false }));
      await loadWorkforce();
      setMessage("Holiday date added.");
    } catch (error: any) {
      setMessage(getApiErrorMessage(error, "Saving failed. Could not add holiday date. Please try again."));
    } finally {
      setSavingSection(null);
    }
  };

  const onAssignRoster = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingSection("roster");
    setMessage(null);

    try {
      await apiClient.post("/workforce/rosters", rosterForm);
      setRosterForm((current) => ({ ...current, workDate: today, isRestDay: false, notes: "" }));
      await loadWorkforce();
      setMessage("Roster assignment saved.");
    } catch (error: any) {
      setMessage(getApiErrorMessage(error, "Saving failed. Could not assign roster. Please try again."));
    } finally {
      setSavingSection(null);
    }
  };

  if (!isManagerView) {
    return (
      <AnimatedPage>
        <PageHeader
          title="Workforce Setup"
          subtitle="Shift, roster, and holiday controls are available to HR and administrators."
        />
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <PageHeader
        title="Workforce Setup"
        subtitle="Configure shift definitions, holiday calendars, and employee rosters that now drive attendance overtime calculations."
      />

      {message ? <div className="soft-pop mt-6 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}

      <div className="mt-6 grid gap-6 xl:grid-cols-[1fr_1fr]">
        <form className="panel space-y-4 p-6" onSubmit={onCreateShift}>
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Shift Library</p>
          <h2 className="font-display text-2xl text-ink">Create shift definition</h2>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Shift name</label>
              <input className="input" placeholder="Morning Shift" value={shiftForm.name} onChange={(event) => setShiftForm((current) => ({ ...current, name: event.target.value }))} />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Shift code</label>
              <input className="input" placeholder="GEN-1" value={shiftForm.code} onChange={(event) => setShiftForm((current) => ({ ...current, code: event.target.value }))} />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Start time</label>
              <TimeField
                value={shiftForm.startTimeLocal}
                onChange={(value) => setShiftForm((current) => ({ ...current, startTimeLocal: value }))}
                placeholder="HH:MM"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">End time</label>
              <TimeField
                value={shiftForm.endTimeLocal}
                onChange={(value) => setShiftForm((current) => ({ ...current, endTimeLocal: value }))}
                placeholder="HH:MM"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Standard hours</label>
              <input
                className="input cursor-not-allowed bg-slate-50 text-slate-600"
                type="number"
                placeholder="8"
                value={calculatedStandardHours}
                readOnly
                disabled
              />
              <p className="pl-1 text-xs text-slate-500">
                Calculated automatically from start time, end time, and break minutes.
              </p>
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Break minutes</label>
              <input className="input" type="number" placeholder="60" value={shiftForm.breakMinutes} onChange={(event) => setShiftForm((current) => ({ ...current, breakMinutes: Number(event.target.value) }))} />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <label className="pl-1 text-xs font-semibold text-slate-500">Minimum overtime minutes</label>
            <input className="input" type="number" placeholder="30" value={shiftForm.minimumOvertimeMinutes} onChange={(event) => setShiftForm((current) => ({ ...current, minimumOvertimeMinutes: Number(event.target.value) }))} />
          </div>
          <button type="submit" className="btn-primary disabled:cursor-not-allowed disabled:opacity-70" disabled={savingSection === "shift"}>
            {savingSection === "shift" ? "Saving..." : "Create Shift"}
          </button>

          <div className="space-y-3 border-t border-slate-100 pt-4">
            {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading shifts...</div> : null}
            {shifts.map((shift) => (
              <div key={shift.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-ink">{shift.name}</p>
                    <p className="mt-1 text-sm text-slate-600">{shift.code} · {shift.startTimeLocal} to {shift.endTimeLocal}</p>
                  </div>
                  <span className="badge bg-lagoon/10 text-lagoon">{shift.standardHours} hrs</span>
                </div>
                <p className="mt-3 text-xs text-slate-500">Break {shift.breakMinutes} min · OT after {shift.minimumOvertimeMinutes} min</p>
              </div>
            ))}
          </div>
        </form>

        <div className="space-y-6">
          <form className="panel space-y-4 p-6" onSubmit={onCreateCalendar}>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Holiday Calendars</p>
            <h2 className="font-display text-2xl text-ink">Create calendar</h2>
            <div className="grid gap-4 md:grid-cols-2">
              <input className="input" placeholder="Calendar name" value={calendarForm.name} onChange={(event) => setCalendarForm((current) => ({ ...current, name: event.target.value }))} />
              <input className="input" placeholder="Calendar code" value={calendarForm.code} onChange={(event) => setCalendarForm((current) => ({ ...current, code: event.target.value }))} />
            </div>
            <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              <input type="checkbox" checked={calendarForm.isDefault} onChange={(event) => setCalendarForm((current) => ({ ...current, isDefault: event.target.checked }))} />
              Make this the default holiday calendar
            </label>
            <button type="submit" className="btn-primary disabled:cursor-not-allowed disabled:opacity-70" disabled={savingSection === "calendar"}>
              {savingSection === "calendar" ? "Saving..." : "Create Calendar"}
            </button>
          </form>

          <form className="panel space-y-4 p-6" onSubmit={onAddHoliday}>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Holiday Dates</p>
            <h2 className="font-display text-2xl text-ink">Add holiday</h2>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Holiday calendar</label>
              <SelectField
                value={holidayForm.holidayCalendarId}
                options={calendars.map((calendar) => ({ value: calendar.id, label: calendar.name }))}
                onChange={(value) => setHolidayForm((current) => ({ ...current, holidayCalendarId: value }))}
                placeholder="Select holiday calendar"
              />
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">Holiday date</label>
                <DateField
                  value={holidayForm.date}
                  onChange={(value) => setHolidayForm((current) => ({ ...current, date: value }))}
                  placeholder="YYYY-MM-DD"
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">Holiday name</label>
                <input className="input" placeholder="Founders Day" value={holidayForm.name} onChange={(event) => setHolidayForm((current) => ({ ...current, name: event.target.value }))} />
              </div>
            </div>
            <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              <input type="checkbox" checked={holidayForm.isOptional} onChange={(event) => setHolidayForm((current) => ({ ...current, isOptional: event.target.checked }))} />
              Mark as optional holiday
            </label>
            <button
              type="submit"
              className="btn-primary disabled:cursor-not-allowed disabled:opacity-70"
              disabled={savingSection === "holiday" || !holidayForm.holidayCalendarId || !holidayForm.date || !holidayForm.name.trim()}
            >
              {savingSection === "holiday" ? "Saving..." : "Add Holiday"}
            </button>

            <div className="space-y-3 border-t border-slate-100 pt-4">
              {calendars.map((calendar) => (
                <div key={calendar.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold text-ink">{calendar.name}</p>
                      <p className="mt-1 text-sm text-slate-600">{calendar.code}</p>
                    </div>
                    {calendar.isDefault ? <span className="badge bg-emerald-50 text-emerald-700">Default</span> : null}
                  </div>
                  <div className="mt-4 space-y-2">
                    {calendar.holidays.length ? calendar.holidays.map((holiday) => (
                      <div key={holiday.id} className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm">
                        <div>
                          <p className="font-semibold text-ink">{holiday.name}</p>
                          <p className="mt-1 text-slate-500">{formatDate(holiday.date)}</p>
                        </div>
                        {holiday.isOptional ? <span className="badge bg-amber-50 text-amber-700">Optional</span> : null}
                      </div>
                    )) : <div className="rounded-2xl border border-dashed border-slate-200 bg-white px-4 py-4 text-sm text-slate-500">No holidays added yet.</div>}
                  </div>
                </div>
              ))}
            </div>
          </form>
        </div>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <form className="panel space-y-4 p-6" onSubmit={onAssignRoster}>
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Roster Management</p>
          <h2 className="font-display text-2xl text-ink">Assign employee shift</h2>
          <SelectField
            value={rosterForm.employeeId}
            options={employees.map((employee) => ({ value: employee.id, label: employee.fullName }))}
            onChange={(value) => setRosterForm((current) => ({ ...current, employeeId: value }))}
            placeholder="Select employee"
          />
          <SelectField
            value={rosterForm.shiftDefinitionId}
            options={shifts.map((shift) => ({ value: shift.id, label: shift.name }))}
            onChange={(value) => setRosterForm((current) => ({ ...current, shiftDefinitionId: value }))}
            placeholder="Select shift"
          />
          <div className="grid gap-4 md:grid-cols-2">
            <DateField
              value={rosterForm.workDate}
              onChange={(value) => setRosterForm((current) => ({ ...current, workDate: value }))}
              placeholder="Select work date"
            />
            <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              <input type="checkbox" checked={rosterForm.isRestDay} onChange={(event) => setRosterForm((current) => ({ ...current, isRestDay: event.target.checked }))} />
              Mark as rest day
            </label>
          </div>
          <textarea className="input min-h-28" placeholder="Notes for this assignment" value={rosterForm.notes} onChange={(event) => setRosterForm((current) => ({ ...current, notes: event.target.value }))} />
          <button type="submit" className="btn-primary disabled:cursor-not-allowed disabled:opacity-70" disabled={savingSection === "roster" || !rosterForm.employeeId || !rosterForm.shiftDefinitionId}>
            {savingSection === "roster" ? "Saving..." : "Assign Roster"}
          </button>
        </form>

        <div className="panel p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Recent Assignments</p>
          <h2 className="mt-2 font-display text-2xl text-ink">Roster activity</h2>
          <div className="mt-5 space-y-3">
            {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading rosters...</div> : null}
            {!loading && !rosters.length ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">No roster assignments yet.</div> : null}
            {rosters.map((roster) => (
              <div key={roster.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-ink">{roster.employeeName}</p>
                    <p className="mt-1 text-sm text-slate-600">{roster.shiftName} · {formatDate(roster.workDate)}</p>
                    {roster.notes ? <p className="mt-2 text-sm text-slate-600">{roster.notes}</p> : null}
                  </div>
                  {roster.isRestDay ? <span className="badge bg-amber-50 text-amber-700">Rest Day</span> : <span className="badge bg-lagoon/10 text-lagoon">Scheduled</span>}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
