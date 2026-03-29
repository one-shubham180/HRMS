import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { useAuthStore } from "../features/auth/authStore";
import type { AttendanceRecord, AttendanceSettings, PagedResult } from "../types/hrms";

interface AttendanceProof {
    blob: Blob;
    previewUrl: string;
    latitude: number;
    longitude: number;
    locationLabel: string;
    capturedPhotoUtc: string;
}

const apiAssetBaseUrl = (apiClient.defaults.baseURL ?? "").replace(/\/api\/?$/, "");

function resolveAssetUrl(path?: string | null) {
    if (!path) {
        return null;
    }

    return path.startsWith("http") ? path : `${apiAssetBaseUrl}${path}`;
}

function formatDateTime(value?: string | null) {
    return value ? new Date(value).toLocaleString("en-IN") : null;
}

function getCurrentPositionAsync() {
    return new Promise<GeolocationPosition>((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject, {
            enableHighAccuracy: true,
            timeout: 15000,
            maximumAge: 0,
        });
    });
}

export function AttendancePage() {
    const roles = useAuthStore((state) => state.roles);
    const employeeId = useAuthStore((state) => state.employeeId);
    const isManagerView = roles.includes("Admin") || roles.includes("HR");
    const canMarkAttendance = Boolean(employeeId);
    const pageSubtitle = isManagerView
        ? "Review attendance activity, roster-linked overtime, control geo-tagged photo proof, and mark your own attendance when you have an employee profile."
        : "Mark attendance with notes, capture proof when required, and review your shift-linked attendance and overtime activity.";

    const [logs, setLogs] = useState<PagedResult<AttendanceRecord> | null>(null);
    const [settings, setSettings] = useState<AttendanceSettings | null>(null);
    const [note, setNote] = useState("");
    const [message, setMessage] = useState<string | null>(null);
    const [working, setWorking] = useState(false);
    const [updatingSettings, setUpdatingSettings] = useState(false);
    const [cameraOpen, setCameraOpen] = useState(false);
    const [cameraError, setCameraError] = useState<string | null>(null);
    const [capturingProof, setCapturingProof] = useState(false);
    const [proof, setProof] = useState<AttendanceProof | null>(null);
    const videoRef = useRef<HTMLVideoElement | null>(null);
    const streamRef = useRef<MediaStream | null>(null);

    const proofRequired = settings?.requireGeoTaggedPhotoForAttendance ?? false;
    const proofStatusText = proofRequired ? "Required for employee attendance" : "Optional";
    const showCameraStage = cameraOpen && proofRequired && canMarkAttendance;

    const logTitle = isManagerView ? "Attendance Logs" : "Attendance Logs";

    const proofInstruction = useMemo(() => {
        if (!canMarkAttendance) {
            return null;
        }

        return proofRequired
            ? "A live photo with your current location and timestamp is required before check-in or check-out."
            : "Photo proof is currently optional. You can mark attendance using notes only.";
    }, [canMarkAttendance, proofRequired]);

    const stopCamera = () => {
        streamRef.current?.getTracks().forEach((track) => track.stop());
        streamRef.current = null;
        setCameraOpen(false);
    };

    const clearProof = () => {
        setProof((current) => {
            if (current) {
                URL.revokeObjectURL(current.previewUrl);
            }

            return null;
        });
    };

    const loadLogs = async () => {
        const response = await apiClient.get<PagedResult<AttendanceRecord>>(
            "/attendance/logs?pageNumber=1&pageSize=15",
        );
        setLogs(response.data);
    };

    const loadSettings = async () => {
        const response = await apiClient.get<AttendanceSettings>("/attendance/settings");
        setSettings(response.data);
    };

    useEffect(() => {
        void Promise.all([loadLogs(), loadSettings()]);

        return () => {
            stopCamera();
            clearProof();
        };
    }, []);

    useEffect(() => {
        const video = videoRef.current;
        const stream = streamRef.current;

        if (!cameraOpen || !video || !stream) {
            return;
        }

        video.srcObject = stream;
        void video.play();

        return () => {
            if (video.srcObject === stream) {
                video.srcObject = null;
            }
        };
    }, [cameraOpen]);

    useEffect(() => {
        if (!cameraOpen) {
            return;
        }

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape" && !capturingProof) {
                stopCamera();
            }
        };

        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [cameraOpen, capturingProof]);

    const startCamera = async () => {
        if (!navigator.mediaDevices?.getUserMedia) {
            setCameraError("This device/browser does not support camera capture.");
            return;
        }

        setCameraError(null);

        try {
            stopCamera();
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: "user" },
                audio: false,
            });

            streamRef.current = stream;
            setCameraOpen(true);
        } catch {
            setCameraError("Camera access was denied or is unavailable.");
        }
    };

    const captureProof = async () => {
        if (!videoRef.current) {
            setCameraError("Camera preview is not ready.");
            return;
        }

        setCapturingProof(true);
        setCameraError(null);

        try {
            const position = await getCurrentPositionAsync();
            const capturedAt = new Date();
            const latitude = Number(position.coords.latitude.toFixed(6));
            const longitude = Number(position.coords.longitude.toFixed(6));
            const locationLabel = `Lat ${latitude.toFixed(6)}, Lng ${longitude.toFixed(6)}`;
            const video = videoRef.current;
            const canvas = document.createElement("canvas");
            canvas.width = video.videoWidth || 1280;
            canvas.height = video.videoHeight || 720;
            const context = canvas.getContext("2d");

            if (!context) {
                throw new Error("Canvas is unavailable.");
            }

            context.drawImage(video, 0, 0, canvas.width, canvas.height);

            const overlayHeight = Math.max(92, canvas.height * 0.14);
            context.fillStyle = "rgba(0, 0, 0, 0.62)";
            context.fillRect(0, canvas.height - overlayHeight, canvas.width, overlayHeight);
            context.fillStyle = "#ffffff";
            context.font = `${Math.max(24, canvas.width * 0.026)}px Outfit, sans-serif`;
            context.fillText(capturedAt.toLocaleString("en-IN"), 24, canvas.height - overlayHeight / 2);
            context.font = `${Math.max(20, canvas.width * 0.022)}px Outfit, sans-serif`;
            context.fillText(locationLabel, 24, canvas.height - 24);

            const blob = await new Promise<Blob>((resolve, reject) => {
                canvas.toBlob(
                    (value) => {
                        if (value) {
                            resolve(value);
                            return;
                        }

                        reject(new Error("Could not create image blob."));
                    },
                    "image/jpeg",
                    0.92,
                );
            });

            clearProof();
            setProof({
                blob,
                previewUrl: URL.createObjectURL(blob),
                latitude,
                longitude,
                locationLabel,
                capturedPhotoUtc: capturedAt.toISOString(),
            });
            stopCamera();
        } catch (error: any) {
            setCameraError(error.message ?? "Could not capture photo proof with location.");
        } finally {
            setCapturingProof(false);
        }
    };

    const buildAttendanceFormData = () => {
        const formData = new FormData();

        if (note.trim()) {
            formData.append("notes", note.trim());
        }

        if (proof) {
            formData.append("photo", proof.blob, `attendance-proof-${Date.now()}.jpg`);
            formData.append("latitude", String(proof.latitude));
            formData.append("longitude", String(proof.longitude));
            formData.append("locationLabel", proof.locationLabel);
            formData.append("capturedPhotoUtc", proof.capturedPhotoUtc);
        }

        return formData;
    };

    const submitAttendance = async (path: string, successMessage: string, event?: FormEvent) => {
        event?.preventDefault();
        setWorking(true);
        setMessage(null);

        if (canMarkAttendance && proofRequired && !proof) {
            setMessage("Capture your live geo-tagged photo before marking attendance.");
            setWorking(false);
            return;
        }

        try {
            await apiClient.post(path, buildAttendanceFormData());
            setMessage(successMessage);
            setNote("");
            clearProof();
            await loadLogs();
        } catch (error: any) {
            setMessage(error.response?.data?.message ?? "Attendance action failed.");
        } finally {
            setWorking(false);
        }
    };

    const updateProofRequirement = async (required: boolean) => {
        setUpdatingSettings(true);
        setMessage(null);

        try {
            const response = await apiClient.put<AttendanceSettings>("/attendance/settings", {
                requireGeoTaggedPhotoForAttendance: required,
            });
            setSettings(response.data);
            setMessage(
                required
                    ? "Geo-tagged attendance photo is now required for employees."
                    : "Geo-tagged attendance photo requirement is now turned off.",
            );
        } catch (error: any) {
            setMessage(error.response?.data?.message ?? "Could not update attendance proof requirement.");
        } finally {
            setUpdatingSettings(false);
        }
    };

    return (
        <AnimatedPage>
            <PageHeader title="Attendance Workspace" subtitle={pageSubtitle} />

            {showCameraStage ? (
                <div className="mt-6 space-y-6">
                    {isManagerView ? (
                        <div className="panel soft-pop space-y-4 p-6">
                            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">
                                Attendance Control
                            </p>
                            <h2 className="font-display text-2xl text-ink">Geo-tagged photo proof</h2>
                            <p className="text-sm text-slate-600">
                                When this is enabled, employee check-in and check-out require a live camera photo
                                stamped with current coordinates and timestamp.
                            </p>
                            <div className="flex items-center justify-between rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                                <div>
                                    <p className="font-semibold text-ink">{proofStatusText}</p>
                                    <p className="mt-1 text-xs text-slate-500">
                                        Employee attendance actions will follow this rule immediately after the backend
                                        restarts with the new code.
                                    </p>
                                </div>
                                <button
                                    type="button"
                                    className={`btn-secondary min-w-36 ${updatingSettings ? "pulse-glow" : ""}`}
                                    onClick={() => void updateProofRequirement(!proofRequired)}
                                    disabled={updatingSettings || settings === null}
                                >
                                    {updatingSettings ? "Saving..." : proofRequired ? "Turn Off" : "Turn On"}
                                </button>
                            </div>
                            {message ? (
                                <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">
                                    {message}
                                </div>
                            ) : null}
                        </div>
                    ) : null}

                    <div className="panel soft-pop overflow-hidden p-0 mx-auto max-w-4xl">
                        <div className="border-b border-slate-100 bg-[radial-gradient(circle_at_top_left,_rgba(201,107,50,0.12),_transparent_22%),radial-gradient(circle_at_top_right,_rgba(15,118,110,0.14),_transparent_26%),rgba(255,255,255,0.88)] px-6 py-4 sm:px-8">
                            <p className="text-xs font-semibold uppercase tracking-[0.32em] text-lagoon">Live Camera</p>
                        </div>

                        <div className="px-6 pb-6 pt-5 sm:px-8">
                            <div className="mx-auto w-full max-w-2xl rounded-[2rem] border-2 border-lagoon/35 bg-slate-50 p-4 transition-all duration-500">
                                <div className="mx-auto w-full rounded-[1.6rem] border border-white/70 bg-white p-1 shadow-[0_18px_48px_rgba(19,38,47,0.08)]">
                                    <video
                                        ref={videoRef}
                                        className="mx-auto w-full rounded-[1.3rem] bg-slate-200 object-cover transition-all duration-500"
                                        style={{ display: "block", height: "clamp(480px, calc(100vh - 360px), 680px)" }}
                                        playsInline
                                        muted
                                    />
                                </div>
                            </div>

                            <div className="mt-5 rounded-[1.8rem] border border-slate-100 bg-slate-50 px-5 py-5 sm:px-6">
                                <div className="mx-auto w-full max-w-2xl flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
                                    <div>
                                        <p className="text-sm leading-6 text-slate-600">
                                            Keep your face visible inside the frame. Your photo will be stamped with the
                                            current date, time, and location before attendance is submitted.
                                        </p>
                                        <p className="mt-3 text-xs uppercase tracking-[0.24em] text-ember">
                                            Capture, then return to your attendance cards automatically
                                        </p>
                                    </div>
                                    <div className="flex flex-col gap-3 sm:flex-row">
                                        <button
                                            type="button"
                                            className="btn-primary disabled:cursor-not-allowed disabled:opacity-70"
                                            onClick={() => void captureProof()}
                                            disabled={capturingProof || working}
                                        >
                                            {capturingProof ? "Capturing..." : "Capture Photo"}
                                        </button>
                                        <button
                                            type="button"
                                            className="btn-secondary disabled:cursor-not-allowed disabled:opacity-70"
                                            onClick={stopCamera}
                                            disabled={capturingProof || working}
                                        >
                                            Cancel Camera
                                        </button>
                                    </div>
                                </div>

                                {cameraError ? (
                                    <div className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">
                                        {cameraError}
                                    </div>
                                ) : null}
                            </div>
                        </div>
                    </div>
                </div>
            ) : (
                <div
                    className={`mt-6 grid gap-6 ${isManagerView ? "xl:grid-cols-[0.85fr_1.15fr]" : "xl:grid-cols-[0.85fr_1.15fr]"}`}
                >
                    <div className="space-y-6 transition-all duration-500">
                        {isManagerView ? (
                            <div className="panel soft-pop space-y-4 p-6">
                                <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">
                                    Attendance Control
                                </p>
                                <h2 className="font-display text-2xl text-ink">Geo-tagged photo proof</h2>
                                <p className="text-sm text-slate-600">
                                    When this is enabled, employee check-in and check-out require a live camera photo
                                    stamped with current coordinates and timestamp.
                                </p>
                                <div className="flex items-center justify-between rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                                    <div>
                                        <p className="font-semibold text-ink">{proofStatusText}</p>
                                        <p className="mt-1 text-xs text-slate-500">
                                            Employee attendance actions will follow this rule immediately after the
                                            backend restarts with the new code.
                                        </p>
                                    </div>
                                    <button
                                        type="button"
                                        className={`btn-secondary min-w-36 ${updatingSettings ? "pulse-glow" : ""}`}
                                        onClick={() => void updateProofRequirement(!proofRequired)}
                                        disabled={updatingSettings || settings === null}
                                    >
                                        {updatingSettings ? "Saving..." : proofRequired ? "Turn Off" : "Turn On"}
                                    </button>
                                </div>
                                {message ? (
                                    <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">
                                        {message}
                                    </div>
                                ) : null}
                            </div>
                        ) : null}

                        {canMarkAttendance ? (
                            <form
                                className="panel soft-pop space-y-4 p-6"
                                onSubmit={(event) =>
                                    void submitAttendance("/attendance/check-in", "Check-in recorded.", event)
                                }
                            >
                                <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">
                                    Daily Actions
                                </p>
                                <h2 className="font-display text-2xl text-ink">Mark attendance</h2>
                                {proofInstruction ? <p className="text-sm text-slate-600">{proofInstruction}</p> : null}

                                <textarea
                                    className="input min-h-28 transition-all duration-300 focus:-translate-y-0.5"
                                    placeholder="Optional note for today's attendance..."
                                    value={note}
                                    onChange={(event) => setNote(event.target.value)}
                                />

                                {proofRequired ? (
                                    <div className="space-y-4 rounded-3xl border border-slate-100 bg-slate-50 p-4 transition-all duration-500">
                                        <div className="flex flex-col gap-3 sm:flex-row">
                                            <button
                                                type="button"
                                                className="btn-secondary"
                                                onClick={() => void startCamera()}
                                                disabled={capturingProof || working}
                                            >
                                                {proof ? "Retake Photo" : "Open Camera"}
                                            </button>
                                        </div>

                                        {cameraError ? (
                                            <div className="rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">
                                                {cameraError}
                                            </div>
                                        ) : null}

                                        <div className="rounded-2xl border border-dashed border-lagoon/25 bg-white px-4 py-4 text-sm text-slate-600">
                                            Open the camera to replace these cards with a larger in-page capture
                                            surface, then come right back here after capture or cancel.
                                        </div>

                                        {proof ? (
                                            <div className="space-y-3">
                                                <img
                                                    src={proof.previewUrl}
                                                    alt="Attendance proof preview"
                                                    className="h-[320px] w-full rounded-3xl object-cover"
                                                />
                                                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
                                                    <p>{formatDateTime(proof.capturedPhotoUtc)}</p>
                                                    <p className="mt-1">{proof.locationLabel}</p>
                                                </div>
                                            </div>
                                        ) : null}
                                    </div>
                                ) : null}

                                {message ? (
                                    <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">
                                        {message}
                                    </div>
                                ) : null}

                                <div className="flex flex-col gap-3 sm:flex-row">
                                    <button
                                        type="submit"
                                        className={`btn-primary ${working ? "pulse-glow" : ""}`}
                                        disabled={working}
                                    >
                                        {working ? "Submitting..." : "Check In"}
                                    </button>
                                    <button
                                        type="button"
                                        className="btn-secondary"
                                        onClick={() =>
                                            void submitAttendance("/attendance/check-out", "Check-out recorded.")
                                        }
                                        disabled={working}
                                    >
                                        Check Out
                                    </button>
                                </div>
                            </form>
                        ) : null}
                    </div>

                    <div className="panel soft-pop p-6 transition-all duration-500">
                        <h2 className="font-display text-2xl text-ink">{logTitle}</h2>
                        <div className="mt-5 space-y-3">
                            {logs?.items.map((record, index) => (
                                <div
                                    key={record.id}
                                    className="soft-pop rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1"
                                    style={{ animationDelay: `${index * 35}ms` }}
                                >
                                    <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                                        <div>
                                            {isManagerView ? (
                                                <p className="font-semibold text-ink">{record.employeeName}</p>
                                            ) : null}
                                            <p className="text-sm text-slate-600">{record.workDate}</p>
                                            <p className="mt-1 text-sm text-slate-600">
                                                In: {formatDateTime(record.checkInUtc)}{" "}
                                                {record.checkOutUtc
                                                    ? `- Out: ${formatDateTime(record.checkOutUtc)}`
                                                    : ""}
                                            </p>
                                            {record.scheduledShiftName ? (
                                                <p className="mt-1 text-sm text-slate-600">
                                                    Shift: {record.scheduledShiftName}
                                                    {record.scheduledStartTimeLocal && record.scheduledEndTimeLocal
                                                        ? ` (${record.scheduledStartTimeLocal} - ${record.scheduledEndTimeLocal})`
                                                        : ""}
                                                </p>
                                            ) : null}
                                            {record.notes ? (
                                                <p className="mt-2 text-sm text-slate-600">{record.notes}</p>
                                            ) : null}
                                        </div>
                                        <div className="flex items-center gap-3">
                                            <span className="badge bg-amber-50 text-amber-700">{record.status}</span>
                                            {record.isHoliday ? (
                                                <span className="badge bg-rose-50 text-rose-700">{record.holidayName ?? "Holiday"}</span>
                                            ) : null}
                                            {record.isRestDay ? (
                                                <span className="badge bg-slate-200 text-slate-700">Rest Day</span>
                                            ) : null}
                                            {record.overtimeHours > 0 ? (
                                                <span className="badge bg-emerald-50 text-emerald-700">OT {record.overtimeHours} hrs</span>
                                            ) : null}
                                            <span className="text-sm font-semibold text-ink">
                                                {record.workedHours} hrs
                                            </span>
                                        </div>
                                    </div>

                                    {(record.scheduledHours > 0 || record.overtimeHours > 0) ? (
                                        <div className="mt-4 grid gap-3 md:grid-cols-2">
                                            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
                                                Scheduled hours: <span className="font-semibold text-ink">{record.scheduledHours}</span>
                                            </div>
                                            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
                                                Overtime hours: <span className="font-semibold text-ink">{record.overtimeHours}</span>
                                            </div>
                                        </div>
                                    ) : null}

                                    {record.checkInPhotoUrl || record.checkOutPhotoUrl ? (
                                        <div className="mt-4 grid gap-4 lg:grid-cols-2">
                                            {record.checkInPhotoUrl ? (
                                                <div className="rounded-2xl border border-slate-200 bg-white p-3">
                                                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">
                                                        Check In Proof
                                                    </p>
                                                    <img
                                                        src={resolveAssetUrl(record.checkInPhotoUrl) ?? ""}
                                                        alt="Check-in proof"
                                                        className="mt-3 h-40 w-full rounded-2xl object-cover"
                                                    />
                                                    <p className="mt-3 text-xs text-slate-500">
                                                        {record.checkInLocationLabel ?? "Location unavailable"}
                                                    </p>
                                                </div>
                                            ) : null}
                                            {record.checkOutPhotoUrl ? (
                                                <div className="rounded-2xl border border-slate-200 bg-white p-3">
                                                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">
                                                        Check Out Proof
                                                    </p>
                                                    <img
                                                        src={resolveAssetUrl(record.checkOutPhotoUrl) ?? ""}
                                                        alt="Check-out proof"
                                                        className="mt-3 h-40 w-full rounded-2xl object-cover"
                                                    />
                                                    <p className="mt-3 text-xs text-slate-500">
                                                        {record.checkOutLocationLabel ?? "Location unavailable"}
                                                    </p>
                                                </div>
                                            ) : null}
                                        </div>
                                    ) : null}
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            )}
        </AnimatedPage>
    );
}
