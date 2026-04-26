import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { getToken } from '../App';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const HOME_URL = 'http://localhost:5173';

interface ComplianceReport {
  financialYear: string;
  totalRegisteredEmployers: number;
  submittedCount: number;
  notSubmittedCount: number;
  totalPAYECollected: number;
}

export default function CompliancePage() {
  const [fy, setFy] = useState('2025-26');
  const [report, setReport] = useState<ComplianceReport | null>(null);
  const [search, setSearch] = useState('');
  const [employer, setEmployer] = useState<any>(null);
  const navigate = useNavigate();
  const headers = { Authorization: `Bearer ${getToken()}` };

  useEffect(() => {
    axios.get(`${API}/ird-dashboard/compliance/${fy}`, { headers })
      .then(r => setReport(r.data))
      .catch(() => setReport(null));
  }, [fy]);

  const searchEmployer = async () => {
    try {
      const res = await axios.get(`${API}/ird-dashboard/employers/${search}`, { headers });
      setEmployer(res.data);
    } catch {
      setEmployer(null);
      alert('Employer not found.');
    }
  };

  const exportCsv = async () => {
    const res = await axios.get(`${API}/ird-dashboard/compliance/${fy}/export`, {
      headers, responseType: 'blob'
    });
    const url = URL.createObjectURL(new Blob([res.data]));
    const a = document.createElement('a');
    a.href = url; a.download = `compliance_${fy}.csv`; a.click();
  };

  return (
    <div style={{ fontFamily: 'sans-serif', background: '#f8f9fa', minHeight: '100vh' }}>
      <nav style={{ background: '#7b1fa2', display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.75rem 1.5rem', color: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.15)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <a href={HOME_URL} style={{ color: '#fff', textDecoration: 'none', fontWeight: 700, fontSize: '1.1rem' }}>🏛️ PAYE Tax Easy</a>
          <span style={{ opacity: 0.4 }}>|</span>
          <span style={{ opacity: 0.85, fontSize: '0.95rem' }}>IRD Dashboard</span>
        </div>
        <a href={HOME_URL} style={{ padding: '5px 14px', background: 'rgba(255,255,255,0.2)', color: '#fff', border: '1px solid rgba(255,255,255,0.4)', borderRadius: '4px', textDecoration: 'none', fontSize: '0.85rem', fontWeight: 600 }}>← Home</a>
      </nav>
      <div style={styles.page}>
      <header style={styles.header}>
        <h1 style={styles.title}>IRD Compliance Dashboard</h1>
        <div style={{ display: 'flex', gap: '1rem' }}>
          <button style={styles.btnSecondary} onClick={() => navigate('/audit-logs')}>Audit Logs</button>
          <button style={styles.btnExport} onClick={exportCsv}>Export CSV</button>
        </div>
      </header>

      <div style={styles.toolbar}>
        <label style={styles.label}>Financial Year:</label>
        <select value={fy} onChange={e => setFy(e.target.value)} style={styles.select}>
          <option value="2025-26">2025-26</option>
          <option value="2024-25">2024-25</option>
        </select>
      </div>

      {report && (
        <div style={styles.cards}>
          <StatCard label="Total Employers" value={report.totalRegisteredEmployers} color="#003366" />
          <StatCard label="Submitted" value={report.submittedCount} color="#27ae60" />
          <StatCard label="Not Submitted" value={report.notSubmittedCount} color="#e74c3c" />
          <StatCard label="Total PAYE Collected" value={`Rs. ${report.totalPAYECollected.toLocaleString()}`} color="#7b1fa2" />
        </div>
      )}

      <div style={styles.searchBar}>
        <input style={styles.input} placeholder="Search by Registration Number..."
          value={search} onChange={e => setSearch(e.target.value)} />
        <button style={styles.btnSearch} onClick={searchEmployer}>Search</button>
      </div>

      {employer && (
        <div style={styles.employerCard}>
          <h3>{employer.organizationName}</h3>
          <p>TIN: {employer.tin} | Reg: {employer.registrationNumber}</p>
          <p>Email: {employer.contactEmail}</p>
        </div>
      )}
    </div>
    </div>
  );
}

function StatCard({ label, value, color }: { label: string; value: any; color: string }) {
  return (
    <div style={{ background: '#fff', borderRadius: '8px', padding: '1.5rem', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', borderTop: `4px solid ${color}`, minWidth: '180px' }}>
      <div style={{ fontSize: '0.85rem', color: '#666', marginBottom: '0.5rem' }}>{label}</div>
      <div style={{ fontSize: '1.8rem', fontWeight: 700, color }}>{value}</div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { fontFamily: 'sans-serif', padding: '1.5rem', background: '#f8f9fa', minHeight: '100vh' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' },
  title: { color: '#7b1fa2', margin: 0 },
  toolbar: { display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '1.5rem' },
  label: { fontWeight: 600 },
  select: { padding: '6px 10px', border: '1px solid #ccc', borderRadius: '4px' },
  cards: { display: 'flex', gap: '1rem', flexWrap: 'wrap', marginBottom: '1.5rem' },
  searchBar: { display: 'flex', gap: '1rem', marginBottom: '1rem' },
  input: { flex: 1, padding: '8px 12px', border: '1px solid #ccc', borderRadius: '4px', fontSize: '1rem' },
  btnSearch: { padding: '8px 20px', background: '#7b1fa2', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  btnSecondary: { padding: '8px 16px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  btnExport: { padding: '8px 16px', background: '#27ae60', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  employerCard: { background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 4px rgba(0,0,0,0.1)' },
};
