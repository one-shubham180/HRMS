import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import html2canvas from "html2canvas";
import { jsPDF } from "jspdf";
import Barcode from "react-barcode";
import { QRCodeSVG } from "qrcode.react";
import { Download, IdCard, Mail, MapPin, Phone, Printer, ShieldCheck, UserRound } from "lucide-react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { useAuthStore } from "../features/auth/authStore";
import type { Employee, PagedResult } from "../types/hrms";

const CARD_WIDTH = 320;
const CARD_HEIGHT = 540;
const STANDARD_CARD_LONG_EDGE_MM = 86;
const STANDARD_CARD_SHORT_EDGE_MM = 54;
const CARD_PRINT_WIDTH_MM = STANDARD_CARD_SHORT_EDGE_MM;
const CARD_PRINT_HEIGHT_MM = STANDARD_CARD_LONG_EDGE_MM;
const PDF_PAGE_WIDTH_MM = 210;
const PDF_PAGE_HEIGHT_MM = 297;
const PDF_PAGE_MARGIN_MM = 12;
const PDF_CARD_GAP_MM = 4;
const PDF_CUT_GUIDE_OFFSET_MM = 1.5;
const PDF_CARDS_PER_ROW = Math.floor((PDF_PAGE_WIDTH_MM - PDF_PAGE_MARGIN_MM * 2 + PDF_CARD_GAP_MM) / (CARD_PRINT_WIDTH_MM + PDF_CARD_GAP_MM));
const PDF_CARDS_PER_COLUMN = Math.floor(
  (PDF_PAGE_HEIGHT_MM - PDF_PAGE_MARGIN_MM * 2 + PDF_CARD_GAP_MM) / (CARD_PRINT_HEIGHT_MM + PDF_CARD_GAP_MM)
);
const PDF_CARDS_PER_PAGE = PDF_CARDS_PER_ROW * PDF_CARDS_PER_COLUMN;

const PDF_ID_CARD_WIDTH_MM = 54;
const PDF_ID_CARD_HEIGHT_MM = 85.6;
const PDF_ID_CARD_MARGIN_MM = 4;
const PDF_ID_CARD_GAP_MM = 4;
const PDF_ID_CARD_MAX_COLUMNS = 2;
const PDF_ID_CARD_SCALE = 3; // 300+ DPI capture for pixel-perfect rendering

type CardSide = "front" | "back";
type CaptureAction = "download" | "print" | "pdf";

interface CaptureState {
  action: CaptureAction;
  side?: CardSide;
}

const bloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

const getImageUrl = (imageUrl?: string | null) => {
  if (!imageUrl) {
    return null;
  }

  if (/^https?:\/\//i.test(imageUrl)) {
    return imageUrl;
  }

  const apiRoot = import.meta.env.VITE_API_ROOT ?? "http://localhost:5108";
  return `${apiRoot}${imageUrl}`;
};

const getEmployeeIdValue = (employee: Employee) => employee.employeeCode?.trim() || employee.id || "HRMS00001";

const formatPhone = (phone?: string | null) => phone?.trim() || "+91 00000 00000";

const formatEmploymentType = (employmentType?: string | null) =>
  employmentType?.replace(/([a-z])([A-Z])/g, "$1 $2") || "Full Time";

const formatDate = (value?: string | null) => {
  if (!value) {
    return "Not Available";
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleDateString("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
};

const getBloodGroup = (employee: Employee) => {
  const code = getEmployeeIdValue(employee);
  const hash = code.split("").reduce((sum, char) => sum + char.charCodeAt(0), 0);
  return bloodTypes[Math.abs(hash) % bloodTypes.length];
};

const waitForImageToLoad = (image: HTMLImageElement) =>
  new Promise<void>((resolve, reject) => {
    if (image.complete) {
      resolve();
      return;
    }

    image.onload = () => resolve();
    image.onerror = () => reject(new Error("Failed to load image."));
  });

function CenteredGlyphRow({
  value,
  className,
  gapClassName,
}: {
  value: string;
  className?: string;
  gapClassName?: string;
}) {
  return (
    <span className={`inline-flex h-full w-full items-center justify-center ${gapClassName ?? "gap-[0.08em]"} ${className ?? ""}`}>
      {value.split("").map((char, index) => (
        <span key={`${char}-${index}`} className="block leading-none align-middle">
          {char === " " ? "\u00A0" : char}
        </span>
      ))}
    </span>
  );
}

function EmployeePhoto({ employee }: { employee: Employee }) {
  const imageUrl = getImageUrl(employee.profileImageUrl);
  const initials = `${employee.firstName?.trim()?.[0] ?? ""}${employee.lastName?.trim()?.[0] ?? ""}`.toUpperCase() || "HR";

  if (imageUrl) {
    return <img src={imageUrl} alt={employee.fullName} className="h-full w-full rounded-[18px] object-cover" />;
  }

  return (
    <div className="flex h-full w-full items-center justify-center rounded-[18px] bg-gradient-to-br from-sky-100 via-blue-100 to-blue-200">
      <CenteredGlyphRow
        value={initials}
        className="relative text-[28px] font-black uppercase text-[#0F172A]"
        gapClassName="gap-[0.08em]"
      />
    </div>
  );
}

function FieldRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="space-y-1.5 border-b border-slate-200 pb-2.5 last:border-b-0 last:pb-0">
      <p className="text-[10px] font-semibold uppercase tracking-[0.3em] text-slate-500">{label}</p>
      <p className="break-words text-[12px] font-semibold leading-[1.4] text-slate-900">{value}</p>
    </div>
  );
}

function FrontBadge({ employee }: { employee: Employee }) {
  const employeeId = getEmployeeIdValue(employee);

  return (
    <div
      className="relative flex flex-col justify-between overflow-hidden rounded-xl bg-white shadow-lg"
      style={{ width: CARD_WIDTH, height: CARD_HEIGHT }}
    >
      <div className="bg-gradient-to-b from-[#1E3A8A] to-[#2563EB] px-5 py-3.5 text-white">
        <p className="text-[10px] font-semibold uppercase tracking-[0.34em] text-white/75">Employee Identity Card</p>
        <div className="mt-1.5 flex items-center justify-between gap-3">
          <div>
            <p className="text-lg font-black uppercase tracking-[0.14em]">HRMS Corp</p>
            <p className="mt-1 text-[11px] text-white/80">People Operations Workspace</p>
          </div>
          <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-white/12">
            <ShieldCheck className="h-5 w-5" strokeWidth={2.1} />
          </div>
        </div>
      </div>

      <div className="flex flex-1 flex-col justify-between bg-[repeating-linear-gradient(135deg,rgba(30,58,138,0.04)_0px,rgba(30,58,138,0.04)_2px,transparent_2px,transparent_14px)] px-5 py-3.5">
        <div className="min-h-0">
          <div className="flex justify-center">
            <div className="flex h-[88px] w-[96px] items-center justify-center rounded-[18px] border-4 border-white bg-white shadow-[0_12px_24px_rgba(15,23,42,0.12)]">
              <EmployeePhoto employee={employee} />
            </div>
          </div>

          <div className="mt-3 flex flex-col items-center text-center">
            <h2 className="w-full break-words px-2 text-center text-[14px] font-black uppercase leading-[1.12] tracking-[0.03em] text-[#0F172A]">
              {employee.fullName}
            </h2>
            <p className="mt-1 min-h-[18px] w-full break-words px-3 text-center text-[9px] font-semibold uppercase leading-[1.2] tracking-[0.14em] text-[#2563EB]">
              {employee.jobTitle}
            </p>
            <div className="flex w-full justify-center">
              <div className="inline-flex h-[24px] min-w-[120px] items-center justify-center rounded-full bg-[#0F172A] px-4 text-center text-[9px] font-bold uppercase text-white">
                <CenteredGlyphRow
                  value={employeeId}
                  className="relative"
                  gapClassName="gap-[0.1em]"
                />
              </div>
            </div>
          </div>

          <div className="mt-3 h-px bg-slate-200" />

          <div className="mt-3 grid grid-cols-2 gap-x-5 gap-y-2">
            <div className="space-y-2">
              <FieldRow label="Department" value={employee.departmentName || "Human Resources"} />
              <FieldRow label="Work Email" value={employee.workEmail} />
              <FieldRow label="Phone" value={formatPhone(employee.phoneNumber)} />
            </div>
            <div className="space-y-2">
              <FieldRow label="Blood Group" value={getBloodGroup(employee)} />
              <FieldRow label="Type" value={formatEmploymentType(employee.employmentType)} />
              <FieldRow label="Join Date" value={formatDate(employee.joinDate)} />
            </div>
          </div>
        </div>

        <div className="mt-3 shrink-0 pb-3">
          <div className="h-px bg-slate-200" />
          <div className="flex flex-col items-center justify-center overflow-visible px-1 pt-2.5 pb-2">
            <p className="mb-1 text-[8px] font-semibold uppercase tracking-[0.22em] text-slate-500">Employee Barcode</p>
            <Barcode
              value={employeeId}
              format="CODE128"
              renderer="svg"
              width={1}
              height={22}
              displayValue={false}
              margin={2}
              background="transparent"
              lineColor="#0F172A"
            />
          </div>
        </div>
      </div>
    </div>
  );
}

function BackBadge({ employee }: { employee: Employee }) {
  const appUrl = (import.meta.env.VITE_APP_URL as string | undefined)?.trim() || window.location.origin;
  const qrValue = `${appUrl.replace(/\/$/, "")}/employees/${employee.id}`;

  return (
    <div
      className="relative flex flex-col overflow-hidden rounded-xl bg-white shadow-lg"
      style={{ width: CARD_WIDTH, height: CARD_HEIGHT }}
    >
      <div className="bg-gradient-to-b from-[#1E3A8A] to-[#2563EB] px-5 py-5 text-white">
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="text-[10px] font-semibold uppercase tracking-[0.34em] text-white/75">Access Summary</p>
            <p className="mt-2 text-lg font-black uppercase tracking-[0.12em]">Information</p>
          </div>
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/12">
            <IdCard className="h-5 w-5" strokeWidth={2.1} />
          </div>
        </div>
      </div>

      <div className="flex flex-1 flex-col justify-between bg-[repeating-linear-gradient(135deg,rgba(30,58,138,0.04)_0px,rgba(30,58,138,0.04)_2px,transparent_2px,transparent_14px)] px-5 py-3">
        <div>
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0 flex-1">
              <p className="text-[10px] font-semibold uppercase tracking-[0.3em] text-slate-500">Verification QR</p>
              <p className="mt-1.5 text-[13px] font-semibold text-slate-900">Scan to open HRMS</p>
              <p className="mt-1 text-[9px] leading-relaxed text-slate-700">
                Use this QR code to open the employee verification page in the HRMS application.
              </p>
            </div>
            <div className="flex h-[78px] w-[78px] shrink-0 items-center justify-center rounded-[18px] border border-slate-200 bg-slate-50 p-2">
              <QRCodeSVG value={qrValue} size={62} bgColor="#ffffff" fgColor="#0F172A" level="M" includeMargin={false} />
            </div>
          </div>

          <div className="mt-3 rounded-[18px] bg-[#0F172A] px-4 py-3 text-white">
            <p className="text-[10px] font-semibold uppercase tracking-[0.3em] text-white/72">Terms and Conditions</p>
            <div className="mt-2 space-y-1.5 text-[9px] leading-relaxed text-white/88">
              <p className="flex gap-2">
                <span className="mt-[2px] text-sky-300">-</span>
                <span>This identity card remains the property of HRMS Corp and must be carried during office hours.</span>
              </p>
              <p className="flex gap-2">
                <span className="mt-[2px] text-sky-300">-</span>
                <span>Present this badge for access control, attendance verification, and visitor clearance when requested.</span>
              </p>
              <p className="flex gap-2">
                <span className="mt-[2px] text-sky-300">-</span>
                <span>Return the card to HR on separation, role transfer, or badge replacement.</span>
              </p>
            </div>
          </div>

          <div className="mt-3 rounded-[18px] border border-slate-200 bg-slate-50 px-4 py-3">
            <p className="text-[10px] font-semibold uppercase tracking-[0.3em] text-slate-500">Information</p>
            <div className="mt-2 space-y-2 text-[9px] text-slate-700">
              <p className="flex items-start gap-2">
                <Mail className="mt-[1px] h-4 w-4 shrink-0 text-[#2563EB]" strokeWidth={2.1} />
                <span>support@hrmscorp.com</span>
              </p>
              <p className="flex items-start gap-2">
                <IdCard className="mt-[1px] h-4 w-4 shrink-0 text-[#2563EB]" strokeWidth={2.1} />
                <span>{appUrl}</span>
              </p>
              <p className="flex items-start gap-2">
                <Phone className="mt-[1px] h-4 w-4 shrink-0 text-[#2563EB]" strokeWidth={2.1} />
                <span>+91 141 400 2400</span>
              </p>
              <p className="flex items-start gap-2 leading-relaxed text-slate-700">
                <MapPin className="mt-[1px] h-4 w-4 shrink-0 text-[#2563EB]" strokeWidth={2.1} />
                <span>245 North 13th Street, Office 103, Jaipur, Rajasthan</span>
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function IdentityCardStudioPage() {
  const roles = useAuthStore((state) => state.roles);
  const isPrivileged = roles.includes("Admin") || roles.includes("HR");

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState("");
  const [selectedEmployeeIds, setSelectedEmployeeIds] = useState<string[]>([]);
  const [activeSide, setActiveSide] = useState<CardSide>("front");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [captureState, setCaptureState] = useState<CaptureState | null>(null);

  const frontCardRefs = useRef<Record<string, HTMLDivElement | null>>({});
  const backCardRefs = useRef<Record<string, HTMLDivElement | null>>({});

  const loadEmployees = useCallback(async () => {
    if (!isPrivileged) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await apiClient.get<PagedResult<Employee>>("/employees?pageNumber=1&pageSize=24&sortBy=name");
      const items = response.data.items ?? [];
      setEmployees(items);
      setSelectedEmployeeId((current) => (current && items.some((employee) => employee.id === current) ? current : items[0]?.id ?? ""));
      setSelectedEmployeeIds((current) => {
        const currentSelection = current.filter((id) => items.some((employee) => employee.id === id));
        if (currentSelection.length) {
          return currentSelection;
        }

        return items[0]?.id ? [items[0].id] : [];
      });
    } catch (requestError: any) {
      setEmployees([]);
      setSelectedEmployeeId("");
      setSelectedEmployeeIds([]);
      setError(requestError.response?.data?.message ?? "Could not load employee data right now. Please retry.");
    } finally {
      setLoading(false);
    }
  }, [isPrivileged]);

  useEffect(() => {
    void loadEmployees();
  }, [loadEmployees]);

  const employeesForRender = useMemo(() => employees, [employees]);

  const selectedEmployee = useMemo(() => {
    const found = employeesForRender.find((employee) => employee.id === selectedEmployeeId);
    return found ?? employeesForRender[0] ?? null;
  }, [employeesForRender, selectedEmployeeId]);

  const selectedEmployees = useMemo(() => {
    const matched = employeesForRender.filter((employee) => selectedEmployeeIds.includes(employee.id));
    if (matched.length) {
      return matched;
    }

    return selectedEmployee ? [selectedEmployee] : [];
  }, [employeesForRender, selectedEmployee, selectedEmployeeIds]);

  const allEmployeesSelected = employees.length > 0 && selectedEmployeeIds.length === employees.length;

  const setCardRef = useCallback((side: CardSide, employeeId: string, node: HTMLDivElement | null) => {
    if (side === "front") {
      frontCardRefs.current[employeeId] = node;
      return;
    }

    backCardRefs.current[employeeId] = node;
  }, []);

  const getCardElement = useCallback((employeeId: string, side: CardSide) => {
    return side === "front" ? frontCardRefs.current[employeeId] : backCardRefs.current[employeeId];
  }, []);

  const toggleEmployeeSelection = useCallback((employeeId: string) => {
    setSelectedEmployeeIds((current) => {
      if (current.includes(employeeId)) {
        const next = current.filter((id) => id !== employeeId);
        return next.length ? next : [employeeId];
      }

      return [...current, employeeId];
    });
  }, []);

  const toggleSelectAll = useCallback(() => {
    setSelectedEmployeeIds((current) => {
      if (employees.length === 0) {
        return [];
      }

      if (current.length === employees.length) {
        return selectedEmployeeId ? [selectedEmployeeId] : [];
      }

      return employees.map((employee) => employee.id);
    });
  }, [employees, selectedEmployeeId]);

  const captureElementAsPng = useCallback(async (element: HTMLDivElement, scale = 2) => {
    const canvas = await html2canvas(element, {
      scale,
      backgroundColor: "#ffffff",
      useCORS: true,
      logging: false,
    });

    return canvas.toDataURL("image/png");
  }, []);

  const printCardImage = useCallback(async (dataUrl: string, side: CardSide) => {
    const printWindow = window.open("", "_blank", "width=700,height=1000");
    if (!printWindow) {
      return;
    }

    printWindow.document.write(`
      <html>
        <head>
          <title>Print Identity Card</title>
          <style>
            @page {
              size: ${CARD_WIDTH}px ${CARD_HEIGHT}px;
              margin: 0;
            }
            html, body {
              margin: 0;
              padding: 0;
              width: ${CARD_WIDTH}px;
              height: ${CARD_HEIGHT}px;
              background: #ffffff;
              overflow: hidden;
            }
            body {
              display: flex;
              align-items: center;
              justify-content: center;
            }
            img {
              width: ${CARD_WIDTH}px;
              height: ${CARD_HEIGHT}px;
              display: block;
            }
            @media print {
              html, body {
                width: ${CARD_WIDTH}px;
                height: ${CARD_HEIGHT}px;
              }
            }
          </style>
        </head>
        <body>
          <img id="identity-print-image" src="${dataUrl}" alt="${side} identity card" />
        </body>
      </html>
    `);
    printWindow.document.close();

    const printImage = printWindow.document.getElementById("identity-print-image") as HTMLImageElement | null;
    if (!printImage) {
      printWindow.close();
      return;
    }

    await waitForImageToLoad(printImage);

    printWindow.focus();
    printWindow.print();
    printWindow.onafterprint = () => {
      printWindow.close();
    };
  }, []);

  const captureCard = useCallback(
    async (side: CardSide, action: "download" | "print") => {
      if (!selectedEmployee) {
        return;
      }

      const element = getCardElement(selectedEmployee.id, side);
      if (!element) {
        return;
      }

      setCaptureState({ side, action });

      try {
        const dataUrl = await captureElementAsPng(element, 3);

        if (action === "download") {
          const link = document.createElement("a");
          link.href = dataUrl;
          link.download = `${getEmployeeIdValue(selectedEmployee)}-${side}-identity-card.png`;
          link.click();
          return;
        }

        await printCardImage(dataUrl, side);
      } catch (captureError) {
        console.error("Failed to export identity card", captureError);
      } finally {
        setCaptureState(null);
      }
    },
    [captureElementAsPng, getCardElement, printCardImage, selectedEmployee]
  );

  const captureCardAsImage = useCallback(
    async (element: HTMLDivElement) => {
      const canvas = await html2canvas(element, {
        scale: PDF_ID_CARD_SCALE,
        backgroundColor: "#ffffff",
        useCORS: true,
        logging: false,
      });

      return canvas.toDataURL("image/png");
    },
    []
  );

  const drawPdfCutGuides = useCallback((doc: jsPDF, x: number, y: number, width: number, height: number) => {
    const offset = PDF_CUT_GUIDE_OFFSET_MM;

    doc.setDrawColor(120, 130, 145);
    doc.setLineWidth(0.15);
    doc.setLineDashPattern([1, 1], 0);

    // Full dashed cut border around the card area for easier trimming after print.
    doc.rect(x - offset, y - offset, width + offset * 2, height + offset * 2);

    doc.setLineDashPattern([], 0);
  }, []);

  const generateIdCardPdf = useCallback(
    async (cards: Array<{ element: HTMLDivElement; employeeId: string; side: CardSide }>) => {
      const cardsPerRow = Math.floor((PDF_PAGE_WIDTH_MM - PDF_PAGE_MARGIN_MM * 2 + PDF_ID_CARD_GAP_MM) / (PDF_ID_CARD_WIDTH_MM + PDF_ID_CARD_GAP_MM));
      const cardsPerColumn = Math.floor((PDF_PAGE_HEIGHT_MM - PDF_PAGE_MARGIN_MM * 2 + PDF_ID_CARD_GAP_MM) / (PDF_ID_CARD_HEIGHT_MM + PDF_ID_CARD_GAP_MM));
      const cardsPerPage = cardsPerRow * cardsPerColumn;

      const doc = new jsPDF({
        unit: "mm",
        format: "a4",
        orientation: "portrait",
      });

      for (let index = 0; index < cards.length; index += 1) {
        if (index > 0 && index % cardsPerPage === 0) {
          doc.addPage();
        }

        const pageIndex = index % cardsPerPage;
        const row = Math.floor(pageIndex / cardsPerRow);
        const column = pageIndex % cardsPerRow;
        const x = PDF_PAGE_MARGIN_MM + column * (PDF_ID_CARD_WIDTH_MM + PDF_ID_CARD_GAP_MM);
        const y = PDF_PAGE_MARGIN_MM + row * (PDF_ID_CARD_HEIGHT_MM + PDF_ID_CARD_GAP_MM);
        const card = cards[index];
        const imageData = await captureCardAsImage(card.element);

        doc.addImage(imageData, "PNG", x, y, PDF_ID_CARD_WIDTH_MM, PDF_ID_CARD_HEIGHT_MM, undefined, "FAST");
        drawPdfCutGuides(doc, x, y, PDF_ID_CARD_WIDTH_MM, PDF_ID_CARD_HEIGHT_MM);
      }

      return doc.output("blob");
    },
    [captureCardAsImage, drawPdfCutGuides]
  );

  const downloadSelectedEmployeesPdf = useCallback(async () => {
    if (!selectedEmployees.length) {
      return;
    }

    setCaptureState({ action: "pdf" });

    try {
      const cards = [] as Array<{ element: HTMLDivElement; employeeId: string; side: CardSide }>;

      for (const employee of selectedEmployees) {
        const frontElement = getCardElement(employee.id, "front");
        const backElement = getCardElement(employee.id, "back");

        if (frontElement) {
          cards.push({ element: frontElement, employeeId: employee.id, side: "front" });
        }

        if (backElement) {
          cards.push({ element: backElement, employeeId: employee.id, side: "back" });
        }
      }

      if (!cards.length) {
        return;
      }

      const blob = await generateIdCardPdf(cards);
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `hrms-identity-cards-${selectedEmployees.length}-${new Date().toISOString().slice(0, 10)}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(downloadUrl);
    } catch (pdfError) {
      console.error("Failed to download identity card PDF", pdfError);
    } finally {
      setCaptureState(null);
    }
  }, [generateIdCardPdf, getCardElement, selectedEmployees]);



  if (!isPrivileged) {
    return (
      <AnimatedPage>
        <PageHeader title="Identity Card" subtitle="Access limited to Admin and HR users." />
        <div className="panel rounded-3xl border border-rose-200 bg-rose-50 px-6 py-8 text-sm text-rose-700">
          You need an Admin or HR role to open the identity card studio.
        </div>
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <PageHeader
        title="Identity Card"
        subtitle="Clean professional portrait employee badge with simple layout, barcode, print, and batch PDF export."
      />

      <div className="grid gap-6 xl:grid-cols-[400px_minmax(0,1fr)]">
        <section className="panel p-5 shadow-[0_20px_50px_rgba(15,23,42,0.06)]">
          <div className="rounded-[24px] bg-gradient-to-b from-[#0F172A] to-[#2563EB] px-5 py-5 text-white">
            <p className="text-xs font-semibold uppercase tracking-[0.34em] text-white/70">Card Controls</p>
            <h2 className="mt-2 text-2xl font-semibold">Portrait HR Badge</h2>
            <p className="mt-2 text-sm text-white/80">Select employees, preview the badge, print it, or download all selected cards in one PDF.</p>
          </div>

          <div className="mt-4 grid grid-cols-2 gap-3">
            <button
              type="button"
              onClick={() => setActiveSide("front")}
              className={`rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                activeSide === "front" ? "bg-[#0F172A] text-white" : "bg-slate-100 text-slate-700"
              }`}
            >
              Front Side
            </button>
            <button
              type="button"
              onClick={() => setActiveSide("back")}
              className={`rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                activeSide === "back" ? "bg-[#0F172A] text-white" : "bg-slate-100 text-slate-700"
              }`}
            >
              Back Side
            </button>
          </div>

          {error ? (
            <div className="mt-4 rounded-[18px] border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">{error}</div>
          ) : null}

          <div className="mt-4 space-y-3">
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-slate-500">Employees</p>
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={toggleSelectAll}
                  className="text-xs font-semibold uppercase tracking-[0.24em] text-[#2563EB]"
                >
                  {allEmployeesSelected ? "Keep one selected" : "Select all"}
                </button>
                <button
                  type="button"
                  onClick={() => void loadEmployees()}
                  className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500"
                >
                  Retry
                </button>
              </div>
            </div>

            {loading ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-600">Loading employees...</div>
            ) : null}

            {!loading && error && employeesForRender.length === 0 ? (
              <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-4 text-sm text-rose-700">{error}</div>
            ) : null}

            {!loading && !employeesForRender.length && !error ? (
              <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-600">
                No employees were returned by the server.
              </div>
            ) : null}

            {employeesForRender.map((employee) => {
              const isActive = selectedEmployee?.id === employee.id;
              const isChecked = selectedEmployeeIds.includes(employee.id);

              return (
                <button
                  key={employee.id}
                  type="button"
                  onClick={() => setSelectedEmployeeId(employee.id)}
                  className={`w-full rounded-[20px] border px-4 py-4 text-left transition ${
                    isActive
                      ? "border-[#93c5fd] bg-[linear-gradient(135deg,#eff6ff_0%,#ffffff_100%)] shadow-[0_10px_22px_rgba(37,99,235,0.08)]"
                      : "border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50"
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <input
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => toggleEmployeeSelection(employee.id)}
                      onClick={(event) => event.stopPropagation()}
                      className="mt-1 h-4 w-4 rounded border-slate-300 text-[#2563EB] focus:ring-[#2563EB]"
                    />
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
                      <UserRound className="h-4 w-4" strokeWidth={2.1} />
                    </div>
                    <div className="min-w-0">
                      <p className="font-semibold text-slate-900">{employee.fullName}</p>
                      <p className="mt-1 text-[10px] font-semibold uppercase tracking-[0.3em] text-[#2563EB]">{employee.jobTitle}</p>
                      <p className="mt-2 text-sm text-slate-600">
                        {getEmployeeIdValue(employee)} - {employee.departmentName || "Not Assigned"}
                      </p>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        </section>

        <section className="panel p-6 shadow-[0_20px_50px_rgba(15,23,42,0.05)]">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-[#2563EB]">Live Preview</p>
              <h2 className="mt-2 text-3xl font-semibold text-[#0F172A]">Professional employee badge</h2>
              <p className="mt-2 max-w-2xl text-sm text-slate-600">
                Straightforward portrait identity card layout designed to feel like a real office HR badge.
              </p>
            </div>
            <div className="w-full rounded-[20px] border border-slate-200 bg-white px-4 py-3 text-left">
              <p className="text-[10px] font-semibold uppercase tracking-[0.3em] text-slate-500">Preview Employee</p>
              <p className="mt-1 text-sm font-semibold text-slate-900">{selectedEmployee?.fullName ?? "No employee selected"}</p>
              <p className="mt-1 text-xs text-slate-500">{selectedEmployees.length} employee(s) selected</p>
            </div>
          </div>

          <div className="mt-6 flex flex-wrap items-center gap-3">
            <button
              type="button"
              onClick={() => void downloadSelectedEmployeesPdf()}
              disabled={!!captureState || !selectedEmployees.length}
              className="inline-flex items-center gap-2 rounded-2xl bg-[#2563EB] px-4 py-3 text-sm font-semibold text-white transition hover:bg-[#1d4ed8] disabled:cursor-not-allowed disabled:opacity-60"
            >
              <Download className="h-4 w-4" strokeWidth={2.1} />
              {captureState?.action === "pdf" ? "Generating PDF..." : `Download ${selectedEmployees.length} Cards PDF`}
            </button>
          </div>

          <div className="mt-8 flex items-center justify-center rounded-[32px] bg-[#F8FAFC] px-6 py-8">
            {selectedEmployee ? (
              activeSide === "front" ? <FrontBadge employee={selectedEmployee} /> : <BackBadge employee={selectedEmployee} />
            ) : (
              <div className="flex h-[540px] w-[320px] items-center justify-center rounded-xl border border-dashed border-slate-300 bg-white px-6 text-center text-sm text-slate-500">
                Load employees to preview and export identity cards.
              </div>
            )}
          </div>
        </section>
      </div>

      <div className="pointer-events-none fixed left-[-10000px] top-0">
        {employeesForRender.map((employee) => (
          <div key={employee.id} className="space-y-6">
            <div ref={(node) => setCardRef("front", employee.id, node)}>
              <FrontBadge employee={employee} />
            </div>
            <div ref={(node) => setCardRef("back", employee.id, node)}>
              <BackBadge employee={employee} />
            </div>
          </div>
        ))}
      </div>
    </AnimatedPage>
  );
}
