import { FormEvent, useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { SelectField } from "../components/SelectField";
import { useAuthStore } from "../features/auth/authStore";
import type { Candidate, CandidateStatus, Department, EmploymentType, PerformanceAppraisal } from "../types/hrms";

const employmentTypes: EmploymentType[] = ["FullTime", "PartTime", "Contract", "Intern"];
const candidateStatuses: CandidateStatus[] = ["Applied", "Screening", "Interviewing", "Offered", "Hired", "Rejected"];

function formatDate(value?: string | null) {
  if (!value) {
    return "Pending";
  }

  return new Date(value).toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function TalentPage() {
  const roles = useAuthStore((state) => state.roles);
  const isManagerView = roles.includes("Admin") || roles.includes("HR");

  const [departments, setDepartments] = useState<Department[]>([]);
  const [candidates, setCandidates] = useState<Candidate[]>([]);
  const [appraisals, setAppraisals] = useState<PerformanceAppraisal[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingSection, setSavingSection] = useState<"candidate" | "status" | null>(null);

  const [candidateForm, setCandidateForm] = useState({
    departmentId: "",
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    jobTitle: "",
    notes: "",
  });

  const [statusForm, setStatusForm] = useState({
    candidateId: "",
    status: "Screening" as CandidateStatus,
    notes: "",
    employeeCode: "",
    joinDate: new Date().toISOString().slice(0, 10),
    employmentType: "FullTime" as EmploymentType,
  });

  const loadTalentData = async () => {
    setLoading(true);

    try {
      if (isManagerView) {
        const [departmentResponse, candidateResponse, appraisalResponse] = await Promise.all([
          apiClient.get<Department[]>("/departments"),
          apiClient.get<Candidate[]>("/recruitment/candidates"),
          apiClient.get<PerformanceAppraisal[]>("/recruitment/appraisals"),
        ]);

        setDepartments(departmentResponse.data);
        setCandidates(candidateResponse.data);
        setAppraisals(appraisalResponse.data);

        setCandidateForm((current) => ({
          ...current,
          departmentId: current.departmentId || departmentResponse.data[0]?.id || "",
        }));

        setStatusForm((current) => ({
          ...current,
          candidateId: current.candidateId || candidateResponse.data[0]?.id || "",
        }));

        return;
      }

      const appraisalResponse = await apiClient.get<PerformanceAppraisal[]>("/recruitment/appraisals");
      setAppraisals(appraisalResponse.data);
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not load talent data.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadTalentData();
  }, [isManagerView]);

  const onCreateCandidate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingSection("candidate");
    setMessage(null);

    try {
      await apiClient.post("/recruitment/candidates", {
        ...candidateForm,
        phoneNumber: candidateForm.phoneNumber || null,
        notes: candidateForm.notes || null,
      });
      setCandidateForm((current) => ({
        ...current,
        firstName: "",
        lastName: "",
        email: "",
        phoneNumber: "",
        jobTitle: "",
        notes: "",
      }));
      await loadTalentData();
      setMessage("Candidate created.");
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not create candidate.");
    } finally {
      setSavingSection(null);
    }
  };

  const onUpdateCandidateStatus = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingSection("status");
    setMessage(null);

    try {
      await apiClient.post(`/recruitment/candidates/${statusForm.candidateId}/status`, {
        candidateId: statusForm.candidateId,
        status: statusForm.status,
        notes: statusForm.notes || null,
        employeeCode: statusForm.status === "Hired" ? statusForm.employeeCode : null,
        joinDate: statusForm.status === "Hired" ? statusForm.joinDate : null,
        employmentType: statusForm.status === "Hired" ? statusForm.employmentType : null,
      });
      setStatusForm((current) => ({
        ...current,
        notes: "",
        employeeCode: "",
      }));
      await loadTalentData();
      setMessage(statusForm.status === "Hired" ? "Candidate converted to employee and appraisal initialized." : "Candidate status updated.");
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not update candidate status.");
    } finally {
      setSavingSection(null);
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Talent Pipeline"
        subtitle={isManagerView
          ? "Track candidates through hiring and watch the first appraisal cycle initialize automatically when someone joins."
          : "Review your current appraisal cycles and performance goals from the employee side of the talent loop."}
      />

      {message ? <div className="soft-pop mt-6 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}

      {isManagerView ? (
        <div className="mt-6 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <div className="space-y-6">
            <form className="panel space-y-4 p-6" onSubmit={onCreateCandidate}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Recruitment</p>
              <h2 className="font-display text-2xl text-ink">Add candidate</h2>
              <SelectField
                value={candidateForm.departmentId}
                options={departments.map((department) => ({ value: department.id, label: department.name }))}
                onChange={(value) => setCandidateForm((current) => ({ ...current, departmentId: value }))}
                placeholder="Select department"
              />
              <div className="grid gap-4 md:grid-cols-2">
                <input className="input" placeholder="First name" value={candidateForm.firstName} onChange={(event) => setCandidateForm((current) => ({ ...current, firstName: event.target.value }))} />
                <input className="input" placeholder="Last name" value={candidateForm.lastName} onChange={(event) => setCandidateForm((current) => ({ ...current, lastName: event.target.value }))} />
                <input className="input" placeholder="Email" type="email" value={candidateForm.email} onChange={(event) => setCandidateForm((current) => ({ ...current, email: event.target.value }))} />
                <input className="input" placeholder="Phone number" value={candidateForm.phoneNumber} onChange={(event) => setCandidateForm((current) => ({ ...current, phoneNumber: event.target.value }))} />
              </div>
              <input className="input" placeholder="Job title" value={candidateForm.jobTitle} onChange={(event) => setCandidateForm((current) => ({ ...current, jobTitle: event.target.value }))} />
              <textarea className="input min-h-28" placeholder="Candidate notes" value={candidateForm.notes} onChange={(event) => setCandidateForm((current) => ({ ...current, notes: event.target.value }))} />
              <button type="submit" className="btn-primary disabled:cursor-not-allowed disabled:opacity-70" disabled={savingSection === "candidate" || !candidateForm.departmentId}>
                {savingSection === "candidate" ? "Saving..." : "Create Candidate"}
              </button>
            </form>

            <form className="panel space-y-4 p-6" onSubmit={onUpdateCandidateStatus}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Hiring Transition</p>
              <h2 className="font-display text-2xl text-ink">Advance candidate</h2>
              <SelectField
                value={statusForm.candidateId}
                options={candidates.map((candidate) => ({ value: candidate.id, label: candidate.fullName }))}
                onChange={(value) => setStatusForm((current) => ({ ...current, candidateId: value }))}
                placeholder="Select candidate"
              />
              <SelectField
                value={statusForm.status}
                options={candidateStatuses.map((status) => ({ value: status, label: status }))}
                onChange={(value) => setStatusForm((current) => ({ ...current, status: value as CandidateStatus }))}
              />
              <textarea className="input min-h-24" placeholder="Notes" value={statusForm.notes} onChange={(event) => setStatusForm((current) => ({ ...current, notes: event.target.value }))} />

              {statusForm.status === "Hired" ? (
                <div className="grid gap-4 md:grid-cols-2">
                  <input className="input" placeholder="Employee code" value={statusForm.employeeCode} onChange={(event) => setStatusForm((current) => ({ ...current, employeeCode: event.target.value }))} />
                  <input className="input" type="date" value={statusForm.joinDate} onChange={(event) => setStatusForm((current) => ({ ...current, joinDate: event.target.value }))} />
                  <SelectField
                    className="md:col-span-2"
                    value={statusForm.employmentType}
                    options={employmentTypes.map((type) => ({ value: type, label: type }))}
                    onChange={(value) => setStatusForm((current) => ({ ...current, employmentType: value as EmploymentType }))}
                  />
                </div>
              ) : null}

              <button type="submit" className="btn-primary disabled:cursor-not-allowed disabled:opacity-70" disabled={savingSection === "status" || !statusForm.candidateId}>
                {savingSection === "status" ? "Saving..." : "Update Candidate"}
              </button>
            </form>
          </div>

          <div className="panel p-6">
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Candidates</p>
            <h2 className="mt-2 font-display text-2xl text-ink">Recruitment board</h2>
            <div className="mt-5 space-y-3">
              {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading candidates...</div> : null}
              {!loading && !candidates.length ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">No candidates in the pipeline yet.</div> : null}
              {candidates.map((candidate) => (
                <div key={candidate.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-semibold text-ink">{candidate.fullName}</p>
                      <p className="mt-1 text-sm text-slate-600">{candidate.jobTitle} · {candidate.departmentName}</p>
                    </div>
                    <span className="badge bg-lagoon/10 text-lagoon">{candidate.status}</span>
                  </div>
                  <p className="mt-3 text-sm text-slate-600">{candidate.email}</p>
                  {candidate.hiredDate ? <p className="mt-1 text-xs text-slate-500">Hired on {formatDate(candidate.hiredDate)}</p> : null}
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : null}

      <div className="panel mt-6 p-6">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Appraisals</p>
        <h2 className="mt-2 font-display text-2xl text-ink">{isManagerView ? "Performance cycles" : "My performance cycles"}</h2>
        <div className="mt-5 space-y-3">
          {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading appraisals...</div> : null}
          {!loading && !appraisals.length ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">No appraisal cycles have been initialized yet.</div> : null}
          {appraisals.map((appraisal) => (
            <div key={appraisal.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="font-semibold text-ink">{appraisal.cycleName}</p>
                  <p className="mt-1 text-sm text-slate-600">{appraisal.employeeName}</p>
                </div>
                <span className="badge bg-amber-50 text-amber-700">{appraisal.status}</span>
              </div>
              <p className="mt-3 text-sm text-slate-600">{formatDate(appraisal.startDate)} to {formatDate(appraisal.endDate)}</p>
              {appraisal.goalsSummary ? <p className="mt-2 text-sm text-slate-600">{appraisal.goalsSummary}</p> : null}
            </div>
          ))}
        </div>
      </div>
    </AnimatedPage>
  );
}
