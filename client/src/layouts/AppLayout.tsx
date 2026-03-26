import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "../features/auth/authStore";

function hasAnyRole(userRoles: string[], allowedRoles?: string[]) {
  return !allowedRoles || allowedRoles.some((role) => userRoles.includes(role));
}

export function AppLayout() {
  const navigate = useNavigate();
  const { roles, email, clearSession } = useAuthStore();

  const navigation = [
    { to: "/dashboard", label: "Dashboard" },
    { to: "/employees", label: "Employees", roles: ["Admin", "HR"] },
    { to: "/attendance", label: "Attendance" },
    { to: "/leaves", label: "Leaves" },
    { to: "/payroll", label: "Payroll" },
  ].filter((item) => hasAnyRole(roles, item.roles));

  const onLogout = () => {
    clearSession();
    navigate("/login");
  };

  return (
    <div className="min-h-screen px-4 py-6 lg:px-8">
      <div className="mx-auto grid max-w-7xl gap-6 lg:grid-cols-[280px_minmax(0,1fr)]">
        <aside className="panel flex flex-col gap-8 p-6">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.35em] text-lagoon">People Ops</p>
            <h1 className="mt-3 font-display text-3xl text-ink">HRMS</h1>
            <p className="mt-3 text-sm text-slate-600">A clean operations workspace for HR, payroll, attendance, and employee lifecycle tasks.</p>
          </div>

          <nav className="space-y-2">
            {navigation.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `flex items-center rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                    isActive ? "bg-ink text-white" : "text-slate-700 hover:bg-slate-100"
                  }`
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="mt-auto rounded-3xl bg-sand p-4">
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
    </div>
  );
}
