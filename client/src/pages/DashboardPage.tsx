import { useEffect, useState } from "react";
import { Activity, Landmark, PieChart as PieChartIcon } from "lucide-react";
import {
  Area,
  AreaChart,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { EmptyStateCard } from "../components/EmptyStateCard";
import { PageHeader } from "../components/PageHeader";
import { StatCard } from "../components/StatCard";
import { useAuthStore } from "../features/auth/authStore";
import type { AdminDashboard, EmployeeDashboard, PayrollRecord } from "../types/hrms";

const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });
const longDate = new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" });
const monthYear = new Intl.DateTimeFormat("en-IN", { month: "short", year: "numeric" });
const shortDayMonth = new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short" });
const chartPalette = ["#0f766e", "#c96b32", "#13262f", "#94a3b8"];

const formatDate = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : longDate.format(date);
};

const formatMonthLabel = (year: number, month: number) => monthYear.format(new Date(year, month - 1, 1));

function normalizeTooltipValue(value: unknown) {
  if (Array.isArray(value)) {
    return Number(value[0] ?? 0);
  }

  return Number(value ?? 0);
}

const formatCurrencyTooltip = (value: unknown) => currency.format(normalizeTooltipValue(value));
const formatCountTooltip = (value: unknown, name: unknown) => [`${normalizeTooltipValue(value)}`, String(name ?? "")];
const formatHoursTooltip = (value: unknown) => [`${normalizeTooltipValue(value)} hrs`, "Worked"];
const formatDaysTooltip = (value: unknown) => [`${normalizeTooltipValue(value)} days`, "Balance"];

function buildPayrollTrend(records: PayrollRecord[]) {
  const monthlyTotals = new Map<string, { label: string; total: number }>();

  records.forEach((record) => {
    const monthKey = `${record.year}-${String(record.month).padStart(2, "0")}`;
    const current = monthlyTotals.get(monthKey);

    if (current) {
      current.total += record.netSalary;
      return;
    }

    monthlyTotals.set(monthKey, {
      label: formatMonthLabel(record.year, record.month),
      total: record.netSalary,
    });
  });

  return Array.from(monthlyTotals.entries())
    .sort(([left], [right]) => left.localeCompare(right))
    .slice(-6)
    .map(([, value]) => value);
}

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

    void loadDashboard();

    return () => {
      isMounted = false;
    };
  }, [roles]);

  if (roles.includes("Admin") || roles.includes("HR")) {
    const attendanceCoverage = adminDashboard?.totalEmployees
      ? Math.round((adminDashboard.presentToday / adminDashboard.totalEmployees) * 100)
      : 0;
    const payrollTrendData = buildPayrollTrend(adminDashboard?.recentPayrolls ?? []);
    const attendanceDonutData = [
      { name: "Present", value: adminDashboard?.presentToday ?? 0, color: "#0f766e" },
      {
        name: "Not marked",
        value: Math.max((adminDashboard?.totalEmployees ?? 0) - (adminDashboard?.presentToday ?? 0), 0),
        color: "#cbd5e1",
      },
    ].filter((item) => item.value > 0);

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
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="font-display text-2xl text-ink">Payroll Expense vs. Month</h2>
                <p className="mt-2 text-sm text-slate-600">A rolling view of recently generated payroll so finance and HR can spot payroll movement quickly.</p>
              </div>
              <span className="badge bg-ember/10 text-ember">{payrollTrendData.length || 0} periods</span>
            </div>

            {loading ? <div className="mt-6 rounded-3xl border border-slate-100 bg-slate-50 p-6 text-sm text-slate-600">Loading payroll chart...</div> : null}
            {!loading && !payrollTrendData.length ? (
              <div className="mt-6">
                <EmptyStateCard
                  title="No payroll trend yet"
                  description="Generate payroll for one or more months to unlock this finance chart."
                  icon={<Landmark className="h-8 w-8 text-emerald-700" strokeWidth={1.8} />}
                />
              </div>
            ) : null}
            {!loading && payrollTrendData.length ? (
              <div className="mt-6 h-72 rounded-[2rem] bg-slate-50 px-4 py-4 md:px-5">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={payrollTrendData} margin={{ top: 12, right: 12, left: 12, bottom: 0 }}>
                    <defs>
                      <linearGradient id="payrollAreaGradient" x1="0" x2="0" y1="0" y2="1">
                        <stop offset="0%" stopColor="#0f766e" stopOpacity={0.34} />
                        <stop offset="100%" stopColor="#0f766e" stopOpacity={0.04} />
                      </linearGradient>
                    </defs>
                    <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: "#64748b", fontSize: 12 }} tickMargin={10} />
                    <YAxis
                      tickLine={false}
                      axisLine={false}
                      width={92}
                      tickMargin={10}
                      tick={{ fill: "#64748b", fontSize: 12 }}
                      tickFormatter={(value) => currency.format(Number(value)).replace("₹", "₹ ")}
                    />
                    <Tooltip
                      cursor={{ stroke: "#cbd5e1", strokeDasharray: "4 4" }}
                      formatter={formatCurrencyTooltip}
                      contentStyle={{ borderRadius: "20px", border: "1px solid #e2e8f0", boxShadow: "0 18px 40px -28px rgba(15, 23, 42, 0.35)" }}
                    />
                    <Area type="monotone" dataKey="total" stroke="#0f766e" strokeWidth={3} fill="url(#payrollAreaGradient)" />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            ) : null}
          </div>

          <div className="panel soft-pop p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="font-display text-2xl text-ink">Attendance Percentage</h2>
                <p className="mt-2 text-sm text-slate-600">Live attendance coverage for today against the active workforce roster.</p>
              </div>
              <span className="badge bg-lagoon/10 text-lagoon">{attendanceCoverage}% present</span>
            </div>

            {loading ? <div className="mt-6 rounded-3xl border border-slate-100 bg-slate-50 p-6 text-sm text-slate-600">Loading attendance chart...</div> : null}
            {!loading && !attendanceDonutData.length ? (
              <div className="mt-6">
                <EmptyStateCard
                  title="Attendance signal unavailable"
                  description="Once employees start checking in, this donut chart will reflect the day&apos;s coverage."
                  icon={<Activity className="h-8 w-8 text-lagoon" strokeWidth={1.8} />}
                />
              </div>
            ) : null}
            {!loading && attendanceDonutData.length ? (
              <>
                <div className="mt-6 h-72 rounded-[2rem] bg-slate-50 px-3 py-4">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie data={attendanceDonutData} dataKey="value" innerRadius={72} outerRadius={102} paddingAngle={4} stroke="none">
                        {attendanceDonutData.map((entry) => (
                          <Cell key={entry.name} fill={entry.color} />
                        ))}
                      </Pie>
                      <Tooltip formatter={formatCountTooltip} />
                      <Legend verticalAlign="bottom" iconType="circle" />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-4 grid gap-4 md:grid-cols-2">
                  <div className="rounded-3xl bg-sand p-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">Checked In</p>
                    <p className="mt-3 font-display text-4xl text-ink">{adminDashboard?.presentToday ?? 0}</p>
                    <p className="mt-2 text-sm text-slate-700">Employees who have marked attendance today.</p>
                  </div>
                  <div className="rounded-3xl border border-slate-100 bg-slate-50 p-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">Workflow Pressure</p>
                    <p className="mt-3 font-display text-4xl text-ink">{adminDashboard?.pendingLeaveRequests.length ?? 0}</p>
                    <p className="mt-2 text-sm text-slate-700">Pending leave approvals waiting for action.</p>
                  </div>
                </div>
              </>
            ) : null}
          </div>
        </div>

        <div className="grid gap-6 lg:grid-cols-[0.95fr_1.05fr]">
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
      </AnimatedPage>
    );
  }

  const attendanceTrendData = [...(employeeDashboard?.recentAttendance ?? [])]
    .sort((left, right) => new Date(left.workDate).getTime() - new Date(right.workDate).getTime())
    .map((record) => ({
      label: shortDayMonth.format(new Date(record.workDate)),
      workedHours: record.workedHours,
    }));
  const leaveBalanceData = [
    { name: "Annual", value: employeeDashboard?.annualLeaveBalance ?? 0, color: chartPalette[0] },
    { name: "Sick", value: employeeDashboard?.sickLeaveBalance ?? 0, color: chartPalette[1] },
    { name: "Casual", value: employeeDashboard?.casualLeaveBalance ?? 0, color: chartPalette[2] },
  ].filter((item) => item.value > 0);

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

      <div className="grid gap-6 lg:grid-cols-[1.05fr_0.95fr]">
        <div className="panel soft-pop p-6">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="font-display text-2xl text-ink">Worked Hours Trend</h2>
              <p className="mt-2 text-sm text-slate-600">A quick chart of your recent attendance so you can spot shorter or longer workdays.</p>
            </div>
            <span className="badge bg-ink/5 text-ink">{attendanceTrendData.length} days</span>
          </div>
          {loading ? <div className="mt-6 rounded-3xl border border-slate-100 bg-slate-50 p-6 text-sm text-slate-600">Loading attendance chart...</div> : null}
          {!loading && !attendanceTrendData.length ? (
            <div className="mt-6">
              <EmptyStateCard
                title="No attendance history yet"
                description="Your worked-hours chart will appear after attendance entries are recorded."
                icon={<Activity className="h-8 w-8 text-lagoon" strokeWidth={1.8} />}
              />
            </div>
          ) : null}
          {!loading && attendanceTrendData.length ? (
            <div className="mt-6 h-72 rounded-[2rem] bg-slate-50 px-4 py-4 md:px-5">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={attendanceTrendData} margin={{ top: 12, right: 12, left: 8, bottom: 0 }}>
                  <defs>
                    <linearGradient id="attendanceAreaGradient" x1="0" x2="0" y1="0" y2="1">
                      <stop offset="0%" stopColor="#c96b32" stopOpacity={0.32} />
                      <stop offset="100%" stopColor="#c96b32" stopOpacity={0.06} />
                    </linearGradient>
                  </defs>
                  <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: "#64748b", fontSize: 12 }} tickMargin={10} />
                  <YAxis tickLine={false} axisLine={false} width={56} tickMargin={10} tick={{ fill: "#64748b", fontSize: 12 }} />
                  <Tooltip
                    formatter={formatHoursTooltip}
                    contentStyle={{ borderRadius: "20px", border: "1px solid #e2e8f0", boxShadow: "0 18px 40px -28px rgba(15, 23, 42, 0.35)" }}
                  />
                  <Area type="monotone" dataKey="workedHours" stroke="#c96b32" strokeWidth={3} fill="url(#attendanceAreaGradient)" />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          ) : null}
        </div>

        <div className="panel soft-pop p-6">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="font-display text-2xl text-ink">Leave Balance Split</h2>
              <p className="mt-2 text-sm text-slate-600">See how your current leave balances are distributed across available leave types.</p>
            </div>
            <span className="badge bg-lagoon/10 text-lagoon">Live balance</span>
          </div>
          {loading ? <div className="mt-6 rounded-3xl border border-slate-100 bg-slate-50 p-6 text-sm text-slate-600">Loading leave chart...</div> : null}
          {!loading && !leaveBalanceData.length ? (
            <div className="mt-6">
              <EmptyStateCard
                title="No leave balance available"
                description="Once balances are assigned, this chart will show the mix of annual, sick, and casual leave."
                icon={<PieChartIcon className="h-8 w-8 text-ember" strokeWidth={1.8} />}
              />
            </div>
          ) : null}
          {!loading && leaveBalanceData.length ? (
            <div className="mt-6 h-72 rounded-[2rem] bg-slate-50 px-3 py-4">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={leaveBalanceData} dataKey="value" innerRadius={70} outerRadius={102} paddingAngle={4} stroke="none">
                    {leaveBalanceData.map((entry) => (
                      <Cell key={entry.name} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip formatter={formatDaysTooltip} />
                  <Legend verticalAlign="bottom" iconType="circle" />
                </PieChart>
              </ResponsiveContainer>
            </div>
          ) : null}
        </div>
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
