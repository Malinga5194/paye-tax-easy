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

interface EmployerResult {
  employerTIN: string;
  organizationName: string;
  registrationNumber: string;
  contactEmail: string;
  employeeCount: number;
  totalSubmissions: number;
  totalPAYESubmitted: number;
  latestSubmissionDate: string | null;
  latestSubmissionRef: string | null;
  financialYear: string;
}

export function validateTin(value: string): string | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) return 'TIN is required';
  if (!/^\d{9}$/.test(trimmed)) return 'A valid 9-digit TIN is required';
  return null;
}

export default function TinSearchPage() {
  const [fy, setFy] = useState('2025-26');
  const navigate = useNavigate();
  const headers = { Authorization: `Bearer ${getToken()}` };

  // Employee search state
  const [empTin, setEmpTin] = useState('');
  const [empError, setEmpError] = useState('');
  const [empResult, setEmpResult] = useState<EmployeeResult | null>(null);
  const [empNotFound, setEmpNotFound] = useState('');
  const [empLoading, setEmpLoading] = useState(false);

  // Employer search state
  const [erTin, setErTin] = useState('');
  const [erError, setErError] = useState('');
  const [erResult, setErResult] = useState<EmployerResult | null>(null);
  const [erNotFound, setErNotFound] = useState('');
  const [erLoading, setErLoading] = useState(false);

  const handleEmpTinChange = (val: string) => {
    const numeric = val.replace(/\D/g, '');
    setEmpTin(numeric);
    setEmpError('');
  };

  const handleErTinChange = (val: string) => {
    const numeric = val.replace(/\D/g, '');
    setErTin(numeric);
    setErError('');
  };

  const searchEmployee = async () => {
    const err = validateTin(empTin);
    if (err) { setEmpError(err); return; }
    setEmpLoading(true);
    setEmpResult(null);
    setEmpNotFound('');
    setEmpError('');
    try {
      const res = await axios.get(`${API}/ird-dashboard/search/employee/${empTin.trim()}?financialYear=${fy}`, { headers });
      setEmpResult(res.data);
    } catch (e: any) {
      if (e.response?.status === 404) {
        setEmpNotFound(`No employee found with TIN ${empTin.trim()}`);
      } else {
        setEmpError('Search failed. Please try again.');
      }
    } finally {
      setEmpLoading(false);
    }
  };

  const searchEmployer = async () => {
    const err = validateTin(erTin);
    if (err) { setErError(err); return; }
    setErLoading(true);
    setErResult(null);
    setErNotFound('');
    setErError('');
    try {
      const res = await axios.get(`${API}/ird-dashboard/search/employer/${erTin.trim()}?financialYear=${fy}`, { headers });
      setErResult(res.data);
    } catch (e: any) {
      if (e.response?.status === 404) {
        setErNotFound(`No employer found with TIN ${erTin.trim()}`);
      } else {
        setErError('Search failed. Please try again.');
      }
    } finally {
      setErLoading(false);
    }
  };

  const downloadEmployeePdf = async () => {
    if (!empResult) return;
    try {
      const res = await axios.get(
        `${API}/ird-dashboard/search/employee/${empResult.employeeTIN}/pdf?financialYear=${fy}`,
        { headers, responseType: 'blob' }
      );
      const url = URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `IRD_Employee_Report_${empResult.employeeTIN}_${fy}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      alert('Failed to download PDF report.');
    }
  };

  const downloadEmployerPdf = async () => {
    if (!erResult) return;
    try {
      const res = await axios.get(
        `${API}/ird-dashboard/search/employer/${erResult.employerTIN}/pdf?financialYear=${fy}`,
        { headers, responseType: 'blob' }
      );
      const url = URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `IRD_Employer_Report_${erResult.employerTIN}_${fy}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      alert('Failed to download PDF report.');
    }
  };

  return (
    <div style={{ fontFamily: "'Segoe UI', sans-serif", background: '#f0f4f8', minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Header portalName="IRD Dashboard" portalIcon="🏛️" color={COLOR} />

      <div style={{ padding: '1.5rem', flex: 1 }}>
        {/* Page Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h1 style={{ margin: 0, color: COLOR }}>TIN Search</h1>
            <p style={{ margin: '4px 0 0', color: '#666', fontSize: '0.9rem' }}>Search employees and employers by Taxpayer Identification Number</p>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <select value={fy} onChange={e => setFy(e.target.value)} style={{ padding: '8px 14px', border: `2px solid ${COLOR}`, borderRadius: '6px', fontWeight: 600, color: COLOR }}>
              <option value="2025-26">FY 2025-26</option>
              <option value="2024-25">FY 2024-25</option>
            </select>
            <button style={{ ...btn, background: '#6c757d' }} onClick={() => navigate('/compliance')}>← Back to Compliance</button>
            <button style={{ ...btn, background: '#6c757d' }} onClick={() => navigate('/audit-logs')}>Audit Logs</button>
          </div>
        </div>

        {/* Two side-by-side search sections */}
        <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
          {/* Employee Search Section */}
          <div style={sectionStyle}>
            <h2 style={{ margin: '0 0 1rem', color: COLOR, fontSize: '1.15rem' }}>👤 Employee Search</h2>
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
              <input
                type="text"
                placeholder="Employee TIN"
                aria-label="Employee TIN"
                value={empTin}
                onChange={e => handleEmpTinChange(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && searchEmployee()}
                maxLength={9}
                style={inputStyle}
              />
              <button onClick={searchEmployee} disabled={empLoading} style={{ ...btn, background: COLOR }}>
                {empLoading ? 'Searching...' : 'Search'}
              </button>
            </div>
            {empError && <p style={errorStyle}>{empError}</p>}
            {empNotFound && <p style={notFoundStyle}>{empNotFound}</p>}

            {empResult && (
              <>
                <div style={cardStyle}>
                  <h3 style={{ margin: '0 0 0.75rem', color: '#003366' }}>{empResult.employeeName}</h3>
                  <div style={gridStyle}>
                    <InfoRow label="TIN" value={empResult.employeeTIN} />
                    <InfoRow label="Current Employer" value={empResult.currentEmployer} />
                    <InfoRow label="Gross Monthly Salary" value={`Rs. ${empResult.grossMonthlySalary.toLocaleString()}`} />
                    <InfoRow label="Annual Tax Liability" value={`Rs. ${empResult.annualTaxLiability.toLocaleString()}`} color="#003366" />
                    <InfoRow label="Total Tax Paid" value={`Rs. ${empResult.totalTaxPaid.toLocaleString()}`} color="#27ae60" />
                    <InfoRow label="Adjusted Monthly Deduction" value={`Rs. ${empResult.adjustedMonthly.toLocaleString()}`} color="#17a2b8" />
                    <InfoRow label="Joining Date" value={new Date(empResult.joiningDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })} />
                    <InfoRow label="Prior Employer" value={
                      empResult.hasPriorEmployer
                        ? <span style={{ background: '#fff3cd', color: '#856404', padding: '2px 8px', borderRadius: '10px', fontSize: '0.8rem', fontWeight: 600 }}>Yes</span>
                        : <span style={{ color: '#999' }}>No</span>
                    } />
                  </div>
                </div>
                <button onClick={downloadEmployeePdf} style={{ ...btn, background: '#27ae60', marginTop: '0.75rem', width: '100%' }}>
                  📄 Download PDF Report
                </button>
              </>
            )}
          </div>

          {/* Employer Search Section */}
          <div style={sectionStyle}>
            <h2 style={{ margin: '0 0 1rem', color: COLOR, fontSize: '1.15rem' }}>🏢 Employer Search</h2>
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
              <input
                type="text"
                placeholder="Employer TIN"
                aria-label="Employer TIN"
                value={erTin}
                onChange={e => handleErTinChange(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && searchEmployer()}
                maxLength={9}
                style={inputStyle}
              />
              <button onClick={searchEmployer} disabled={erLoading} style={{ ...btn, background: COLOR }}>
                {erLoading ? 'Searching...' : 'Search'}
              </button>
            </div>
            {erError && <p style={errorStyle}>{erError}</p>}
            {erNotFound && <p style={notFoundStyle}>{erNotFound}</p>}

            {erResult && (
              <>
                <div style={cardStyle}>
                  <h3 style={{ margin: '0 0 0.75rem', color: '#003366' }}>{erResult.organizationName}</h3>
                  <div style={gridStyle}>
                    <InfoRow label="TIN" value={erResult.employerTIN} />
                    <InfoRow label="Registration Number" value={erResult.registrationNumber} />
                    <InfoRow label="Contact Email" value={erResult.contactEmail} />
                    <InfoRow label="Employee Count" value={erResult.employeeCount} />
                    <InfoRow label="Total Submissions" value={erResult.totalSubmissions} />
                    <InfoRow label="Total PAYE Submitted" value={`Rs. ${erResult.totalPAYESubmitted.toLocaleString()}`} color="#003366" />
                    <InfoRow label="Latest Submission Date" value={erResult.latestSubmissionDate ? new Date(erResult.latestSubmissionDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'} />
                  </div>
                </div>
                <button onClick={downloadEmployerPdf} style={{ ...btn, background: '#27ae60', marginTop: '0.75rem', width: '100%' }}>
                  📄 Download PDF Report
                </button>
              </>
            )}
          </div>
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

const sectionStyle: React.CSSProperties = {
  flex: 1,
  minWidth: '380px',
  background: '#fff',
  borderRadius: '8px',
  padding: '1.5rem',
  boxShadow: '0 1px 4px rgba(0,0,0,0.1)',
};

const inputStyle: React.CSSProperties = {
  flex: 1,
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: '6px',
  fontSize: '0.95rem',
};

const cardStyle: React.CSSProperties = {
  background: '#f8f9fa',
  borderRadius: '8px',
  padding: '1.2rem',
  marginTop: '1rem',
  borderLeft: `4px solid ${COLOR}`,
};

const gridStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: '1fr 1fr',
  gap: '0.25rem 1.5rem',
};

const errorStyle: React.CSSProperties = {
  color: '#dc3545',
  fontSize: '0.85rem',
  margin: '0.25rem 0 0',
};

const notFoundStyle: React.CSSProperties = {
  color: '#856404',
  background: '#fff3cd',
  padding: '0.75rem 1rem',
  borderRadius: '6px',
  fontSize: '0.9rem',
  marginTop: '1rem',
};