# Implementation Plan: PAYE Tax Easy

## Overview

Incremental implementation of the PAYE Tax Easy platform across three sprints. Each task builds on the previous, ending with a fully wired, deployable system. The stack is .NET Core 8 Web API (C#) + React (Vite) + SQL Server + Azure services.

## Tasks

---

## Sprint 1 — Foundation

- [x] 1. Set up solution structure and project scaffolding
  - [x] 1.1 Create .NET Core 8 solution with projects: `PayeTaxEasy.Api`, `PayeTaxEasy.Core`, `PayeTaxEasy.Infrastructure`, `PayeTaxEasy.Tests`
    - Use `dotnet new sln` and `dotnet new webapi / classlib / xunit`
    - Add project references: Api → Core + Infrastructure; Tests → Core + Infrastructure
    - _Requirements: 16.1_
  - [x] 1.2 Create React (Vite) frontend workspace with three apps: `employer-portal`, `employee-portal`, `ird-dashboard`
    - Use `npm create vite@latest` for each app inside a `frontend/` folder
    - Install shared dependencies: `react-router-dom`, `axios`, `@tanstack/react-query`
    - _Requirements: 10.1, 12.1_
  - [x] 1.3 Initialise Git repository, add `.gitignore` (dotnet + node), and write README skeleton
    - README must include: project name, tech stack table, setup prerequisites, placeholder sections for setup instructions
    - _Requirements: 17 (documentation)_
  - [x] 1.4 Add NuGet packages to solution: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `FsCheck.Xunit`, `xunit`, `Polly`, `SendGrid`, `Twilio`, `Azure.Security.KeyVault.Secrets`, `QuestPDF`
    - _Requirements: 4.1, 5.5, 9.1, 11.1_

- [ ] 2. Database schema and EF Core migrations
  - [x] 2.1 Define all 9 EF Core entity classes in `PayeTaxEasy.Infrastructure/Entities/`: `Employer`, `Employee`, `EmployeePayroll`, `MonthlyDeduction`, `PayrollSubmission`, `SubmissionLine`, `AuditLog`, `NotificationLog`, `IrdCumulativeCache`
    - Match column types and constraints exactly to the ERD in design.md
    - Mark `AuditLog.Id` as `ValueGeneratedOnAdd` with no cascade deletes
    - _Requirements: 14.1, 13.1_
  - [x] 2.2 Configure `DbContext` (`PayeTaxEasyDbContext`) with Fluent API: unique indexes on TIN/NIC, append-only constraint comment on `AuditLog`, `IrdCumulativeCache` insert-only note
    - _Requirements: 13.5, 14.4_
  - [ ] 2.3 Create and apply initial EF Core migration (`InitialCreate`) targeting SQL Server LocalDB for local dev
    - Verify all 9 tables are created with correct columns and constraints
    - _Requirements: 14.1_
  - [ ] 2.4 Configure Always Encrypted column encryption for PII columns: `Employee.TIN`, `Employee.NICNumber`, `EmployeePayroll.GrossMonthlySalary`, `IrdCumulativeCache.CumulativeIncome`, `IrdCumulativeCache.CumulativeDeduction`
    - Store Column Master Key reference in `appsettings.json`; actual key in Azure Key Vault
    - _Requirements: 14.1, 14.6_
  - [x] 2.5 Add database-level trigger (via EF migration raw SQL) on `AuditLog` that rejects `UPDATE`/`DELETE` and inserts into `SecurityAlertLog`
    - _Requirements: 13.4, 13.5_

- [ ] 3. Auth service — Azure AD B2C + JWT middleware
  - [ ] 3.1 Register Azure AD B2C tenant, create user flows for sign-up/sign-in, and configure app registrations for API and each SPA client
    - Define custom claims: `role` (Employer / Employee / IRD_Officer / SystemAdmin), `organizationId`
    - _Requirements: 1.1, 1.6_
  - [ ] 3.2 Implement `AuthController` in `PayeTaxEasy.Api` with endpoints: `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/register`, `GET /auth/me`
    - Validate JWT (RS256) via `AddAuthentication().AddJwtBearer()`; read JWKS from B2C metadata endpoint
    - _Requirements: 1.1, 1.6_
  - [ ] 3.3 Implement account lockout middleware: track failed attempts in `FailedLoginAttempts` in-memory cache (or DB table); lock after 5 failures in 15 minutes; send lockout email via `NotificationService`
    - _Requirements: 1.3_
  - [ ] 3.4 Implement session idle timeout: sliding expiry of 30 minutes enforced via refresh token validation; return `AUTH_003` on expired session
    - _Requirements: 1.5_
  - [ ] 3.5 Add `[Authorize(Roles = "...")]` policy attributes to all controllers; create `AuthorizationPolicies` static class mapping each role to allowed endpoints
    - _Requirements: 1.4, 14.4_
  - [ ]* 3.6 Write unit tests for Auth_Service: login success, invalid credentials (AUTH_001), lockout after 5 attempts (AUTH_002), session expiry (AUTH_003), role enforcement (AUTH_004)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 4. PAYE Calculator engine
  - [ ] 4.1 Implement `PayeCalculator` static class in `PayeTaxEasy.Core/Calculator/`: methods `CalculateAnnualTax(decimal annualIncome)`, `CalculateMonthlyDeduction(decimal annualIncome)`, `CalculateAdjustedDeduction(decimal projectedAnnualIncome, decimal cumulativeDeduction, int remainingMonths)`
    - Implement exact slab logic from design.md; `CalculateMonthlyDeduction` rounds to nearest rupee
    - `CalculateAdjustedDeduction` returns `Max(0, Round((annualTax - cumDed) / remainingMonths, 0))`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 6.2_
  - [ ] 4.2 Implement `CalculateWithGap(decimal projectedAnnualIncome, decimal cumulativeDeduction, int remainingMonths, int zeroIncomeMonths)` — excludes zero-income months from denominator
    - _Requirements: 15.2_
  - [ ] 4.3 Implement `CalculateDualEmployerShare(decimal employerGross, decimal totalGross, decimal adjustedMonthlyTotal)` — proportional distribution
    - _Requirements: 15.6_
  - [ ]* 4.4 Write property test P1 (Slab Calculation Correctness) using FsCheck + xUnit
    - **Property 1: Slab Calculation Correctness**
    - Generate random `decimal` incomes in `[0, 10_000_000]`; verify against reference formula
    - Tag: `// Feature: paye-tax-easy, Property 1: Slab Calculation Correctness`
    - **Validates: Requirements 4.1, 4.2, 4.3**
  - [ ]* 4.5 Write property test P2 (Monthly Deduction Round-Trip) using FsCheck
    - **Property 2: Monthly Deduction Round-Trip**
    - Sum of 12 monthly deductions must equal annual tax ± 12 rupees
    - Tag: `// Feature: paye-tax-easy, Property 2: Monthly Deduction Round-Trip`
    - **Validates: Requirements 4.4, 4.6**
  - [ ]* 4.6 Write property test P3 (Adjustment Formula Correctness) using FsCheck
    - **Property 3: Adjustment Formula Correctness**
    - Generate `(income, cumDed, months)` triples; verify adjusted deduction matches formula
    - Tag: `// Feature: paye-tax-easy, Property 3: Adjustment Formula Correctness`
    - **Validates: Requirements 4.5, 6.1, 6.2**
  - [ ]* 4.7 Write property test P4 (Non-Negative Deduction Invariant) using FsCheck
    - **Property 4: Non-Negative Deduction Invariant**
    - Generate inputs where `cumDed >= annualTax`; assert result is always Rs. 0
    - Tag: `// Feature: paye-tax-easy, Property 4: Non-Negative Deduction Invariant`
    - **Validates: Requirements 6.3, 15.5**
  - [ ]* 4.8 Write property test P5 (Employment Gap Month Exclusion) using FsCheck
    - **Property 5: Employment Gap Month Exclusion**
    - Generate `(income, cumDed, totalMonths, zeroMonths)`; verify active months used as denominator
    - Tag: `// Feature: paye-tax-easy, Property 5: Employment Gap Month Exclusion`
    - **Validates: Requirements 15.2**
  - [ ]* 4.9 Write property test P6 (Dual-Employer Proportional Distribution) using FsCheck
    - **Property 6: Dual-Employer Proportional Distribution**
    - Generate `(s1, s2, cumDed, months)`; verify `deduction(s1) + deduction(s2) = deduction(s1+s2)`
    - Tag: `// Feature: paye-tax-easy, Property 6: Dual-Employer Proportional Distribution`
    - **Validates: Requirements 15.6**

- [ ] 5. Checkpoint — Calculator tests pass
  - Ensure all PBT and unit tests pass. Run `dotnet test`. Ask the user if any property fails or edge case is unclear.

- [ ] 6. Salary data entry API + basic tax calculation endpoint
  - [x] 6.1 Implement `PayrollController` with `POST /payroll/employees/{tin}/salary`: validate TIN exists, validate `grossMonthlySalary > 0`, store `EmployeePayroll` record, call `PayeCalculator.CalculateMonthlyDeduction`, store `MonthlyDeduction` record, return response schema from design.md
    - Return `PAYROLL_002` if TIN not found; `PAYROLL_001` if salary invalid
    - _Requirements: 3.1, 3.2, 3.4, 4.1, 4.4_
  - [x] 6.2 Implement `PUT /payroll/employees/{tin}/salary/{id}` for salary corrections: record `EffectiveDate`, retain prior record, trigger recalculation, store new `MonthlyDeduction` with trigger `SalaryAdjustment`
    - _Requirements: 3.3, 6.4, 6.5_
  - [x] 6.3 Implement `GET /payroll/employees/{tin}/salary` to return salary history for an employee under the authenticated employer
    - _Requirements: 3.5_
  - [x] 6.4 Add `AuditService.Record(...)` calls in `PayrollController` for every write operation
    - _Requirements: 13.1_
  - [ ]* 6.5 Write unit tests for `PayrollController`: valid submission, invalid TIN (PAYROLL_002), invalid salary (PAYROLL_001), salary correction retains history
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [ ] 7. Deduction summary API
  - [x] 7.1 Implement `GET /payroll/summary/{period}` returning `DeductionSummary[]` for all employees under the authenticated employer for the given period
    - Include: `employeeTIN`, `grossSalary`, `monthlyDeduction`, `yearToDateCumulativeDeduction`, `hasPriorEmployerData` flag
    - Use indexed query; target < 3 seconds for 500 records
    - _Requirements: 7.1, 7.2, 7.4_
  - [x] 7.2 Implement `POST /payroll/summary/{period}/finalize` to lock the payroll period: set a `IsLocked` flag on all `MonthlyDeduction` records for that period; reject further edits with `PAYROLL_004`
    - _Requirements: 7.5_
  - [x] 7.3 Implement correction workflow: `PUT /payroll/employees/{tin}/salary/{id}` must check `IsLocked`; if locked, return `PAYROLL_004` unless request includes `correctionAuthorization` header (SystemAdmin role)
    - _Requirements: 7.3_
  - [ ]* 7.4 Write unit tests for summary endpoint: correct aggregation, locking behaviour, correction rejection on locked period
    - _Requirements: 7.1, 7.2, 7.3, 7.5_

---

## Sprint 2 — Core Business Logic

- [ ] 8. IRD Integration Service
  - [x] 8.1 Implement `IrdIntegrationService` in `PayeTaxEasy.Infrastructure/Services/`: `GetCumulativeData(string tin, string financialYear)` — call IRD OAuth 2.0 token endpoint (client credentials), then call IRD data API; read credentials from Azure Key Vault via `SecretClient`
    - _Requirements: 5.1, 5.5, 5.6_
  - [ ] 8.2 Add Polly retry policy: 3 attempts with exponential back-off (1s, 2s, 4s); add circuit breaker (open for 60s after 3 consecutive failures); return `IRD_001` with `Retry-After` header when circuit is open
    - _Requirements: 5.3, 17.4_
  - [x] 8.3 Implement `IrdController` with `GET /ird/cumulative/{tin}/{financialYear}`: call `IrdIntegrationService`, store result in `IrdCumulativeCache` (insert-only), write audit log entry, return response
    - Return `IRD_002` (empty result) if IRD returns no records; set `CumulativeDeduction = 0` for calculator
    - _Requirements: 5.1, 5.2, 5.4, 5.7_
  - [ ] 8.4 Implement `POST /ird/submit/{submissionId}`: forward finalized `PayrollSubmission` to IRD API; handle `IRD_003` on IRD validation failure
    - _Requirements: 8.2, 8.3_
  - [ ]* 8.5 Write unit tests for `IrdIntegrationService`: successful retrieval, API unavailable (mock HttpClient), no-records response, circuit breaker opens after 3 failures
    - _Requirements: 5.1, 5.3, 5.4_

- [ ] 9. Cumulative tax retrieval + adjusted deduction calculation
  - [ ] 9.1 Implement `CalculateAdjustedDeductionForEmployee(string tin, string financialYear, decimal currentMonthlyGross)` in `PayrollService`: fetch from `IrdCumulativeCache` (or call IRD if not cached), compute `RemainingMonths`, call `PayeCalculator.CalculateAdjustedDeduction`, store result with trigger `IRDDataRetrieved`
    - _Requirements: 5.2, 6.1, 6.2_
  - [ ] 9.2 Implement overpayment flag: if `AdjustedMonthly == 0` due to overpayment, set `IsOverpaid = true` on `MonthlyDeduction` record and include flag in summary response
    - _Requirements: 6.3, 15.5_
  - [ ]* 9.3 Write unit tests for adjusted deduction: new employee (no prior data), mid-year joiner with prior deductions, overpayment scenario (deduction = 0, flag set)
    - _Requirements: 6.1, 6.2, 6.3_

- [ ] 10. Monthly adjustment engine — edge cases
  - [ ] 10.1 Implement employment gap handling in `PayrollService.ComputeActiveRemainingMonths(...)`: accept list of zero-income months, subtract from remaining months, pass `activeRemainingMonths` to calculator
    - _Requirements: 15.2_
  - [ ] 10.2 Implement dual-employer aggregation: `GET /payroll/employees/{tin}/dual-employer-split` — accept `employerGross` and `totalGross`, return proportional deduction for this employer
    - _Requirements: 15.6_
  - [ ] 10.3 Implement mid-year salary adjustment recalculation: when `PUT /payroll/employees/{tin}/salary/{id}` is called, recompute all future `MonthlyDeduction` records from `EffectiveDate` using updated projected income
    - _Requirements: 6.4, 15.4_
  - [ ] 10.4 Handle new financial year start (April 1): reset `CumulativeDeduction` to 0 for all employees; treat as fresh year
    - _Requirements: 15.3_
  - [ ]* 10.5 Write unit tests for edge cases: employment gap, dual employer split, mid-year adjustment, new FY start
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.6_

- [ ] 11. Payroll submission + electronic filing API
  - [x] 11.1 Implement `POST /payroll/submissions`: validate all mandatory fields (TINs, salaries, deductions); call `IrdIntegrationService.SubmitPayroll`; store `PayrollSubmission` + `SubmissionLine` records; return submission reference number
    - Return `PAYROLL_001` / `PAYROLL_002` on validation failure; `IRD_003` on IRD rejection
    - _Requirements: 8.1, 8.2, 8.3_
  - [x] 11.2 Implement filing deadline enforcement: check if submission date > 30 November for the financial year; return `PAYROLL_003` if deadline passed; display countdown in response metadata
    - _Requirements: 8.4_
  - [x] 11.3 Implement `POST /payroll/submissions/bulk`: accept array of submission lines for multiple employees in one call; process each line, aggregate `TotalPAYEAmount`, return `BulkSubmissionResult`
    - _Requirements: 8.6_
  - [x] 11.4 Write audit log entry on successful IRD acceptance: include `IRDReferenceNumber`, employer identity, timestamp, `TotalPAYEAmount`
    - _Requirements: 8.5_
  - [ ]* 11.5 Write unit tests for submission: valid submission accepted, missing TIN rejected, deadline enforcement, bulk submission aggregation
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.6_

- [ ] 12. Notification service
  - [x] 12.1 Implement `NotificationService` in `PayeTaxEasy.Infrastructure/Services/`: `SendEmail(string recipientId, NotificationType type, object payload)` via SendGrid SDK; `SendSms(string recipientId, string message)` via Twilio SDK
    - _Requirements: 9.1, 9.2, 9.5_
  - [ ] 12.2 Integrate Azure Service Bus: publish `PayrollSubmissionAccepted`, `PayrollSubmissionFailed`, `FilingDeadlineReminder`, `MaintenanceWindow` events; `NotificationService` subscribes and processes asynchronously
    - _Requirements: 9.1, 9.2, 9.3_
  - [ ] 12.3 Implement deadline reminder job: background `IHostedService` that runs daily, checks employers with no submission for current FY, sends reminder 7 days before 30 November
    - _Requirements: 9.3_
  - [x] 12.4 Store `NotificationLog` record for every sent notification: recipient, type, channel, status, timestamp, referenceId
    - _Requirements: 9.4_
  - [ ] 12.5 Implement SMS opt-in check: only send SMS if `Employer.SMSNotificationsEnabled == true`
    - _Requirements: 9.5_
  - [ ]* 12.6 Write unit tests for `NotificationService`: email sent on submission accepted, SMS sent when enabled, SMS skipped when disabled, deadline reminder triggered at 7 days, notification log written
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 13. Checkpoint — Sprint 2 integration
  - Wire `PayrollController` → `IrdIntegrationService` → `PayeCalculator` → `NotificationService` end-to-end. Run `dotnet test`. Ensure all unit and property tests pass. Ask the user if any integration point is unclear.

---

## Sprint 3 — Portals + QA

- [ ] 14. Employer portal (React)
  - [x] 14.1 Implement login page in `employer-portal`: call `POST /auth/login`, store JWT in memory (not localStorage), redirect to dashboard on success; display `AUTH_001` / `AUTH_002` error messages
    - _Requirements: 1.1, 1.2, 1.3_
  - [x] 14.2 Implement salary entry form: fields for employee TIN, gross monthly salary, employment start date; display current financial year period; call `POST /payroll/employees/{tin}/salary`; show inline validation errors per design error format
    - _Requirements: 3.1, 3.2, 3.5, 17.3_
  - [x] 14.3 Implement deduction summary page: call `GET /payroll/summary/{period}`; render table with employee TIN, gross salary, monthly deduction, YTD cumulative, prior-employer flag; add "Finalize" button calling `POST /payroll/summary/{period}/finalize`
    - _Requirements: 7.1, 7.2, 7.4, 7.5_
  - [x] 14.4 Implement IRD data retrieval UI: button "Fetch IRD History" per employee row; call `GET /ird/cumulative/{tin}/{financialYear}`; display cumulative income and deduction; show adjusted monthly deduction after retrieval
    - _Requirements: 5.1, 5.2, 6.1_
  - [x] 14.5 Implement payroll submission page: review submission lines, call `POST /payroll/submissions`; display submission reference number and status; show filing deadline countdown
    - _Requirements: 8.1, 8.2, 8.4_
  - [ ] 14.6 Implement notifications panel: display recent `NotificationLog` entries for the employer; show submission confirmations and failure alerts
    - _Requirements: 9.1, 9.2_

- [ ] 15. Employee portal (React)
  - [x] 15.1 Implement employee login and tax history page in `employee-portal`: call `GET /employee/history/{financialYear}`; render chronological deduction list with employer name, amount, date; show cumulative total; handle no-records message
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
  - [x] 15.2 Implement PDF export button: call `GET /employee/history/{financialYear}/pdf`; trigger browser download of returned binary stream; disable button and show error if no records exist
    - _Requirements: 11.1, 11.5_
  - [x] 15.3 Implement `Employee_Portal_Service` backend endpoints: `GET /employee/history/{financialYear}` — query `MonthlyDeduction` joined with `EmployeePayroll` and `Employer` for authenticated employee only; enforce employee-only access (`AUTH_004` for other roles)
    - _Requirements: 10.1, 10.5_
  - [x] 15.4 Implement PDF generation in backend: use QuestPDF to render employee name, TIN, financial year, deduction table, cumulative total; format for A4; store audit log entry on generation
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

- [ ] 16. IRD dashboard (React)
  - [x] 16.1 Implement IRD officer login and compliance report page in `ird-dashboard`: call `GET /ird-dashboard/compliance/{financialYear}`; display total employers, submitted count, non-submitted count, total PAYE collected; enforce `IRD_Officer` role
    - _Requirements: 12.1, 12.2, 12.4_
  - [x] 16.2 Implement employer search filter: input for registration number or name; call `GET /ird-dashboard/employers/{registrationNo}`; display matching records within 5 seconds
    - _Requirements: 12.3_
  - [x] 16.3 Implement audit log viewer: call `GET /ird-dashboard/audit-logs` with query params (employer TIN, employee TIN, date range, action type); paginated table display; write audit log on export
    - _Requirements: 13.1, 13.2_
  - [x] 16.4 Implement `IRD_Dashboard_Service` backend endpoints: `GET /ird-dashboard/compliance/{financialYear}`, `GET /ird-dashboard/employers/{registrationNo}`, `GET /ird-dashboard/audit-logs` with filtering; restrict to `IRD_Officer` role
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 13.2_
  - [x] 16.5 Implement compliance report export: `GET /ird-dashboard/compliance/{financialYear}/export` returns CSV; write audit log entry with officer identity, report parameters, timestamp
    - _Requirements: 12.5_

- [ ] 17. Edge case validation + full PBT test suite
  - [ ]* 17.1 Write property test P7 (Audit Log Immutability) using FsCheck
    - **Property 7: Audit Log Immutability**
    - Generate random `AuditEntry`; write to DB; attempt `UPDATE` and `DELETE` via raw SQL; assert both are rejected and `SecurityAlertLog` has a new entry
    - Tag: `// Feature: paye-tax-easy, Property 7: Audit Log Immutability`
    - **Validates: Requirements 13.4, 13.5**
  - [ ]* 17.2 Write integration tests for end-to-end payroll submission flow: employer login → salary entry → IRD retrieval → adjusted deduction → submission → notification log written
    - _Requirements: 8.1, 8.2, 9.1, 13.1_
  - [ ]* 17.3 Write integration tests for IRD circuit breaker: simulate IRD API outage (mock); verify circuit opens after 3 failures; verify `IRD_001` returned with `Retry-After` header
    - _Requirements: 5.3, 17.4_
  - [ ]* 17.4 Write integration tests for audit log: verify audit entry written for each action type (salary entry, IRD retrieval, PDF export, submission, login)
    - _Requirements: 13.1_
  - [ ]* 17.5 Write integration tests for employee portal access control: employee A cannot access employee B's history (expect `AUTH_004`)
    - _Requirements: 10.5_

- [ ] 18. Performance and security hardening
  - [ ] 18.1 Add response caching (`IMemoryCache`) for `GET /payroll/summary/{period}` with 30-second TTL; add `ETag` support for conditional requests
    - _Requirements: 7.2, 16.1_
  - [ ] 18.2 Add database indexes: composite index on `MonthlyDeduction(EmployeePayrollId, Year, Month)`; index on `AuditLog(ActorId, Timestamp)`; index on `IrdCumulativeCache(EmployeeTIN, FinancialYear)`
    - _Requirements: 7.2, 13.2_
  - [ ] 18.3 Implement global exception handler middleware: catch unhandled exceptions, return consistent error envelope (design.md error format), never expose stack traces or DB messages
    - _Requirements: 17.1, 17.2, 17.5_
  - [ ] 18.4 Add rate limiting middleware: 100 requests/minute per authenticated client via `AspNetCoreRateLimit` or .NET 8 built-in rate limiter
    - _Requirements: 16.1_
  - [ ] 18.5 Configure Azure App Service auto-scaling rules: scale out when CPU > 80% for 5 minutes; configure health check endpoint `GET /health` returning 200
    - _Requirements: 16.2, 16.3_
  - [ ] 18.6 Add performance metric logging: log API response time, error rate, and resource utilization at 1-minute intervals via `ILogger` + Application Insights
    - _Requirements: 16.6_

- [ ] 19. TIN registration workflow
  - [ ] 19.1 Implement `POST /auth/register` full flow: validate mandatory fields (full name, NIC, contact details); check for duplicate NIC/TIN; create account; publish `TINRequest` event to IRD Integration Service
    - Return conflict error on duplicate; return `PAYROLL_002`-style error on missing fields
    - _Requirements: 2.1, 2.3, 2.4_
  - [ ] 19.2 Implement TIN confirmation handler: when IRD confirms TIN, associate TIN with account (store encrypted), send confirmation email via `NotificationService`
    - _Requirements: 2.2, 2.5_
  - [ ]* 19.3 Write unit tests for registration: valid registration, duplicate NIC rejected, missing fields rejected, TIN stored encrypted
    - _Requirements: 2.1, 2.3, 2.4, 2.5_

- [ ] 20. Final checkpoint — full test suite
  - Run `dotnet test --verbosity normal`. Ensure all unit tests, property tests (P1–P7), and integration tests pass. Ask the user if any test is failing or any requirement needs clarification before proceeding to documentation.

- [ ] 21. README and documentation
  - [x] 21.1 Complete README with: project overview, technology stack table, prerequisites (Node 20, .NET 8 SDK, SQL Server, Azure CLI), local setup steps (clone → `dotnet ef database update` → `npm install` → run instructions), environment variables reference
    - _Requirements: 17 (documentation)_
  - [ ] 21.2 Add `appsettings.Development.json.example` with all required keys: Azure AD B2C tenant, Key Vault URI, SendGrid API key placeholder, Twilio credentials placeholder, SQL connection string placeholder
    - _Requirements: 14.6_
  - [x] 21.3 Write brief user guide section in README: Employer workflow (login → salary entry → IRD fetch → submit), Employee workflow (login → view history → export PDF), IRD Officer workflow (login → compliance report → audit log)
    - _Requirements: 17 (documentation)_

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP build
- Property tests P1–P7 map directly to the Correctness Properties in design.md
- Each task references specific requirements for traceability
- Always Encrypted (task 2.4) requires the Azure Key Vault CMK to be provisioned before running migrations in production
- The `IrdCumulativeCache` table is insert-only by design — never update existing rows; always insert a new snapshot
- Financial year runs 1 April – 31 March; `RemainingMonths` must account for this boundary
