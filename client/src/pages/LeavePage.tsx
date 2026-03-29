import { FormEvent, useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { DateField } from "../components/DateField";
import { PageHeader } from "../components/PageHeader";
import { SelectField } from "../components/SelectField";
import { useAuthStore } from "../features/auth/authStore";
import type { LeaveRequest, LeaveStatus, LeaveType, PagedResult } from "../types/hrms";

export function LeavePage() {
  const roles = useAuthStore((state) => state.roles);
  const employeeId = useAuthStore((state) => state.employeeId);
  const [requests, setRequests] = useState<PagedResult<LeaveRequest> | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [form, setForm] = useState({
    leaveType: "Annual" as LeaveType,
    startDate: new Date().toISOString().slice(0, 10),
    endDate: new Date().toISOString().slice(0, 10),
    reason: "",
  });

  const isReviewer = roles.includes("Admin") || roles.includes("HR");

  const loadRequests = async () => {
    if (!isReviewer && !employeeId) {
      setRequests(null);
      return;
    }

    const query = isReviewer
      ? "/leaves?pageNumber=1&pageSize=20"
      : `/leaves?employeeId=${employeeId}&pageNumber=1&pageSize=20`;
    const response = await apiClient.get<PagedResult<LeaveRequest>>(query);
    setRequests(response.data);
  };

  useEffect(() => {
    loadRequests();
  }, [employeeId, isReviewer]);

  const onApply = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    try {
      await apiClient.post("/leaves", form);
      setMessage("Leave request submitted.");
      setForm((current) => ({ ...current, reason: "" }));
      await loadRequests();
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Leave request failed.");
    }
  };

  const onReview = async (leaveRequestId: string, approve: boolean) => {
    try {
      await apiClient.post(`/leaves/${leaveRequestId}/review`, {
        leaveRequestId,
        approve,
        remarks: approve ? "Approved from HRMS portal." : "Reviewed and rejected from HRMS portal.",
      });
      setMessage(approve ? "Leave approved." : "Leave rejected.");
      await loadRequests();
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Leave review failed.");
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Leave Management"
        subtitle="Apply for time off, monitor request history, and review pending submissions with quick approval actions."
      />

      <div className="grid gap-6 xl:grid-cols-[0.85fr_1.15fr]">
        {!isReviewer ? (
          <form className="panel space-y-4 p-6" onSubmit={onApply}>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Apply Leave</p>
            <h2 className="font-display text-2xl text-ink">Submit a request</h2>
            <SelectField
              value={form.leaveType}
              options={[
                { value: "Annual", label: "Annual leave" },
                { value: "Sick", label: "Sick leave" },
                { value: "Casual", label: "Casual leave" },
                { value: "Unpaid", label: "Unpaid leave" },
              ]}
              onChange={(value) => setForm((current) => ({ ...current, leaveType: value as LeaveType }))}
            />
            <div className="grid gap-4 md:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">Start date</label>
                <DateField
                  value={form.startDate}
                  onChange={(value) => setForm((current) => ({ ...current, startDate: value }))}
                  placeholder="YYYY-MM-DD"
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">End date</label>
                <DateField
                  value={form.endDate}
                  onChange={(value) => setForm((current) => ({ ...current, endDate: value }))}
                  placeholder="YYYY-MM-DD"
                />
              </div>
            </div>
            <textarea className="input min-h-32" placeholder="Reason for leave" value={form.reason} onChange={(event) => setForm((current) => ({ ...current, reason: event.target.value }))} />
            {message ? <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}
            <button type="submit" className="btn-primary">Submit Leave Request</button>
          </form>
        ) : (
          <div className="panel p-6">
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Review Queue</p>
            <h2 className="mt-3 font-display text-2xl text-ink">HR / Admin review mode</h2>
            <p className="mt-3 text-sm text-slate-600">
              Pending requests can be approved or rejected directly from the list. Approved unpaid leave affects payroll calculations.
            </p>
            {message ? <div className="soft-pop mt-4 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}
          </div>
        )}

        <div className="panel p-6">
          <h2 className="font-display text-2xl text-ink">Leave Requests</h2>
          <div className="mt-5 space-y-3">
            {requests?.items.map((request, index) => (
              <div
                key={request.id}
                className="soft-pop rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1"
                style={{ animationDelay: `${index * 35}ms` }}
              >
                <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                  <div>
                    <div className="flex items-center gap-3">
                      <p className="font-semibold text-ink">{request.leaveType}</p>
                      <span className="badge bg-amber-50 text-amber-700">{request.status}</span>
                    </div>
                    <p className="mt-2 text-sm text-slate-600">{request.employeeName} · {request.startDate} to {request.endDate}</p>
                    <p className="mt-2 text-sm text-slate-600">{request.reason}</p>
                  </div>
                  {isReviewer && request.status === ("Pending" as LeaveStatus) ? (
                    <div className="flex gap-2">
                      <button type="button" className="btn-primary px-4 py-2" onClick={() => onReview(request.id, true)}>Approve</button>
                      <button type="button" className="btn-secondary px-4 py-2" onClick={() => onReview(request.id, false)}>Reject</button>
                    </div>
                  ) : null}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
