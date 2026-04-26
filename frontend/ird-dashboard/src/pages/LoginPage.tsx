import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { setToken } from '../App';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await axios.post(`${API}/auth/login`, { email, password });
      setToken(res.data.accessToken);
      navigate('/compliance');
    } catch {
      setError('Invalid credentials.');
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>PAYE Tax Easy</h1>
        <p style={styles.subtitle}>IRD Officer Dashboard</p>
        <div style={{ background: '#f3e5f5', border: '1px solid #ce93d8', borderRadius: '4px', padding: '10px', marginBottom: '1rem', fontSize: '0.85rem', color: '#4a148c' }}>
          <strong>Dev credentials:</strong><br />
          Email: <code>ird@test.com</code> | Password: <code>Test@1234</code>
        </div>
        <form onSubmit={handleLogin}>
          <div style={styles.field}>
            <label style={styles.label}>Email</label>
            <input style={styles.input} type="email" value={email} onChange={e => setEmail(e.target.value)} required />
          </div>
          <div style={styles.field}>
            <label style={styles.label}>Password</label>
            <input style={styles.input} type="password" value={password} onChange={e => setPassword(e.target.value)} required />
          </div>
          {error && <p style={{ color: '#c0392b' }}>{error}</p>}
          <button style={styles.button} type="submit">Sign In</button>
        </form>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: { minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#f0f4f8' },
  card: { background: '#fff', padding: '2rem', borderRadius: '8px', boxShadow: '0 2px 12px rgba(0,0,0,0.1)', width: '360px' },
  title: { margin: 0, color: '#7b1fa2', fontSize: '1.8rem' },
  subtitle: { color: '#666', marginBottom: '1.5rem' },
  field: { marginBottom: '1rem' },
  label: { display: 'block', marginBottom: '4px', fontWeight: 600 },
  input: { width: '100%', padding: '8px 12px', border: '1px solid #ccc', borderRadius: '4px', fontSize: '1rem', boxSizing: 'border-box' },
  button: { width: '100%', padding: '10px', background: '#7b1fa2', color: '#fff', border: 'none', borderRadius: '4px', fontSize: '1rem', cursor: 'pointer' },
};
