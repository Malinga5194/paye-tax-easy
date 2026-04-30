import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { getToken } from '../App';
import { Header, Footer } from '../components/Layout';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const COLOR = '#7b1fa2';

interface EmployeeResult {
  employeeTIN: string;
  employeeName: string;
  currentEmployer: string;
  grossMonthlySalary: number;
  projectedAnnualIncome: number;
  taxRelief: number;
  annualTaxLiability: number;
  standardMonthly: number;
  adjustedMonthly: number;
  priorEmployerIncome: number;
  priorEmployerDeduction: number;
  currentEmployerYTD: number;
  totalTaxPaid: number;
  remainingTaxForYear: number;
  remainingMonthsInFY: number;
  withoutSystemTotal: number;
  withSystemTotal: number;
  savingsPerMonth: number;
  joiningDate: string;
  hasPriorEmployer: boolean;
  financialYear: string;
}

export function validateTin(value: string): string | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) return 'TIN is required';
  if (!/^\d{9}$/.test(trimmed)) return 'A valid 9-digit TIN is required';
  return null;
}

export default function EmployeeSearchPage() {
  const [fy, setFy] = useState('2025-26');
  const navigate = useNavigate();
  const headers = { Authorization: `Bearer ${getToken()}` };

  const [tin, setTin] = useState('');
  const [error, setError] = useState('');
  const [result, setResult] = useState<EmployeeResult | null>(null);
  const [notFound, setNotFound] = useState('');
  const [loading, setLoading] = useState(false);

  const handleTinChange = (val: string) => {
    setTin(val.replace(/\D/g, ''));
    setError('');
  };

  const search = async () => {
    const err = validateTin(tin);
    if (err) { setError(err); return; }
    setLoading(true);
    setResult(null);
    setNotFound('');
    setError('');
    try {
      const res = await axios.get(`${API}/ird-dashboard/search/employee/${tin.trim()}?financialYear=${fy}`, { headers });
      setResult(res.data);
    } catch (e: any) {
      if (e.response?.status === 404) {
        setNotFound(`No employee found with TIN ${tin.trim()}`);
      } else {
        setError('Search failed. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const downloadPdf = async () => {
    if (!result) return;
    try {
      const res = await axios.get(
        `${API}/ird-dashboard/search/employee/${result.employeeTIN}/pdf?financialYear=${fy}`,
        { headers, responseType: 'blob' }
      );
      const url = URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `IRD_Employee_Report_${result.employeeTIN}_${fy}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      alert('Failed to download PDF report.');
    }
  };

  return (
    <div style={{ fontFamily: "'Segoe UI', sans-serif", background: '#f0f4f8', minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Header portalName="IRD Dashboard" portalIcon="🏛️" color={COLOR} />

      <div style={{ padding: '1.5rem', flex: 1, maxWidth: '800px', margin: '0 auto', width: '100%' }}>
        {/* Page Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h1 style={{ margin: 0, color: COLOR }}>👤 Employee Search</h1>
            <p style={{ margin: '4px 0 0', color: '#666', fontSize: '0.9rem' }}>Search employees by Taxpayer Identification Number</p>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <select value={fy} onChange={e => setFy(e.target.value)} style={{ padding: '8px 14px', border: `2px solid ${COLOR}`, borderRadius: '6px', fontWeight: 600, color: COLOR }}>
              <option value="2025-26">FY 2025-26</option>
              <option value="2024-25">FY 2024-25</option>
            </select>
            <button style={{ ...btn, background: '#6c757d' }} onClick={() => navigate('/compliance')}>← Back</button>
            <button style={{ ...btn, background: '#6c757d' }} onClick={() => navigate('/employer-search')}>🏢 Employer Search</button>
          </div>
        </div>

        {/* Search Section */}
        <div style={sectionStyle}>
          <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <input
              type="text"
              placeholder="Enter 9-digit Employee TIN"
              aria-label="Employee TIN"
              value={tin}
              onChange={e => handleTinChange(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && search()}
              maxLength={9}
              style={inputStyle}
            />
            <button onClick={search} disabled={loading} style={{ ...btn, background: COLOR }}>
              {loading ? 'Searching...' : '🔍 Search'}
            </button>
            <button onClick={() => { setTin(''); setResult(null); setNotFound(''); setError(''); }} style={{ ...btn, background: '#dc3545' }}>
              ✕ Clear
            </button>
          </div>
          {error && <p style={errorStyle}>{error}</p>}
          {notFound && <p style={notFoundStyle}>{notFound}</p>}

          {result && (
            <>
              <div style={cardStyle}>
                <h3 style={{ margin: '0 0 0.75rem', color: '#003366' }}>{result.employeeName}</h3>
                <div style={gridStyle}>
                  <InfoRow label="TIN" value={result.employeeTIN} />
                  <InfoRow label="Current Employer" value={result.currentEmployer} />
                  <InfoRow label="Gross Monthly Salary" value={`Rs. ${result.grossMonthlySalary.toLocaleString()}`} />
                  <InfoRow label="Annual Tax Liability" value={`Rs. ${result.annualTaxLiability.toLocaleString()}`} color="#003366" />
                  <InfoRow label="Standard Monthly" value={`Rs. ${result.standardMonthly.toLocaleString()}`} />
                  <InfoRow label="Adjusted Monthly Deduction" value={`Rs. ${result.adjustedMonthly.toLocaleString()}`} color="#17a2b8" />
                  <InfoRow label="Total Tax Paid" value={`Rs. ${result.totalTaxPaid.toLocaleString()}`} color="#27ae60" />
                  <InfoRow label="Remaining Tax" value={`Rs. ${result.remainingTaxForYear.toLocaleString()}`} color="#e67e22" />
                  <InfoRow label="Joining Date" value={new Date(result.joiningDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })} />
                  <InfoRow label="Prior Employer" value={
                    result.hasPriorEmployer
                      ? <span style={{ background: '#fff3cd', color: '#856404', padding: '2px 8px', borderRadius: '10px', fontSize: '0.8rem', fontWeight: 600 }}>Yes — Rs. {result.priorEmployerDeduction.toLocaleString()} paid</span>
                      : <span style={{ color: '#999' }}>No</span>
                  } />
                  <InfoRow label="Remaining Months in FY" value={result.remainingMonthsInFY} />
                  <InfoRow label="Savings per Month" value={result.savingsPerMonth > 0 ? `Rs. ${result.savingsPerMonth.toLocaleString()}` : '—'} color="#27ae60" />
                </div>
              </div>
              <button onClick={downloadPdf} style={{ ...btn, background: '#27ae60', marginTop: '0.75rem', width: '100%', padding: '12px 16px', fontSize: '1rem' }}>
                📄 Download PDF Report
              </button>
            </>
          )}
        </div>
      </div>

      <Footer />
    </div>
  );
}

function InfoRow({ label, value, color }: { label: string; value: any; color?: string }) {
  return (
    <div style={{ marginBottom: '0.5rem' }}>
      <div style={{ fontSize: '0.78rem', color: '#888', marginBottom: '2px' }}>{label}</div>
      <div style={{ fontSize: '0.95rem', fontWeight: 600, color: color || '#333' }}>{value}</div>
    </div>
  );
}

const btn: React.CSSProperties = { padding: '8px 16px', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600, fontSize: '0.9rem' };
const sectionStyle: React.CSSProperties = { background: '#fff', borderRadius: '8px', padding: '1.5rem', boxShadow: '0 1px 4px rgba(0,0,0,0.1)' };
const inputStyle: React.CSSProperties = { flex: 1, padding: '10px 14px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem' };
const cardStyle: React.CSSProperties = { background: '#f8f9fa', borderRadius: '8px', padding: '1.2rem', marginTop: '1rem', borderLeft: `4px solid ${COLOR}` };
const gridStyle: React.CSSProperties = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.25rem 1.5rem' };
const errorStyle: React.CSSProperties = { color: '#dc3545', fontSize: '0.85rem', margin: '0.25rem 0 0' };
const notFoundStyle: React.CSSProperties = { color: '#856404', background: '#fff3cd', padding: '0.75rem 1rem', borderRadius: '6px', fontSize: '0.9rem', marginTop: '1rem' };
