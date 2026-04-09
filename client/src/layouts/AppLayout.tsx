import type { LucideIcon } from "lucide-react";
import {
  BellRing,
  BriefcaseBusiness,
  Building2,
  CalendarClock,
  CalendarRange,
  ClipboardCheck,
  FolderLock,
  LayoutDashboard,
  PlaneTakeoff,
  Users,
  WalletCards,
} from "lucide-react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { AiChatWidget } from "../components/AiChatWidget";
import { useAuthStore } from "../features/auth/authStore";

function hasAnyRole(userRoles: string[], allowedRoles?: string[]) {
  return !allowedRoles || allowedRoles.some((role) => userRoles.includes(role));
}

export function AppLayout() {
  const navigate = useNavigate();
  const { roles, email, clearSession } = useAuthStore();

  const navigation: { to: string; label: string; roles?: string[]; icon: LucideIcon }[] = [
    { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
    { to: "/employees", label: "Employees", roles: ["Admin", "HR"], icon: Users },
    { to: "/departments", label: "Departments", roles: ["Admin", "HR"], icon: Building2 },
    { to: "/workforce", label: "Workforce", roles: ["Admin", "HR"], icon: CalendarClock },
    { to: "/attendance", label: "Attendance", icon: ClipboardCheck },
    { to: "/my-roster", label: "My Roster", roles: ["Employee"], icon: CalendarRange },
    { to: "/leaves", label: "Leaves", icon: PlaneTakeoff },
    { to: "/payroll", label: "Payroll", icon: WalletCards },
    { to: "/documents", label: "Documents", icon: FolderLock },
    { to: "/notifications", label: "Notifications", icon: BellRing },
    { to: "/talent", label: "Talent", icon: BriefcaseBusiness },
  ].filter((item) => hasAnyRole(roles, item.roles));

  const onLogout = () => {
    clearSession();
    navigate("/login");
  };

  return (
    <div className="min-h-screen px-4 py-6 lg:px-8">
      <div className="mx-auto grid max-w-7xl gap-6 lg:grid-cols-[280px_minmax(0,1fr)]">
        <aside className="panel flex flex-col gap-6 p-6 lg:sticky lg:top-6 lg:max-h-[calc(100vh-3rem)] lg:self-start">
          <div className="app-sidebar flex-1 overflow-y-auto pr-2">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.35em] text-lagoon">People Ops</p>
              <h1 className="mt-3 font-display text-3xl text-ink">HRMS</h1>
              <p className="mt-3 text-sm text-slate-600">A clean operations workspace for HR, payroll, attendance, and employee lifecycle tasks.</p>
            </div>

            <nav className="mt-8 space-y-2">
              {navigation.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                      isActive ? "bg-ink text-white" : "text-slate-700 hover:bg-slate-100"
                    }`
                  }
                >
                  <item.icon className="h-4 w-4 shrink-0" strokeWidth={2.1} />
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>

          <div className="shrink-0 rounded-3xl bg-sand p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Signed In</p>
            <p className="mt-2 text-sm font-semibold text-ink">{email}</p>
            <p className="mt-1 text-xs text-slate-600">{roles.join(" / ")}</p>
            <button type="button" className="btn-secondary mt-4 w-full" onClick={onLogout}>
              Sign Out
            </button>
          </div>
        </aside>

        <main className="space-y-6">
          <Outlet />
        </main>
      </div>

      <AiChatWidget />
    </div>
  );
}
