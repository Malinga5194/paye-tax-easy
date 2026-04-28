import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { setToken } from '../App';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const COLOR = '#b71c1c';

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
      if (res.data.role !== 'SystemAdmin') {
        setError('Access denied. Admin credentials required.');
        return;
      }
      setToken(res.data.accessToken);
      navigate('/users');
    } catch {
      setError('Invalid credentials. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.page}>
      <header style={{ ...styles.header, background: COLOR }}>
        <div style={styles.headerLeft}>
          <span style={styles.headerLogo}>🏛️</span>
          <div>
            <div style={styles.headerTitle}>PAYE Tax Easy</div>
            <div style={styles.headerSub}>System Administration</div>
          </div>
        </div>
        <a href="http://localhost:5173" style={styles.homeBtn}>← Back to Home</a>
      </header>

      <main style={styles.main}>
        <div style={styles.card}>
          <div style={{ ...styles.cardHeader, background: COLOR }}>
            <span style={styles.cardIcon}>🔐</span>
            <h2 style={styles.cardTitle}>Admin Portal</h2>
            <p style={styles.cardSubtitle}>User management and system administration</p>
          </div>
          <div style={styles.cardBody}>
            <div style={styles.hint}>
              <strong>Default admin credentials:</strong><br />
              Email: <code>admin@payetaxeasy.lk</code> &nbsp;|&nbsp; Password: <code>Admin@1234</code>
            </div>
            <form onSubmit={handleLogin}>
              <div style={styles.field}>
                <label style={styles.label}>Email Address</label>
                <input style={styles.input} type="email" value={email}
                  onChange={e => setEmail(e.target.value)} placeholder="Admin email" required />
              </div>
              <div style={styles.field}>
                <label style={styles.label}>Password</label>
                <input style={styles.input} type="password" value={password}
                  onChange={e => setPassword(e.target.value)} placeholder="Admin password" required />
              </div>
              {error && <p style={styles.error}>⚠ {error}</p>}
              <button style={{ ...styles.button, background: COLOR }} type="submit" disabled={loading}>
                {loading ? 'Signing in...' : 'Sign In to Admin Portal'}
              </button>
            </form>
          </div>
        </div>
      </main>

      <footer style={styles.footer}>
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
  hint: { background: '#fdecea', border: '1px solid #ef9a9a', borderRadius: '6px', padding: '10px 14px', marginBottom: '1.2rem', fontSize: '0.85rem', color: '#b71c1c' },
  field: { marginBottom: '1rem' },
  label: { display: 'block', marginBottom: '5px', fontWeight: 600, color: '#333', fontSize: '0.9rem' },
  input: { width: '100%', padding: '10px 12px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box' },
  error: { color: '#c0392b', fontSize: '0.9rem', marginBottom: '1rem', background: '#fdecea', padding: '8px 12px', borderRadius: '6px' },
  button: { width: '100%', padding: '12px', color: '#fff', border: 'none', borderRadius: '6px', fontSize: '1rem', cursor: 'pointer', fontWeight: 600 },
  footer: { background: '#1a1a2e', color: '#ccc', padding: '1rem 2rem' },
  footerBottom: { textAlign: 'center', fontSize: '0.8rem', color: '#888' },
};
