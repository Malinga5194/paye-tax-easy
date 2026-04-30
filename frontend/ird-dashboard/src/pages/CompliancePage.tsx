import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { getToken } from '../App';
import { Header, Footer } from '../components/Layout';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const COLOR = '#7b1fa2';

interface ComplianceReport {
  financialYear: string;
  totalRegisteredEmployers: number;
  submittedCount: number;
  notSubmittedCount: number;
  totalPAYECollected: number;
}

interface EmployerSummary {
  employerTIN: string;
  organizationName: string;
  registrationNumber: string;
  totalSubmissions: number;
  totalPAYESubmitted: number;
  employeeCount: number;
  latestSubmissionRef: string;
  latestSubmissionDate: string | null;
}

interface EmployeeTax {
  employeeTIN: string;
  employeeName: string;
  currentEmployer: string;
  grossMonthlySalary: number;
  annualTaxLiability: number;
  totalTaxPaid: number;
  adjustedMonthly: number;
  joiningDate: string;
  hasPriorEmployer: boolean;
}

export default function CompliancePage() {
  const [fy, setFy] = useState('2025-26');
  const [report, setReport] = useState<ComplianceReport | null>(null);
  const [employers, setEmployers] = useState<EmployerSummary[]>([]);
  const [employees, setEmployees] = useState<EmployeeTax[]>([]);
  const [tab, setTab] = useState<'employers' | 'employees'>('employers');
  const navigate = useNavigate();
  const headers = { Authorization: `Bearer ${getToken()}` };

  useEffect(() => {
    axios.get(`${API}/ird-dashboard/compliance/${fy}`, { headers }).then(r => setReport(r.data)).catch(() => {});
    axios.get(`${API}/ird-dashboard/employers?financialYear=${fy}`, { headers }).then(r => setEmployers(r.data)).catch(() => {});
    axios.get(`${API}/ird-dashboard/employees?financialYear=${fy}`, { headers }).then(r => setEmployees(r.data)).catch(() => {});
  }, [fy]);

  const exportCsv = async () => {
    const res = await axios.get(`${API}/ird-dashboard/compliance/${fy}/export`, { headers, responseType: 'blob' });
    const url = URL.createObjectURL(new Blob([res.data]));
    const a = document.createElement('a'); a.href = url; a.download = `compliance_${fy}.csv`; a.click();
  };

  return (
    <div style={{ fontFamily: "'Segoe UI', sans-serif", background: '#f0f4f8', minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Header portalName="IRD Dashboard" portalIcon="🏛️" color={COLOR} />

      <div style={{ padding: '1.5rem', flex: 1 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h1 style={{ margin: 0, color: COLOR }}>IRD Compliance Dashboard</h1>
            <p style={{ margin: '4px 0 0', color: '#666', fontSize: '0.9rem' }}>Monitor employer PAYE submissions and employee tax details</p>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <select value={fy} onChange={e => setFy(e.target.value)} style={{ padding: '8px 14px', border: `2px solid ${COLOR}`, borderRadius: '6px', fontWeight: 600, color: COLOR }}>
              <option value="2025-26">FY 2025-26</option>
              <option value="2024-25">FY 2024-25</option>
            </select>
            <button style={{ ...btn, background: '#6c757d' }} onClick={() => navigate('/audit-logs')}>Audit Logs</button>
            <button style={{ ...btn, background: COLOR }} onClick={() => navigate('/employee-search')}>👤 Employee Search</button>
            <button style={{ ...btn, background: COLOR }} onClick={() => navigate('/employer-search')}>🏢 Employer Search</button>
            <button style={{ ...btn, background: '#27ae60' }} onClick={exportCsv}>Export CSV</button>
          </div>
        </div>

        {/* Summary Cards */}
        {report && (
          <div style={{ display: 'flex', gap: '1rem', marginBottom: '1.5rem', flexWrap: 'wrap' }}>
            <Card label="Total Employers" value={report.totalRegisteredEmployers} color="#003366" />
            <Card label="Submitted Returns" value={report.submittedCount} color="#27ae60" />
            <Card label="Not Submitted" value={report.notSubmittedCount} color="#e74c3c" />
            <Card label="Total PAYE Collected" value={`Rs. ${report.totalPAYECollected.toLocaleString()}`} color={COLOR} />
          </div>
        )}

        {/* Tab Buttons */}
        <div style={{ display: 'flex', gap: '0', marginBottom: '1.5rem' }}>
          <button style={{ ...tabBtn, background: tab === 'employers' ? COLOR : '#e0e0e0', color: tab === 'employers' ? '#fff' : '#333', borderRadius: '6px 0 0 6px' }}
            onClick={() => setTab('employers')}>🏢 Employer Submissions ({employers.length})</button>
          <button style={{ ...tabBtn, background: tab === 'employees' ? COLOR : '#e0e0e0', color: tab === 'employees' ? '#fff' : '#333', borderRadius: '0 6px 6px 0' }}
            onClick={() => setTab('employees')}>👤 Employee Tax Details ({employees.length})</button>
        </div>

        {/* Employer Submissions Table */}
        {tab === 'employers' && (
          <div style={tableWrapper}>
            <table style={table}>
              <thead>
                <tr style={{ background: COLOR, color: '#fff' }}>
                  <th style={th}>Organization</th>
                  <th style={th}>TIN</th>
                  <th style={th}>Reg. No</th>
                  <th style={th}>Employees</th>
                  <th style={th}>Submissions</th>
                  <th style={th}>Total PAYE</th>
                  <th style={th}>Latest Ref</th>
                  <th style={th}>Latest Date</th>
                </tr>
              </thead>
              <tbody>
                {employers.map(emp => (
                  <tr key={emp.employerTIN} style={{ borderBottom: '1px solid #eee' }}>
                    <td style={td}><strong>{emp.organizationName}</strong></td>
                    <td style={td}><code>{emp.employerTIN}</code></td>
                    <td style={td}>{emp.registrationNumber}</td>
                    <td style={{ ...td, textAlign: 'center' }}>{emp.employeeCount}</td>
                    <td style={{ ...td, textAlign: 'center' }}>
                      <span style={{ background: emp.totalSubmissions > 0 ? '#d4edda' : '#fdecea', color: emp.totalSubmissions > 0 ? '#155724' : '#c0392b', padding: '2px 10px', borderRadius: '12px', fontWeight: 600, fontSize: '0.85rem' }}>
                        {emp.totalSubmissions}
                      </span>
                    </td>
                    <td style={{ ...td, fontWeight: 600, color: '#003366' }}>Rs. {emp.totalPAYESubmitted.toLocaleString()}</td>
                    <td style={td}><code style={{ fontSize: '0.75rem' }}>{emp.latestSubmissionRef}</code></td>
                    <td style={td}>{emp.latestSubmissionDate ? new Date(emp.latestSubmissionDate).toLocaleDateString('en-GB') : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Employee Tax Details Table */}
        {tab === 'employees' && (
          <div style={tableWrapper}>
            <table style={table}>
              <thead>
                <tr style={{ background: COLOR, color: '#fff' }}>
                  <th style={th}>Employee</th>
                  <th style={th}>TIN</th>
                  <th style={th}>Current Employer</th>
                  <th style={th}>Gross Salary</th>
                  <th style={th}>Annual Tax</th>
                  <th style={th}>Total Paid</th>
                  <th style={th}>Monthly Deduction</th>
                  <th style={th}>Joining Date</th>
                  <th style={th}>Prior Employer</th>
                </tr>
              </thead>
              <tbody>
                {employees.map(emp => (
                  <tr key={emp.employeeTIN} style={{ borderBottom: '1px solid #eee' }}>
                    <td style={td}><strong>{emp.employeeName}</strong></td>
                    <td style={td}><code>{emp.employeeTIN}</code></td>
                    <td style={td}>{emp.currentEmployer}</td>
                    <td style={td}>Rs. {emp.grossMonthlySalary.toLocaleString()}</td>
                    <td style={{ ...td, fontWeight: 600, color: '#003366' }}>Rs. {emp.annualTaxLiability.toLocaleString()}</td>
                    <td style={{ ...td, fontWeight: 600, color: '#27ae60' }}>Rs. {emp.totalTaxPaid.toLocaleString()}</td>
                    <td style={{ ...td, fontWeight: 600, color: '#17a2b8' }}>Rs. {emp.adjustedMonthly.toLocaleString()}</td>
                    <td style={td}>{new Date(emp.joiningDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td style={td}>
                      {emp.hasPriorEmployer
                        ? <span style={{ background: '#fff3cd', color: '#856404', padding: '2px 8px', borderRadius: '10px', fontSize: '0.8rem', fontWeight: 600 }}>Yes</span>
                        : <span style={{ color: '#999' }}>No</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <Footer />
    </div>
  );
}

function Card({ label, value, color }: { label: string; value: any; color: string }) {
  return (
    <div style={{ background: '#fff', borderRadius: '8px', padding: '1.2rem 1.5rem', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', borderLeft: `5px solid ${color}`, flex: 1, minWidth: '180px' }}>
      <div style={{ fontSize: '0.82rem', color: '#666', marginBottom: '0.3rem' }}>{label}</div>
      <div style={{ fontSize: '1.5rem', fontWeight: 700, color }}>{value}</div>
    </div>
  );
}

const btn: React.CSSProperties = { padding: '8px 16px', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 };
const tabBtn: React.CSSProperties = { padding: '10px 20px', border: 'none', cursor: 'pointer', fontWeight: 600, fontSize: '0.95rem' };
const tableWrapper: React.CSSProperties = { background: '#fff', borderRadius: '8px', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', overflow: 'auto' };
const table: React.CSSProperties = { width: '100%', borderCollapse: 'collapse', minWidth: '900px' };
const th: React.CSSProperties = { padding: '12px 14px', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem', whiteSpace: 'nowrap' };
const td: React.CSSProperties = { padding: '10px 14px', fontSize: '0.88rem' };
