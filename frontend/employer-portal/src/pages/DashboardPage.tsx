import { useState, useEffect } from 'react';
import { apiClient } from '../api/client';
import SalaryEntryForm from '../components/SalaryEntryForm';
import TaxReportModal from '../components/TaxReportModal';
import { Header, Footer } from '../components/Layout';

const CURRENT_FY = '2025-26';

// Financial year months for the picker
const FY_MONTHS = [
  { value: '2025-04', label: 'April 2025' },
  { value: '2025-05', label: 'May 2025' },
  { value: '2025-06', label: 'June 2025' },
  { value: '2025-07', label: 'July 2025' },
  { value: '2025-08', label: 'August 2025' },
  { value: '2025-09', label: 'September 2025' },
  { value: '2025-10', label: 'October 2025' },
  { value: '2025-11', label: 'November 2025' },
  { value: '2025-12', label: 'December 2025' },
  { value: '2026-01', label: 'January 2026' },
  { value: '2026-02', label: 'February 2026' },
  { value: '2026-03', label: 'March 2026' },
];

interface DeductionSummary {
  employeeTIN: string;
  employeeName: string;
  grossSalary: number;
  monthlyDeduction: number;
  yearToDateCumulativeDeduction: number;
  hasPriorEmployerData: boolean;
  isOverpaid: boolean;
  annualTaxLiability: number;
  remainingTaxForYear: number;
  remainingMonthsInFY: number;
  scenario: string;
}

const SCENARIO_COLORS: Record<string, string> = {
  'Resigned': '#e74c3c',
  'Mid-Year Joiner': '#17a2b8',
  'Salary Increased': '#27ae60',
  'Salary Decreased': '#e67e22',
  'Stable': '#6c757d',
};

export default function DashboardPage() {
  const [summary, setSummary] = useState<DeductionSummary[]>([]);
  const [period, setPeriod] = useState('2026-03');
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [message, setMessage] = useState('');
  const [selectedReport, setSelectedReport] = useState<{ data: any; tin: string } | null>(null);

  const loadSummary = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(`/payroll/summary/${period}`);
      setSummary(res.data);
    } catch {
      setSummary([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadSummary(); }, [period]);

  const handleFinalize = async () => {
    if (!confirm(`Finalize and lock payroll for ${period}?`)) return;
    await apiClient.post(`/payroll/summary/${period}/finalize`);
    setMessage(`Period ${period} finalized.`);
    loadSummary();
  };

  const handleSubmit = async () => {
    if (summary.length === 0) { setMessage('No employees to submit.'); return; }
    const lines = summary.map(s => ({
      employeeTIN: s.employeeTIN,
      grossSalary: s.grossSalary,
      monthlyDeduction: s.monthlyDeduction,
      month: parseInt(period.split('-')[1]),
      year: parseInt(period.split('-')[0]),
    }));
    try {
      const res = await apiClient.post('/payroll/submissions', {
        financialYear: CURRENT_FY,
        period,
        lines,
      });
      setMessage(`Submitted! IRD Ref: ${res.data.irdReferenceNumber}`);
    } catch (err: any) {
      setMessage(err.response?.data?.message || 'Submission failed.');
    }
  };

  const selectedLabel = FY_MONTHS.find(m => m.value === period)?.label || period;
  const totalAnnualTax = summary.reduce((s, e) => s + e.annualTaxLiability, 0);
  const totalYTD = summary.reduce((s, e) => s + e.yearToDateCumulativeDeduction, 0);
  const totalRemaining = summary.reduce((s, e) => s + e.remainingTaxForYear, 0);

  return (
    <div style={{ fontFamily: "'Segoe UI', sans-serif", background: '#f0f4f8', minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Header portalName="Employer Portal" portalIcon="🏢" color="#003366" />

      <div style={styles.page}>
        {/* Page Header */}
        <div style={styles.pageHeader}>
          <div>
            <h1 style={styles.title}>PAYE Deduction Dashboard</h1>
            <p style={styles.subtitle}>Financial Year: {CURRENT_FY} &nbsp;|&nbsp; Select a month to view cumulative and remaining tax</p>
          </div>
          <div style={styles.actions}>
            <button style={styles.btnPrimary} onClick={() => setShowForm(true)}>+ Add Employee</button>
            <button style={styles.btnSecondary} onClick={handleFinalize}>Finalize Period</button>
            <button style={styles.btnSuccess} onClick={handleSubmit}>Submit to IRD</button>
          </div>
        </div>

        {message && <div style={styles.message}>✓ {message}</div>}

        {/* Period Picker */}
        <div style={styles.pickerBar}>
          <label style={styles.pickerLabel}>📅 Select Period:</label>
          <select style={styles.picker} value={period} onChange={e => setPeriod(e.target.value)}>
            {FY_MONTHS.map(m => (
              <option key={m.value} value={m.value}>{m.label}</option>
            ))}
          </select>
          <span style={styles.pickerInfo}>Showing data as of <strong>{selectedLabel}</strong></span>
        </div>

        {/* Summary Cards */}
        <div style={styles.cards}>
          <SummaryCard label="Total Employees" value={summary.length} color="#003366" icon="👥" />
          <SummaryCard label="Total Annual Tax Liability" value={`Rs. ${totalAnnualTax.toLocaleString()}`} color="#17a2b8" icon="📊" />
          <SummaryCard label="Cumulative Tax Paid (YTD)" value={`Rs. ${totalYTD.toLocaleString()}`} color="#27ae60" icon="✓" />
          <SummaryCard label="Remaining Tax for FY" value={`Rs. ${totalRemaining.toLocaleString()}`} color="#e67e22" icon="⏳" />
        </div>

        {/* Employee Table */}
        <div style={styles.tableWrapper}>
          <table style={styles.table}>
            <thead>
              <tr style={styles.thead}>
                <th style={th}>Employee</th>
                <th style={th}>TIN</th>
                <th style={th}>Gross Salary</th>
                <th style={th}>Scenario</th>
                <th style={th}>Annual Tax Liability</th>
                <th style={th}>Cumulative Tax Paid</th>
                <th style={th}>Monthly Deduction</th>
                <th style={th}>Remaining Tax</th>
                <th style={th}>Remaining Months</th>
                <th style={th}>Status</th>
                <th style={th}>IRD Data</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={11} style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>Loading...</td></tr>
              ) : summary.length === 0 ? (
                <tr><td colSpan={11} style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>No employees found for this period.</td></tr>
              ) : summary.map(row => (
                <tr key={row.employeeTIN} style={styles.tr}>
                  <td style={td}><strong>{row.employeeName}</strong></td>
                  <td style={td}><code style={{ fontSize: '0.8rem' }}>{row.employeeTIN}</code></td>
                  <td style={td}>Rs. {row.grossSalary.toLocaleString()}</td>
                  <td style={td}>
                    <span style={{ background: (SCENARIO_COLORS[row.scenario] || '#6c757d') + '20', color: SCENARIO_COLORS[row.scenario] || '#6c757d', padding: '3px 10px', borderRadius: '12px', fontSize: '0.78rem', fontWeight: 600, whiteSpace: 'nowrap' }}>
                      {row.scenario}
                    </span>
                  </td>
                  <td style={{ ...td, fontWeight: 600 }}>Rs. {row.annualTaxLiability.toLocaleString()}</td>
                  <td style={{ ...td, color: '#27ae60', fontWeight: 600 }}>Rs. {row.yearToDateCumulativeDeduction.toLocaleString()}</td>
                  <td style={{ ...td, color: '#003366', fontWeight: 600 }}>Rs. {row.monthlyDeduction.toLocaleString()}</td>
                  <td style={{ ...td, color: '#e67e22', fontWeight: 600 }}>Rs. {row.remainingTaxForYear.toLocaleString()}</td>
                  <td style={{ ...td, textAlign: 'center' }}>{row.remainingMonthsInFY}</td>
                  <td style={td}>
                    {row.isOverpaid
                      ? <span style={{ color: '#e67e22', fontWeight: 600 }}>⚠ Overpaid</span>
                      : row.hasPriorEmployerData
                        ? <span style={{ color: '#17a2b8', fontWeight: 600 }}>✓ Adjusted</span>
                        : <span style={{ color: '#27ae60', fontWeight: 600 }}>✓ OK</span>}
                  </td>
                  <td style={td}>
                    <button
                      type="button"
                      style={{ padding: '5px 12px', background: '#17a2b8', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.82rem', fontWeight: 600 }}
                      onClick={async (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        try {
                          await apiClient.get(`/ird/cumulative/${row.employeeTIN}/${CURRENT_FY}`);
                          const res = await apiClient.get(`/tax-report/${row.employeeTIN}/${CURRENT_FY}/${period}`);
                          setSelectedReport({ data: res.data, tin: row.employeeTIN });
                        } catch (err: any) {
                          alert(`Error: ${err.response?.data?.message || err.message}`);
                        }
                      }}
                    >
                      📋 Fetch IRD & Report
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showForm && (
        <SalaryEntryForm financialYear={CURRENT_FY}
          onSuccess={() => { setShowForm(false); loadSummary(); }}
          onCancel={() => setShowForm(false)} />
      )}

      {selectedReport && (
        <TaxReportModal
          report={selectedReport.data}
          period={period}
          onClose={() => setSelectedReport(null)}
        />
      )}

      <Footer />
    </div>
  );
}

function SummaryCard({ label, value, color, icon }: { label: string; value: any; color: string; icon: string }) {
  return (
    <div style={{ background: '#fff', borderRadius: '8px', padding: '1.2rem 1.5rem', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', borderLeft: `5px solid ${color}`, flex: 1, minWidth: '200px' }}>
      <div style={{ fontSize: '1.5rem', marginBottom: '0.3rem' }}>{icon}</div>
      <div style={{ fontSize: '0.82rem', color: '#666', marginBottom: '0.3rem' }}>{label}</div>
      <div style={{ fontSize: '1.3rem', fontWeight: 700, color }}>{value}</div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { padding: '1.5rem', flex: 1 },
  pageHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' },
  title: { margin: 0, color: '#003366', fontSize: '1.5rem' },
  subtitle: { margin: '4px 0 0', color: '#666', fontSize: '0.9rem' },
  actions: { display: 'flex', gap: '0.75rem', flexWrap: 'wrap' },
  btnPrimary: { padding: '8px 16px', background: '#003366', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 },
  btnSecondary: { padding: '8px 16px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 },
  btnSuccess: { padding: '8px 16px', background: '#27ae60', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 },
  message: { background: '#d4edda', color: '#155724', padding: '10px 16px', borderRadius: '6px', marginBottom: '1rem' },
  pickerBar: { display: 'flex', alignItems: 'center', gap: '1rem', background: '#fff', padding: '1rem 1.5rem', borderRadius: '8px', boxShadow: '0 1px 4px rgba(0,0,0,0.08)', marginBottom: '1.5rem', flexWrap: 'wrap' },
  pickerLabel: { fontWeight: 700, color: '#003366', fontSize: '1rem' },
  picker: { padding: '8px 14px', border: '2px solid #003366', borderRadius: '6px', fontSize: '1rem', color: '#003366', fontWeight: 600, cursor: 'pointer', background: '#f0f4f8' },
  pickerInfo: { color: '#555', fontSize: '0.9rem' },
  cards: { display: 'flex', gap: '1rem', marginBottom: '1.5rem', flexWrap: 'wrap' },
  tableWrapper: { background: '#fff', borderRadius: '8px', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', overflow: 'auto' },
  table: { width: '100%', borderCollapse: 'collapse', minWidth: '1100px' },
  thead: { background: '#003366', color: '#fff' },
  tr: { borderBottom: '1px solid #eee' },
};

const th: React.CSSProperties = { padding: '12px 14px', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem', whiteSpace: 'nowrap' };
const td: React.CSSProperties = { padding: '10px 14px', fontSize: '0.88rem' };
