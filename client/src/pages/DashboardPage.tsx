import { useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { StatCard } from "../components/StatCard";
import { useAuthStore } from "../features/auth/authStore";
import type { AdminDashboard, EmployeeDashboard } from "../types/hrms";

const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });

export function DashboardPage() {
  const roles = useAuthStore((state) => state.roles);
  const [adminDashboard, setAdminDashboard] = useState<AdminDashboard | null>(null);
  const [employeeDashboard, setEmployeeDashboard] = useState<EmployeeDashboard | null>(null);

  useEffect(() => {
    if (roles.includes("Admin") || roles.includes("HR")) {
      apiClient.get<AdminDashboard>("/dashboard/admin").then((response) => setAdminDashboard(response.data));
    } else {
      apiClient.get<EmployeeDashboard>("/dashboard/employee").then((response) => setEmployeeDashboard(response.data));
    }
  }, [roles]);

  if (roles.includes("Admin") || roles.includes("HR")) {
    return (
      <AnimatedPage>
        <PageHeader
          title="Admin Command Center"
          subtitle="Track operational health across departments, people movement, leave approvals, and monthly payroll in one glance."
        />

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
            <div className="mt-6 rounded-3xl bg-sand p-5">
              <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">Recommendation</p>
              <p className="mt-3 text-sm text-slate-700">
                Review pending leaves before payroll generation so unpaid leave deductions stay aligned with approvals.
              </p>
            </div>
          </div>

          <div className="panel soft-pop p-6">
            <h2 className="font-display text-2xl text-ink">Operational notes</h2>
            <div className="mt-4 space-y-3 text-sm text-slate-600">
              <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                Department setup, employee onboarding, and payroll generation are all available from the left navigation.
              </div>
              <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                Seed data includes Admin, HR, and Employee personas so you can demo the full lifecycle immediately.
              </div>
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
            {employeeDashboard?.recentAttendance.map((record) => (
              <div key={record.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                <div className="flex items-center justify-between">
                  <p className="font-semibold text-ink">{record.workDate}</p>
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
            {employeeDashboard?.recentLeaves.map((leave) => (
              <div key={leave.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                <div className="flex items-center justify-between">
                  <p className="font-semibold text-ink">{leave.leaveType}</p>
                  <span className="badge bg-amber-50 text-amber-700">{leave.status}</span>
                </div>
                <p className="mt-2 text-sm text-slate-600">{leave.startDate} to {leave.endDate}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
