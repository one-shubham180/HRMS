import { FormEvent, useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { useAuthStore } from "../features/auth/authStore";
import type { Employee, PagedResult, PayrollRecord } from "../types/hrms";

const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });

export function PayrollPage() {
  const roles = useAuthStore((state) => state.roles);
  const isAdminView = roles.includes("Admin") || roles.includes("HR");

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [records, setRecords] = useState<PagedResult<PayrollRecord> | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [salaryForm, setSalaryForm] = useState({
    employeeId: "",
    basicSalary: 42000,
    houseRentAllowance: 8500,
    conveyanceAllowance: 2500,
    medicalAllowance: 1800,
    otherAllowance: 1000,
    providentFundDeduction: 1500,
    taxDeduction: 2600,
  });
  const [generateForm, setGenerateForm] = useState({
    employeeId: "",
    year: new Date().getFullYear(),
    month: new Date().getMonth() + 1,
  });

  const loadData = async () => {
    const payrollResponse = await apiClient.get<PagedResult<PayrollRecord>>("/payroll?pageNumber=1&pageSize=20");
    setRecords(payrollResponse.data);

    if (isAdminView) {
      const employeeResponse = await apiClient.get<PagedResult<Employee>>("/employees?pageNumber=1&pageSize=50");
      setEmployees(employeeResponse.data.items);
      const firstEmployeeId = employeeResponse.data.items[0]?.id ?? "";
      setSalaryForm((current) => ({ ...current, employeeId: current.employeeId || firstEmployeeId }));
      setGenerateForm((current) => ({ ...current, employeeId: current.employeeId || firstEmployeeId }));
    }
  };

  useEffect(() => {
    loadData();
  }, [isAdminView]);

  const onSaveStructure = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    try {
      await apiClient.post("/payroll/salary-structures", salaryForm);
      setMessage("Salary structure saved.");
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not save salary structure.");
    }
  };

  const onGenerate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    try {
      await apiClient.post("/payroll/generate", generateForm);
      setMessage("Payroll generated.");
      await loadData();
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Payroll generation failed.");
    }
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Payroll"
        subtitle="Set salary structures, generate monthly payroll, and review payslip history with deduction visibility."
      />

      <div className={`grid gap-6 ${isAdminView ? "xl:grid-cols-[0.95fr_1.05fr]" : ""}`}>
        {isAdminView ? (
          <div className="space-y-6">
            <form className="panel space-y-4 p-6" onSubmit={onSaveStructure}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Salary Structure</p>
              <h2 className="font-display text-2xl text-ink">Configure components</h2>
              <select className="input" value={salaryForm.employeeId} onChange={(event) => setSalaryForm((current) => ({ ...current, employeeId: event.target.value }))}>
                {employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>{employee.fullName}</option>
                ))}
              </select>
              <div className="grid gap-4 md:grid-cols-2">
                {[
                  ["basicSalary", "Basic salary"],
                  ["houseRentAllowance", "HRA"],
                  ["conveyanceAllowance", "Conveyance"],
                  ["medicalAllowance", "Medical"],
                  ["otherAllowance", "Other allowance"],
                  ["providentFundDeduction", "PF deduction"],
                  ["taxDeduction", "Tax deduction"],
                ].map(([key, label]) => (
                  <input
                    key={key}
                    className="input"
                    type="number"
                    placeholder={label}
                    value={salaryForm[key as keyof typeof salaryForm]}
                    onChange={(event) => setSalaryForm((current) => ({ ...current, [key]: Number(event.target.value) }))}
                  />
                ))}
              </div>
              <button type="submit" className="btn-primary">Save Salary Structure</button>
            </form>

            <form className="panel space-y-4 p-6" onSubmit={onGenerate}>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Payroll Run</p>
              <h2 className="font-display text-2xl text-ink">Generate monthly payroll</h2>
              <select className="input" value={generateForm.employeeId} onChange={(event) => setGenerateForm((current) => ({ ...current, employeeId: event.target.value }))}>
                {employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>{employee.fullName}</option>
                ))}
              </select>
              <div className="grid gap-4 md:grid-cols-2">
                <input className="input" type="number" value={generateForm.year} onChange={(event) => setGenerateForm((current) => ({ ...current, year: Number(event.target.value) }))} />
                <input className="input" type="number" min={1} max={12} value={generateForm.month} onChange={(event) => setGenerateForm((current) => ({ ...current, month: Number(event.target.value) }))} />
              </div>
              {message ? <div className="soft-pop rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}
              <button type="submit" className="btn-primary">Generate Payroll</button>
            </form>
          </div>
        ) : null}

        <div className="panel p-6">
          <h2 className="font-display text-2xl text-ink">Payslip History</h2>
          <div className="mt-5 space-y-3">
            {records?.items.map((record, index) => (
              <div
                key={record.id}
                className="soft-pop rounded-2xl border border-slate-100 bg-slate-50 p-4 transition-all duration-300 hover:-translate-y-1"
                style={{ animationDelay: `${index * 35}ms` }}
              >
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="font-semibold text-ink">{record.employeeName}</p>
                    <p className="mt-1 text-sm text-slate-600">
                      {record.month}/{record.year} · Payslip {record.payslipNumber}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="font-display text-2xl text-ink">{currency.format(record.netSalary)}</p>
                    <p className="mt-1 text-xs text-slate-500">Deductions: {currency.format(record.totalDeductions)}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
