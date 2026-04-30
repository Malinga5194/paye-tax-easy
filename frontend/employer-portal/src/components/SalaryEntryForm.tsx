import { useState } from 'react';
import { apiClient } from '../api/client';

interface Props {
  financialYear: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export default function SalaryEntryForm({ financialYear, onSuccess, onCancel }: Props) {
  const [tin, setTin] = useState('');
  const [salary, setSalary] = useState('');
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  const validate = () => {
    const e: Record<string, string> = {};
    if (!tin.trim()) e.tin = 'Employee TIN is required.';
    else if (!/^\d{9}$/.test(tin.trim())) e.tin = 'TIN must be exactly 9 digits.';
    if (!salary || parseFloat(salary) <= 0) e.salary = 'Gross monthly salary must be greater than 0.';
    if (!startDate) e.startDate = 'Employment start date is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    try {
      await apiClient.post(`/payroll/employees/${tin}/salary`, {
        grossMonthlySalary: parseFloat(salary),
        employmentStartDate: startDate,
        financialYear,
      });
      onSuccess();
    } catch (err: any) {
      const data = err.response?.data;
      if (data?.field) setErrors({ [data.field]: data.message });
      else setErrors({ general: data?.message || 'An error occurred.' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.overlay}>
      <div style={styles.modal}>
        <h2 style={styles.title}>Add Employee Salary</h2>
        <p style={styles.fy}>Financial Year: {financialYear}</p>
        <form onSubmit={handleSubmit}>
          <Field label="Employee TIN" error={errors.tin}>
            <input style={styles.input} value={tin} onChange={e => setTin(e.target.value)} placeholder="e.g. 100000001" maxLength={9} pattern="\d{9}" />
          </Field>
          <Field label="Gross Monthly Salary (Rs.)" error={errors.salary}>
            <input style={styles.input} type="number" value={salary} onChange={e => setSalary(e.target.value)} placeholder="e.g. 150000" min="1" />
          </Field>
          <Field label="Employment Start Date" error={errors.startDate}>
            <input style={styles.input} type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
          </Field>
          {errors.general && <p style={styles.error}>{errors.general}</p>}
          <div style={styles.actions}>
            <button type="button" style={styles.btnCancel} onClick={onCancel}>Cancel</button>
            <button type="submit" style={styles.btnSubmit} disabled={loading}>
              {loading ? 'Saving...' : 'Save & Calculate'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: '1rem' }}>
      <label style={{ display: 'block', fontWeight: 600, marginBottom: '4px' }}>{label}</label>
      {children}
      {error && <p style={{ color: '#c0392b', fontSize: '0.85rem', margin: '4px 0 0' }}>{error}</p>}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  overlay: { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 },
  modal: { background: '#fff', padding: '2rem', borderRadius: '8px', width: '420px', boxShadow: '0 4px 20px rgba(0,0,0,0.2)' },
  title: { margin: '0 0 0.5rem', color: '#003366' },
  fy: { color: '#666', marginBottom: '1.5rem', fontSize: '0.9rem' },
  input: { width: '100%', padding: '8px 12px', border: '1px solid #ccc', borderRadius: '4px', fontSize: '1rem', boxSizing: 'border-box' },
  error: { color: '#c0392b', fontSize: '0.9rem' },
  actions: { display: 'flex', gap: '1rem', justifyContent: 'flex-end', marginTop: '1.5rem' },
  btnCancel: { padding: '8px 20px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  btnSubmit: { padding: '8px 20px', background: '#003366', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
};
