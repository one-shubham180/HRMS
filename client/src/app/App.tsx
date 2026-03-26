import { createBrowserRouter, Navigate } from "react-router-dom";
import { RequireAuth } from "../components/RequireAuth";
import { AppLayout } from "../layouts/AppLayout";
import { AttendancePage } from "../pages/AttendancePage";
import { DashboardPage } from "../pages/DashboardPage";
import { EmployeeDetailPage } from "../pages/EmployeeDetailPage";
import { EmployeesPage } from "../pages/EmployeesPage";
import { LeavePage } from "../pages/LeavePage";
import { LoginPage } from "../pages/LoginPage";
import { NotFoundPage } from "../pages/NotFoundPage";
import { PayrollPage } from "../pages/PayrollPage";
import { RegisterPage } from "../pages/RegisterPage";

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
          { path: "/employees", element: <EmployeesPage /> },
          { path: "/employees/:employeeId", element: <EmployeeDetailPage /> },
          { path: "/attendance", element: <AttendancePage /> },
          { path: "/leaves", element: <LeavePage /> },
          { path: "/payroll", element: <PayrollPage /> },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
