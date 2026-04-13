export type Role = "Admin" | "HR" | "Employee";
export type EmploymentType = "FullTime" | "PartTime" | "Contract" | "Intern";
export type LeaveType = "Annual" | "Sick" | "Casual" | "Unpaid";
export type LeaveStatus = "Pending" | "Approved" | "Rejected";
export type AttendanceStatus = "Present" | "Late" | "HalfDay" | "Absent";
export type CandidateStatus = "Applied" | "Screening" | "Interviewing" | "Offered" | "Hired" | "Rejected";
export type AppraisalStatus = "Initialized" | "InProgress" | "Completed" | "Archived";
export type DocumentCategory = "IdProof" | "Contract" | "Resume" | "Payslip" | "OfferLetter" | "Other";
export type NotificationStatus = "Pending" | "Delivered" | "Read" | "Failed";
export type NotificationType = "Leave" | "Payroll" | "Onboarding" | "Recruitment" | "General";

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresUtc: string;
  userId: string;
  email: string;
  roles: Role[];
  employeeId?: string;
}

export type AiChatRole = "user" | "assistant";

export interface AiChatMessage {
  role: AiChatRole;
  content: string;
}

export interface AiAssistantAction {
  label: string;
  path: string;
  description: string;
  autoNavigate: boolean;
}

export interface AiChatResponse {
  message: string;
  actions: AiAssistantAction[];
  autoNavigatePath?: string | null;
}

export interface AiChatStreamEvent {
  type: "delta" | "complete" | "error";
  delta?: string | null;
  message?: string | null;
  actions: AiAssistantAction[];
  autoNavigatePath?: string | null;
  error?: string | null;
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
  rosterAssignmentId?: string | null;
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
  scheduledShiftName?: string | null;
  scheduledStartTimeLocal?: string | null;
  scheduledEndTimeLocal?: string | null;
  scheduledHours: number;
  overtimeHours: number;
  isHoliday: boolean;
  holidayName?: string | null;
  isRestDay: boolean;
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

export interface HolidayDate {
  id: string;
  date: string;
  name: string;
  isOptional: boolean;
}

export interface HolidayCalendar {
  id: string;
  name: string;
  code: string;
  isDefault: boolean;
  holidays: HolidayDate[];
}

export interface ShiftDefinition {
  id: string;
  name: string;
  code: string;
  startTimeLocal: string;
  endTimeLocal: string;
  standardHours: number;
  breakMinutes: number;
  minimumOvertimeMinutes: number;
}

export interface RosterAssignment {
  id: string;
  employeeId: string;
  employeeName: string;
  shiftDefinitionId: string;
  shiftName: string;
  shiftStartTimeLocal?: string | null;
  shiftEndTimeLocal?: string | null;
  shiftHours: number;
  breakMinutes: number;
  workDate: string;
  isRestDay: boolean;
  notes?: string | null;
}

export interface NotificationItem {
  id: string;
  recipientUserId: string;
  triggeredByUserId?: string | null;
  type: NotificationType;
  status: NotificationStatus;
  title: string;
  message: string;
  relatedEntityType: string;
  relatedEntityId?: string | null;
  deliveredUtc?: string | null;
  readUtc?: string | null;
}

export interface AuditTrailEntry {
  id: string;
  actorUserId?: string | null;
  notificationItemId?: string | null;
  entityType: string;
  entityId?: string | null;
  action: string;
  oldState?: string | null;
  newState?: string | null;
  metadata?: string | null;
  occurredUtc: string;
}

export interface EmployeeDocument {
  id: string;
  employeeId: string;
  payrollRecordId?: string | null;
  category: DocumentCategory;
  fileName: string;
  storagePath: string;
  contentType: string;
  fileSize: number;
  isSystemGenerated: boolean;
  uploadedByUserId?: string | null;
  createdUtc: string;
}

export interface Candidate {
  id: string;
  departmentId: string;
  departmentName: string;
  convertedEmployeeId?: string | null;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  jobTitle: string;
  status: CandidateStatus;
  hiredDate?: string | null;
  notes?: string | null;
}

export interface PerformanceAppraisal {
  id: string;
  employeeId: string;
  employeeName: string;
  initializedFromCandidateId?: string | null;
  cycleName: string;
  startDate: string;
  endDate: string;
  status: AppraisalStatus;
  goalsSummary?: string | null;
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
