import { useEffect, useMemo, useState } from "react";
import { BellRing, CheckCheck, Eye, History } from "lucide-react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { EmptyStateCard } from "../components/EmptyStateCard";
import { PageHeader } from "../components/PageHeader";
import { ToastBanner } from "../components/ToastBanner";
import { useAuthStore } from "../features/auth/authStore";
import type { AuditTrailEntry, NotificationItem } from "../types/hrms";

function formatDateTime(value?: string | null) {
  return value ? new Date(value).toLocaleString("en-IN") : "Pending";
}

function formatAuditState(entry: AuditTrailEntry) {
  return entry.oldState ? `${entry.oldState} -> ${entry.newState ?? "Updated"}` : entry.newState ?? "Recorded";
}

export function NotificationsPage() {
  const roles = useAuthStore((state) => state.roles);
  const isManagerView = roles.includes("Admin") || roles.includes("HR");

  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [auditTrail, setAuditTrail] = useState<AuditTrailEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [isMarkingAll, setIsMarkingAll] = useState(false);
  const [activeNotificationId, setActiveNotificationId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ tone: "success" | "error" | "info"; title: string; message: string } | null>(null);

  useEffect(() => {
    let active = true;

    const loadData = async () => {
      setLoading(true);
      setFeedback(null);

      try {
        const notificationRequest = apiClient.get<NotificationItem[]>("/notifications");
        const auditRequest = isManagerView
          ? apiClient.get<AuditTrailEntry[]>("/notifications/audit?take=12")
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

        setFeedback({
          tone: "error",
          title: "Notifications unavailable",
          message: error.response?.data?.message ?? "Could not load notifications right now.",
        });
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

  const unreadNotifications = useMemo(
    () => notifications.filter((notification) => notification.status !== "Read"),
    [notifications],
  );

  const onMarkAsRead = async (notificationId: string) => {
    setActiveNotificationId(notificationId);
    setFeedback(null);

    try {
      await apiClient.post(`/notifications/${notificationId}/read`);
      setNotifications((current) => current.map((notification) => (
        notification.id === notificationId
          ? { ...notification, status: "Read", readUtc: new Date().toISOString() }
          : notification
      )));
    } catch (error: any) {
      setFeedback({
        tone: "error",
        title: "Update failed",
        message: error.response?.data?.message ?? "Could not mark the notification as read.",
      });
    } finally {
      setActiveNotificationId(null);
    }
  };

  const onMarkAllAsRead = async () => {
    setIsMarkingAll(true);
    setFeedback(null);

    try {
      const response = await apiClient.post<{ updatedCount: number }>("/notifications/read-all");
      const readUtc = new Date().toISOString();
      setNotifications((current) => current.map((notification) => ({ ...notification, status: "Read", readUtc })));
      setFeedback({
        tone: "success",
        title: "Inbox cleared",
        message: response.data.updatedCount
          ? `${response.data.updatedCount} notifications were marked as read.`
          : "Everything is already up to date.",
      });
    } catch (error: any) {
      setFeedback({
        tone: "error",
        title: "Bulk update failed",
        message: error.response?.data?.message ?? "Could not mark all notifications as read.",
      });
    } finally {
      setIsMarkingAll(false);
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Notifications"
        subtitle={isManagerView
          ? "Track your personal alerts and a concise operational audit snapshot without wading through a giant log dump."
          : "Review your leave, payroll, onboarding, and workflow alerts in one place."}
        actions={(
          <>
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-600">
              <span className="font-semibold text-ink">{unreadNotifications.length}</span> unread
            </div>
            <button
              type="button"
              className="btn-secondary disabled:cursor-not-allowed disabled:opacity-70"
              disabled={isMarkingAll || !unreadNotifications.length}
              onClick={onMarkAllAsRead}
            >
              {isMarkingAll ? "Updating..." : "Mark All As Read"}
            </button>
          </>
        )}
      />

      {feedback ? (
        <div className="mt-6">
          <ToastBanner tone={feedback.tone} title={feedback.title} message={feedback.message} onDismiss={() => setFeedback(null)} />
        </div>
      ) : null}

      <div className={`mt-6 grid gap-6 ${isManagerView ? "xl:grid-cols-[1fr_0.95fr]" : ""}`}>
        <div className="panel p-6">
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Inbox</p>
              <h2 className="mt-2 font-display text-2xl text-ink">My notifications</h2>
            </div>
            <span className="badge bg-lagoon/10 text-lagoon">{notifications.length} total</span>
          </div>

          <div className="mt-5 space-y-3">
            {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading notifications...</div> : null}
            {!loading && !notifications.length ? (
              <EmptyStateCard
                title="No notifications yet"
                description="When leave, payroll, onboarding, or recruitment events affect your account, they will show up here."
                icon={<BellRing className="h-8 w-8 text-lagoon" strokeWidth={1.8} />}
              />
            ) : null}
            {notifications.map((notification) => {
              const isRead = notification.status === "Read";

              return (
                <div key={notification.id} className={`rounded-2xl border p-4 ${isRead ? "border-slate-100 bg-slate-50" : "border-lagoon/20 bg-lagoon/5"}`}>
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-3">
                        <p className="font-semibold text-ink">{notification.title}</p>
                        {!isRead ? <span className="badge bg-amber-50 text-amber-700">Unread</span> : null}
                      </div>
                      <p className="mt-1 text-sm text-slate-600">{notification.message}</p>
                    </div>
                    <span className="badge bg-lagoon/10 text-lagoon">{notification.type}</span>
                  </div>

                  <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-slate-500">
                    <span>Status: {notification.status}</span>
                    <span>Delivered: {formatDateTime(notification.deliveredUtc)}</span>
                    {notification.readUtc ? <span>Read: {formatDateTime(notification.readUtc)}</span> : null}
                    {notification.relatedEntityType ? <span>Entity: {notification.relatedEntityType}</span> : null}
                  </div>

                  {!isRead ? (
                    <div className="mt-4">
                      <button
                        type="button"
                        className="btn-secondary gap-2 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-70"
                        disabled={activeNotificationId === notification.id}
                        onClick={() => void onMarkAsRead(notification.id)}
                      >
                        <Eye className="h-4 w-4" />
                        {activeNotificationId === notification.id ? "Saving..." : "Mark As Read"}
                      </button>
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
        </div>

        {isManagerView ? (
          <div className="panel p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Audit Trail</p>
                <h2 className="mt-2 font-display text-2xl text-ink">Latest operational changes</h2>
                <p className="mt-2 text-sm text-slate-600">A short audit snapshot for admins and HR, capped to the latest 12 entries.</p>
              </div>
              <span className="badge bg-ember/10 text-ember">12 latest</span>
            </div>

            <div className="mt-5 space-y-3">
              {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading audit trail...</div> : null}
              {!loading && !auditTrail.length ? (
                <EmptyStateCard
                  title="No recent audit activity"
                  description="Approvals, payroll publication, onboarding, and notification actions will show up here for administrators."
                  icon={<History className="h-8 w-8 text-ember" strokeWidth={1.8} />}
                />
              ) : null}
              {auditTrail.map((entry) => (
                <div key={entry.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-semibold text-ink">{entry.entityType} · {entry.action}</p>
                      <p className="mt-1 text-sm text-slate-600">{formatAuditState(entry)}</p>
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
