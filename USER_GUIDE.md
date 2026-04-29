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

### Scenario 1 — Below Tax Relief (Kamal Perera)
- Monthly salary: Rs. 150,000
- Annual income: Rs. 1,800,000
- Tax: Rs. 0 (fully within tax relief)
- Monthly deduction: Rs. 0

### Scenario 2 — 6% Tax Slab (Nimal Silva)
- Monthly salary: Rs. 250,000
- Annual income: Rs. 3,000,000
- Annual tax: Rs. 96,000
- Monthly deduction: Rs. 8,000

### Scenario 3 — Recently Joined Mid-Year (Priya Jayasinghe)
- Joined: October 2025
- Monthly salary: Rs. 320,000
- Adjusted deduction calculated for remaining 6 months only

### Scenario 4 — Salary Increase (Amali Fernando)
- Apr–Jul 2025: Rs. 200,000/month
- Aug 2025–Mar 2026: Rs. 350,000/month
- System recalculates deduction from August using actual cumulative income

### Scenario 5 — Salary Decrease (Suresh Bandara)
- Apr–Aug 2025: Rs. 400,000/month
- Sep 2025–Mar 2026: Rs. 280,000/month
- System recalculates deduction from September

### Scenario 6 — Changed Employer (Roshan Wickrama)
- Prior employer: Apr–Oct 2025 at Rs. 500,000/month
- Joined this company: November 2025
- IRD data loaded showing prior deductions
- Adjusted monthly deduction calculated for remaining months

### Scenario 7 — Resigned Mid-Year (Dilani Rathnayake)
- Monthly salary: Rs. 180,000
- Resigned: December 2025
- Deductions recorded only for active months (Apr–Dec)

### Scenario 8 — High Earner 36% Slab (Chamara Dissanayake)
- Monthly salary: Rs. 600,000
- Annual income: Rs. 7,200,000
- Annual tax: Rs. 1,464,000
- Monthly deduction: Rs. 122,000

---

## 8. Login Credentials

### Portal Accounts

| Role | Email | Password | Portal |
|---|---|---|---|
| Employer | `employer@test.com` | `Test@1234` | Employer Portal |
| Employee | `employee@test.com` | `Test@1234` | Employee Portal |
| IRD Officer | `ird@test.com` | `Test@1234` | IRD Dashboard |
| System Admin | `admin@payetaxeasy.lk` | `Admin@1234` | Admin Portal |

### Test Employee Accounts

| Employee | Email | Password |
|---|---|---|
| Kamal Perera | `kamal.perera@test.com` | `Test@1234` |
| Nimal Silva | `nimal.silva@test.com` | `Test@1234` |
| Priya Jayasinghe | `priya.jayasinghe@test.com` | `Test@1234` |
| Amali Fernando | `amali.fernando@test.com` | `Test@1234` |
| Suresh Bandara | `suresh.bandara@test.com` | `Test@1234` |
| Roshan Wickrama | `roshan.wickrama@test.com` | `Test@1234` |
| Dilani Rathnayake | `dilani.rathnayake@test.com` | `Test@1234` |
| Chamara Dissanayake | `chamara.dissanayake@test.com` | `Test@1234` |

---

*PAYE Tax Easy | SLIIT MSc IT — Software Engineering Practices (IT5030) | Group 02 | 2026*
