# PAYE Tax Easy — User Guide

**Group 02 | SLIIT MSc IT | Software Engineering Practices (IT5030)**

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Getting Started — Landing Page](#2-getting-started--landing-page)
3. [Employer Portal](#3-employer-portal)
4. [Employee Portal](#4-employee-portal)
5. [IRD Dashboard](#5-ird-dashboard)
6. [Admin Portal](#6-admin-portal)
7. [Test Scenarios](#7-test-scenarios)
8. [Login Credentials](#8-login-credentials)

---

## 1. System Overview

PAYE Tax Easy is a centralized PAYE tax management platform for Sri Lanka. It solves the problem of inaccurate tax deductions when employees change jobs mid-financial-year by connecting employer payroll systems with the Inland Revenue Department (IRD).

**Financial Year:** 1 April 2025 – 31 March 2026  
**Tax Relief:** Rs. 1,800,000 per employee per year  
**Filing Deadline:** 30 November 2026

### System Architecture

```
Landing Page (http://localhost:5173)
    ├── Employer Portal  → Manage payroll, calculate PAYE, submit returns
    ├── Employee Portal  → View deduction history, download PDF
    ├── IRD Dashboard    → Compliance monitoring, audit logs
    └── Admin Portal     → User management
```

---

## 2. Getting Started — Landing Page

**URL:** http://localhost:5173

The landing page is the entry point for all users. It displays four portal cards:

| Card | Who uses it |
|---|---|
| 🏢 Employer Portal | HR/Payroll departments |
| 👤 Employee Portal | Individual employees |
| 🏛️ IRD Dashboard | IRD officers |
| 🔐 Admin Portal | System administrators |

Click any card to open that portal in a **new browser tab**.

---

## 3. Employer Portal

**URL:** http://localhost:5173 → Employer Portal  
**Login:** `employer@test.com` / `Test@1234`

### 3.1 Login

1. Click **Employer Portal** on the landing page
2. Enter your email and password
3. Click **Sign In to Employer Portal**

### 3.2 PAYE Deduction Dashboard

After login you will see the main dashboard with:

- **Period Selector** — dropdown to select any month from April 2025 to March 2026
- **Summary Cards** — Total employees, Annual Tax Liability, Cumulative Tax Paid, Remaining Tax
- **Employee Table** — all employees with their tax details for the selected period

#### Dashboard Columns

| Column | Description |
|---|---|
| Employee | Full name |
| TIN | Taxpayer Identification Number |
| Gross Salary | Monthly gross salary |
| Scenario | Employment status (Stable / Salary Increased / Salary Decreased / Mid-Year Joiner / Resigned) |
| Annual Tax Liability | Total PAYE tax for the full financial year |
| Cumulative Tax Paid | Total tax deducted up to the selected period |
| Monthly Deduction | PAYE deduction for the selected month |
| Remaining Tax | Tax still to be collected for the rest of the FY |
| Remaining Months | Months left in the financial year |
| Status | OK / Adjusted / Overpaid |

### 3.3 Fetch IRD & Tax Report

Click **📋 Fetch IRD & Report** on any employee row to:

1. Retrieve the employee's cumulative deduction history from the IRD
2. Open a detailed **Tax Report popup** showing:
   - Employee information
   - Annual tax liability
   - Tax paid to date (progress bar)
   - Prior employer deductions (if applicable)
   - **Important Notice** — adjusted monthly deduction vs standard next FY deduction
   - Full tax slab breakdown
   - Monthly deduction history

3. Click **⬇ Download PDF** to save the report as a PDF file

#### Important Notice in the Report

The notice explains:
- **Cumulative Tax Paid to Date** — how much has already been deducted this FY
- **Adjusted Monthly Deduction (This FY)** — the corrected amount to charge for remaining months
- **Standard Monthly Deduction (Next FY)** — what will be charged from April next year

### 3.4 Add Employee Salary

1. Click **+ Add Employee** button
2. Enter:
   - Employee TIN
   - Gross Monthly Salary (Rs.)
   - Employment Start Date
3. Click **Save & Calculate** — the system automatically calculates the PAYE deduction

### 3.5 Finalize Payroll Period

1. Select the period from the dropdown
2. Click **Finalize Period**
3. Confirm the action — the period is locked and no further edits are allowed

### 3.6 Submit to IRD

1. Review the deduction summary
2. Click **Submit to IRD**
3. A confirmation message shows the IRD Reference Number
4. A confirmation email is sent to the employer's registered address

---

## 4. Employee Portal

**URL:** http://localhost:5173 → Employee Portal  
**Login:** Use any employee email (see Section 8)

### 4.1 Login

1. Click **Employee Portal** on the landing page
2. Enter your email and password
3. Click **Sign In to Employee Portal**

### 4.2 View Tax Deduction History

After login you will see:

- **Financial Year selector** — choose 2025-26, 2024-25, or 2023-24
- **Employee information** — your name and TIN
- **Monthly deduction table** showing:
  - Employer name
  - Period (month/year)
  - Deduction amount for that month

- **Total PAYE Deducted** — cumulative total for the selected year

### 4.3 Download PDF

1. Select the financial year
2. Click **⬇ Download PDF**
3. A PDF is generated containing:
   - Your employee information
   - Complete monthly deduction history
   - Total PAYE deducted
   - **Important Notice** explaining the adjusted deduction and next FY standard amount

> The PDF can be used for personal records and tax return filing.

---

## 5. IRD Dashboard

**URL:** http://localhost:5173 → IRD Dashboard  
**Login:** `ird@test.com` / `Test@1234`

### 5.1 Login

1. Click **IRD Dashboard** on the landing page
2. Enter your IRD officer credentials
3. Click **Sign In to IRD Dashboard**

### 5.2 Compliance Report

The main page shows a compliance report for the selected financial year:

| Card | Description |
|---|---|
| Total Employers | All registered employers in the system |
| Submitted | Employers who have filed PAYE returns |
| Not Submitted | Employers who have not yet filed |
| Total PAYE Collected | Sum of all accepted PAYE submissions |

**To change the financial year:** Use the dropdown at the top.

### 5.3 Employer Search

1. Enter a registration number in the search box
2. Click **Search**
3. The employer's details are displayed below

### 5.4 Export Compliance Report

1. Click **Export CSV**
2. A CSV file is downloaded with the compliance summary

### 5.5 Audit Logs

1. Click **Audit Logs** button
2. View a paginated list of all system actions including:
   - Login events
   - Salary entries
   - IRD data retrievals
   - Payroll submissions
   - PDF exports
3. Filter by action type using the search box
4. Navigate pages using **← Prev** and **Next →**

---

## 6. Admin Portal

**URL:** http://localhost:5173 → Admin Portal  
**Login:** `admin@payetaxeasy.lk` / `Admin@1234`

### 6.1 Login

1. Click **Admin Portal** on the landing page
2. Enter admin credentials
3. Click **Sign In to Admin Portal**

> Only SystemAdmin accounts can access this portal.

### 6.2 User Management Dashboard

The dashboard shows:
- **Role filter cards** — click to filter by All / Employer / Employee / IRD_Officer / SystemAdmin
- **User table** with name, email, role badge, TIN, status, and creation date

### 6.3 Create New User

1. Click **+ Create New User**
2. Fill in:
   - Full Name
   - Email Address
   - Role (Employer / Employee / IRD_Officer / SystemAdmin)
   - TIN (optional)
   - Password (minimum 6 characters)
3. Click **Create User**

The new user can immediately log in to their respective portal.

### 6.4 Activate / Deactivate User

- Click **Deactivate** to prevent a user from logging in
- Click **Activate** to restore access
- Deactivated users appear greyed out in the table

### 6.5 Reset Password

1. Click **Reset Pwd** next to any user
2. Enter a new password (minimum 6 characters)
3. Click **Reset Password**

### 6.6 Delete User

1. Click **Delete** next to any user
2. Confirm the action
3. The user is permanently removed

---

## 7. Test Scenarios

The system includes 8 pre-loaded employees covering all PAYE tax scenarios:

### Scenario A — Stable Employment (Amal Perera)
- Monthly salary: Rs. 200,000
- Annual income: Rs. 2,400,000
- Taxable: Rs. 600,000 at 6% = Rs. 36,000 annual tax
- Monthly deduction: Rs. 3,000
- **Takeaway**: System works correctly for stable employment

### Scenario B — Mid-Year Promotion (Bhagya Silva)
- Apr–Sep 2025: Rs. 150,000/month
- Oct 2025–Mar 2026: Rs. 250,000/month (promoted)
- Actual annual income: Rs. 2,400,000
- **Without system**: Employer assumes Rs. 250,000 × 12 = Rs. 3,000,000 → overtaxed
- **With system**: Uses actual cumulative → correct tax on Rs. 2,400,000

### Scenario C — Job Change (Chaminda Fernando)
- Prior employer (XYZ Holdings): Apr–Jul 2025 at Rs. 300,000/month
- Current employer (ABC Company): Aug 2025–Mar 2026 at Rs. 400,000/month
- Prior tax paid (IRD): Rs. 74,000
- **Without system**: Rs. 50,000/month × 8 = Rs. 400,000 (overtaxed)
- **With system**: (Rs. 400,000 − Rs. 74,000) ÷ 8 = **Rs. 40,750/month**
- Next FY: Rs. 50,000/month (no adjustments)

### Scenario D — Layoff & Re-employment (Dilini Jayasinghe)
- Prior employer (Lanka Tech): Apr–Aug 2025 at Rs. 250,000/month
- Unemployed: Sep–Dec 2025
- Current employer (ABC Company): Jan–Mar 2026 at Rs. 200,000/month
- Prior tax paid (IRD): Rs. 40,000
- **With system**: Adjusted deduction considers prior payments

### Scenario E — Migration Attempt (Eranga Bandara)
- Prior employer (XYZ Holdings): Apr–Jun 2025 at Rs. 350,000/month
- Resigned for migration, migration failed
- Unemployed: Jul–Sep 2025
- Current employer (ABC Company): Oct 2025–Mar 2026 at Rs. 350,000/month
- Prior tax paid (IRD): Rs. 97,500
- **Without system**: Rs. 50,000 × 6 = Rs. 300,000 (overtaxed)
- **With system**: Adjusted using cumulative → correct deduction

### Scenario F — Below Tax Relief (Fathima Rizna)
- Monthly salary: Rs. 120,000
- Annual income: Rs. 1,440,000 (below Rs. 1,800,000 relief)
- Tax: Rs. 0
- Monthly deduction: Rs. 0

### Scenario G — High Earner 36% Slab (Gayan Wickrama)
- Monthly salary: Rs. 600,000
- Annual income: Rs. 7,200,000
- Annual tax: Rs. 1,464,000
- Monthly deduction: Rs. 122,000

### Scenario H — Resigned Mid-Year (Harsha Rathnayake)
- Monthly salary: Rs. 180,000
- Worked: Apr–Dec 2025 (9 months)
- Annual income: Rs. 1,620,000 (below relief)
- Tax: Rs. 0

---

## 8. Login Credentials

### Portal Accounts

| Role | Email | Password | Portal |
|---|---|---|---|
| Employer | `employer@test.com` | `Test@1234` | Employer Portal |
| Employee | (see employee accounts below) | `Test@1234` | Employee Portal |
| IRD Officer | `ird@test.com` | `Test@1234` | IRD Dashboard |
| System Admin | `admin@payetaxeasy.lk` | `Admin@1234` | Admin Portal |

### Employee Accounts

All employee accounts use password: `Test@1234`

| Employee | Email | TIN | Scenario | Current Employer |
|---|---|---|---|---|
| Amal Perera | `amal.perera@test.com` | 100000001 | Stable employment | ABC Company Ltd |
| Bhagya Silva | `bhagya.silva@test.com` | 100000002 | Mid-year promotion | ABC Company Ltd |
| Chaminda Fernando | `chaminda.fernando@test.com` | 100000003 | Job change (prior: XYZ Holdings) | ABC Company Ltd |
| Dilini Jayasinghe | `dilini.jayasinghe@test.com` | 100000004 | Layoff & re-employment (prior: Lanka Tech) | ABC Company Ltd |
| Eranga Bandara | `eranga.bandara@test.com` | 100000005 | Migration attempt (prior: XYZ Holdings) | ABC Company Ltd |
| Fathima Rizna | `fathima.rizna@test.com` | 100000006 | Below tax relief | ABC Company Ltd |
| Gayan Wickrama | `gayan.wickrama@test.com` | 100000007 | High earner — 36% slab | ABC Company Ltd |
| Harsha Rathnayake | `harsha.rathnayake@test.com` | 100000008 | Resigned mid-year | ABC Company Ltd |

### Employers in the System

| Employer | TIN | Role |
|---|---|---|
| ABC Company Ltd | 200000001 | Current employer (login: `employer@test.com`) |
| XYZ Holdings Pvt Ltd | 200000002 | Prior employer for Chaminda & Eranga |
| Lanka Tech Solutions Pvt Ltd | 200000003 | Prior employer for Dilini |

---

*PAYE Tax Easy | SLIIT MSc IT — Software Engineering Practices (IT5030) | Group 02 | 2026*
