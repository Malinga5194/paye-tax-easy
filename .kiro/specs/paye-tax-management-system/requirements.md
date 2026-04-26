# Requirements Document

## Introduction

The PAYE Tax Management System is a solution for the Inland Revenue Department (IRD) of Sri Lanka to address inaccurate PAYE (Pay As You Earn) tax deductions that occur when employees change employers mid-financial-year. The current system causes over- or under-deduction because each new employer recalculates tax from scratch, ignoring cumulative deductions already made by previous employers within the same financial year (April 1 – March 31). This system enables employers to access an employee's cumulative tax deduction history, accurately compute adjusted monthly PAYE deductions, and eliminate the need for employees to file for refunds or pay additional tax at year-end.

## Glossary

- **System**: The PAYE Tax Management System
- **IRD**: Inland Revenue Department of Sri Lanka — the authority that collects PAYE tax and maintains the central tax records
- **Employer**: A registered organisation that employs staff, deducts PAYE tax from salaries, and remits it to the IRD
- **Employee**: An individual taxpayer whose PAYE tax is deducted at source by their employer
- **Financial_Year**: The tax year running from April 1 to March 31
- **Cumulative_Tax_Record**: The total PAYE tax deducted from an employee across all employers within the current Financial_Year
- **Tax_Calculator**: The component responsible for computing annual tax liability and adjusted monthly deductions
- **Tax_Registry**: The central IRD-managed store of Cumulative_Tax_Records for all employees
- **Employer_Portal**: The web interface through which employers access employee tax records and submit monthly deductions
- **PAYE_Deduction**: The monthly tax amount withheld from an employee's salary by an employer
- **Tax_Slab**: A defined income band with an associated tax rate as per the Inland Revenue (Amendment) Act No. 02 of 2025
- **Remittance**: The act of an employer transferring collected PAYE_Deductions to the IRD by the 15th of the following month
- **Tax_Return**: The annual individual tax filing submitted by an employee to the IRD by 30th November

---

## Requirements

### Requirement 1: Employee Tax Record Access

**User Story:** As an employer, I want to access the cumulative tax deduction record of a newly joined employee, so that I can account for tax already deducted by their previous employer(s) within the same Financial_Year.

#### Acceptance Criteria

1. WHEN an employer submits a valid Employee national identity number and the current Financial_Year, THE Tax_Registry SHALL return the employee's Cumulative_Tax_Record including total income earned and total PAYE_Deduction made to date within that Financial_Year.
2. WHEN an employer submits a national identity number that does not exist in the Tax_Registry, THE Tax_Registry SHALL return a descriptive error indicating the employee record was not found.
3. WHEN an employer submits a national identity number for an employee who has no prior employment in the current Financial_Year, THE Tax_Registry SHALL return a Cumulative_Tax_Record with zero cumulative income and zero cumulative PAYE_Deduction.
4. THE Tax_Registry SHALL restrict access so that only verified, registered Employers may retrieve employee Cumulative_Tax_Records.
5. WHEN an employer's authentication credentials are invalid or expired, THE Employer_Portal SHALL deny access and return an authentication error.

---

### Requirement 2: PAYE Tax Calculation

**User Story:** As an employer, I want the system to calculate the correct adjusted monthly PAYE deduction for an employee, so that the total tax deducted across all employers for the Financial_Year matches the employee's actual annual tax liability.

#### Acceptance Criteria

1. THE Tax_Calculator SHALL compute annual tax liability by applying the following Tax_Slabs to the employee's projected annual income:
   - Rs. 0 – Rs. 1,800,000: 0% (tax relief)
   - Rs. 1,800,001 – Rs. 2,800,000: 6%
   - Rs. 2,800,001 – Rs. 3,300,000: 18%
   - Rs. 3,300,001 – Rs. 3,800,000: 24%
   - Rs. 3,800,001 – Rs. 4,300,000: 30%
   - Rs. 4,300,001 and above: 36%
2. WHEN an employee's projected annual income falls entirely within the tax relief band (Rs. 0 – Rs. 1,800,000), THE Tax_Calculator SHALL return a PAYE_Deduction of Rs. 0 for that month.
3. WHEN calculating the adjusted monthly PAYE_Deduction for an employee who has prior Cumulative_Tax_Record in the current Financial_Year, THE Tax_Calculator SHALL subtract the cumulative PAYE_Deduction already made from the remaining annual tax liability and divide the result by the number of remaining months in the Financial_Year.
4. WHEN the adjusted monthly PAYE_Deduction computed for an employee is negative (indicating over-deduction to date), THE Tax_Calculator SHALL return a PAYE_Deduction of Rs. 0 and flag the record for IRD review.
5. THE Tax_Calculator SHALL project annual income as the sum of cumulative income already earned in the Financial_Year and the product of the current monthly salary multiplied by the number of remaining months in the Financial_Year.
6. FOR ALL valid projected annual income values, the sum of all monthly PAYE_Deductions computed across all employers within a Financial_Year SHALL equal the annual tax liability computed by the Tax_Calculator for that projected annual income (round-trip correctness property).

---

### Requirement 3: Cumulative Tax Record Update

**User Story:** As an employer, I want the system to update the employee's cumulative tax record after each monthly deduction, so that subsequent employers always have accurate and up-to-date deduction history.

#### Acceptance Criteria

1. WHEN an employer submits a monthly PAYE_Deduction for an employee, THE Tax_Registry SHALL update the employee's Cumulative_Tax_Record by adding the submitted deduction amount and the corresponding monthly salary to the running totals for the current Financial_Year.
2. WHEN an employer submits a PAYE_Deduction amount that is negative or non-numeric, THE Tax_Registry SHALL reject the submission and return a descriptive validation error.
3. WHEN an employer submits a PAYE_Deduction for an employee for a month that already has a recorded deduction from the same employer, THE Tax_Registry SHALL reject the duplicate submission and return a descriptive error.
4. THE Tax_Registry SHALL record the employer identifier, employee identifier, month, salary amount, and PAYE_Deduction amount for every accepted submission.
5. WHILE a Cumulative_Tax_Record update is in progress, THE Tax_Registry SHALL prevent concurrent updates to the same employee record to maintain data consistency.

---

### Requirement 4: Monthly Remittance Tracking

**User Story:** As the IRD, I want to track whether each employer has remitted collected PAYE tax by the 15th of the following month, so that I can enforce compliance and identify late remittances.

#### Acceptance Criteria

1. THE System SHALL record the date on which each employer's Remittance is received by the IRD for each month of the Financial_Year.
2. WHEN the 15th of a month passes and an employer has not submitted a Remittance for the previous month, THE System SHALL mark that employer's remittance status for that month as overdue.
3. WHEN an employer submits a Remittance after the 15th deadline, THE System SHALL record the submission as a late Remittance and retain the original due date alongside the actual submission date.
4. THE System SHALL allow IRD administrators to retrieve a list of all employers with overdue or late Remittances for any given month within the current Financial_Year.

---

### Requirement 5: Employee Tax Summary

**User Story:** As an employee, I want to view my cumulative tax deduction summary for the current Financial_Year, so that I can verify the accuracy of deductions and prepare my annual Tax_Return.

#### Acceptance Criteria

1. WHEN an employee authenticates and requests their tax summary for the current Financial_Year, THE System SHALL return a breakdown of PAYE_Deductions by employer, by month, and the total cumulative PAYE_Deduction for the Financial_Year.
2. WHEN an employee's total cumulative PAYE_Deduction for the Financial_Year equals their annual tax liability as computed by the Tax_Calculator, THE System SHALL indicate that no additional payment or refund is expected on the Tax_Return.
3. WHEN an employee's total cumulative PAYE_Deduction for the Financial_Year is less than their annual tax liability, THE System SHALL display the outstanding balance the employee must settle in their Tax_Return.
4. WHEN an employee's total cumulative PAYE_Deduction for the Financial_Year exceeds their annual tax liability, THE System SHALL display the refund amount the employee may claim in their Tax_Return.
5. IF an employee's authentication credentials are invalid or expired, THEN THE System SHALL deny access and return an authentication error.

---

### Requirement 6: Financial Year Rollover

**User Story:** As the IRD, I want the system to reset cumulative tax records at the start of each new Financial_Year, so that prior-year deductions do not affect the new year's calculations.

#### Acceptance Criteria

1. WHEN a new Financial_Year begins (April 1), THE Tax_Registry SHALL initialise a new Cumulative_Tax_Record with zero cumulative income and zero cumulative PAYE_Deduction for every registered employee.
2. WHEN a new Financial_Year begins, THE Tax_Registry SHALL archive the previous Financial_Year's Cumulative_Tax_Records in read-only storage.
3. THE System SHALL retain archived Cumulative_Tax_Records for a minimum of 7 years to support audit and Tax_Return verification.
4. WHEN an employer or employee requests a Cumulative_Tax_Record for a prior Financial_Year, THE Tax_Registry SHALL return the archived record for that year in read-only form.
