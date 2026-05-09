import { useState, useEffect } from 'react';
import axios from 'axios';
import { getToken } from '../App';
import TaxCalculator from '../components/TaxCalculator';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const HOME_URL = 'http://localhost:5173';

interface DeductionEntry {
  employerName: string;
  deductionAmount: number;
  deductionDate: string;
  month: number;
  year: number;
}

interface DeductionHistory {
  employeeTIN: string;
  employeeFullName: string;
  financialYear: string;
  entries: DeductionEntry[];
  cumulativeTotal: number;
}

export default function HistoryPage() {
  const [fy, setFy] = useState('2025-26');
  const [history, setHistory] = useState<DeductionHistory | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [downloading, setDownloading] = useState(false);
  const [showCalc, setShowCalc] = useState(false);

  const headers = { Authorization: `Bearer ${getToken()}` };

  const loadHistory = async () => {
    setLoading(true); setError('');
    try {
      const res = await axios.get(`${API}/employee/history/${fy}`, { headers });
      setHistory(res.data);
    } catch {
      setError('No deduction records found for this financial year.');
      setHistory(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadHistory(); }, [fy]);

  const downloadPdf = async () => {
    setDownloading(true);
    try {
      const res = await axios.get(`${API}/employee/history/${fy}/pdf`, {
        headers, responseType: 'blob'
      });
      const url = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
      const a = document.createElement('a');
      a.href = url; a.download = `PAYE_History_${fy}.pdf`; a.click();
      URL.revokeObjectURL(url);
    } catch {
      alert('No data available for export.');
    } finally {
      setDownloading(false);
    }
  };

  return (
    <div style={{ fontFamily: 'sans-serif', background: '#f8f9fa', minHeight: '100vh' }}>
      <nav style={{ background: '#1a6b3c', display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.75rem 1.5rem', color: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.15)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <a href={HOME_URL} style={{ color: '#fff', textDecoration: 'none', fontWeight: 700, fontSize: '1.1rem' }}>🏛️ PAYE Tax Easy</a>
          <span style={{ opacity: 0.4 }}>|</span>
          <span style={{ opacity: 0.85, fontSize: '0.95rem' }}>Employee Portal</span>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <a href={HOME_URL} style={{ padding: '5px 14px', background: 'rgba(255,255,255,0.2)', color: '#fff', border: '1px solid rgba(255,255,255,0.4)', borderRadius: '4px', textDecoration: 'none', fontSize: '0.85rem', fontWeight: 600 }}>← Home</a>
          <button onClick={() => { sessionStorage.removeItem('token'); window.location.href = '/login'; }} style={{ padding: '5px 14px', background: '#e74c3c', color: '#fff', border: 'none', borderRadius: '4px', fontSize: '0.85rem', fontWeight: 600, cursor: 'pointer' }}>⏻ Logout</button>
        </div>
      </nav>
      <div style={styles.page}>
      <header style={styles.header}>
        <h1 style={styles.title}>My PAYE Tax History</h1>
        {history && <span style={styles.name}>{history.employeeFullName} | TIN: {history.employeeTIN}</span>}
      </header>

      <div style={styles.toolbar}>
        <label style={styles.label}>Financial Year:</label>
        <select value={fy} onChange={e => setFy(e.target.value)} style={styles.select}>
          <option value="2025-26">2025-26</option>
          <option value="2024-25">2024-25</option>
          <option value="2023-24">2023-24</option>
        </select>
        <button style={styles.btnPdf} onClick={downloadPdf} disabled={downloading || !history}>
          {downloading ? 'Generating...' : '⬇ Download PDF'}
        </button>
        <button style={{ ...styles.btnPdf, background: '#17a2b8' }} onClick={() => setShowCalc(true)}>
          🧮 Tax Calculator
        </button>
      </div>

      {error && <div style={styles.error}>{error}</div>}

      {loading ? <p>Loading...</p> : history && (
        <>
          <table style={styles.table}>
            <thead>
              <tr style={styles.thead}>
                <th>Employer</th><th>Period</th><th>Deduction (Rs.)</th>
              </tr>
            </thead>
            <tbody>
              {history.entries.map((e, i) => (
                <tr key={i} style={styles.tr}>
                  <td style={styles.td}>{e.employerName}</td>
                  <td style={styles.td}>{String(e.month).padStart(2, '0')}/{e.year}</td>
                  <td style={styles.td}>Rs. {e.deductionAmount.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr style={{ background: '#e8f5e9' }}>
                <td colSpan={2} style={{ ...styles.td, fontWeight: 700 }}>Total PAYE Deducted</td>
                <td style={{ ...styles.td, fontWeight: 700 }}>Rs. {history.cumulativeTotal.toLocaleString()}</td>
              </tr>
            </tfoot>
          </table>
        </>
      )}
    </div>
    {showCalc && <TaxCalculator onClose={() => setShowCalc(false)} color="#1a6b3c" />}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { fontFamily: 'sans-serif', padding: '1.5rem', background: '#f8f9fa', minHeight: '100vh' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' },
  title: { color: '#1a6b3c', margin: 0 },
  name: { color: '#555', fontSize: '0.95rem' },
  toolbar: { display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '1rem' },
  label: { fontWeight: 600 },
  select: { padding: '6px 10px', border: '1px solid #ccc', borderRadius: '4px' },
  btnPdf: { padding: '8px 16px', background: '#1a6b3c', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  error: { background: '#fdecea', color: '#c0392b', padding: '10px', borderRadius: '4px', marginBottom: '1rem' },
  table: { width: '100%', borderCollapse: 'collapse', background: '#fff', borderRadius: '8px', overflow: 'hidden', boxShadow: '0 1px 4px rgba(0,0,0,0.1)' },
  thead: { background: '#1a6b3c', color: '#fff' },
  tr: { borderBottom: '1px solid #eee' },
  td: { padding: '10px 12px' },
};
