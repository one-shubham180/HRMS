import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import type { Department, Employee, EmploymentType, PagedResult, Role } from "../types/hrms";

const initialForm = {
  departmentId: "",
  employeeCode: "",
  firstName: "",
  lastName: "",
  email: "",
  password: "Emp@12345",
  jobTitle: "",
  dateOfBirth: "1998-01-01",
  joinDate: new Date().toISOString().slice(0, 10),
  employmentType: "FullTime" as EmploymentType,
  phoneNumber: "",
  role: "Employee" as Role,
};

export function EmployeesPage() {
  const [employees, setEmployees] = useState<PagedResult<Employee> | null>(null);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [creating, setCreating] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [form, setForm] = useState(initialForm);

  const loadData = async () => {
    const [employeesResponse, departmentsResponse] = await Promise.all([
      apiClient.get<PagedResult<Employee>>("/employees?pageNumber=1&pageSize=12&sortBy=name"),
      apiClient.get<Department[]>("/departments"),
    ]);

    setEmployees(employeesResponse.data);
    setDepartments(departmentsResponse.data);
    setForm((current) => ({
      ...current,
      departmentId: current.departmentId || departmentsResponse.data[0]?.id || "",
    }));
  };

  useEffect(() => {
    loadData();
  }, []);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setCreating(true);
    setFeedback(null);

    try {
      await apiClient.post("/employees", form);
      setFeedback("Employee profile created successfully.");
      setForm((current) => ({
        ...initialForm,
        departmentId: current.departmentId,
      }));
      await loadData();
    } catch (error: any) {
      setFeedback(error.response?.data?.message ?? "Employee creation failed.");
    } finally {
      setCreating(false);
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Employee Management"
        subtitle="Create new profiles, review workforce information, and move into a richer detail page for each employee."
      />

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="panel p-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="font-display text-2xl text-ink">Employee Directory</h2>
              <p className="mt-2 text-sm text-slate-600">{employees?.totalCount ?? 0} employee records available.</p>
            </div>
            <span className="badge bg-lagoon/10 text-lagoon">HR View</span>
          </div>

          <div className="mt-6 space-y-3">
            {employees?.items.map((employee, index) => (
              <Link
                key={employee.id}
                to={`/employees/${employee.id}`}
                className="soft-pop block rounded-3xl border border-slate-100 bg-slate-50 p-5 transition-all duration-300 hover:-translate-y-1 hover:border-lagoon/30"
                style={{ animationDelay: `${index * 45}ms` }}
              >
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="font-semibold text-ink">{employee.fullName}</p>
                    <p className="mt-1 text-sm text-slate-600">{employee.jobTitle} · {employee.departmentName}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <span className="badge bg-slate-200 text-slate-700">{employee.employeeCode}</span>
                    <span className={`badge ${employee.isActive ? "bg-emerald-50 text-emerald-700" : "bg-rose-50 text-rose-700"}`}>
                      {employee.isActive ? "Active" : "Inactive"}
                    </span>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </div>

        <form className="panel space-y-4 p-6" onSubmit={onSubmit}>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Create Employee</p>
            <h2 className="mt-3 font-display text-2xl text-ink">Add a new team member</h2>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="First name" value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Last name" value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Work email" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Temporary password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Employee code" value={form.employeeCode} onChange={(event) => setForm((current) => ({ ...current, employeeCode: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Job title" value={form.jobTitle} onChange={(event) => setForm((current) => ({ ...current, jobTitle: event.target.value }))} />
            <select className="input transition-all duration-300 focus:-translate-y-0.5" value={form.departmentId} onChange={(event) => setForm((current) => ({ ...current, departmentId: event.target.value }))}>
              {departments.map((department) => (
                <option key={department.id} value={department.id}>{department.name}</option>
              ))}
            </select>
            <select className="input transition-all duration-300 focus:-translate-y-0.5" value={form.role} onChange={(event) => setForm((current) => ({ ...current, role: event.target.value as Role }))}>
              <option value="Employee">Employee</option>
              <option value="HR">HR</option>
              <option value="Admin">Admin</option>
            </select>
            <input className="input transition-all duration-300 focus:-translate-y-0.5" type="date" value={form.dateOfBirth} onChange={(event) => setForm((current) => ({ ...current, dateOfBirth: event.target.value }))} />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" type="date" value={form.joinDate} onChange={(event) => setForm((current) => ({ ...current, joinDate: event.target.value }))} />
          </div>

          <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Phone number" value={form.phoneNumber} onChange={(event) => setForm((current) => ({ ...current, phoneNumber: event.target.value }))} />

          {feedback ? <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{feedback}</div> : null}

          <button type="submit" className={`btn-primary transition-all duration-300 ${creating ? "pulse-glow" : ""}`} disabled={creating}>
            {creating ? "Creating..." : "Create Employee"}
          </button>
        </form>
      </div>
    </AnimatedPage>
  );
}
