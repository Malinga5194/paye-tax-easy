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
    setError('');
    try {
      const res = await axios.post(`${API}/auth/login`, { email, password });
      setToken(res.data.accessToken);
      navigate('/history');
    } catch {
      setError('Invalid credentials. Please try again.');
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>PAYE Tax Easy</h1>
        <p style={styles.subtitle}>Employee Portal</p>
        <div style={{ background: '#e8f4fd', border: '1px solid #bee3f8', borderRadius: '4px', padding: '10px', marginBottom: '1rem', fontSize: '0.85rem', color: '#2c5282' }}>
          <strong>Dev credentials:</strong><br />
          Email: <code>employee@test.com</code> | Password: <code>Test@1234</code>
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
  title: { margin: 0, color: '#1a6b3c', fontSize: '1.8rem' },
  subtitle: { color: '#666', marginBottom: '1.5rem' },
  field: { marginBottom: '1rem' },
  label: { display: 'block', marginBottom: '4px', fontWeight: 600 },
  input: { width: '100%', padding: '8px 12px', border: '1px solid #ccc', borderRadius: '4px', fontSize: '1rem', boxSizing: 'border-box' },
  button: { width: '100%', padding: '10px', background: '#1a6b3c', color: '#fff', border: 'none', borderRadius: '4px', fontSize: '1rem', cursor: 'pointer' },
};
