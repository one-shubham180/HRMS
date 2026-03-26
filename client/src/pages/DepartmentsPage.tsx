import { FormEvent, useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import type { Department } from "../types/hrms";

const initialForm = {
  name: "",
  code: "",
  description: "",
};

export function DepartmentsPage() {
  const [departments, setDepartments] = useState<Department[] | null>(null);
  const [creating, setCreating] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [form, setForm] = useState(initialForm);

  const loadData = async () => {
    const response = await apiClient.get<Department[]>("/departments");
    setDepartments(response.data);
  };

  useEffect(() => {
    loadData();
  }, []);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setCreating(true);
    setFeedback(null);

    try {
      await apiClient.post("/departments", form);
      setFeedback("Department created successfully.");
      setForm(initialForm);
      await loadData();
    } catch (error: any) {
      setFeedback(error.response?.data?.message ?? "Department creation failed.");
    } finally {
      setCreating(false);
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Department Management"
        subtitle="Create and view company departments for structuring your workforce."
      />

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="panel p-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="font-display text-2xl text-ink">Departments Directory</h2>
              <p className="mt-2 text-sm text-slate-600">{departments?.length ?? 0} active departments.</p>
            </div>
            <span className="badge bg-lagoon/10 text-lagoon">Admin View</span>
          </div>

          <div className="mt-6 space-y-3">
            {departments?.map((department, index) => (
              <div
                key={department.id}
                className="soft-pop block rounded-3xl border border-slate-100 bg-slate-50 p-5 transition-all duration-300 hover:-translate-y-1 hover:border-lagoon/30"
                style={{ animationDelay: `${index * 45}ms` }}
              >
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="font-semibold text-ink">{department.name}</p>
                    <p className="mt-1 text-sm text-slate-600">{department.description}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <span className="badge bg-slate-200 text-slate-700">{department.code}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form className="panel space-y-4 p-6" onSubmit={onSubmit}>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Create Department</p>
            <h2 className="mt-3 font-display text-2xl text-ink">Add a new department</h2>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Department Name" value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required />
            <input className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Department Code (e.g. ENG)" value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} required />
          </div>

          <textarea className="input transition-all duration-300 focus:-translate-y-0.5" placeholder="Description" value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={3} required />

          {feedback ? <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{feedback}</div> : null}

          <button type="submit" className={`btn-primary transition-all duration-300 ${creating ? "pulse-glow" : ""}`} disabled={creating}>
            {creating ? "Creating..." : "Create Department"}
          </button>
        </form>
      </div>
    </AnimatedPage>
  );
}
