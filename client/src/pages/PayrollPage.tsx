import { FormEvent, useEffect, useRef, useState } from "react";
import { FileClock, LoaderCircle, Save } from "lucide-react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { EmptyStateCard } from "../components/EmptyStateCard";
import { PageHeader } from "../components/PageHeader";
import { SelectField } from "../components/SelectField";
import { ToastBanner } from "../components/ToastBanner";
import { useAuthStore } from "../features/auth/authStore";
import type { Department, Employee, PagedResult, PayrollBatchResult, PayrollRecord, SalaryStructure } from "../types/hrms";

type PayrollGenerationScope = "all" | "department" | "employee";
type FeedbackTone = "success" | "error" | "info";

const now = new Date();
const currentYear = now.getFullYear();
const currentMonth = now.getMonth() + 1;
const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });
const salaryFields = [
  ["basicSalary", "Basic Salary"],
  ["houseRentAllowance", "House Rent Allowance"],
  ["conveyanceAllowance", "Conveyance Allowance"],
  ["medicalAllowance", "Medical Allowance"],
  ["otherAllowance", "Other Allowance"],
  ["providentFundDeduction", "Provident Fund Deduction"],
  ["taxDeduction", "Tax Deduction"],
] as const;

const createDefaultSalaryForm = (employeeId = "") => ({
  employeeId,
  basicSalary: 42000,
  houseRentAllowance: 8500,
  conveyanceAllowance: 2500,
  medicalAllowance: 1800,
  otherAllowance: 1000,
  providentFundDeduction: 1500,
  taxDeduction: 2600,
});

const mapSalaryStructureToForm = (employeeId: string, salaryStructure?: SalaryStructure) => (
  salaryStructure
    ? {
        employeeId,
        basicSalary: salaryStructure.basicSalary,
        houseRentAllowance: salaryStructure.houseRentAllowance,
        conveyanceAllowance: salaryStructure.conveyanceAllowance,
        medicalAllowance: salaryStructure.medicalAllowance,
        otherAllowance: salaryStructure.otherAllowance,
        providentFundDeduction: salaryStructure.providentFundDeduction,
        taxDeduction: salaryStructure.taxDeduction,
      }
    : createDefaultSalaryForm(employeeId)
);

function buildBatchMessage(result: PayrollBatchResult) {
  const summary = `Payroll generated for ${result.generatedCount} of ${result.totalEmployees} employees in ${result.scope} for ${result.month}/${result.year}.`;
  if (!result.skippedCount) {
    return summary;
  }

  const skippedPreview = result.skippedEmployees.slice(0, 3).join(", ");
  const suffix = result.skippedEmployees.length > 3 ? ", and more" : "";
  return `${summary} Skipped ${result.skippedCount}: ${skippedPreview}${suffix}.`;
}

async function getBlobErrorMessage(error: any, fallbackMessage: string) {
  const responseData = error.response?.data;

  if (responseData instanceof Blob) {
    try {
      const text = await responseData.text();
      const parsed = JSON.parse(text) as { message?: string };
      return parsed.message ?? `${fallbackMessage} (${error.response?.status ?? "unknown status"})`;
    } catch {
      return `${fallbackMessage} (${error.response?.status ?? "unknown status"})`;
    }
  }

  return error.response?.data?.message ?? fallbackMessage;
}

export function PayrollPage() {
  const roles = useAuthStore((state) => state.roles);
  const isAdminView = roles.includes("Admin") || roles.includes("HR");
  const pageSubtitle = isAdminView
    ? "Set salary structures, generate payroll in bulk, and export monthly payroll history."
    : "Review your payslip history and export your payroll records for a chosen month.";

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [records, setRecords] = useState<PagedResult<PayrollRecord> | null>(null);
  const [feedback, setFeedback] = useState<{ tone: FeedbackTone; title: string; message: string } | null>(null);
  const [salaryForm, setSalaryForm] = useState(() => createDefaultSalaryForm());
  const [isLoadingSalaryStructure, setIsLoadingSalaryStructure] = useState(false);
  const [isSavingStructure, setIsSavingStructure] = useState(false);
  const [isGeneratingPayroll, setIsGeneratingPayroll] = useState(false);
  const [isExportingPayroll, setIsExportingPayroll] = useState(false);
  const latestSalaryRequestId = useRef(0);
  const [generateForm, setGenerateForm] = useState({
    scope: "all" as PayrollGenerationScope,
    employeeId: "",
    departmentId: "",
    year: currentYear,
    month: currentMonth,
  });
  const [historyFilters, setHistoryFilters] = useState({
    year: currentYear,
    month: currentMonth,
    departmentId: "",
  });

  useEffect(() => {
    if (!feedback) {
      return;
    }

    const timer = window.setTimeout(() => setFeedback(null), 3600);
    return () => window.clearTimeout(timer);
  }, [feedback]);

  const showFeedback = (tone: FeedbackTone, title: string, message: string) => {
    setFeedback({ tone, title, message });
  };

  const loadSalaryStructure = async (employeeId: string) => {
    const requestId = ++latestSalaryRequestId.current;

    if (!employeeId) {
      setSalaryForm(createDefaultSalaryForm());
      setIsLoadingSalaryStructure(false);
      return;
    }

    setIsLoadingSalaryStructure(true);

    try {
      const response = await apiClient.get<SalaryStructure | null>(`/payroll/salary-structures/${employeeId}`, {
        validateStatus: (status) => status >= 200 && status < 300,
      });

      if (latestSalaryRequestId.current !== requestId) {
        return;
      }

      setSalaryForm(mapSalaryStructureToForm(employeeId, response.data ?? undefined));
    } catch (error: any) {
      if (latestSalaryRequestId.current !== requestId) {
        return;
      }

      showFeedback("error", "Salary structure unavailable", error.response?.data?.message ?? "Could not load salary structure.");
    } finally {
      if (latestSalaryRequestId.current === requestId) {
        setIsLoadingSalaryStructure(false);
      }
    }
  };

  const loadHistory = async (overrides?: Partial<typeof historyFilters>) => {
    const activeFilters = { ...historyFilters, ...overrides };
    const query = new URLSearchParams({
      pageNumber: "1",
      pageSize: "20",
      year: String(activeFilters.year),
      month: String(activeFilters.month),
    });

    if (isAdminView && activeFilters.departmentId) {
      query.set("departmentId", activeFilters.departmentId);
    }

    const response = await apiClient.get<PagedResult<PayrollRecord>>(`/payroll?${query.toString()}`);
    setRecords(response.data);
  };

  const loadAdminData = async () => {
    const [employeeResponse, departmentResponse] = await Promise.all([
      apiClient.get<PagedResult<Employee>>("/employees?pageNumber=1&pageSize=100"),
      apiClient.get<Department[]>("/departments"),
    ]);

    setEmployees(employeeResponse.data.items);
    setDepartments(departmentResponse.data);

    const firstEmployeeId = employeeResponse.data.items[0]?.id ?? "";
    const firstDepartmentId = departmentResponse.data[0]?.id ?? "";
    const selectedEmployeeId = salaryForm.employeeId || firstEmployeeId;

    setGenerateForm((current) => ({
      ...current,
      employeeId: current.employeeId || firstEmployeeId,
      departmentId: current.departmentId || firstDepartmentId,
    }));

    await loadSalaryStructure(selectedEmployeeId);
  };

  useEffect(() => {
    void loadHistory();
  }, [historyFilters.departmentId, historyFilters.month, historyFilters.year, isAdminView]);

  useEffect(() => {
    if (isAdminView) {
      void loadAdminData();
      return;
    }

    setEmployees([]);
    setDepartments([]);
    setSalaryForm(createDefaultSalaryForm());
  }, [isAdminView]);

  const onSaveStructure = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSavingStructure(true);
    setFeedback(null);

    try {
      const response = await apiClient.post<SalaryStructure>("/payroll/salary-structures", salaryForm);
      setSalaryForm(mapSalaryStructureToForm(salaryForm.employeeId, response.data));
      showFeedback("success", "Salary structure saved", "Payroll components were updated and are ready for the next payroll run.");
    } catch (error: any) {
      showFeedback("error", "Save failed", error.response?.data?.message ?? "Could not save salary structure.");
    } finally {
      setIsSavingStructure(false);
    }
  };

  const onSalaryEmployeeChange = async (employeeId: string) => {
    setFeedback(null);
    setSalaryForm(createDefaultSalaryForm(employeeId));
    setGenerateForm((current) => ({ ...current, employeeId }));
    await loadSalaryStructure(employeeId);
  };

  const onGenerate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsGeneratingPayroll(true);
    setFeedback(null);

    try {
      if (generateForm.scope === "employee") {
        await apiClient.post("/payroll/generate", {
          employeeId: generateForm.employeeId,
          year: generateForm.year,
          month: generateForm.month,
        });
        showFeedback("success", "Payroll generated", "Payroll was generated for the selected employee.");
      } else {
        const response = await apiClient.post<PayrollBatchResult>("/payroll/generate-batch", {
          departmentId: generateForm.scope === "department" ? generateForm.departmentId : null,
          year: generateForm.year,
          month: generateForm.month,
        });
        showFeedback("success", "Payroll run completed", buildBatchMessage(response.data));
      }

      const nextHistoryFilters = { ...historyFilters, year: generateForm.year, month: generateForm.month };
      setHistoryFilters(nextHistoryFilters);
      await loadHistory(nextHistoryFilters);
    } catch (error: any) {
      showFeedback("error", "Payroll generation failed", error.response?.data?.message ?? "Payroll generation failed.");
    } finally {
      setIsGeneratingPayroll(false);
    }
  };

  const onExportHistory = async () => {
    setIsExportingPayroll(true);
    setFeedback(null);

    try {
      const query = new URLSearchParams({
        year: String(historyFilters.year),
        month: String(historyFilters.month),
      });

      if (isAdminView && historyFilters.departmentId) {
        query.set("departmentId", historyFilters.departmentId);
      }

      const response = await apiClient.get(`/payroll/export?${query.toString()}`, {
        responseType: "blob",
      });

      const downloadUrl = window.URL.createObjectURL(new Blob([response.data], { type: "text/csv;charset=utf-8;" }));
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `payroll-${historyFilters.year}-${String(historyFilters.month).padStart(2, "0")}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(downloadUrl);

      showFeedback("success", "Payroll history exported", `CSV export is ready for ${historyFilters.month}/${historyFilters.year}.`);
    } catch (error: any) {
      showFeedback("error", "Payroll export failed", await getBlobErrorMessage(error, "Payroll export failed."));
    } finally {
      setIsExportingPayroll(false);
    }
  };

  const isGenerateDisabled = isGeneratingPayroll ||
    (generateForm.scope === "employee" && !generateForm.employeeId) ||
    (generateForm.scope === "department" && !generateForm.departmentId);

  const generateButtonLabel = isGeneratingPayroll
    ? "Generating..."
    : generateForm.scope === "all"
      ? "Generate All Payroll"
      : generateForm.scope === "department"
        ? "Generate Department Payroll"
        : "Generate Employee Payroll";

  return (
    <AnimatedPage>
      {feedback ? (
        <div className="fixed right-4 top-4 z-50 w-[min(24rem,calc(100vw-2rem))] lg:right-8 lg:top-8">
          <ToastBanner
            tone={feedback.tone}
            title={feedback.title}
            message={feedback.message}
            onDismiss={() => setFeedback(null)}
          />
        </div>
      ) : null}

      <PageHeader
        title="Payroll"
        subtitle={pageSubtitle}
      />

      <div className={`mt-6 grid gap-6 ${isAdminView ? "xl:grid-cols-[0.95fr_1.05fr]" : ""}`}>
        {isAdminView ? (
          <div className="space-y-6">
            <form className="panel space-y-4 p-6" onSubmit={onSaveStructure}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Salary Structure</p>
              <h2 className="font-display text-2xl text-ink">Configure components</h2>
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">Employee</label>
                <SelectField
                  value={salaryForm.employeeId}
                  options={employees.map((employee) => ({ value: employee.id, label: employee.fullName }))}
                  onChange={(value) => void onSalaryEmployeeChange(value)}
                  placeholder="Select employee"
                />
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                {salaryFields.map(([key, label]) => (
                  <div key={key} className="flex flex-col gap-1.5">
                    <label className="pl-1 text-xs font-semibold text-slate-500">{label}</label>
                    <input
                      className="input"
                      type="number"
                      disabled={isLoadingSalaryStructure}
                      value={salaryForm[key]}
                      onChange={(event) => setSalaryForm((current) => ({ ...current, [key]: Number(event.target.value) }))}
                    />
                  </div>
                ))}
              </div>
              <button
                type="submit"
                className="btn-primary gap-2 disabled:cursor-not-allowed disabled:opacity-70"
                disabled={isSavingStructure || isLoadingSalaryStructure || !salaryForm.employeeId}
              >
                {isSavingStructure ? (
                  <>
                    <LoaderCircle className="h-4 w-4 animate-spin" />
                    Saving structure...
                  </>
                ) : isLoadingSalaryStructure ? (
                  <>
                    <LoaderCircle className="h-4 w-4 animate-spin" />
                    Loading structure...
                  </>
                ) : (
                  <>
                    <Save className="h-4 w-4" />
                    Save Salary Structure
                  </>
                )}
              </button>
              {isSavingStructure ? <p className="text-sm text-slate-500">Updating salary components and refreshing the structure in payroll.</p> : null}
            </form>

            <form className="panel space-y-4 p-6" onSubmit={onGenerate}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Payroll Run</p>
              <h2 className="font-display text-2xl text-ink">Generate monthly payroll</h2>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="flex flex-col gap-1.5">
                  <label className="pl-1 text-xs font-semibold text-slate-500">Scope</label>
                  <SelectField
                    value={generateForm.scope}
                    options={[
                      { value: "all", label: "All employees" },
                      { value: "department", label: "Department" },
                      { value: "employee", label: "Single employee" },
                    ]}
                    onChange={(value) => setGenerateForm((current) => ({ ...current, scope: value as PayrollGenerationScope }))}
                  />
                </div>
                {generateForm.scope === "department" ? (
                  <div className="flex flex-col gap-1.5">
                    <label className="pl-1 text-xs font-semibold text-slate-500">Department</label>
                    <SelectField
                      value={generateForm.departmentId}
                      options={departments.map((department) => ({ value: department.id, label: department.name }))}
                      onChange={(value) => setGenerateForm((current) => ({ ...current, departmentId: value }))}
                      placeholder="Select department"
                    />
                  </div>
                ) : null}
                {generateForm.scope === "employee" ? (
                  <div className="flex flex-col gap-1.5">
                    <label className="pl-1 text-xs font-semibold text-slate-500">Employee</label>
                    <SelectField
                      value={generateForm.employeeId}
                      options={employees.map((employee) => ({ value: employee.id, label: employee.fullName }))}
                      onChange={(value) => setGenerateForm((current) => ({ ...current, employeeId: value }))}
                      placeholder="Select employee"
                    />
                  </div>
                ) : null}
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="flex flex-col gap-1.5">
                  <label className="pl-1 text-xs font-semibold text-slate-500">Year</label>
                  <input className="input" type="number" value={generateForm.year} onChange={(event) => setGenerateForm((current) => ({ ...current, year: Number(event.target.value) }))} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="pl-1 text-xs font-semibold text-slate-500">Month</label>
                  <input className="input" type="number" min={1} max={12} value={generateForm.month} onChange={(event) => setGenerateForm((current) => ({ ...current, month: Number(event.target.value) }))} />
                </div>
              </div>
              <button type="submit" className="btn-primary disabled:cursor-not-allowed disabled:opacity-70" disabled={isGenerateDisabled}>
                {generateButtonLabel}
              </button>
            </form>
          </div>
        ) : null}

        <div className="panel p-6">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h2 className="font-display text-2xl text-ink">Payslip History</h2>
              <p className="mt-2 text-sm text-slate-600">Filter a month and export the current payroll history as a CSV file.</p>
            </div>
            <button type="button" className="btn-secondary disabled:cursor-not-allowed disabled:opacity-70" disabled={isExportingPayroll} onClick={onExportHistory}>
              {isExportingPayroll ? "Exporting..." : "Export Month CSV"}
            </button>
          </div>

          <div className={`mt-5 grid gap-4 ${isAdminView ? "md:grid-cols-3" : "md:grid-cols-2"}`}>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Year</label>
              <input className="input" type="number" value={historyFilters.year} onChange={(event) => setHistoryFilters((current) => ({ ...current, year: Number(event.target.value) }))} />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="pl-1 text-xs font-semibold text-slate-500">Month</label>
              <input className="input" type="number" min={1} max={12} value={historyFilters.month} onChange={(event) => setHistoryFilters((current) => ({ ...current, month: Number(event.target.value) }))} />
            </div>
            {isAdminView ? (
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">Department</label>
                <SelectField
                  value={historyFilters.departmentId}
                  options={[
                    { value: "", label: "All departments" },
                    ...departments.map((department) => ({ value: department.id, label: department.name })),
                  ]}
                  onChange={(value) => setHistoryFilters((current) => ({ ...current, departmentId: value }))}
                />
              </div>
            ) : null}
          </div>

          <div className="mt-5 space-y-3">
            {records?.items.length ? records.items.map((record, index) => (
              <div
                key={record.id}
                className="soft-pop rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1"
                style={{ animationDelay: `${index * 35}ms` }}
              >
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="font-semibold text-ink">{record.employeeName}</p>
                    <p className="mt-1 text-sm text-slate-600">
                      {record.month}/{record.year} - Payslip {record.payslipNumber}
                    </p>
                    <p className="mt-1 text-xs text-slate-500">Payable days: {record.payableDays} - Loss of pay: {record.lossOfPayDays}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-display text-2xl text-ink">{currency.format(record.netSalary)}</p>
                    <p className="mt-1 text-xs text-slate-500">Gross: {currency.format(record.grossSalary)}</p>
                    <p className="mt-1 text-xs text-slate-500">Deductions: {currency.format(record.totalDeductions)}</p>
                  </div>
                </div>
              </div>
            )) : (
              <EmptyStateCard
                title="No payslip history found"
                description={`There are no payroll records for ${historyFilters.month}/${historyFilters.year} yet. Try another period or generate payroll for this cycle first.`}
                icon={<FileClock className="h-8 w-8 text-ember" strokeWidth={1.8} />}
              />
            )}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
