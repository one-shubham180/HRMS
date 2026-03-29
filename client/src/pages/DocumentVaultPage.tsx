import { useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { AnimatedPage } from "../components/AnimatedPage";
import { PageHeader } from "../components/PageHeader";
import { SelectField } from "../components/SelectField";
import { useAuthStore } from "../features/auth/authStore";
import type { Employee, EmployeeDocument, PagedResult } from "../types/hrms";

const apiAssetBaseUrl = (apiClient.defaults.baseURL ?? "").replace(/\/api\/?$/, "");

function resolveAssetUrl(path: string) {
  return path.startsWith("http") ? path : `${apiAssetBaseUrl}${path}`;
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("en-IN");
}

function formatSize(bytes: number) {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function DocumentVaultPage() {
  const roles = useAuthStore((state) => state.roles);
  const employeeId = useAuthStore((state) => state.employeeId);
  const isManagerView = roles.includes("Admin") || roles.includes("HR");

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [documents, setDocuments] = useState<EmployeeDocument[]>([]);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState(employeeId ?? "");
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState<string | null>(null);

  const loadDocuments = async (targetEmployeeId: string) => {
    if (!targetEmployeeId) {
      setDocuments([]);
      setLoading(false);
      return;
    }

    setLoading(true);

    try {
      const response = await apiClient.get<EmployeeDocument[]>(`/documents/${targetEmployeeId}`);
      setDocuments(response.data);
    } catch (error: any) {
      setMessage(error.response?.data?.message ?? "Could not load document vault.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let active = true;

    const loadData = async () => {
      setMessage(null);

      try {
        if (isManagerView) {
          const employeeResponse = await apiClient.get<PagedResult<Employee>>("/employees?pageNumber=1&pageSize=100&sortBy=name");
          if (!active) {
            return;
          }

          setEmployees(employeeResponse.data.items);
          const initialEmployeeId = selectedEmployeeId || employeeResponse.data.items[0]?.id || "";
          setSelectedEmployeeId(initialEmployeeId);
          await loadDocuments(initialEmployeeId);
          return;
        }

        await loadDocuments(employeeId ?? "");
      } catch (error: any) {
        if (!active) {
          return;
        }

        setMessage(error.response?.data?.message ?? "Could not load vault data.");
        setLoading(false);
      }
    };

    void loadData();

    return () => {
      active = false;
    };
  }, [employeeId, isManagerView]);

  const onEmployeeChange = async (nextEmployeeId: string) => {
    setSelectedEmployeeId(nextEmployeeId);
    setMessage(null);
    await loadDocuments(nextEmployeeId);
  };

  return (
    <AnimatedPage>
      <PageHeader
        title="Document Vault"
        subtitle={isManagerView
          ? "Browse permanent employee records, including system-published payslips and uploaded employment documents."
          : "Your document vault now stores employment records and system-generated payslips as part of your digital employee file."}
      />

      {message ? <div className="soft-pop mt-6 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{message}</div> : null}

      <div className="panel mt-6 p-6">
        {isManagerView ? (
          <div className="mb-5 flex flex-col gap-1.5">
            <label className="pl-1 text-xs font-semibold text-slate-500">Employee</label>
            <SelectField
              className="max-w-md"
              value={selectedEmployeeId}
              options={employees.map((employee) => ({ value: employee.id, label: employee.fullName }))}
              onChange={(value) => void onEmployeeChange(value)}
              placeholder="Select employee"
            />
          </div>
        ) : null}

        <div className="space-y-3">
          {loading ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">Loading documents...</div> : null}
          {!loading && !documents.length ? <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">No documents were found in this vault yet.</div> : null}
          {documents.map((document) => (
            <div key={document.id} className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
              <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <div>
                  <p className="font-semibold text-ink">{document.fileName}</p>
                  <p className="mt-1 text-sm text-slate-600">
                    {document.category} · {document.contentType} · {formatSize(document.fileSize)}
                  </p>
                  <p className="mt-1 text-xs text-slate-500">
                    Added {formatDateTime(document.createdUtc)}{document.isSystemGenerated ? " · System published" : ""}
                  </p>
                </div>
                <a className="btn-secondary" href={resolveAssetUrl(document.storagePath)} target="_blank" rel="noreferrer">
                  Open Document
                </a>
              </div>
            </div>
          ))}
        </div>
      </div>
    </AnimatedPage>
  );
}
