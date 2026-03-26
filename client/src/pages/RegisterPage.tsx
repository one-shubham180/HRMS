import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { apiClient } from "../api/client";
import type { AuthResponse, Department, EmploymentType } from "../types/hrms";
import { useAuthStore } from "../features/auth/authStore";

export function RegisterPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);

  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    departmentId: "",
    employeeCode: "",
    jobTitle: "",
    dateOfBirth: "1998-01-01",
    joinDate: new Date().toISOString().slice(0, 10),
    employmentType: "FullTime" as EmploymentType,
    phoneNumber: "",
  });

  useEffect(() => {
    apiClient.get<Department[]>("/departments").then((response) => {
      setDepartments(response.data);
      setForm((current) => ({
        ...current,
        departmentId: current.departmentId || response.data[0]?.id || "",
      }));
    });
  }, []);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoading(true);
    setMessage(null);

    try {
      const response = await apiClient.post<AuthResponse>("/auth/register", form);
      setSession(response.data);
      navigate("/dashboard");
    } catch (requestError: any) {
      setMessage(requestError.response?.data?.message ?? "Registration could not be completed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen px-4 py-8 lg:px-8">
      <div className="mx-auto grid max-w-6xl items-start gap-8 lg:grid-cols-[0.9fr_1.1fr]">
        <section className="page-enter panel p-8 lg:sticky lg:top-8">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Employee Onboarding</p>
          <h1 className="mt-4 font-display text-4xl text-ink">Create your HRMS profile</h1>
          <p className="mt-4 text-sm leading-7 text-slate-600">
            Registration creates a login plus your employee profile, so you can start tracking attendance, requesting leave,
            and accessing the employee dashboard immediately.
          </p>
          <div className="mt-8 rounded-3xl bg-sand p-5">
            <p className="text-sm font-semibold text-ink">What happens next</p>
            <ul className="mt-3 space-y-2 text-sm text-slate-600">
              <li>Your account is created with the Employee role.</li>
              <li>Your department and job profile are stored for dashboard use.</li>
              <li>You can sign in right away after successful registration.</li>
            </ul>
          </div>
        </section>

        <form className="panel page-enter space-y-5 p-8 lg:p-10" onSubmit={onSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="First name" value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Last name" value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Work email" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Password" type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Employee code" value={form.employeeCode} onChange={(event) => setForm((current) => ({ ...current, employeeCode: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Job title" value={form.jobTitle} onChange={(event) => setForm((current) => ({ ...current, jobTitle: event.target.value }))} />
            <select className="input transition-all duration-300 focus:-translate-y-0.5" value={form.departmentId} onChange={(event) => setForm((current) => ({ ...current, departmentId: event.target.value }))}>
              {departments.map((department) => (
                <option key={department.id} value={department.id}>
                  {department.name}
                </option>
              ))}
            </select>
            <select className="input transition-all duration-300 focus:-translate-y-0.5" value={form.employmentType} onChange={(event) => setForm((current) => ({ ...current, employmentType: event.target.value as EmploymentType }))}>
              <option value="FullTime">Full time</option>
              <option value="PartTime">Part time</option>
              <option value="Contract">Contract</option>
              <option value="Intern">Intern</option>
            </select>
            <input className="input transition-all duration-300 focus:-translate-y-0.5" type="date" value={form.dateOfBirth} onChange={(event) => setForm((current) => ({ ...current, dateOfBirth: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" type="date" value={form.joinDate} onChange={(event) => setForm((current) => ({ ...current, joinDate: event.target.value }))} />
          </div>

          <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Phone number" value={form.phoneNumber} onChange={(event) => setForm((current) => ({ ...current, phoneNumber: event.target.value }))} />

          {message ? <div className="soft-pop rounded-2xl bg-amber-50 px-4 py-3 text-sm text-amber-800">{message}</div> : null}

          <div className="flex flex-col gap-3 sm:flex-row">
            <button type="submit" className={`btn-primary transition-all duration-300 ${loading ? "pulse-glow" : ""}`} disabled={loading}>
              {loading ? "Creating profile..." : "Create Account"}
            </button>
            <Link className="btn-secondary" to="/login">
              Back to sign in
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
