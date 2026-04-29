# PAYE Tax Easy

A centralized digital platform for accurate PAYE (Pay-As-You-Earn) tax management in Sri Lanka, integrating employer payroll systems with the Inland Revenue Department (IRD).

**Group 02 | SLIIT MSc IT | Software Engineering Practices (IT5030)**

---

## Problem Solved

The current PAYE system has no mechanism for employers to access an employee's prior tax deduction history when they change jobs mid-financial-year. This causes overpayment or underpayment discovered only at annual tax return filing time. PAYE Tax Easy solves this by enabling employers to retrieve cumulative deduction records from the IRD and automatically adjust monthly deductions.

---

## Technologies Used

| Layer | Technology |
|---|---|
| Frontend | React 18 + TypeScript + Vite |
| Backend | .NET Core 8 Web API (C#) |
| Database | SQL Server LocalDB (dev) |
| ORM | Entity Framework Core 8 |
| Authentication | JWT Bearer (dev mode — local symmetric key) |
| PDF Generation | QuestPDF |
| Email | SendGrid (dev mode — logs to console) |
| Password Hashing | BCrypt.Net |
| Testing | xUnit + FsCheck (Property-Based Testing) |

---

## Prerequisites

Install the following before running the project:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 8.0 or higher | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Node.js | 20.0 or higher | https://nodejs.org/ |
| SQL Server LocalDB | Included with Visual Studio | https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb |
| EF Core CLI tools | Latest | Run: `dotnet tool install --global dotnet-ef` |

> **Check your versions:**
> ```powershell
> dotnet --version    # should show 8.x
> node --version      # should show 20.x
> dotnet ef           # should show Entity Framework Core tools
> ```

---

## Setup Instructions

### Step 1 — Clone the repository

```powershell
git clone <repository-url>
cd paye-tax-easy
```

### Step 2 — Install frontend dependencies

```powershell
cd frontend/employer-portal
npm install

cd ../employee-portal
npm install

cd ../ird-dashboard
npm install

cd ../admin-portal
npm install

cd ../..
```

### Step 3 — Create the database

```powershell
dotnet ef database update --project src/PayeTaxEasy.Infrastructure --startup-project src/PayeTaxEasy.Api
```

This creates the `PayeTaxEasyDev` database in SQL Server LocalDB with all 10 tables.

> If you see `dotnet ef not found`, run: `dotnet tool install --global dotnet-ef` first.

### Step 4 — Run the system

**Option A — One script (recommended):**

Right-click `start.ps1` in the project root → **Run with PowerShell**

This starts all services and opens the browser automatically.

**Option B — Manual (4 separate terminals):**

```powershell
# Terminal 1 — Backend API
dotnet run --project src/PayeTaxEasy.Api --urls "http://localhost:5050"

# Terminal 2 — Employer Portal
cd frontend/employer-portal && npm run dev

# Terminal 3 — Employee Portal
cd frontend/employee-portal && npm run dev

# Terminal 4 — IRD Dashboard
cd frontend/ird-dashboard && npm run dev
```

### Step 5 — Open in browser

| URL | Description |
|---|---|
| http://localhost:5173 | **Landing Page** — start here |
| http://localhost:5050/swagger | API documentation (Swagger UI) |

---

## Login Credentials

The system uses dev-mode authentication. Use these credentials:

| Role | Email | Password |
|---|---|---|
| Employer / HR | `employer@test.com` | `Test@1234` |
| Employee | `employee@test.com` | `Test@1234` |
| IRD Officer | `ird@test.com` | `Test@1234` |
| System Admin | `admin@payetaxeasy.lk` | `Admin@1234` |

> Credentials are also shown on each login page.

### Test Employee Accounts (8 scenarios)

| Employee | Email | Password | Scenario |
|---|---|---|---|
| Kamal Perera | `kamal.perera@test.com` | `Test@1234` | Below tax relief (0%) |
| Nimal Silva | `nimal.silva@test.com` | `Test@1234` | 6% slab — stable salary |
| Priya Jayasinghe | `priya.jayasinghe@test.com` | `Test@1234` | Recently joined mid-year |
| Amali Fernando | `amali.fernando@test.com` | `Test@1234` | Salary increase mid-year |
| Suresh Bandara | `suresh.bandara@test.com` | `Test@1234` | Salary decrease mid-year |
| Roshan Wickrama | `roshan.wickrama@test.com` | `Test@1234` | Changed employer (prior IRD deductions) |
| Dilani Rathnayake | `dilani.rathnayake@test.com` | `Test@1234` | Resigned mid-year |
| Chamara Dissanayake | `chamara.dissanayake@test.com` | `Test@1234` | High earner — 36% slab |

---

## Project Structure

```
paye-tax-easy/
├── src/
│   ├── PayeTaxEasy.Api/              # ASP.NET Core 8 Web API
│   │   ├── Controllers/              # REST API controllers
│   │   └── Program.cs                # App startup + dev auth endpoint
│   ├── PayeTaxEasy.Core/             # Domain logic (no dependencies)
│   │   ├── Calculator/               # PayeCalculator — pure PAYE tax engine
│   │   ├── Interfaces/               # Service interfaces
│   │   └── Models/                   # DTOs and request/response models
│   └── PayeTaxEasy.Infrastructure/   # Data access + external services
│       ├── Data/                     # EF Core DbContext
│       ├── Entities/                 # 10 database entity classes
│       ├── Migrations/               # EF Core database migrations
│       └── Services/                 # PayrollService, IrdService, etc.
├── tests/
│   └── PayeTaxEasy.Tests/
│       └── Calculator/               # 17 unit + property-based tests
├── frontend/
│   ├── employer-portal/              # React app — Employer/HR interface
│   ├── employee-portal/              # React app — Employee self-service
│   └── ird-dashboard/               # React app — IRD compliance dashboard
├── start.ps1                         # One-click start script
├── stop.ps1                          # Stop all services
└── README.md
```

---

## Running Tests

```powershell
dotnet test tests/PayeTaxEasy.Tests --verbosity normal
```

Expected output: **17 tests passed** — covering all PAYE tax slab calculations, monthly deduction logic, mid-year adjustment formula, employment gap handling, and dual-employer scenarios.

---

## User Guide

### Employer Workflow
1. Go to http://localhost:5173 → click **Employer Portal**
2. Login with `employer@test.com` / `Test@1234`
3. Select the payroll period
4. Click **+ Add Employee Salary** — enter employee TIN, gross salary, start date
5. Click **Fetch IRD** to retrieve the employee's prior deduction history
6. Review the adjusted monthly deduction (calculated automatically)
7. Click **Finalize Period** to lock the payroll
8. Click **Submit to IRD** to file the PAYE return

### Employee Workflow
1. Go to http://localhost:5173 → click **Employee Portal**
2. Login with `employee@test.com` / `Test@1234`
3. Select the financial year
4. View your complete PAYE deduction history across all employers
5. Click **⬇ Download PDF** to export your tax history

### IRD Officer Workflow
1. Go to http://localhost:5173 → click **IRD Dashboard**
2. Login with `ird@test.com` / `Test@1234`
3. View compliance report — total employers, submitted/not submitted, total PAYE collected
4. Search for a specific employer by registration number
5. Click **Export CSV** to download the report
6. Click **Audit Logs** to view the full audit trail

### Admin Workflow
1. Go to http://localhost:5173 → click **Admin Portal**
2. Login with `admin@payetaxeasy.lk` / `Admin@1234`
3. View all users across all roles
4. Click **+ Create New User** to add Employer, Employee, or IRD Officer accounts
5. Activate / Deactivate users or reset passwords as needed

---

## PAYE Tax Calculation Rules

Based on the Inland Revenue (Amendment) Act, No. 02 of 2025:

| Annual Income | Tax Rate |
|---|---|
| Up to Rs. 1,800,000 | 0% (Tax Relief) |
| Rs. 1,800,001 – Rs. 2,800,000 | 6% |
| Rs. 2,800,001 – Rs. 3,300,000 | 18% |
| Rs. 3,300,001 – Rs. 3,800,000 | 24% |
| Rs. 3,800,001 – Rs. 4,300,000 | 30% |
| Above Rs. 4,300,000 | 36% |

**Monthly deduction** = Annual PAYE tax ÷ 12

**Mid-year adjustment formula:**
```
Adjusted Monthly = (Annual Tax on Projected Income − Prior Deductions) ÷ Remaining Months
```

---

## Viewing the Database

The database runs on SQL Server LocalDB. To view tables:

1. Open **SQL Server Management Studio (SSMS)**
2. Connect with:
   - Server name: `(localdb)\mssqllocaldb`
   - Authentication: **Windows Authentication**
3. Expand: **Databases → PayeTaxEasyDev → Tables**

---

## Stopping the System

```powershell
.\stop.ps1
```
Or close all 4 terminal windows.

---

## Group Members

| Student ID | Name | Role |
|---|---|---|
| MS26906294 | Perera D.S.M. | Scrum Master |
| MS26906508 | Fernando G.J.S. | Backend Developer |
| MS26904214 | Fonseka E.A.R. | Product Owner |
| MS26900322 | Gallage G.H.G.D.W. | Tester |
| MS26907802 | Jayasinghe J.A.D.V.D. | Frontend Developer |
