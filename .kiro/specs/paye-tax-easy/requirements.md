# Requirements Document

## Introduction

PAYE Tax Easy is a centralized digital platform that integrates with Sri Lanka's Inland Revenue Department (IRD) to ensure accurate and transparent Pay-As-You-Earn (PAYE) tax deductions. The system addresses the core problem of employers calculating PAYE tax in isolation — without access to an employee's prior deduction history from previous employers during the same financial year. This leads to systematic overpayment or underpayment that employees only discover at annual tax return filing time (deadline: 30th November).

The platform enables employers and HR/payroll departments to securely retrieve cumulative PAYE deduction records from the IRD, perform accurate monthly tax recalculations, and submit payroll data electronically. Employees gain a self-service portal to view and download their tax history, while IRD officials gain compliance dashboards and audit capabilities.

Financial year: 1st April to 31st March (Sri Lanka).

Tax slabs (Inland Revenue Amendment Act No. 02 of 2025):
- Up to Rs. 1,800,000: 0% (Tax Relief)
- Rs. 1,800,001 – Rs. 2,800,000: 6%
- Rs. 2,800,001 – Rs. 3,300,000: Rs. 60,000 + 18% of amount over Rs. 2,800,000
- Rs. 3,300,001 – Rs. 3,800,000: Rs. 150,000 + 24% of amount over Rs. 3,300,000
- Rs. 3,800,001 – Rs. 4,300,000: Rs. 270,000 + 30% of amount over Rs. 3,800,000
- Above Rs. 4,300,000: Rs. 420,000 + 36% of amount over Rs. 4,300,000

Monthly PAYE deduction = Annual PAYE tax / 12

---

## Glossary

- **System**: The PAYE Tax Easy platform as a whole.
- **Employer**: An organization or individual registered with the IRD as liable for PAYE deductions on behalf of employees.
- **Employee**: A Sri Lankan resident with employment income subject to PAYE tax.
- **IRD**: Inland Revenue Department of Sri Lanka — the government authority responsible for tax administration.
- **IRD_Integration_Service**: The System component responsible for secure API communication with the IRD's data services.
- **Auth_Service**: The System component responsible for authentication and authorization using Azure AD B2C.
- **PAYE_Calculator**: The System component that computes annual PAYE tax liability and monthly deduction amounts based on cumulative income and prior deductions.
- **Payroll_Service**: The System component that manages salary records, payroll submissions, and deduction tracking per employer.
- **Employee_Portal**: The System component providing employees with self-service access to their tax deduction history.
- **IRD_Dashboard**: The System component providing IRD officials with compliance monitoring and audit capabilities.
- **Notification_Service**: The System component responsible for sending confirmation and alert notifications to employers.
- **TIN**: Taxpayer Identification Number — a unique identifier assigned by the IRD to each taxpayer.
- **Cumulative_Income**: The total gross employment income earned by an employee from all employers within the current financial year.
- **Cumulative_Deduction**: The total PAYE tax already deducted from an employee across all employers within the current financial year.
- **Financial_Year**: The period from 1st April to 31st March used for PAYE tax calculations in Sri Lanka.
- **Tax_Relief**: The annual income threshold of Rs. 1,800,000 below which no PAYE tax is applicable.
- **Monthly_Deduction**: The PAYE tax amount to be deducted from an employee's salary in a given month, calculated as Annual PAYE Tax / 12.
- **Payroll_Submission**: A formal record submitted by an employer to the IRD containing employee salary and PAYE deduction data for a given period.
- **Audit_Log**: An immutable record of system actions, data access events, and employer submissions for compliance tracing.
- **PDF_Export**: A portable document format file containing an employee's tax deduction history for personal record-keeping.

---

## Requirements

### Requirement 1: Secure Employer Authentication

**User Story:** As an employer, I want to log in securely so that I can access the PAYE system and manage employee payroll data with confidence.

#### Acceptance Criteria

1. WHEN an employer submits valid credentials via the login interface, THE Auth_Service SHALL authenticate the employer using Azure AD B2C and grant access within 5 seconds.
2. WHEN an employer submits invalid credentials, THE Auth_Service SHALL deny access and return a descriptive error message specifying whether the username or password is incorrect, without revealing which field failed for security purposes.
3. WHEN an employer fails authentication 5 consecutive times within a 15-minute window, THE Auth_Service SHALL lock the account and notify the registered email address.
4. WHILE an employer session is active, THE Auth_Service SHALL enforce role-based access control so that each employer can only access data belonging to their registered organization.
5. WHEN an employer session remains idle for 30 minutes, THE Auth_Service SHALL invalidate the session token and require re-authentication.
6. THE Auth_Service SHALL encrypt all authentication tokens using industry-standard encryption and transmit them exclusively over HTTPS.
7. WHERE multi-factor authentication is enabled for an employer account, THE Auth_Service SHALL require a second verification factor before granting access.

---

### Requirement 2: TIN Registration and Taxpayer Identification

**User Story:** As a user, I want to register and request a Taxpayer Identification Number (TIN) so that I can be uniquely identified within the PAYE system.

#### Acceptance Criteria

1. WHEN an employer or employee submits a registration request with valid identity documents, THE System SHALL create a new account and initiate a TIN request to the IRD within 1 business day.
2. WHEN the IRD confirms a TIN assignment, THE System SHALL associate the TIN with the registered account and notify the user via email.
3. IF a registration request contains duplicate identity information matching an existing account, THEN THE System SHALL reject the request and return an error message identifying the conflict.
4. THE System SHALL validate that all mandatory registration fields — including full legal name, National Identity Card number, and contact details — are present and correctly formatted before submission.
5. WHEN a TIN is successfully assigned, THE System SHALL store the TIN in encrypted form and make it available for all subsequent PAYE transactions for that user.

---

### Requirement 3: Employee Salary Data Entry

**User Story:** As an employer, I want to enter employee salary details so that I can calculate the correct monthly PAYE tax deduction.

#### Acceptance Criteria

1. WHEN an employer submits salary details for an employee including gross monthly salary and employment start date, THE Payroll_Service SHALL store the record and trigger a PAYE calculation.
2. THE Payroll_Service SHALL validate that the gross monthly salary is a positive numeric value expressed in Sri Lankan Rupees before storing the record.
3. WHEN an employer updates an employee's salary due to a promotion or adjustment, THE Payroll_Service SHALL record the effective date of the change and retain the prior salary record for audit purposes.
4. IF an employer submits salary data for an employee whose TIN does not exist in the System, THEN THE Payroll_Service SHALL reject the submission and return an error message instructing the employer to verify the employee's TIN.
5. WHILE an employer is entering salary data, THE Payroll_Service SHALL display the current Financial_Year period so that the employer can confirm the data applies to the correct year.

---

### Requirement 4: PAYE Tax Calculation Engine

**User Story:** As an employer, I want the system to calculate monthly PAYE tax accurately so that I deduct the correct amount from each employee's salary every month.

#### Acceptance Criteria

1. WHEN salary data is submitted for an employee, THE PAYE_Calculator SHALL compute the annual PAYE tax liability using the tax slabs defined in the Inland Revenue Amendment Act No. 02 of 2025.
2. THE PAYE_Calculator SHALL apply the Tax_Relief threshold of Rs. 1,800,000 before applying any tax slab rate.
3. WHEN computing the annual PAYE tax, THE PAYE_Calculator SHALL apply the following slab rates in order:
   - 0% on the first Rs. 1,800,000 of annual income
   - 6% on income between Rs. 1,800,001 and Rs. 2,800,000
   - 18% on income between Rs. 2,800,001 and Rs. 3,300,000 (plus Rs. 60,000 cumulative)
   - 24% on income between Rs. 3,300,001 and Rs. 3,800,000 (plus Rs. 150,000 cumulative)
   - 30% on income between Rs. 3,800,001 and Rs. 4,300,000 (plus Rs. 270,000 cumulative)
   - 36% on income above Rs. 4,300,000 (plus Rs. 420,000 cumulative)
4. THE PAYE_Calculator SHALL compute the Monthly_Deduction as the annual PAYE tax divided by 12, rounded to the nearest Sri Lankan Rupee.
5. WHEN Cumulative_Deduction data is available for an employee from prior employment in the same Financial_Year, THE PAYE_Calculator SHALL incorporate the Cumulative_Income and Cumulative_Deduction into the recalculation to determine the adjusted Monthly_Deduction for remaining months.
6. FOR ALL valid salary inputs, the sum of Monthly_Deduction values across all 12 months of the Financial_Year SHALL equal the total annual PAYE tax liability computed for the employee's total annual income (round-trip property).
7. IF an employee's Cumulative_Income already exceeds the annual income threshold for a higher tax slab, THEN THE PAYE_Calculator SHALL apply the correct marginal rate to the remaining income for the Financial_Year.

---

### Requirement 5: IRD Cumulative Tax Data Retrieval

**User Story:** As an employer, I want to retrieve cumulative tax data from the IRD so that I can adjust PAYE deductions for employees who have changed jobs mid-year.

#### Acceptance Criteria

1. WHEN an employer requests cumulative PAYE data for an employee by providing a valid TIN and Financial_Year, THE IRD_Integration_Service SHALL query the IRD's secure API and return the Cumulative_Income and Cumulative_Deduction within 10 seconds.
2. WHEN the IRD API returns a successful response, THE IRD_Integration_Service SHALL store the retrieved data in the System with a timestamp and associate it with the employer's payroll record for that employee.
3. IF the IRD API is unavailable, THEN THE IRD_Integration_Service SHALL return a descriptive error message to the employer and log the failure with a timestamp for retry processing.
4. IF the IRD API returns no records for a given TIN and Financial_Year, THEN THE IRD_Integration_Service SHALL return a response indicating no prior deductions exist, and THE PAYE_Calculator SHALL treat Cumulative_Deduction as Rs. 0.
5. THE IRD_Integration_Service SHALL authenticate all outbound API requests to the IRD using OAuth 2.0 client credentials and transmit data exclusively over HTTPS.
6. WHILE retrieving IRD data, THE IRD_Integration_Service SHALL not expose raw IRD API credentials to the employer-facing interface.
7. WHEN IRD data is retrieved for an employee, THE System SHALL record an Audit_Log entry containing the employer's identity, the employee's TIN, the timestamp, and the data fields accessed.

---

### Requirement 6: Monthly PAYE Tax Adjustment for Job Transitions

**User Story:** As an employer, I want to adjust monthly PAYE tax based on prior deductions from a previous employer so that I avoid overcharging or undercharging the employee.

#### Acceptance Criteria

1. WHEN an employer retrieves Cumulative_Deduction data for a new employee and submits the employee's current monthly salary, THE PAYE_Calculator SHALL compute the adjusted Monthly_Deduction for the remaining months of the Financial_Year.
2. THE PAYE_Calculator SHALL compute the adjusted Monthly_Deduction using the formula: (Annual_Tax_Liability_On_Total_Projected_Income − Cumulative_Deduction) / Remaining_Months_In_Financial_Year.
3. WHEN the adjusted Monthly_Deduction is a negative value — indicating the employee has already overpaid — THE PAYE_Calculator SHALL set the Monthly_Deduction to Rs. 0 and flag the record for employer review.
4. WHEN a salary change occurs mid-year due to a promotion or adjustment, THE PAYE_Calculator SHALL recalculate the Monthly_Deduction from the effective date of the change using the updated projected annual income.
5. THE Payroll_Service SHALL retain a complete history of all Monthly_Deduction values and the events that triggered each recalculation for audit purposes.
6. WHEN an employer confirms the adjusted Monthly_Deduction for an employee, THE Payroll_Service SHALL apply the adjustment to the next payroll cycle.

---

### Requirement 7: PAYE Deduction Summary and Verification

**User Story:** As an employer, I want to view a summary of PAYE deductions so that I can verify monthly amounts before processing payroll.

#### Acceptance Criteria

1. WHEN an employer requests a deduction summary for a payroll period, THE Payroll_Service SHALL return a list of all employees with their gross salary, computed Monthly_Deduction, and year-to-date Cumulative_Deduction for that Financial_Year.
2. THE Payroll_Service SHALL display the deduction summary within 3 seconds of the employer's request for payroll periods containing up to 500 employee records.
3. WHEN an employer identifies an error in the deduction summary and submits a correction request, THE Payroll_Service SHALL allow the employer to update the salary record and trigger a recalculation before payroll is finalized.
4. THE Payroll_Service SHALL clearly indicate in the summary which employees have prior-employer Cumulative_Deduction data incorporated into their Monthly_Deduction calculation.
5. WHEN an employer finalizes the deduction summary, THE Payroll_Service SHALL lock the records for that payroll period and prevent further modification without an authorized correction workflow.

---

### Requirement 8: Electronic PAYE Tax Return Filing

**User Story:** As an employer, I want to file PAYE tax returns electronically so that I can meet compliance deadlines efficiently.

#### Acceptance Criteria

1. WHEN an employer submits a Payroll_Submission for a completed payroll period, THE Payroll_Service SHALL validate that all mandatory fields — including employee TINs, gross salaries, and Monthly_Deduction amounts — are present and correctly formatted.
2. WHEN a Payroll_Submission passes validation, THE Payroll_Service SHALL transmit the submission to the IRD via the IRD_Integration_Service and return a submission reference number to the employer within 15 seconds.
3. IF a Payroll_Submission fails IRD validation, THEN THE Payroll_Service SHALL return a descriptive error message identifying each invalid field and the correction required.
4. THE Payroll_Service SHALL enforce the annual PAYE return filing deadline of 30th November by displaying a countdown and preventing submission of returns for a Financial_Year after the deadline has passed without an authorized extension.
5. WHEN a Payroll_Submission is successfully accepted by the IRD, THE System SHALL record an Audit_Log entry containing the submission reference number, employer identity, submission timestamp, and total PAYE amount submitted.
6. THE Payroll_Service SHALL support bulk submission of Payroll_Submissions for employers with multiple employees in a single API call.

---

### Requirement 9: Employer Confirmation Notifications

**User Story:** As an employer, I want to receive confirmation notifications after submitting payroll data so that I have proof of compliance.

#### Acceptance Criteria

1. WHEN a Payroll_Submission is successfully accepted by the IRD, THE Notification_Service SHALL send a confirmation notification to the employer's registered email address within 2 minutes, including the submission reference number, submission timestamp, and total PAYE amount.
2. IF a Payroll_Submission fails IRD validation, THEN THE Notification_Service SHALL send a failure notification to the employer's registered email address within 2 minutes, including a description of each validation error.
3. WHEN the PAYE return filing deadline is 7 days away and an employer has not submitted a return for the current Financial_Year, THE Notification_Service SHALL send a reminder notification to the employer's registered email address.
4. THE Notification_Service SHALL retain a delivery record for each notification, including the recipient, timestamp, notification type, and delivery status.
5. WHERE an employer has configured SMS notifications, THE Notification_Service SHALL send confirmation and failure notifications via SMS in addition to email.

---

### Requirement 10: Employee Tax Deduction History

**User Story:** As an employee, I want to view my tax deduction history so that I can verify the accuracy of PAYE deductions across all employers in the current Financial_Year.

#### Acceptance Criteria

1. WHEN an employee authenticates via the Employee_Portal and requests their tax deduction history, THE Employee_Portal SHALL display a chronological list of all PAYE deductions recorded for the employee in the current Financial_Year, including employer name, deduction amount, and deduction date.
2. THE Employee_Portal SHALL retrieve and display the employee's tax deduction history within 5 seconds of the request.
3. WHEN an employee's tax history includes deductions from multiple employers within the same Financial_Year, THE Employee_Portal SHALL display each employer's contributions separately and show the Cumulative_Deduction total.
4. IF no PAYE deduction records exist for an employee in the current Financial_Year, THEN THE Employee_Portal SHALL display a message indicating no deductions have been recorded for the period.
5. THE Employee_Portal SHALL restrict each employee's access to their own tax deduction records and prevent access to records belonging to other employees.

---

### Requirement 11: PDF Export of Tax Deduction History

**User Story:** As an employee, I want to download my tax deduction history as a PDF so that I can keep personal records and use the document for tax return filing.

#### Acceptance Criteria

1. WHEN an employee requests a PDF export of their tax deduction history, THE Employee_Portal SHALL generate and return a downloadable PDF file within 10 seconds.
2. THE PDF_Export SHALL include the employee's full name, TIN, Financial_Year, a chronological list of all PAYE deductions with employer names and dates, and the Cumulative_Deduction total.
3. THE PDF_Export SHALL be formatted to be human-readable and printable on A4 paper.
4. WHEN a PDF is generated, THE System SHALL record an Audit_Log entry containing the employee's TIN, the timestamp of the export, and the Financial_Year covered.
5. IF the employee's tax deduction history contains no records for the requested Financial_Year, THEN THE Employee_Portal SHALL return an error message indicating no data is available for export rather than generating an empty PDF.

---

### Requirement 12: IRD Compliance Reporting Dashboard

**User Story:** As an IRD officer, I want to generate compliance reports so that I can monitor employer submissions and identify non-compliant organizations.

#### Acceptance Criteria

1. WHEN an IRD officer authenticates via the IRD_Dashboard and requests a compliance report for a specified Financial_Year, THE IRD_Dashboard SHALL generate and display the report within 15 seconds.
2. THE IRD_Dashboard SHALL include in each compliance report: total number of registered employers, number of employers who have submitted PAYE returns, number of employers who have not submitted returns, and total PAYE collected for the period.
3. WHEN an IRD officer filters the compliance report by employer registration number or organization name, THE IRD_Dashboard SHALL return matching records within 5 seconds.
4. THE IRD_Dashboard SHALL restrict access to compliance reports to authenticated IRD officers only and prevent employer or employee accounts from accessing this data.
5. WHEN an IRD officer exports a compliance report, THE System SHALL record an Audit_Log entry containing the officer's identity, the report parameters, and the export timestamp.

---

### Requirement 13: Audit Logging and Compliance Tracing

**User Story:** As an IRD officer, I want to access audit logs of employer submissions so that I can trace compliance issues and investigate discrepancies.

#### Acceptance Criteria

1. THE System SHALL record an Audit_Log entry for every data access event, Payroll_Submission, IRD data retrieval, PDF export, and authentication event, including the actor's identity, action performed, timestamp, and affected data identifiers.
2. WHEN an IRD officer queries the Audit_Log by employer TIN, employee TIN, date range, or action type, THE IRD_Dashboard SHALL return matching log entries within 10 seconds.
3. THE System SHALL retain Audit_Log entries for a minimum of 7 years in accordance with Sri Lankan tax record retention requirements.
4. IF an attempt is made to modify or delete an Audit_Log entry, THEN THE System SHALL reject the operation and record a security alert in a separate immutable security log.
5. THE System SHALL store Audit_Log entries in an append-only data store that prevents modification after creation.

---

### Requirement 14: Secure Data Storage

**User Story:** As a developer, I want to store tax data securely so that it can be retrieved reliably and protected from unauthorized access.

#### Acceptance Criteria

1. THE System SHALL encrypt all personally identifiable information — including TINs, salary records, and tax deduction data — at rest using AES-256 encryption.
2. THE System SHALL encrypt all data in transit between the frontend, backend API, database, and IRD Integration Service using TLS 1.2 or higher.
3. WHEN a database backup is performed, THE System SHALL encrypt the backup using the same encryption standard as the primary data store.
4. THE System SHALL enforce role-based access control at the database level so that each application component can only access the data tables required for its function.
5. IF an unauthorized access attempt is detected on the database, THEN THE System SHALL log the attempt in the security log and trigger an alert to the system administrator within 5 minutes.
6. THE System SHALL store all encryption keys in Azure Key Vault and rotate keys on a schedule of no less than once every 12 months.

---

### Requirement 15: Edge Case Handling for Employment Gaps and Transitions

**User Story:** As a tester, I want the system to correctly handle edge cases such as job changes, salary gaps, and mid-year employment so that the system manages real-world employment scenarios accurately.

#### Acceptance Criteria

1. WHEN an employee joins a new employer after a period of unemployment within the same Financial_Year, THE PAYE_Calculator SHALL use the Cumulative_Income and Cumulative_Deduction from prior employment to compute the correct Monthly_Deduction for the remaining months.
2. WHEN an employee has zero income for one or more months within the Financial_Year due to an employment gap, THE PAYE_Calculator SHALL exclude those months from the Monthly_Deduction calculation and distribute the remaining tax liability across the months with active employment.
3. WHEN an employee joins an employer on the first day of the Financial_Year with no prior employment history, THE PAYE_Calculator SHALL treat Cumulative_Income and Cumulative_Deduction as Rs. 0 and compute the Monthly_Deduction based solely on the current employer's salary.
4. WHEN an employee's projected annual income changes due to a mid-year salary adjustment, THE PAYE_Calculator SHALL recalculate the Monthly_Deduction for all remaining months in the Financial_Year from the effective date of the adjustment.
5. IF an employee's Cumulative_Deduction exceeds the total annual PAYE tax liability computed on their total projected annual income, THEN THE PAYE_Calculator SHALL set the Monthly_Deduction to Rs. 0 for all remaining months and flag the record for employer review.
6. WHEN an employee works for two employers simultaneously within the same Financial_Year, THE PAYE_Calculator SHALL aggregate income from both employers when computing the annual tax liability and distribute the Monthly_Deduction proportionally across both employers.

---

### Requirement 16: System Performance and Scalability

**User Story:** As a system admin, I want to monitor system performance so that I can ensure the platform remains responsive during peak payroll processing periods.

#### Acceptance Criteria

1. WHILE the System is processing payroll submissions during peak periods, THE System SHALL maintain an API response time of under 3 seconds for 95% of requests with up to 1,000 concurrent users.
2. THE System SHALL scale horizontally on Microsoft Azure to accommodate increased load during peak payroll periods without manual intervention.
3. WHEN the System's CPU utilization exceeds 80% for more than 5 consecutive minutes, THE System SHALL trigger an auto-scaling event and notify the system administrator.
4. THE System SHALL achieve a minimum uptime of 99.5% measured on a monthly basis, excluding scheduled maintenance windows.
5. WHEN a scheduled maintenance window is planned, THE System SHALL notify all registered employers and IRD officers at least 48 hours in advance via the Notification_Service.
6. THE System SHALL log performance metrics — including API response times, error rates, and resource utilization — at 1-minute intervals for monitoring and capacity planning.

---

### Requirement 17: Clear and Actionable Error Messages

**User Story:** As an employer, I want error messages to be clear and actionable so that I can correct mistakes quickly without requiring support intervention.

#### Acceptance Criteria

1. WHEN the System returns an error response, THE System SHALL include a human-readable description of the error, the field or operation that caused the error, and a suggested corrective action.
2. THE System SHALL use consistent error codes across all API responses so that developers can programmatically identify and handle error conditions.
3. WHEN a validation error occurs during salary data entry, THE Payroll_Service SHALL highlight the specific fields containing invalid data and display the expected format alongside the error message.
4. IF a network or integration error occurs during IRD data retrieval, THEN THE IRD_Integration_Service SHALL return an error message distinguishing between a temporary service unavailability and a permanent data-not-found condition.
5. THE System SHALL not expose internal stack traces, database error messages, or system configuration details in error responses returned to the employer-facing interface.
