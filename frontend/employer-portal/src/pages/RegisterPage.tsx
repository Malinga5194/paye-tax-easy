import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../api/client';

const ROLES = ['Employer', 'Employee', 'IRD_Officer'];

export default function RegisterPage() {
  const [form, setForm] = useState({ email: '', password: '', confirmPassword: '', fullName: '', role: 'Employer', tin: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const validate = () => {
    const e: Record<string, string> = {};
    if (!form.fullName.trim()) e.fullName = 'Full name is required.';
    if (!form.email.trim()) e.email = 'Email is required.';
    if (!form.password || form.password.length < 6) e.password = 'Password must be at least 6 characters.';
    if (form.password !== form.confirmPassword) e.confirmPassword = 'Passwords do not match.';
    if (!form.role) e.role = 'Role is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    try {
      await apiClient.post('/auth/register', {
        email: form.email,
        password: form.password,
        fullName: form.fullName,
        role: form.role,
        tin: form.tin || null,
      });
      setSuccess('Account created successfully! You can now sign in.');
      setTimeout(() => navigate('/login'), 2000);
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Registration failed.';
      setErrors({ general: msg });
    } finally {
      setLoading(false);
    }
  };

  const set = (field: string, value: string) => setForm(f => ({ ...f, [field]: value }));

  return (
    <div style={styles.page}>
      {/* Header */}
      <header style={styles.header}>
        <div style={styles.headerLeft}>
          <span style={styles.headerLogo}>🏛️</span>
          <div>
            <div style={styles.headerTitle}>PAYE Tax Easy</div>
            <div style={styles.headerSub}>Inland Revenue Department — Sri Lanka</div>
          </div>
        </div>
        <a href="http://localhost:5173" style={styles.homeBtn}>← Back to Home</a>
      </header>

      {/* Main */}
      <main style={styles.main}>
        <div style={styles.card}>
          <div style={styles.cardHeader}>
            <span style={styles.cardIcon}>📝</span>
            <h2 style={styles.cardTitle}>Create Account</h2>
            <p style={styles.cardSubtitle}>Register to access PAYE Tax Easy</p>
          </div>
          <div style={styles.cardBody}>
            {success && <div style={styles.success}>✓ {success}</div>}
            {errors.general && <div style={styles.error}>⚠ {errors.general}</div>}
            <form onSubmit={handleSubmit}>
              <Field label="Full Name *" error={errors.fullName}>
                <input style={styles.input} value={form.fullName} onChange={e => set('fullName', e.target.value)} placeholder="Enter your full name" />
              </Field>
              <Field label="Email Address *" error={errors.email}>
                <input style={styles.input} type="email" value={form.email} onChange={e => set('email', e.target.value)} placeholder="Enter your email" />
              </Field>
              <Field label="Role *" error={errors.role}>
                <select style={styles.input} value={form.role} onChange={e => set('role', e.target.value)}>
                  {ROLES.map(r => <option key={r} value={r}>{r.replace('_', ' ')}</option>)}
                </select>
              </Field>
              <Field label="TIN (optional)" error={''}>
                <input style={styles.input} value={form.tin} onChange={e => set('tin', e.target.value)} placeholder="Taxpayer Identification Number" />
              </Field>
              <Field label="Password *" error={errors.password}>
                <input style={styles.input} type="password" value={form.password} onChange={e => set('password', e.target.value)} placeholder="Minimum 6 characters" />
              </Field>
              <Field label="Confirm Password *" error={errors.confirmPassword}>
                <input style={styles.input} type="password" value={form.confirmPassword} onChange={e => set('confirmPassword', e.target.value)} placeholder="Re-enter your password" />
              </Field>
              <button style={styles.button} type="submit" disabled={loading}>
                {loading ? 'Creating account...' : 'Create Account'}
              </button>
              <p style={styles.loginLink}>
                Already have an account? <a href="/login" style={{ color: '#003366', fontWeight: 600 }}>Sign In</a>
              </p>
            </form>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer style={styles.footer}>
        <div style={styles.footerBottom}>
          © 2025 PAYE Tax Easy &nbsp;|&nbsp; SLIIT MSc IT — Software Engineering Practices (IT5030) &nbsp;|&nbsp; Group 02
        </div>
      </footer>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: '1rem' }}>
      <label style={{ display: 'block', marginBottom: '5px', fontWeight: 600, color: '#333', fontSize: '0.9rem' }}>{label}</label>
      {children}
      {error && <p style={{ color: '#c0392b', fontSize: '0.82rem', margin: '4px 0 0' }}>{error}</p>}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { minHeight: '100vh', display: 'flex', flexDirection: 'column', background: '#f0f4f8', fontFamily: "'Segoe UI', sans-serif" },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem 2rem', background: '#003366', color: '#fff' },
  headerLeft: { display: 'flex', alignItems: 'center', gap: '1rem' },
  headerLogo: { fontSize: '2rem' },
  headerTitle: { fontWeight: 700, fontSize: '1.3rem' },
  headerSub: { fontSize: '0.8rem', opacity: 0.8 },
  homeBtn: { padding: '8px 18px', background: 'rgba(255,255,255,0.2)', color: '#fff', border: '1.5px solid rgba(255,255,255,0.5)', borderRadius: '6px', textDecoration: 'none', fontWeight: 600, fontSize: '0.9rem' },
  main: { flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' },
  card: { width: '440px', borderRadius: '12px', overflow: 'hidden', boxShadow: '0 8px 30px rgba(0,0,0,0.12)' },
  cardHeader: { padding: '1.5rem', background: '#003366', color: '#fff', textAlign: 'center' },
  cardIcon: { fontSize: '2.5rem' },
  cardTitle: { margin: '0.5rem 0 0.25rem', fontSize: '1.4rem' },
  cardSubtitle: { margin: 0, opacity: 0.85, fontSize: '0.9rem' },
  cardBody: { background: '#fff', padding: '1.5rem' },
  input: { width: '100%', padding: '10px 12px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box' },
  button: { width: '100%', padding: '12px', background: '#003366', color: '#fff', border: 'none', borderRadius: '6px', fontSize: '1rem', cursor: 'pointer', fontWeight: 600, marginTop: '0.5rem' },
  success: { background: '#d4edda', color: '#155724', padding: '10px 14px', borderRadius: '6px', marginBottom: '1rem', fontSize: '0.9rem' },
  error: { background: '#fdecea', color: '#c0392b', padding: '10px 14px', borderRadius: '6px', marginBottom: '1rem', fontSize: '0.9rem' },
  loginLink: { textAlign: 'center', marginTop: '1rem', fontSize: '0.9rem', color: '#666' },
  footer: { background: '#1a1a2e', color: '#ccc', padding: '1rem 2rem' },
  footerBottom: { textAlign: 'center', fontSize: '0.8rem', color: '#888' },
};
