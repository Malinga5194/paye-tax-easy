import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient, setToken } from '../api/client';

const COLOR = '#003366';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await apiClient.post('/auth/login', { email, password });
      setToken(res.data.accessToken);
      navigate('/dashboard');
    } catch (err: any) {
      const code = err.response?.data?.errorCode;
      if (code === 'AUTH_002') setError('Account locked. Check your email for instructions.');
      else setError('Invalid credentials. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.page}>
      {/* Header */}
      <header style={{ ...styles.header, background: COLOR }}>
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
          <div style={{ ...styles.cardHeader, background: COLOR }}>
            <span style={styles.cardIcon}>🏢</span>
            <h2 style={styles.cardTitle}>Employer Portal</h2>
            <p style={styles.cardSubtitle}>Manage payroll and PAYE deductions</p>
          </div>
          <div style={styles.cardBody}>
            <div style={styles.hint}>
              <strong>Dev credentials:</strong><br />
              Email: <code>employer@test.com</code> &nbsp;|&nbsp; Password: <code>Test@1234</code>
            </div>
            <form onSubmit={handleLogin}>
              <div style={styles.field}>
                <label style={styles.label}>Email Address</label>
                <input style={styles.input} type="email" value={email}
                  onChange={e => setEmail(e.target.value)} placeholder="Enter your email" required />
              </div>
              <div style={styles.field}>
                <label style={styles.label}>Password</label>
                <input style={styles.input} type="password" value={password}
                  onChange={e => setPassword(e.target.value)} placeholder="Enter your password" required />
              </div>
              {error && <p style={styles.error}>⚠ {error}</p>}
              <button style={{ ...styles.button, background: COLOR }} type="submit" disabled={loading}>
                {loading ? 'Signing in...' : 'Sign In to Employer Portal'}
              </button>
              <p style={{ textAlign: 'center', marginTop: '1rem', fontSize: '0.9rem', color: '#666' }}>
                Don't have an account? <a href="/register" style={{ color: COLOR, fontWeight: 600 }}>Create Account</a>
              </p>
            </form>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer style={styles.footer}>
        <div style={styles.footerTop}>
          <div style={styles.footerCol}>
            <strong>PAYE Tax Easy</strong>
            <p>A centralized PAYE tax management platform for Sri Lanka, integrating employer payroll systems with the Inland Revenue Department.</p>
          </div>
          <div style={styles.footerCol}>
            <strong>Portals</strong>
            <a href="http://localhost:5173" style={styles.footerLink}>🏠 Landing Page</a>
            <a href="http://localhost:5173" style={styles.footerLink}>🏢 Employer Portal</a>
            <a href="http://localhost:5174" target="_blank" style={styles.footerLink}>👤 Employee Portal</a>
            <a href="http://localhost:5175" target="_blank" style={styles.footerLink}>🏛️ IRD Dashboard</a>
          </div>
          <div style={styles.footerCol}>
            <strong>Legal</strong>
            <p style={{ fontSize: '0.8rem', color: '#aaa' }}>Inland Revenue (Amendment) Act, No. 02 of 2025</p>
            <p style={{ fontSize: '0.8rem', color: '#aaa' }}>Financial Year: 1 April 2025 – 31 March 2026</p>
            <p style={{ fontSize: '0.8rem', color: '#aaa' }}>Filing Deadline: 30 November 2026</p>
          </div>
        </div>
        <div style={styles.footerBottom}>
          © 2025 PAYE Tax Easy &nbsp;|&nbsp; SLIIT MSc IT — Software Engineering Practices (IT5030) &nbsp;|&nbsp; Group 02
        </div>
      </footer>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { minHeight: '100vh', display: 'flex', flexDirection: 'column', background: '#f0f4f8', fontFamily: "'Segoe UI', sans-serif" },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem 2rem', color: '#fff' },
  headerLeft: { display: 'flex', alignItems: 'center', gap: '1rem' },
  headerLogo: { fontSize: '2rem' },
  headerTitle: { fontWeight: 700, fontSize: '1.3rem' },
  headerSub: { fontSize: '0.8rem', opacity: 0.8 },
  homeBtn: { padding: '8px 18px', background: 'rgba(255,255,255,0.2)', color: '#fff', border: '1.5px solid rgba(255,255,255,0.5)', borderRadius: '6px', textDecoration: 'none', fontWeight: 600, fontSize: '0.9rem' },
  main: { flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' },
  card: { width: '420px', borderRadius: '12px', overflow: 'hidden', boxShadow: '0 8px 30px rgba(0,0,0,0.12)' },
  cardHeader: { padding: '1.5rem', color: '#fff', textAlign: 'center' },
  cardIcon: { fontSize: '2.5rem' },
  cardTitle: { margin: '0.5rem 0 0.25rem', fontSize: '1.4rem' },
  cardSubtitle: { margin: 0, opacity: 0.85, fontSize: '0.9rem' },
  cardBody: { background: '#fff', padding: '1.5rem' },
  hint: { background: '#e8f4fd', border: '1px solid #bee3f8', borderRadius: '6px', padding: '10px 14px', marginBottom: '1.2rem', fontSize: '0.85rem', color: '#2c5282' },
  field: { marginBottom: '1rem' },
  label: { display: 'block', marginBottom: '5px', fontWeight: 600, color: '#333', fontSize: '0.9rem' },
  input: { width: '100%', padding: '10px 12px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box' },
  error: { color: '#c0392b', fontSize: '0.9rem', marginBottom: '1rem', background: '#fdecea', padding: '8px 12px', borderRadius: '6px' },
  button: { width: '100%', padding: '12px', color: '#fff', border: 'none', borderRadius: '6px', fontSize: '1rem', cursor: 'pointer', fontWeight: 600, marginTop: '0.5rem' },
  footer: { background: '#1a1a2e', color: '#ccc', padding: '2rem' },
  footerTop: { display: 'flex', gap: '2rem', flexWrap: 'wrap', marginBottom: '1.5rem', justifyContent: 'space-between' },
  footerCol: { display: 'flex', flexDirection: 'column', gap: '0.4rem', flex: 1, minWidth: '180px', fontSize: '0.85rem' },
  footerLink: { color: '#aaa', textDecoration: 'none', fontSize: '0.85rem' },
  footerBottom: { borderTop: '1px solid #333', paddingTop: '1rem', textAlign: 'center', fontSize: '0.8rem', color: '#888' },
};
