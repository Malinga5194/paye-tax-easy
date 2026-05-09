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
| VS Code (optional) | Latest | https://code.visualstudio.com/ |
| Git | Latest | https://git-scm.com/ |

> **Verify your installations:**
> ```powershell
> dotnet --version    # should show 8.x
> node --version      # should show 20.x
> ```

---

## Setup Instructions

### Step 1 — Clone the repository

```powershell
git clone https://github.com/Malinga5194/paye-tax-easy.git
cd paye-tax-easy
```

---

### Step 2 — Open in VS Code (recommended)

```powershell
code .
```

VS Code will open the project. When prompted **"Install recommended extensions?"** click **Install All**.
This installs C# Dev Kit, ESLint, and Prettier automatically.

---

### Step 3 — Install frontend dependencies

Open the VS Code terminal (`Ctrl+` ` `) and run:

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

---

### Step 4 — Run the system

**Option A — One script (recommended):**

In the VS Code terminal, run:

```powershell
.\start.ps1
```

Or in File Explorer, right-click `start.ps1` → **Run with PowerShell**

> ✅ The script automatically:
> - Starts the backend API
> - **Creates the database and all 10 tables on first run** (no manual steps needed)
> - **Inserts all test data** — 8 employees covering all tax scenarios, employer, admin user
> - Starts all 4 frontend portals
> - Opens the landing page in your browser

**Option B — Manual (5 separate terminals in VS Code):**

```powershell
# Terminal 1 — Backend API (creates DB + seeds data automatically on first run)
dotnet run --project src/PayeTaxEasy.Api --urls "http://localhost:5050" --environment Development

# Terminal 2 — Employer Portal
cd frontend/employer-portal
npm run dev

# Terminal 3 — Employee Portal
cd frontend/employee-portal
npm run dev

# Terminal 4 — IRD Dashboard
cd frontend/ird-dashboard
npm run dev

# Terminal 5 — Admin Portal
cd frontend/admin-portal
npm run dev
```

> **Note:** The database is created and test data is inserted automatically when the API starts for the first time. No manual `dotnet ef` commands are needed.

---

### Step 5 — Open in browser

Open your browser and go to:

| URL | Description |
|---|---|
| **http://localhost:5173** | **Landing Page — start here** |
| http://localhost:5050/swagger | API documentation (Swagger UI) |

---

### If PowerShell blocks the script

Run this once in PowerShell, then try `.\start.ps1` again:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

## Login Credentials

| Role | Email | Password |
|---|---|---|
| Employer / HR | `employer@test.com` | `Test@1234` |
| Employee | `employee@test.com` | `Test@1234` |
| IRD Officer | `ird@test.com` | `Test@1234` |
| System Admin | `admin@payetaxeasy.lk` | `Admin@1234` |

> Credentials are also shown on each login page.

### Test Employee Accounts (8 tax scenarios)

| Employee | Email | Password | Scenario |
|---|---|---|---|
| Amal Perera | `amal.perera@test.com` | `Test@1234` | Stable employment — Rs. 200,000/month |
| Bhagya Silva | `bhagya.silva@test.com` | `Test@1234` | Mid-year promotion — Rs. 150k → Rs. 250k |
| Chaminda Fernando | `chaminda.fernando@test.com` | `Test@1234` | Job change (prior employer) — Rs. 400,000/month |
| Dilini Jayasinghe | `dilini.jayasinghe@test.com` | `Test@1234` | Layoff & re-employment — Rs. 200,000/month |
| Eranga Bandara | `eranga.bandara@test.com` | `Test@1234` | Mid-year joiner Oct 2025 — Rs. 350,000/month |
| Fathima Rizna | `fathima.rizna@test.com` | `Test@1234` | Below tax relief (0%) — Rs. 120,000/month |
| Gayan Wickrama | `gayan.wickrama@test.com` | `Test@1234` | High earner 36% slab — Rs. 600,000/month |
| Harsha Rathnayake | `harsha.rathnayake@test.com` | `Test@1234` | Resigned mid-year |

---

## Project Structure

```
paye-tax-easy/
├── src/
│   ├── PayeTaxEasy.Api/              # ASP.NET Core 8 Web API
│   │   ├── Controllers/              # REST API controllers
│   │   ├── SeedData.cs               # Test data seeding
│   │   └── Program.cs                # App startup, auth, auto-migration
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
│   ├── employer-portal/              # React app (port 5173) — Employer/HR
│   ├── employee-portal/              # React app (port 5174) — Employee
│   ├── ird-dashboard/                # React app (port 5175) — IRD Officer
│   └── admin-portal/                 # React app (port 5176) — System Admin
├── .vscode/
│   ├── launch.json                   # VS Code debug configuration
│   ├── tasks.json                    # VS Code task definitions
│   └── extensions.json               # Recommended extensions
├── start.ps1                         # One-click start script
├── stop.ps1                          # Stop all services
└── README.md
```

---

## Running Tests

```powershell
dotnet test tests/PayeTaxEasy.Tests --verbosity normal
```

Expected: **17 tests passed** — covering all PAYE tax slab calculations, monthly deduction logic, mid-year adjustment formula, employment gap handling, and dual-employer scenarios.

---

## User Guide

### Employer Workflow
1. Go to **http://localhost:5173** → click **Employer Portal**
2. Login with `employer@test.com` / `Test@1234`
3. Select a payroll period from the dropdown (April 2025 – March 2026)
4. View the deduction summary table showing all 8 test employees
5. Click **📋 Fetch IRD & Report** on any employee to:
   - Retrieve their cumulative IRD deduction history
   - View a detailed tax report popup with slab breakdown
   - See the adjusted monthly deduction and important notice
   - Download the report as a PDF
6. Click **+ Add Employee Salary** to add a new employee
7. Click **Finalize Period** to lock the payroll period
8. Click **Submit to IRD** to file the PAYE return electronically

### Employee Workflow
1. Go to **http://localhost:5173** → click **Employee Portal**
2. Login with any employee email (e.g. `amali.fernando@test.com`) / `Test@1234`
3. Select the financial year **2025-26**
4. View your complete PAYE deduction history with monthly breakdown
5. Click **⬇ Download PDF** to export your tax history with the adjustment notice

### IRD Officer Workflow
1. Go to **http://localhost:5173** → click **IRD Dashboard**
2. Login with `ird@test.com` / `Test@1234`
3. View compliance report — total employers, submitted/not submitted, total PAYE collected
4. Search for a specific employer by registration number
5. Click **Export CSV** to download the compliance report
6. Click **Audit Logs** to view the full audit trail of all system actions

### Admin Workflow
1. Go to **http://localhost:5173** → click **Admin Portal**
2. Login with `admin@payetaxeasy.lk` / `Admin@1234`
3. View all users across all roles with status indicators
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

The database is created automatically on first run. To view tables:

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
Or close all terminal windows.

---

## Group Members

| Student ID | Name | Role |
|---|---|---|
| MS26906294 | Perera D.S.M. | Scrum Master, Backend Developer, Tester |
| MS26904214 | Fonseka E.A.R. | Product Owner, Tester |
| MS26907802 | Jayasinghe J.A.D.V.D. | Frontend Developer, Tester |
| MS26906508 | Fernando G.J.S. | Discontinued the program |
| MS26900322 | Gallage G.H.G.D.W. | Discontinued the program |
