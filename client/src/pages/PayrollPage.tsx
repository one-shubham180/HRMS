import { FormEvent, useEffect, useRef, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { useAuthStore } from "../features/auth/authStore";
import type { Department, Employee, PagedResult, PayrollBatchResult, PayrollRecord, SalaryStructure } from "../types/hrms";

type PayrollGenerationScope = "all" | "department" | "employee";

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
  const [message, setMessage] = useState<string | null>(null);
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

      setMessage(error.response?.data?.message ?? "Could not load salary structure.");
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
    setMessage(null);

    try {
      const response = await apiClient.post<SalaryStructure>("/payroll/salary-structures", salaryForm);
      setSalaryForm(mapSalaryStructureToForm(salaryForm.employeeId, response.data));
      setMessage("Salary structure saved.");
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not save salary structure.");
    } finally {
      setIsSavingStructure(false);
    }
  };

  const onSalaryEmployeeChange = async (employeeId: string) => {
    setMessage(null);
    setSalaryForm(createDefaultSalaryForm(employeeId));
    setGenerateForm((current) => ({ ...current, employeeId }));
    await loadSalaryStructure(employeeId);
  };

  const onGenerate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsGeneratingPayroll(true);
    setMessage(null);

    try {
      if (generateForm.scope === "employee") {
        await apiClient.post("/payroll/generate", {
          employeeId: generateForm.employeeId,
          year: generateForm.year,
          month: generateForm.month,
        });
        setMessage("Payroll generated for the selected employee.");
      } else {
        const response = await apiClient.post<PayrollBatchResult>("/payroll/generate-batch", {
          departmentId: generateForm.scope === "department" ? generateForm.departmentId : null,
          year: generateForm.year,
          month: generateForm.month,
        });
        setMessage(buildBatchMessage(response.data));
      }

      const nextHistoryFilters = { ...historyFilters, year: generateForm.year, month: generateForm.month };
      setHistoryFilters(nextHistoryFilters);
      await loadHistory(nextHistoryFilters);
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Payroll generation failed.");
    } finally {
      setIsGeneratingPayroll(false);
    }
  };

  const onExportHistory = async () => {
    setIsExportingPayroll(true);
    setMessage(null);

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

      setMessage(`Payroll history exported for ${historyFilters.month}/${historyFilters.year}.`);
    } catch (error: any) {
      setMessage(await getBlobErrorMessage(error, "Payroll export failed."));
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
      <PageHeader
        title="Payroll"
        subtitle={pageSubtitle}
      />

      {message ? <div className="soft-pop mt-6 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}

      <div className={`mt-6 grid gap-6 ${isAdminView ? "xl:grid-cols-[0.95fr_1.05fr]" : ""}`}>
        {isAdminView ? (
          <div className="space-y-6">
            <form className="panel space-y-4 p-6" onSubmit={onSaveStructure}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Salary Structure</p>
              <h2 className="font-display text-2xl text-ink">Configure components</h2>
              <div className="flex flex-col gap-1.5">
                <label className="pl-1 text-xs font-semibold text-slate-500">Employee</label>
                <select className="input" value={salaryForm.employeeId} onChange={(event) => void onSalaryEmployeeChange(event.target.value)}>
                  {employees.map((employee) => (
                    <option key={employee.id} value={employee.id}>{employee.fullName}</option>
                  ))}
                </select>
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
                className="btn-primary disabled:cursor-not-allowed disabled:opacity-70"
                disabled={isSavingStructure || isLoadingSalaryStructure || !salaryForm.employeeId}
              >
                {isSavingStructure ? "Saving..." : isLoadingSalaryStructure ? "Loading..." : "Save Salary Structure"}
              </button>
            </form>

            <form className="panel space-y-4 p-6" onSubmit={onGenerate}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Payroll Run</p>
              <h2 className="font-display text-2xl text-ink">Generate monthly payroll</h2>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="flex flex-col gap-1.5">
                  <label className="pl-1 text-xs font-semibold text-slate-500">Scope</label>
                  <select
                    className="input"
                    value={generateForm.scope}
                    onChange={(event) => setGenerateForm((current) => ({ ...current, scope: event.target.value as PayrollGenerationScope }))}
                  >
                    <option value="all">All employees</option>
                    <option value="department">Department</option>
                    <option value="employee">Single employee</option>
                  </select>
                </div>
                {generateForm.scope === "department" ? (
                  <div className="flex flex-col gap-1.5">
                    <label className="pl-1 text-xs font-semibold text-slate-500">Department</label>
                    <select
                      className="input"
                      value={generateForm.departmentId}
                      onChange={(event) => setGenerateForm((current) => ({ ...current, departmentId: event.target.value }))}
                    >
                      {departments.map((department) => (
                        <option key={department.id} value={department.id}>{department.name}</option>
                      ))}
                    </select>
                  </div>
                ) : null}
                {generateForm.scope === "employee" ? (
                  <div className="flex flex-col gap-1.5">
                    <label className="pl-1 text-xs font-semibold text-slate-500">Employee</label>
                    <select
                      className="input"
                      value={generateForm.employeeId}
                      onChange={(event) => setGenerateForm((current) => ({ ...current, employeeId: event.target.value }))}
                    >
                      {employees.map((employee) => (
                        <option key={employee.id} value={employee.id}>{employee.fullName}</option>
                      ))}
                    </select>
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
                <select className="input" value={historyFilters.departmentId} onChange={(event) => setHistoryFilters((current) => ({ ...current, departmentId: event.target.value }))}>
                  <option value="">All departments</option>
                  {departments.map((department) => (
                    <option key={department.id} value={department.id}>{department.name}</option>
                  ))}
                </select>
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
              <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-sm text-slate-600">
                No payroll records were found for {historyFilters.month}/{historyFilters.year}.
              </div>
            )}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
