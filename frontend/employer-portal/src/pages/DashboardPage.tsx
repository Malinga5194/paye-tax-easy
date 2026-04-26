import { useState, useEffect } from 'react';
import { apiClient } from '../api/client';
import { DeductionSummary } from '../types';
import SalaryEntryForm from '../components/SalaryEntryForm';
import IrdFetchButton from '../components/IrdFetchButton';
import Navbar from '../components/Navbar';

const CURRENT_FY = '2025-26';

export default function DashboardPage() {
  const [summary, setSummary] = useState<DeductionSummary[]>([]);
  const [period, setPeriod] = useState(new Date().toISOString().slice(0, 7));
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [message, setMessage] = useState('');

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

  return (
    <div style={{ fontFamily: 'sans-serif', background: '#f8f9fa', minHeight: '100vh' }}>
      <Navbar title="Employer Portal" color="#003366" />
      <div style={styles.page}>
      <header style={styles.header}>
        <h1 style={styles.title}>PAYE Tax Easy — Employer Dashboard</h1>
        <span style={styles.fy}>FY {CURRENT_FY}</span>
      </header>

      <div style={styles.toolbar}>
        <label style={styles.label}>Period:</label>
        <input type="month" value={period} onChange={e => setPeriod(e.target.value)} style={styles.input} />
        <button style={styles.btnPrimary} onClick={() => setShowForm(true)}>+ Add Employee Salary</button>
        <button style={styles.btnSecondary} onClick={handleFinalize}>Finalize Period</button>
        <button style={styles.btnSuccess} onClick={handleSubmit}>Submit to IRD</button>
      </div>

      {message && <div style={styles.message}>{message}</div>}

      {showForm && (
        <SalaryEntryForm financialYear={CURRENT_FY}
          onSuccess={() => { setShowForm(false); loadSummary(); }}
          onCancel={() => setShowForm(false)} />
      )}

      <table style={styles.table}>
        <thead>
          <tr style={styles.thead}>
            <th>Employee TIN</th><th>Name</th><th>Gross Salary</th>
            <th>Monthly Deduction</th><th>YTD Deduction</th>
            <th>Prior Employer</th><th>Status</th><th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr><td colSpan={8} style={{ textAlign: 'center', padding: '2rem' }}>Loading...</td></tr>
          ) : summary.length === 0 ? (
            <tr><td colSpan={8} style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>No employees found for this period.</td></tr>
          ) : summary.map(row => (
            <tr key={row.employeeTIN} style={styles.tr}>
              <td style={styles.td}>{row.employeeTIN}</td>
              <td style={styles.td}>{row.employeeName}</td>
              <td style={styles.td}>Rs. {row.grossSalary.toLocaleString()}</td>
              <td style={styles.td}>Rs. {row.monthlyDeduction.toLocaleString()}</td>
              <td style={styles.td}>Rs. {row.yearToDateCumulativeDeduction.toLocaleString()}</td>
              <td style={styles.td}>{row.hasPriorEmployerData ? '✓ Yes' : '—'}</td>
              <td style={styles.td}>
                {row.isOverpaid
                  ? <span style={{ color: '#e67e22' }}>⚠ Overpaid</span>
                  : <span style={{ color: '#27ae60' }}>✓ OK</span>}
              </td>
              <td style={styles.td}>
                <IrdFetchButton employeeTin={row.employeeTIN} financialYear={CURRENT_FY}
                  onFetched={loadSummary} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { fontFamily: 'sans-serif', padding: '1.5rem', background: '#f8f9fa', minHeight: '100vh' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' },
  title: { color: '#003366', margin: 0 },
  fy: { background: '#003366', color: '#fff', padding: '4px 12px', borderRadius: '4px' },
  toolbar: { display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap' },
  label: { fontWeight: 600 },
  input: { padding: '6px 10px', border: '1px solid #ccc', borderRadius: '4px' },
  btnPrimary: { padding: '8px 16px', background: '#003366', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  btnSecondary: { padding: '8px 16px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  btnSuccess: { padding: '8px 16px', background: '#27ae60', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  message: { background: '#d4edda', color: '#155724', padding: '10px', borderRadius: '4px', marginBottom: '1rem' },
  table: { width: '100%', borderCollapse: 'collapse', background: '#fff', borderRadius: '8px', overflow: 'hidden', boxShadow: '0 1px 4px rgba(0,0,0,0.1)' },
  thead: { background: '#003366', color: '#fff' },
  tr: { borderBottom: '1px solid #eee' },
  td: { padding: '10px 12px', fontSize: '0.9rem' },
};
