import { useState } from 'react';

const TAX_RELIEF = 1_800_000;
const SLABS = [
  { label: 'Up to Rs. 1,800,000', rate: 0, width: 1_800_000 },
  { label: 'Rs. 1,800,001 – Rs. 2,800,000', rate: 0.06, width: 1_000_000 },
  { label: 'Rs. 2,800,001 – Rs. 3,300,000', rate: 0.18, width: 500_000 },
  { label: 'Rs. 3,300,001 – Rs. 3,800,000', rate: 0.24, width: 500_000 },
  { label: 'Rs. 3,800,001 – Rs. 4,300,000', rate: 0.30, width: 500_000 },
  { label: 'Above Rs. 4,300,000', rate: 0.36, width: Infinity },
];

function calculateTax(monthlySalary: number) {
  const annual = monthlySalary * 12;
  let taxable = Math.max(0, annual - TAX_RELIEF);
  const breakdown: { band: string; rate: string; taxableAmount: number; tax: number }[] = [];

  breakdown.push({ band: SLABS[0].label, rate: '0%', taxableAmount: Math.min(annual, TAX_RELIEF), tax: 0 });

  for (let i = 1; i < SLABS.length; i++) {
    if (taxable <= 0) break;
    const amount = Math.min(taxable, SLABS[i].width);
    const tax = amount * SLABS[i].rate;
    breakdown.push({ band: SLABS[i].label, rate: `${SLABS[i].rate * 100}%`, taxableAmount: amount, tax });
    taxable -= amount;
  }

  const totalTax = breakdown.reduce((sum, s) => sum + s.tax, 0);
  const monthlyDeduction = Math.round(totalTax / 12);

  return { annual, breakdown, totalTax, monthlyDeduction };
}

interface Props {
  onClose: () => void;
  color?: string;
}

export default function TaxCalculator({ onClose, color = '#003366' }: Props) {
  const [salary, setSalary] = useState('');
  const [result, setResult] = useState<ReturnType<typeof calculateTax> | null>(null);

  const formatWithCommas = (val: string) => {
    const num = val.replace(/,/g, '');
    if (!num) return '';
    return parseInt(num).toLocaleString();
  };

  const handleSalaryChange = (val: string) => {
    const raw = val.replace(/[^0-9]/g, '');
    setSalary(raw ? parseInt(raw).toLocaleString() : '');
  };

  const handleCalculate = () => {
    const val = parseFloat(salary.replace(/,/g, ''));
    if (isNaN(val) || val <= 0) return;
    setResult(calculateTax(val));
  };

  const handleClear = () => {
    setSalary('');
    setResult(null);
  };

  return (
    <div style={overlay} onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={modal}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h2 style={{ margin: 0, color }}>🧮 PAYE Tax Calculator</h2>
          <button onClick={onClose} style={{ background: '#e74c3c', color: '#fff', border: 'none', borderRadius: '6px', padding: '6px 12px', cursor: 'pointer', fontWeight: 700 }}>✕</button>
        </div>

        <p style={{ color: '#666', fontSize: '0.9rem', margin: '0 0 1rem' }}>
          Calculate the standard PAYE tax deduction based on monthly salary (without any adjustments).
          This shows how the <strong>current system without PAYE Tax Easy</strong> would calculate tax.
        </p>

        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', alignItems: 'center' }}>
          <span style={{ fontWeight: 600, color: '#666' }}>Rs.</span>
          <input
            type="text"
            placeholder="Enter monthly salary (e.g. 350,000)"
            value={salary}
            onChange={e => handleSalaryChange(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleCalculate()}
            style={{ flex: 1, padding: '12px 14px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 600 }}
          />
          <button onClick={handleCalculate} style={{ ...btn, background: color }}>Calculate</button>
          <button onClick={handleClear} style={{ ...btn, background: '#dc3545' }}>Clear</button>
        </div>

        {result && (
          <>
            {/* Summary */}
            <div style={{ display: 'flex', gap: '1rem', marginBottom: '1.5rem', flexWrap: 'wrap' }}>
              <SummaryCard label="Annual Income" value={`Rs. ${result.annual.toLocaleString()}`} color="#003366" />
              <SummaryCard label="Annual Tax" value={`Rs. ${result.totalTax.toLocaleString()}`} color="#e74c3c" />
              <SummaryCard label="Monthly Deduction" value={`Rs. ${result.monthlyDeduction.toLocaleString()}`} color={color} />
            </div>

            {/* Slab Breakdown */}
            <h4 style={{ margin: '0 0 8px', color }}>Tax Slab Breakdown</h4>
            <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '1rem' }}>
              <thead>
                <tr style={{ background: color, color: '#fff' }}>
                  <th style={th}>Income Band</th>
                  <th style={th}>Rate</th>
                  <th style={{ ...th, textAlign: 'right' }}>Taxable Amount</th>
                  <th style={{ ...th, textAlign: 'right' }}>Tax</th>
                </tr>
              </thead>
              <tbody>
                {result.breakdown.map((s, i) => (
                  <tr key={i} style={{ background: i % 2 === 0 ? '#fff' : '#f8f9fa', borderBottom: '1px solid #eee' }}>
                    <td style={td}>{s.band}</td>
                    <td style={{ ...td, fontWeight: 600, color: s.rate === '0%' ? '#27ae60' : color }}>{s.rate}</td>
                    <td style={{ ...td, textAlign: 'right' }}>Rs. {s.taxableAmount.toLocaleString()}</td>
                    <td style={{ ...td, textAlign: 'right', fontWeight: 600 }}>Rs. {s.tax.toLocaleString()}</td>
                  </tr>
                ))}
                <tr style={{ background: color, color: '#fff' }}>
                  <td colSpan={3} style={{ ...td, fontWeight: 700 }}>Total Annual Tax</td>
                  <td style={{ ...td, textAlign: 'right', fontWeight: 700 }}>Rs. {result.totalTax.toLocaleString()}</td>
                </tr>
              </tbody>
            </table>

            {/* Formula explanation */}
            <div style={{ background: '#fff3cd', border: '1px solid #ffc107', borderRadius: '8px', padding: '12px 16px', fontSize: '0.85rem', color: '#856404' }}>
              <strong>Formula (Without PAYE Tax Easy):</strong><br />
              Annual Income = Rs. {salary ? parseInt(salary).toLocaleString() : '0'} × 12 = Rs. {result.annual.toLocaleString()}<br />
              Taxable Income = Rs. {result.annual.toLocaleString()} − Rs. 1,800,000 (relief) = Rs. {Math.max(0, result.annual - TAX_RELIEF).toLocaleString()}<br />
              Annual Tax = Rs. {result.totalTax.toLocaleString()}<br />
              <strong>Monthly Deduction = Rs. {result.totalTax.toLocaleString()} ÷ 12 = Rs. {result.monthlyDeduction.toLocaleString()}</strong>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function SummaryCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div style={{ flex: 1, minWidth: '150px', background: '#f8f9fa', border: `2px solid ${color}`, borderRadius: '8px', padding: '12px', textAlign: 'center' }}>
      <div style={{ fontSize: '0.75rem', color: '#666', marginBottom: '4px' }}>{label}</div>
      <div style={{ fontSize: '1.1rem', fontWeight: 700, color }}>{value}</div>
    </div>
  );
}

const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 2000, padding: '1rem' };
const modal: React.CSSProperties = { background: '#fff', borderRadius: '12px', width: '750px', maxWidth: '95%', maxHeight: '90vh', overflowY: 'auto', padding: '2rem', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const btn: React.CSSProperties = { padding: '10px 18px', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600, fontSize: '0.9rem' };
const th: React.CSSProperties = { padding: '10px 12px', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem' };
const td: React.CSSProperties = { padding: '8px 12px', fontSize: '0.88rem' };
