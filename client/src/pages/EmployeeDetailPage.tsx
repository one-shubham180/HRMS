import { FormEvent, useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import type { Employee } from "../types/hrms";

const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });

export function EmployeeDetailPage() {
  const { employeeId } = useParams();
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!employeeId) {
      return;
    }

    apiClient.get<Employee>(`/employees/${employeeId}`).then((response) => setEmployee(response.data));
  }, [employeeId]);

  const initials = useMemo(() => {
    if (!employee) {
      return "--";
    }

    return `${employee.firstName[0] ?? ""}${employee.lastName[0] ?? ""}`.toUpperCase();
  }, [employee]);

  const onUpload = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!employeeId || !selectedFile) {
      return;
    }

    const formData = new FormData();
    formData.append("file", selectedFile);

    const response = await apiClient.post<{ imageUrl: string }>(`/employees/${employeeId}/profile-image`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    setEmployee((current) => (current ? { ...current, profileImageUrl: response.data.imageUrl } : current));
    setMessage("Profile image updated.");
  };

  return (
    <AnimatedPage>
      <PageHeader
        title={employee?.fullName ?? "Employee detail"}
        subtitle="Review the employee profile, contact details, leave balances, and salary snapshot from one consolidated page."
      />

      <div className="grid gap-6 lg:grid-cols-[0.8fr_1.2fr]">
        <div className="panel p-6">
          <div className="flex flex-col items-center text-center">
            {employee?.profileImageUrl ? (
              <img className="h-28 w-28 rounded-full object-cover ring-4 ring-white shadow-panel" src={`${import.meta.env.VITE_API_ROOT ?? "http://localhost:5108"}${employee.profileImageUrl}`} alt={employee.fullName} />
            ) : (
              <div className="flex h-28 w-28 items-center justify-center rounded-full bg-ink text-3xl font-display text-white shadow-panel">
                {initials}
              </div>
            )}
            <h2 className="mt-5 font-display text-2xl text-ink">{employee?.fullName}</h2>
            <p className="mt-1 text-sm text-slate-600">{employee?.jobTitle}</p>
            <span className="badge mt-4 bg-lagoon/10 text-lagoon">{employee?.departmentName}</span>
          </div>

          <form className="mt-6 space-y-3" onSubmit={onUpload}>
            <input className="input" type="file" accept="image/*" onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)} />
            <button type="submit" className="btn-secondary w-full">Upload profile image</button>
          </form>

          {message ? <div className="soft-pop mt-4 rounded-2xl bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{message}</div> : null}
        </div>

        <div className="grid gap-6">
          <div className="panel p-6">
            <h2 className="font-display text-2xl text-ink">Profile Summary</h2>
            <div className="mt-5 grid gap-4 md:grid-cols-2">
              {[
                ["Employee code", employee?.employeeCode],
                ["Work email", employee?.workEmail],
                ["Phone", employee?.phoneNumber || "--"],
                ["Employment", employee?.employmentType],
                ["Join date", employee?.joinDate],
                ["Date of birth", employee?.dateOfBirth],
              ].map(([label, value]) => (
                <div key={label} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1">
                  <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">{label}</p>
                  <p className="mt-3 text-sm font-semibold text-ink">{value}</p>
                </div>
              ))}
            </div>
          </div>

          <div className="grid gap-6 md:grid-cols-2">
            <div className="panel p-6">
              <h2 className="font-display text-2xl text-ink">Leave Balances</h2>
              <div className="mt-5 space-y-3">
                {[
                  ["Annual", employee?.annualLeaveBalance],
                  ["Sick", employee?.sickLeaveBalance],
                  ["Casual", employee?.casualLeaveBalance],
                ].map(([label, value]) => (
                  <div key={label} className="flex items-center justify-between rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                    <span className="text-sm text-slate-600">{label}</span>
                    <span className="font-semibold text-ink">{value}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="panel p-6">
              <h2 className="font-display text-2xl text-ink">Compensation Snapshot</h2>
              <p className="mt-5 text-5xl font-display text-ink">{currency.format(employee?.grossSalary ?? 0)}</p>
              <p className="mt-3 text-sm text-slate-600">Current gross salary view from the linked salary structure.</p>
            </div>
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
