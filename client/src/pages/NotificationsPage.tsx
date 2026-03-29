import { useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { useAuthStore } from "../features/auth/authStore";
import type { AuditTrailEntry, NotificationItem } from "../types/hrms";

function formatDateTime(value?: string | null) {
  return value ? new Date(value).toLocaleString("en-IN") : "Pending";
}

export function NotificationsPage() {
  const roles = useAuthStore((state) => state.roles);
  const isManagerView = roles.includes("Admin") || roles.includes("HR");

  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [auditTrail, setAuditTrail] = useState<AuditTrailEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    const loadData = async () => {
      setLoading(true);
      setMessage(null);

      try {
        const notificationRequest = apiClient.get<NotificationItem[]>("/notifications");
        const auditRequest = isManagerView
          ? apiClient.get<AuditTrailEntry[]>("/notifications/audit?take=80")
          : Promise.resolve({ data: [] as AuditTrailEntry[] });

        const [notificationResponse, auditResponse] = await Promise.all([notificationRequest, auditRequest]);
        if (!active) {
          return;
        }

        setNotifications(notificationResponse.data);
        setAuditTrail(auditResponse.data);
      } catch (error: any) {
        if (!active) {
          return;
        }

        setMessage(error.response?.data?.message ?? "Could not load notifications.");
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    void loadData();

    return () => {
      active = false;
    };
  }, [isManagerView]);

  return (
    <AnimatedPage>
      <PageHeader
        title="Notifications"
        subtitle={isManagerView
          ? "Review personal notifications and the audit trail that now records approvals, payroll publication, onboarding, and other state changes."
          : "Review the real-time alerts generated across leave, payroll, onboarding, and other workflow actions."}
      />

      {message ? <div className="soft-pop mt-6 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}

      <div className={`mt-6 grid gap-6 ${isManagerView ? "xl:grid-cols-[0.9fr_1.1fr]" : ""}`}>
        <div className="panel p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Inbox</p>
          <h2 className="mt-2 font-display text-2xl text-ink">My notifications</h2>
          <div className="mt-5 space-y-3">
            {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading notifications...</div> : null}
            {!loading && !notifications.length ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">No notifications yet.</div> : null}
            {notifications.map((notification) => (
              <div key={notification.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-ink">{notification.title}</p>
                    <p className="mt-1 text-sm text-slate-600">{notification.message}</p>
                  </div>
                  <span className="badge bg-lagoon/10 text-lagoon">{notification.type}</span>
                </div>
                <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-slate-500">
                  <span>Status: {notification.status}</span>
                  <span>Delivered: {formatDateTime(notification.deliveredUtc)}</span>
                  {notification.relatedEntityType ? <span>Entity: {notification.relatedEntityType}</span> : null}
                </div>
              </div>
            ))}
          </div>
        </div>

        {isManagerView ? (
          <div className="panel p-6">
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Audit Trail</p>
            <h2 className="mt-2 font-display text-2xl text-ink">Recent state changes</h2>
            <div className="mt-5 space-y-3">
              {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading audit trail...</div> : null}
              {!loading && !auditTrail.length ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">No audit entries yet.</div> : null}
              {auditTrail.map((entry) => (
                <div key={entry.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-semibold text-ink">{entry.entityType} · {entry.action}</p>
                      <p className="mt-1 text-sm text-slate-600">
                        {entry.oldState ? `${entry.oldState} -> ${entry.newState ?? "Updated"}` : entry.newState ?? "Recorded"}
                      </p>
                    </div>
                    <span className="text-xs font-semibold text-slate-500">{formatDateTime(entry.occurredUtc)}</span>
                  </div>
                  {entry.metadata ? <p className="mt-3 text-sm text-slate-600">{entry.metadata}</p> : null}
                </div>
              ))}
            </div>
          </div>
        ) : null}
      </div>
    </AnimatedPage>
  );
}
