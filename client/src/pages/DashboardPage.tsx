import { useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { StatCard } from "../components/StatCard";
import { useAuthStore } from "../features/auth/authStore";
import type { AdminDashboard, EmployeeDashboard } from "../types/hrms";

const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });
const longDate = new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" });
const monthYear = new Intl.DateTimeFormat("en-IN", { month: "short", year: "numeric" });

const formatDate = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : longDate.format(date);
};

const formatMonthLabel = (year: number, month: number) => monthYear.format(new Date(year, month - 1, 1));

export function DashboardPage() {
  const roles = useAuthStore((state) => state.roles);
  const [adminDashboard, setAdminDashboard] = useState<AdminDashboard | null>(null);
  const [employeeDashboard, setEmployeeDashboard] = useState<EmployeeDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadDashboard = async () => {
      setLoading(true);
      setError(null);

      try {
        if (roles.includes("Admin") || roles.includes("HR")) {
          const response = await apiClient.get<AdminDashboard>("/dashboard/admin");
          if (!isMounted) {
            return;
          }

          setAdminDashboard(response.data);
          setEmployeeDashboard(null);
          return;
        }

        const response = await apiClient.get<EmployeeDashboard>("/dashboard/employee");
        if (!isMounted) {
          return;
        }

        setEmployeeDashboard(response.data);
        setAdminDashboard(null);
      } catch (requestError: any) {
        if (!isMounted) {
          return;
        }

        setError(requestError.response?.data?.message ?? "Unable to load dashboard data right now.");
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    loadDashboard();

    return () => {
      isMounted = false;
    };
  }, [roles]);

  if (roles.includes("Admin") || roles.includes("HR")) {
    const attendanceCoverage = adminDashboard?.totalEmployees
      ? Math.round((adminDashboard.presentToday / adminDashboard.totalEmployees) * 100)
      : 0;

    return (
      <AnimatedPage>
        <PageHeader
          title="Admin Command Center"
          subtitle="Track operational health across departments, people movement, leave approvals, and monthly payroll in one glance."
        />

        {error ? <div className="panel mb-5 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Employees" value={`${adminDashboard?.totalEmployees ?? 0}`} hint="Active workforce profiles in the system." />
          <StatCard label="Departments" value={`${adminDashboard?.totalDepartments ?? 0}`} hint="Organizational units available for assignments." />
          <StatCard label="Pending Leaves" value={`${adminDashboard?.pendingLeaves ?? 0}`} hint="Requests waiting for HR/Admin review." />
          <StatCard label="Monthly Payroll" value={currency.format(adminDashboard?.monthlyPayroll ?? 0)} hint="Current month net salary generated so far." />
        </div>

        <div className="grid gap-6 lg:grid-cols-[1.1fr_0.9fr]">
          <div className="panel soft-pop p-6">
            <h2 className="font-display text-2xl text-ink">Today at a glance</h2>
            <p className="mt-2 text-sm text-slate-600">
              {adminDashboard?.presentToday ?? 0} employees have marked attendance today. Pair this with pending leave count to catch staffing gaps early.
            </p>
            <div className="mt-6 grid gap-4 md:grid-cols-2">
              <div className="rounded-3xl bg-sand p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">Attendance Coverage</p>
                <p className="mt-3 font-display text-4xl text-ink">{attendanceCoverage}%</p>
                <p className="mt-2 text-sm text-slate-700">Calculated from today&apos;s marked attendance against total employee records.</p>
              </div>
              <div className="rounded-3xl border border-slate-100 bg-slate-50 p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">Workflow Pressure</p>
                <p className="mt-3 font-display text-4xl text-ink">{adminDashboard?.pendingLeaveRequests.length ?? 0}</p>
                <p className="mt-2 text-sm text-slate-700">Pending leave requests are surfaced live from the database for same-day review.</p>
              </div>
            </div>
          </div>

          <div className="panel soft-pop p-6">
            <h2 className="font-display text-2xl text-ink">Recent Team Members</h2>
            <div className="mt-4 space-y-3 text-sm text-slate-600">
              {loading ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">Loading dashboard activity...</div> : null}
              {!loading && !adminDashboard?.recentEmployees.length ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">No employee records available yet.</div> : null}
              {adminDashboard?.recentEmployees.map((employee) => (
                <div key={employee.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="font-semibold text-ink">{employee.fullName}</p>
                      <p className="mt-1 text-sm text-slate-600">{employee.jobTitle} - {employee.departmentName}</p>
                    </div>
                    <span className="badge bg-lagoon/10 text-lagoon">{employee.employeeCode}</span>
                  </div>
                  <p className="mt-3 text-sm text-slate-600">Joined {formatDate(employee.joinDate)}</p>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
          <div className="panel soft-pop p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="font-display text-2xl text-ink">Pending Leave Queue</h2>
                <p className="mt-2 text-sm text-slate-600">Latest approval items pulled directly from leave records.</p>
              </div>
              <span className="badge bg-amber-50 text-amber-700">{adminDashboard?.pendingLeaves ?? 0} open</span>
            </div>

            <div className="mt-5 space-y-3">
              {loading ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">Loading leave requests...</div> : null}
              {!loading && !adminDashboard?.pendingLeaveRequests.length ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">No pending leave requests right now.</div> : null}
              {adminDashboard?.pendingLeaveRequests.map((leave) => (
                <div key={leave.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold text-ink">{leave.employeeName}</p>
                      <p className="mt-1 text-sm text-slate-600">{leave.leaveType} leave - {leave.totalDays} day{leave.totalDays === 1 ? "" : "s"}</p>
                    </div>
                    <span className="badge bg-amber-50 text-amber-700">{leave.status}</span>
                  </div>
                  <p className="mt-3 text-sm text-slate-600">{formatDate(leave.startDate)} to {formatDate(leave.endDate)}</p>
                </div>
              ))}
            </div>
          </div>

          <div className="panel soft-pop p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="font-display text-2xl text-ink">Recent Payroll</h2>
                <p className="mt-2 text-sm text-slate-600">Latest generated payroll entries from the database.</p>
              </div>
              <span className="badge bg-emerald-50 text-emerald-700">{currency.format(adminDashboard?.monthlyPayroll ?? 0)}</span>
            </div>

            <div className="mt-5 space-y-3">
              {loading ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">Loading payroll activity...</div> : null}
              {!loading && !adminDashboard?.recentPayrolls.length ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">No payroll records have been generated yet.</div> : null}
              {adminDashboard?.recentPayrolls.map((record) => (
                <div key={record.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold text-ink">{record.employeeName}</p>
                      <p className="mt-1 text-sm text-slate-600">{formatMonthLabel(record.year, record.month)} - {record.payslipNumber}</p>
                    </div>
                    <span className="badge bg-emerald-50 text-emerald-700">{currency.format(record.netSalary)}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <PageHeader
        title={`Welcome, ${employeeDashboard?.profile.firstName ?? "Employee"}`}
        subtitle="Your personal dashboard brings together attendance, leave balances, and recent activity in one place."
      />

      {error ? <div className="panel mb-5 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

      <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Role" value={employeeDashboard?.profile.jobTitle ?? "--"} hint={employeeDashboard?.profile.departmentName ?? "Department pending"} />
        <StatCard label="Annual Leave" value={`${employeeDashboard?.annualLeaveBalance ?? 0}`} hint="Remaining annual balance." />
        <StatCard label="Sick Leave" value={`${employeeDashboard?.sickLeaveBalance ?? 0}`} hint="Remaining sick leave balance." />
        <StatCard label="Casual Leave" value={`${employeeDashboard?.casualLeaveBalance ?? 0}`} hint="Remaining casual leave balance." />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="panel soft-pop p-6">
          <h2 className="font-display text-2xl text-ink">Recent Attendance</h2>
          <div className="mt-5 space-y-3">
            {loading ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">Loading attendance history...</div> : null}
            {!loading && !employeeDashboard?.recentAttendance.length ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">No attendance records available yet.</div> : null}
            {employeeDashboard?.recentAttendance.map((record) => (
              <div key={record.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                <div className="flex items-center justify-between">
                  <p className="font-semibold text-ink">{formatDate(record.workDate)}</p>
                  <span className="badge bg-teal-50 text-lagoon">{record.status}</span>
                </div>
                <p className="mt-2 text-sm text-slate-600">{record.workedHours} worked hours</p>
              </div>
            ))}
          </div>
        </div>

        <div className="panel soft-pop p-6">
          <h2 className="font-display text-2xl text-ink">Recent Leave Activity</h2>
          <div className="mt-5 space-y-3">
            {loading ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">Loading leave history...</div> : null}
            {!loading && !employeeDashboard?.recentLeaves.length ? <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">No leave activity yet.</div> : null}
            {employeeDashboard?.recentLeaves.map((leave) => (
              <div key={leave.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                <div className="flex items-center justify-between">
                  <p className="font-semibold text-ink">{leave.leaveType}</p>
                  <span className="badge bg-amber-50 text-amber-700">{leave.status}</span>
                </div>
                <p className="mt-2 text-sm text-slate-600">{formatDate(leave.startDate)} to {formatDate(leave.endDate)}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
