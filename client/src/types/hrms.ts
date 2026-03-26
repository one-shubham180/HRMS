export type Role = "Admin" | "HR" | "Employee";
export type EmploymentType = "FullTime" | "PartTime" | "Contract" | "Intern";
export type LeaveType = "Annual" | "Sick" | "Casual" | "Unpaid";
export type LeaveStatus = "Pending" | "Approved" | "Rejected";
export type AttendanceStatus = "Present" | "Late" | "HalfDay" | "Absent";

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresUtc: string;
  userId: string;
  email: string;
  roles: Role[];
  employeeId?: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Department {
  id: string;
  name: string;
  code: string;
  description?: string | null;
}

export interface Employee {
  id: string;
  userId: string;
  departmentId: string;
  departmentName: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  workEmail: string;
  phoneNumber?: string | null;
  dateOfBirth: string;
  joinDate: string;
  jobTitle: string;
  employmentType: EmploymentType;
  annualLeaveBalance: number;
  sickLeaveBalance: number;
  casualLeaveBalance: number;
  profileImageUrl?: string | null;
  isActive: boolean;
  grossSalary: number;
}

export interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  workDate: string;
  checkInUtc: string;
  checkInCapturedPhotoUtc?: string | null;
  checkInPhotoUrl?: string | null;
  checkInLatitude?: number | null;
  checkInLongitude?: number | null;
  checkInLocationLabel?: string | null;
  checkOutUtc?: string | null;
  checkOutCapturedPhotoUtc?: string | null;
  checkOutPhotoUrl?: string | null;
  checkOutLatitude?: number | null;
  checkOutLongitude?: number | null;
  checkOutLocationLabel?: string | null;
  status: AttendanceStatus;
  workedHours: number;
  notes?: string | null;
}

export interface AttendanceSettings {
  requireGeoTaggedPhotoForAttendance: boolean;
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveType: LeaveType;
  status: LeaveStatus;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  reviewRemarks?: string | null;
}

export interface SalaryStructure {
  id: string;
  employeeId: string;
  basicSalary: number;
  houseRentAllowance: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  otherAllowance: number;
  providentFundDeduction: number;
  taxDeduction: number;
  grossSalary: number;
  totalDeductions: number;
}

export interface PayrollRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  year: number;
  month: number;
  payableDays: number;
  lossOfPayDays: number;
  grossSalary: number;
  totalDeductions: number;
  netSalary: number;
  payslipNumber: string;
  generatedUtc: string;
}

export interface PayrollBatchResult {
  year: number;
  month: number;
  scope: string;
  totalEmployees: number;
  generatedCount: number;
  skippedCount: number;
  skippedEmployees: string[];
}

export interface AdminDashboard {
  totalEmployees: number;
  totalDepartments: number;
  pendingLeaves: number;
  presentToday: number;
  monthlyPayroll: number;
  recentEmployees: Employee[];
  pendingLeaveRequests: LeaveRequest[];
  recentPayrolls: PayrollRecord[];
}

export interface EmployeeDashboard {
  profile: Employee;
  annualLeaveBalance: number;
  sickLeaveBalance: number;
  casualLeaveBalance: number;
  recentAttendance: AttendanceRecord[];
  recentLeaves: LeaveRequest[];
}
