import { apiClient } from '../api/client';

interface Slab {
  band: string;
  rate: string;
  taxableAmount: number;
  tax: number;
}

interface MonthlyEntry {
  monthLabel: string;
  grossIncome: number;
  deductionAmount: number;
  cumulativeAtMonth: number;
  trigger: string;
}

interface TaxReport {
  employeeTIN: string;
  employeeName: string;
  employerName: string;
  financialYear: string;
  reportPeriod: string;
  generatedAt: string;
  grossMonthlySalary: number;
  projectedAnnualIncome: number;
  annualTaxLiability: number;
  taxRelief: number;
  taxableIncome: number;
  slabs: Slab[];
  priorEmployerIncome: number;
  priorEmployerDeduction: number;
  currentEmployerYTD: number;
  totalTaxPaidToDate: number;
  remainingTaxForYear: number;
  remainingMonthsInFY: number;
  adjustedMonthlyDeduction: number;
  monthlyHistory: MonthlyEntry[];
  hasPriorEmployer?: boolean;
  nextFyStandardMonthly?: number;
  fullYearAnnualTax?: number;
  withoutSystemMonthly?: number;
  withoutSystemTotal?: number;
  withSystemTotal?: number;
  standardMonthly?: number;
  taxForRemainingMonths?: number;
}

interface Props {
  report: TaxReport;
  period: string;
  onClose: () => void;
}

export default function TaxReportModal({ report, period, onClose }: Props) {
  const downloadPdf = async () => {
    try {
      const res = await apiClient.get(
        `/tax-report/${report.employeeTIN}/${report.financialYear}/${period}/pdf`,
        { responseType: 'blob' }
      );
      const url = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
      const a = document.createElement('a');
      a.href = url;
      a.download = `PAYE_Report_${report.employeeTIN}_${period}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      alert('Failed to generate PDF. Please try again.');
    }
  };

  const pct = report.annualTaxLiability > 0
    ? Math.min(100, Math.round((report.totalTaxPaidToDate / report.annualTaxLiability) * 100))
    : 0;

  return (
    <div style={styles.overlay} onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={styles.modal}>
        {/* Header */}
        <div style={styles.header}>
          <div>
            <h2 style={styles.title}>PAYE Tax Report</h2>
            <p style={styles.subtitle}>Financial Year: {report.financialYear} &nbsp;|&nbsp; As of: {period}</p>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <button style={styles.btnPdf} onClick={downloadPdf}>⬇ Download PDF</button>
            <button style={styles.btnClose} onClick={onClose}>✕</button>
          </div>
        </div>

        <div style={styles.body}>
          {/* Employee Info */}
          <div style={styles.infoGrid}>
            <InfoBox label="Employee Name" value={report.employeeName} />
            <InfoBox label="TIN" value={report.employeeTIN} />
            <InfoBox label="Employer" value={report.employerName} />
            <InfoBox label="Gross Monthly Salary" value={`Rs. ${report.grossMonthlySalary.toLocaleString()}`} />
            <InfoBox label="Projected Annual Income (Salary × 12)" value={`Rs. ${report.projectedAnnualIncome.toLocaleString()}`} />
            <InfoBox label="Tax Relief" value="Rs. 1,800,000" />
            <InfoBox label="Joining Date" value={report.joiningDate ? new Date(report.joiningDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'} />
            <InfoBox label="Remaining Months in FY" value={`${report.remainingMonthsInFY} months`} />
            <InfoBox label="Standard Monthly (No Adjustment)" value={`Rs. ${(report.standardMonthly || Math.round(report.annualTaxLiability / 12)).toLocaleString()}`} />
          </div>

          {/* Calculation Breakdown */}
          <div style={{ background: '#e8f0fb', border: '1px solid #003366', borderRadius: '8px', padding: '16px', marginBottom: '1.5rem' }}>
            <h4 style={{ margin: '0 0 10px', color: '#003366' }}>📐 Tax Calculation Breakdown</h4>
            <table style={{ width: '100%', fontSize: '0.9rem' }}>
              <tbody>
                <tr><td style={{ padding: '4px 0' }}><StepBadge n={1} /> Standard monthly deduction (based on current salary)</td><td style={{ textAlign: 'right', fontWeight: 600 }}>Rs. {(report.withoutSystemMonthly || report.standardMonthly || Math.round((report.fullYearAnnualTax || 0) / 12)).toLocaleString()}</td></tr>
                <tr><td style={{ padding: '4px 0' }}><StepBadge n={2} /> Tax for remaining {report.remainingMonthsInFY} months (<StepBadge n={1} small /> × {report.remainingMonthsInFY})</td><td style={{ textAlign: 'right', fontWeight: 600 }}>Rs. {(report.withoutSystemTotal || ((report.withoutSystemMonthly || report.standardMonthly || Math.round((report.fullYearAnnualTax || 0) / 12)) * report.remainingMonthsInFY)).toLocaleString()}</td></tr>
                <tr><td style={{ padding: '4px 0' }}><StepBadge n={3} /> Cumulative tax already paid (from IRD)</td><td style={{ textAlign: 'right', fontWeight: 600, color: '#27ae60' }}>− Rs. {report.priorEmployerDeduction.toLocaleString()}</td></tr>
                <tr style={{ borderTop: '2px solid #003366' }}><td style={{ padding: '6px 0', fontWeight: 700 }}><StepBadge n={4} /> Remaining tax to collect (② − ③)</td><td style={{ textAlign: 'right', fontWeight: 700, color: '#e67e22' }}>Rs. {(report.withSystemTotal || report.remainingTaxForYear || 0).toLocaleString()}</td></tr>
                <tr><td style={{ padding: '6px 0', fontWeight: 700, color: '#003366' }}><StepBadge n={5} /> Adjusted monthly deduction (<StepBadge n={4} small /> ÷ {report.remainingMonthsInFY})</td><td style={{ textAlign: 'right', fontWeight: 700, color: '#17a2b8', fontSize: '1.1rem' }}>Rs. {report.adjustedMonthlyDeduction.toLocaleString()}</td></tr>
              </tbody>
            </table>
          </div>

          {/* Tax Summary Cards */}
          <div style={styles.summaryCards}>
            <TaxCard label="Annual PAYE Tax (Current Salary)" value={report.annualTaxLiability} color="#003366" icon="📊" />
            <TaxCard label="Monthly PAYE Tax (Without Adjustment)" value={report.withoutSystemMonthly || Math.round(report.annualTaxLiability / 12)} color="#e74c3c" icon="❌" />
            <TaxCard label="Cumulative PAYE Tax Already Paid" value={report.priorEmployerDeduction} color="#27ae60" icon="✓" />
            <TaxCard label="PAYE Tax to Collect (This Employer)" value={report.remainingTaxForYear} color="#e67e22" icon="⏳" />
            <TaxCard label="Adjusted Monthly PAYE Tax (This FY)" value={report.adjustedMonthlyDeduction} color="#17a2b8" icon="✅" />
          </div>

          {/* Prior Employer Section */}
          {report.priorEmployerDeduction > 0 && (
            <div style={styles.priorBox}>
              <h4 style={{ margin: '0 0 8px', color: '#17a2b8' }}>📋 Prior Employer (IRD Data)</h4>
              <div style={{ display: 'flex', gap: '2rem' }}>
                <span>Gross Employment Income (Prior Employer): <strong>Rs. {report.priorEmployerIncome.toLocaleString()}</strong></span>
                <span>PAYE Tax Deducted (Prior Employer): <strong>Rs. {report.priorEmployerDeduction.toLocaleString()}</strong></span>
              </div>
            </div>
          )}

          {/* ── Important Notice ─────────────────────────────────────────── */}
          <div style={styles.noticeBox}>
            <div style={styles.noticeHeader}>
              <span style={styles.noticeIcon}>⚠️</span>
              <span style={styles.noticeTitle}>Important Notice — PAYE Tax Adjustment</span>
            </div>
            {report.hasPriorEmployer ? (
              <p style={styles.noticeText}>
                The monthly PAYE deduction for this employee has been <strong>adjusted</strong> based on the
                cumulative tax already paid during this financial year ({report.financialYear}).
              </p>
            ) : report.adjustedMonthlyDeduction === 0 && report.annualTaxLiability === 0 ? (
              <p style={styles.noticeText}>
                This employee's projected income for the current financial year ({report.financialYear}) is
                <strong> below the tax relief threshold of Rs. 1,800,000</strong>. Therefore, <strong>no PAYE tax
                is applicable</strong> for the remaining {report.remainingMonthsInFY} month{report.remainingMonthsInFY !== 1 ? 's' : ''} of this financial year.
              </p>
            ) : (
              <p style={styles.noticeText}>
                The monthly PAYE deduction for this employee has been calculated based on the projected income
                for the remaining {report.remainingMonthsInFY} month{report.remainingMonthsInFY !== 1 ? 's' : ''} of this financial year ({report.financialYear}).
              </p>
            )}
            <div style={styles.noticeHighlights}>
              {report.hasPriorEmployer && (
                <>
                  <div style={styles.highlightItem}>
                    <span style={styles.highlightLabel}>Cumulative Tax Already Paid (IRD)</span>
                    <span style={{ ...styles.highlightValue, color: '#27ae60' }}>
                      Rs. {report.priorEmployerDeduction.toLocaleString()}
                    </span>
                  </div>
                  <div style={styles.highlightDivider} />
                </>
              )}
              <div style={styles.highlightItem}>
                <span style={styles.highlightLabel}>Adjusted Monthly Deduction (This FY)</span>
                <span style={{ ...styles.highlightValue, color: '#17a2b8' }}>
                  Rs. {report.adjustedMonthlyDeduction.toLocaleString()}
                </span>
              </div>
              <div style={styles.highlightDivider} />
              <div style={styles.highlightItem}>
                <span style={styles.highlightLabel}>Standard Monthly (Next FY — No Adjustments)</span>
                <span style={{ ...styles.highlightValue, color: '#003366' }}>
                  Rs. {(report.nextFyStandardMonthly || Math.round((report.fullYearAnnualTax || report.annualTaxLiability) / 12)).toLocaleString()}
                </span>
              </div>
            </div>
            {report.adjustedMonthlyDeduction === 0 && report.annualTaxLiability === 0 && !report.hasPriorEmployer ? (
              <p style={styles.noticeFooter}>
                📌 <strong>Note:</strong> No PAYE tax will be charged for the remaining{' '}
                <span style={styles.inlineHighlight}>{report.remainingMonthsInFY} month{report.remainingMonthsInFY !== 1 ? 's' : ''}</span>{' '}
                of the current financial year ({report.financialYear}) as the employee's projected income is within the tax relief.{' '}
                <strong>From the next financial year</strong>, if the employee continues at the current salary, the standard monthly deduction of{' '}
                <span style={styles.inlineHighlight}>Rs. {(report.nextFyStandardMonthly || Math.round((report.fullYearAnnualTax || report.annualTaxLiability) / 12)).toLocaleString()}</span>{' '}
                will apply based on a full year's projected income.
              </p>
            ) : (
              <p style={styles.noticeFooter}>
                📌 <strong>Note:</strong> The adjusted amount of{' '}
                <span style={styles.inlineHighlight}>Rs. {report.adjustedMonthlyDeduction.toLocaleString()}</span>{' '}
                will be charged for the remaining{' '}
                <span style={styles.inlineHighlight}>{report.remainingMonthsInFY} month{report.remainingMonthsInFY !== 1 ? 's' : ''}</span>{' '}
                of the current financial year ({report.financialYear}).{' '}
                {report.hasPriorEmployer && 'This adjusted amount considers the cumulative tax already paid by the employee during this financial year. '}
                <strong>From the next financial year, no adjustments will be applied</strong> — the standard monthly deduction of{' '}
                <span style={styles.inlineHighlight}>Rs. {(report.nextFyStandardMonthly || Math.round((report.fullYearAnnualTax || report.annualTaxLiability) / 12)).toLocaleString()}</span>{' '}
                will be charged fresh based on the employee's current salary, without considering any prior cumulative payments.
              </p>
            )}
          </div>

          {/* Tax Slab Breakdown */}
          <h4 style={styles.sectionTitle}>Tax Slab Breakdown</h4>
          <table style={styles.table}>
            <thead>
              <tr style={styles.thead}>
                <th style={th}>Income Band</th>
                <th style={th}>Rate</th>
                <th style={{ ...th, textAlign: 'right' }}>Taxable Amount</th>
                <th style={{ ...th, textAlign: 'right' }}>Tax</th>
              </tr>
            </thead>
            <tbody>
              {report.slabs.map((s, i) => (
                <tr key={i} style={{ background: i % 2 === 0 ? '#fff' : '#f8f9fa', borderBottom: '1px solid #eee' }}>
                  <td style={td}>{s.band}</td>
                  <td style={{ ...td, fontWeight: 600, color: s.rate === '0%' ? '#27ae60' : '#003366' }}>{s.rate}</td>
                  <td style={{ ...td, textAlign: 'right' }}>Rs. {s.taxableAmount.toLocaleString()}</td>
                  <td style={{ ...td, textAlign: 'right', fontWeight: 600 }}>Rs. {s.tax.toLocaleString()}</td>
                </tr>
              ))}
              <tr style={{ background: '#003366', color: '#fff' }}>
                <td colSpan={3} style={{ ...td, fontWeight: 700 }}>Total Annual Tax Liability</td>
                <td style={{ ...td, textAlign: 'right', fontWeight: 700 }}>Rs. {report.annualTaxLiability.toLocaleString()}</td>
              </tr>
            </tbody>
          </table>

          {/* Monthly History */}
          <h4 style={styles.sectionTitle}>Monthly PAYE Tax Deduction History (All Employers)</h4>
          <div style={{ maxHeight: '300px', overflowY: 'auto', border: '1px solid #eee', borderRadius: '6px' }}>
            <table style={{ ...styles.table, marginBottom: 0 }}>
              <thead>
                <tr style={styles.thead}>
                  <th style={th}>Month</th>
                  <th style={th}>Employer</th>
                  <th style={{ ...th, textAlign: 'right' }}>Gross Salary</th>
                  <th style={{ ...th, textAlign: 'right' }}>PAYE Deduction</th>
                  <th style={{ ...th, textAlign: 'right' }}>Cumulative Total</th>
                </tr>
              </thead>
              <tbody>
                {report.monthlyHistory.map((m: any, i: number) => {
                  const isPrior = m.trigger === 'PriorEmployer';
                  return (
                    <tr key={i} style={{ background: isPrior ? '#fff8e1' : (i % 2 === 0 ? '#fff' : '#f8f9fa'), borderBottom: '1px solid #eee' }}>
                      <td style={td}>{m.monthLabel}</td>
                      <td style={td}>
                        <span style={{ fontSize: '0.82rem' }}>{m.employerName || report.employerName}</span>
                        {isPrior && <span style={{ marginLeft: '6px', background: '#ffc107', color: '#333', padding: '1px 6px', borderRadius: '8px', fontSize: '0.7rem', fontWeight: 600 }}>Prior</span>}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>Rs. {m.grossIncome.toLocaleString()}</td>
                      <td style={{ ...td, textAlign: 'right', fontWeight: 600, color: isPrior ? '#e67e22' : '#003366' }}>Rs. {m.deductionAmount.toLocaleString()}</td>
                      <td style={{ ...td, textAlign: 'right', color: '#27ae60' }}>Rs. {m.cumulativeAtMonth.toLocaleString()}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Footer note */}
          <p style={{ fontSize: '0.78rem', color: '#888', marginTop: '1rem', textAlign: 'center' }}>
            Generated by PAYE Tax Easy | Inland Revenue (Amendment) Act No. 02 of 2025 | {new Date(report.generatedAt).toLocaleString()}
          </p>
        </div>
      </div>
    </div>
  );
}

function InfoBox({ label, value }: { label: string; value: string }) {
  return (
    <div style={{ background: '#f8f9fa', padding: '8px 12px', borderRadius: '6px' }}>
      <div style={{ fontSize: '0.75rem', color: '#666', marginBottom: '2px' }}>{label}</div>
      <div style={{ fontWeight: 600, color: '#333', fontSize: '0.9rem' }}>{value}</div>
    </div>
  );
}

function StepBadge({ n, small }: { n: number; small?: boolean }) {
  const size = small ? '16px' : '22px';
  const font = small ? '0.65rem' : '0.75rem';
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
      width: size, height: size, borderRadius: '50%',
      background: '#003366', color: '#fff', fontSize: font, fontWeight: 700,
      lineHeight: 1, verticalAlign: 'middle', marginRight: small ? '0' : '6px'
    }}>{n}</span>
  );
}

function TaxCard({ label, value, color, icon }: { label: string; value: number; color: string; icon: string }) {
  return (
    <div style={{ background: '#fff', border: `2px solid ${color}`, borderRadius: '8px', padding: '12px 16px', flex: 1, minWidth: '140px', textAlign: 'center' }}>
      <div style={{ fontSize: '1.3rem' }}>{icon}</div>
      <div style={{ fontSize: '0.75rem', color: '#666', margin: '4px 0' }}>{label}</div>
      <div style={{ fontWeight: 700, color, fontSize: '1rem' }}>Rs. {value.toLocaleString()}</div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  overlay: { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 2000, padding: '1rem' },
  modal: { background: '#fff', borderRadius: '12px', width: '900px', maxWidth: '100%', maxHeight: '90vh', display: 'flex', flexDirection: 'column', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', padding: '1.5rem', borderBottom: '2px solid #003366', background: '#f0f4f8', borderRadius: '12px 12px 0 0' },
  title: { margin: 0, color: '#003366', fontSize: '1.3rem' },
  subtitle: { margin: '4px 0 0', color: '#666', fontSize: '0.85rem' },
  btnPdf: { padding: '8px 18px', background: '#003366', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600, fontSize: '0.9rem' },
  btnClose: { padding: '8px 14px', background: '#e74c3c', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 700, fontSize: '1rem' },
  body: { padding: '1.5rem', overflowY: 'auto', flex: 1 },
  infoGrid: { display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.75rem', marginBottom: '1.5rem' },
  summaryCards: { display: 'flex', gap: '0.75rem', marginBottom: '1.5rem', flexWrap: 'wrap' },
  progressSection: { background: '#f8f9fa', padding: '1rem', borderRadius: '8px', marginBottom: '1.5rem' },
  progressBg: { background: '#e9ecef', borderRadius: '10px', height: '16px', overflow: 'hidden' },
  progressFill: { background: 'linear-gradient(90deg, #27ae60, #003366)', height: '100%', borderRadius: '10px', transition: 'width 0.5s ease' },
  priorBox: { background: '#e8f8ff', border: '1px solid #17a2b8', borderRadius: '8px', padding: '12px 16px', marginBottom: '1.5rem', fontSize: '0.9rem' },
  sectionTitle: { margin: '0 0 8px', color: '#003366', fontSize: '1rem', borderBottom: '1px solid #eee', paddingBottom: '6px' },
  // Notice box
  noticeBox: { background: '#fffbea', border: '2px solid #f59e0b', borderRadius: '10px', padding: '1.2rem', marginBottom: '1.5rem' },
  noticeHeader: { display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.75rem' },
  noticeIcon: { fontSize: '1.3rem' },
  noticeTitle: { fontWeight: 700, color: '#92400e', fontSize: '1rem' },
  noticeText: { color: '#78350f', fontSize: '0.9rem', margin: '0 0 1rem', lineHeight: 1.6 },
  noticeHighlights: { display: 'flex', gap: '0', background: '#fff', borderRadius: '8px', border: '1px solid #fcd34d', overflow: 'hidden', marginBottom: '1rem' },
  highlightItem: { flex: 1, padding: '0.75rem 1rem', display: 'flex', flexDirection: 'column' as const, gap: '4px' },
  highlightDivider: { width: '1px', background: '#fcd34d' },
  highlightLabel: { fontSize: '0.75rem', color: '#92400e', fontWeight: 600 },
  highlightValue: { fontSize: '1.1rem', fontWeight: 700 },
  noticeFooter: { color: '#78350f', fontSize: '0.85rem', margin: 0, lineHeight: 1.7, background: '#fef3c7', padding: '10px 14px', borderRadius: '6px' },
  inlineHighlight: { background: '#fcd34d', padding: '1px 6px', borderRadius: '4px', fontWeight: 700, color: '#92400e' },
  table: { width: '100%', borderCollapse: 'collapse', marginBottom: '1.5rem' },
  thead: { background: '#003366', color: '#fff' },
};

const th: React.CSSProperties = { padding: '10px 12px', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem' };
const td: React.CSSProperties = { padding: '8px 12px', fontSize: '0.88rem' };
