import { createBrowserRouter, Navigate } from "react-router-dom";
import { RequireAuth } from "../components/RequireAuth";
import { AppLayout } from "../layouts/AppLayout";
import { AttendancePage } from "../pages/AttendancePage";
import { DashboardPage } from "../pages/DashboardPage";
import { DepartmentsPage } from "../pages/DepartmentsPage";
import { DocumentVaultPage } from "../pages/DocumentVaultPage";
import { EmployeeDetailPage } from "../pages/EmployeeDetailPage";
import { EmployeesPage } from "../pages/EmployeesPage";
import { LeavePage } from "../pages/LeavePage";
import { LoginPage } from "../pages/LoginPage";
import { NotFoundPage } from "../pages/NotFoundPage";
import { NotificationsPage } from "../pages/NotificationsPage";
import { PayrollPage } from "../pages/PayrollPage";
import { RegisterPage } from "../pages/RegisterPage";
import { TalentPage } from "../pages/TalentPage";
import { WorkforcePage } from "../pages/WorkforcePage";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  { path: "/register", element: <RegisterPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: "/", element: <Navigate to="/dashboard" replace /> },
          { path: "/dashboard", element: <DashboardPage /> },
          { path: "/departments", element: <DepartmentsPage /> },
          { path: "/employees", element: <EmployeesPage /> },
          { path: "/employees/:employeeId", element: <EmployeeDetailPage /> },
          { path: "/attendance", element: <AttendancePage /> },
          { path: "/leaves", element: <LeavePage /> },
          { path: "/payroll", element: <PayrollPage /> },
          { path: "/workforce", element: <WorkforcePage /> },
          { path: "/notifications", element: <NotificationsPage /> },
          { path: "/documents", element: <DocumentVaultPage /> },
          { path: "/talent", element: <TalentPage /> },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
