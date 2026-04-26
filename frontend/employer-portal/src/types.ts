export interface SalaryRecord {
  salaryRecordId: string;
  employeeTIN: string;
  grossMonthlySalary: number;
  annualTaxLiability: number;
  monthlyDeduction: number;
  effectiveDate: string;
  financialYear: string;
}

export interface DeductionSummary {
  employeeTIN: string;
  employeeName: string;
  grossSalary: number;
  monthlyDeduction: number;
  yearToDateCumulativeDeduction: number;
  hasPriorEmployerData: boolean;
  isOverpaid: boolean;
}

export interface CumulativeData {
  employeeTIN: string;
  financialYear: string;
  cumulativeIncome: number;
  cumulativeDeduction: number;
  retrievedAt: string;
  source: string;
}

export interface SubmissionResult {
  submissionId: string;
  irdReferenceNumber: string;
  status: string;
  totalPAYEAmount: number;
  submittedAt: string;
}

export interface ApiError {
  errorCode: string;
  message: string;
  field: string;
  suggestedAction: string;
}
