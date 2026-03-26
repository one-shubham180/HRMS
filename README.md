# HRMS

Production-style Human Resource Management System built with ASP.NET Core 8 Web API and React + Vite.

## Stack

- Backend: ASP.NET Core 8, Clean Architecture, MediatR CQRS, EF Core, SQL Server, ASP.NET Identity, JWT + refresh tokens, FluentValidation, AutoMapper, Serilog
- Frontend: React 18, Vite, TypeScript, React Router v6, Axios, Zustand, Tailwind CSS
- Tests: xUnit

## Solution Structure

```text
api/
  HRMS.slnx
  src/
    HRMS.Domain/
    HRMS.Application/
    HRMS.Infrastructure/
    HRMS.Api/
  tests/
    HRMS.Application.Tests/
client/
  src/
    api/
    app/
    components/
    features/
    layouts/
    pages/
    types/
```

## Architecture

### Backend

- `HRMS.Domain`: core entities, enums, and domain rules such as attendance status and payroll calculation.
- `HRMS.Application`: DTOs, CQRS commands/queries, validators, mappings, and repository/service contracts.
- `HRMS.Infrastructure`: EF Core persistence, repository implementations, ASP.NET Identity, JWT generation, refresh token storage, local file storage, and seed data.
- `HRMS.Api`: controllers, authentication setup, Swagger, Serilog request logging, and global exception middleware.

### Frontend

- `src/api`: Axios client with JWT injection and refresh-token retry flow.
- `src/features/auth`: Zustand auth persistence.
- `src/layouts`: authenticated shell with role-aware navigation.
- `src/pages`: dashboard, employee management, attendance, leave, payroll, login/register.
- Motion: page-entry animations, feedback transitions, and animated panel interactions are included through shared CSS utilities.

## Implemented Modules

- Authentication: register, login, refresh token, role assignment.
- Employee Management: create/list/detail, deactivate, profile image upload.
- Department Management: create, update, delete, list.
- Attendance: check-in, check-out, attendance logs, late and half-day logic.
- Leave Management: apply leave, approve/reject, leave balance updates.
- Payroll: salary structure, monthly payroll generation, payslip history.
- Dashboard: admin/HR summary and employee self-service dashboard.

## Seeded Accounts

- Admin: `admin@hrms.local` / `Admin@123`
- HR: `hr@hrms.local` / `Hr@12345`
- Employee: `employee@hrms.local` / `Emp@12345`

## Local Setup

### 1. Backend

```powershell
cd d:\HRMS\api
$env:DOTNET_CLI_HOME='D:\HRMS\.dotnet'
dotnet restore .\HRMS.slnx
dotnet run --project .\src\HRMS.Api\HRMS.Api.csproj
```

Backend default URL from launch settings:

- `http://localhost:5108`
- Swagger: `http://localhost:5108/swagger`

### 2. Frontend

```powershell
cd d:\HRMS\client
Copy-Item .env.example .env
npm.cmd install
npm.cmd run dev
```

Frontend default URL:

- `http://localhost:5173`

### 3. Database

- Default connection string points to SQL Server host `KAJU`.
- Windows Authentication is enabled through `Trusted_Connection=True` / `Integrated Security=True`.
- At startup the app seeds roles, departments, sample users, salary structures, attendance, and sample leave data.
- For a production deployment, replace `EnsureCreated` fallback with EF Core migrations and a managed SQL Server instance.

## Key API Endpoints

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/assign-role`

Sample login response:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<refresh-token>",
  "expiresUtc": "2026-03-26T06:30:00Z",
  "userId": "6e3f0e39-6a3b-4e0f-91af-a4f225109b18",
  "email": "admin@hrms.local",
  "roles": ["Admin"],
  "employeeId": "89f1c31b-59f1-4d8f-89d7-fc9959de2c0a"
}
```

### Employees

- `GET /api/employees?pageNumber=1&pageSize=10&search=ethan&sortBy=name`
- `GET /api/employees/{employeeId}`
- `POST /api/employees`
- `PUT /api/employees/{employeeId}`
- `DELETE /api/employees/{employeeId}`
- `POST /api/employees/{employeeId}/profile-image`

### Attendance

- `POST /api/attendance/check-in`
- `POST /api/attendance/check-out`
- `GET /api/attendance/logs?pageNumber=1&pageSize=15`

### Leave

- `GET /api/leaves?pageNumber=1&pageSize=20`
- `POST /api/leaves`
- `POST /api/leaves/{leaveRequestId}/review`

### Payroll

- `GET /api/payroll?pageNumber=1&pageSize=20`
- `POST /api/payroll/salary-structures`
- `POST /api/payroll/generate`

### Dashboard

- `GET /api/dashboard/admin`
- `GET /api/dashboard/employee`

## Notes

- Entities are never exposed directly; controllers return DTOs only.
- Pagination is applied on list endpoints through `PagedResult<T>`.
- Validation runs through a MediatR pipeline behavior with FluentValidation.
- Profile images are stored under `wwwroot/uploads/profiles`.
- Role-based authorization is enforced in controllers and self-service workflows are constrained through current-user checks.

## Tests

```powershell
cd d:\HRMS\api
$env:DOTNET_CLI_HOME='D:\HRMS\.dotnet'
dotnet test .\tests\HRMS.Application.Tests\HRMS.Application.Tests.csproj
```
