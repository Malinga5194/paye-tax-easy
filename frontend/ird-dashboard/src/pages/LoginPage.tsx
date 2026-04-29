import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { setToken } from '../App';
import { Header, Footer } from '../components/Layout';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const COLOR = '#7b1fa2';

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
      const res = await axios.post(`${API}/auth/login`, { email, password });
      setToken(res.data.accessToken);
      navigate('/compliance');
    } catch {
      setError('Invalid credentials. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.page}>
      <Header portalName="IRD Dashboard" portalIcon="🏛️" color={COLOR} />
      <main style={styles.main}>
        <div style={styles.card}>
          <div style={{ ...styles.cardHeader, background: COLOR }}>
            <span style={styles.cardIcon}>🏛️</span>
            <h2 style={styles.cardTitle}>IRD Officer Dashboard</h2>
            <p style={styles.cardSubtitle}>Compliance monitoring and audit management</p>
          </div>
          <div style={styles.cardBody}>
            <div style={{ ...styles.hint, background: '#f3e5f5', border: '1px solid #ce93d8', color: '#4a148c' }}>
              <strong>Dev credentials:</strong><br />
              Email: <code>ird@test.com</code> &nbsp;|&nbsp; Password: <code>Test@1234</code>
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
                {loading ? 'Signing in...' : 'Sign In to IRD Dashboard'}
              </button>
            </form>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { minHeight: '100vh', display: 'flex', flexDirection: 'column', background: '#f0f4f8', fontFamily: "'Segoe UI', sans-serif" },
  main: { flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' },
  card: { width: '420px', borderRadius: '12px', overflow: 'hidden', boxShadow: '0 8px 30px rgba(0,0,0,0.12)' },
  cardHeader: { padding: '1.5rem', color: '#fff', textAlign: 'center' },
  cardIcon: { fontSize: '2.5rem' },
  cardTitle: { margin: '0.5rem 0 0.25rem', fontSize: '1.4rem' },
  cardSubtitle: { margin: 0, opacity: 0.85, fontSize: '0.9rem' },
  cardBody: { background: '#fff', padding: '1.5rem' },
  hint: { borderRadius: '6px', padding: '10px 14px', marginBottom: '1.2rem', fontSize: '0.85rem' },
  field: { marginBottom: '1rem' },
  label: { display: 'block', marginBottom: '5px', fontWeight: 600, color: '#333', fontSize: '0.9rem' },
  input: { width: '100%', padding: '10px 12px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box' },
  error: { color: '#c0392b', fontSize: '0.9rem', marginBottom: '1rem', background: '#fdecea', padding: '8px 12px', borderRadius: '6px' },
  button: { width: '100%', padding: '12px', color: '#fff', border: 'none', borderRadius: '6px', fontSize: '1rem', cursor: 'pointer', fontWeight: 600, marginTop: '0.5rem' },
};
