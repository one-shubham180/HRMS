import { FormEvent, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { apiClient } from "../api/client";
import { useAuthStore } from "../features/auth/authStore";
import type { AuthResponse } from "../types/hrms";

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const setSession = useAuthStore((state) => state.setSession);

  const [form, setForm] = useState({ email: "admin@hrms.local", password: "Admin@123" });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/dashboard";

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await apiClient.post<AuthResponse>("/auth/login", form);
      setSession(response.data);
      navigate(from, { replace: true });
    } catch (requestError: any) {
      setError(
        requestError.response?.data?.message ??
          "The API could not be reached. Make sure the backend is running on http://localhost:5108.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen px-4 py-8 lg:px-8">
      <div className="mx-auto grid min-h-[calc(100vh-4rem)] max-w-6xl items-center gap-8 lg:grid-cols-[1.1fr_0.9fr]">
        <section className="page-enter space-y-6">
          <p className="text-xs font-semibold uppercase tracking-[0.35em] text-lagoon">Production HRMS</p>
          <h1 className="max-w-xl font-display text-5xl leading-tight text-ink">
            Human resource operations with a calmer, faster workflow.
          </h1>
          <p className="max-w-2xl text-lg text-slate-600">
            Manage people, attendance, leave, and payroll from one clean workspace built with ASP.NET Core 8 and React.
          </p>

          <div className="grid gap-4 md:grid-cols-3">
            {[
              { label: "Admin", value: "admin@hrms.local / Admin@123" },
              { label: "HR", value: "hr@hrms.local / Hr@12345" },
              { label: "Employee", value: "employee@hrms.local / Emp@12345" },
            ].map((item, index) => (
              <div
                key={item.label}
                className="panel p-5"
                style={{ animationDelay: `${index * 70}ms` }}
              >
                <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">{item.label}</p>
                <p className="mt-3 text-sm font-semibold text-ink">{item.value}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="panel page-enter p-8 lg:p-10">
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Secure Sign In</p>
            <h2 className="font-display text-3xl text-ink">Welcome back</h2>
            <p className="text-sm text-slate-600">Use one of the seeded accounts or your own registered profile.</p>
          </div>

          <form className="mt-8 space-y-4" onSubmit={onSubmit}>
            <input
              className="input transition-all duration-300 focus:-translate-y-0.5"
              placeholder="Email address"
              type="email"
              value={form.email}
              onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
            />
            <input
              className="input transition-all duration-300 focus:-translate-y-0.5"
              placeholder="Password"
              type="password"
              value={form.password}
              onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
            />

            {error ? <div className="soft-pop rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div> : null}

            <button type="submit" className={`btn-primary w-full transition-all duration-300 ${loading ? "pulse-glow" : ""}`} disabled={loading}>
              {loading ? "Signing in..." : "Sign In"}
            </button>
          </form>

          <p className="mt-6 text-sm text-slate-600">
            New employee?
            {" "}
            <Link className="font-semibold text-lagoon" to="/register">
              Create an account
            </Link>
          </p>
        </section>
      </div>
    </div>
  );
}
