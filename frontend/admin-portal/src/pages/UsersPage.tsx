import { useState, useEffect } from 'react';
import axios from 'axios';
import { getToken } from '../App';
import { Header, Footer } from '../components/Layout';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const COLOR = '#b71c1c';

interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  tin: string;
  isActive: boolean;
  createdAt: string;
  isProtected: boolean;
}

const ROLES = ['Employer', 'Employee', 'IRD_Officer', 'SystemAdmin'];
const ROLE_COLORS: Record<string, string> = {
  Employer: '#003366', Employee: '#1a6b3c', IRD_Officer: '#7b1fa2', SystemAdmin: '#b71c1c'
};

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [resetId, setResetId] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [message, setMessage] = useState('');
  const [filterRole, setFilterRole] = useState('All');

  const headers = { Authorization: `Bearer ${getToken()}` };

  const loadUsers = async () => {
    const res = await axios.get(`${API}/admin/users`, { headers });
    setUsers(res.data);
  };

  useEffect(() => { loadUsers(); }, []);

  const toggleUser = async (id: string, name: string, active: boolean) => {
    if (!confirm(`${active ? 'Deactivate' : 'Activate'} user "${name}"?`)) return;
    await axios.put(`${API}/admin/users/${id}/toggle`, {}, { headers });
    setMessage(`User "${name}" ${active ? 'deactivated' : 'activated'}.`);
    loadUsers();
  };

  const deleteUser = async (id: string, name: string) => {
    if (!confirm(`Permanently delete user "${name}"? This cannot be undone.`)) return;
    await axios.delete(`${API}/admin/users/${id}`, { headers });
    setMessage(`User "${name}" deleted.`);
    loadUsers();
  };

  const resetPassword = async () => {
    if (!newPassword || newPassword.length < 6) { alert('Password must be at least 6 characters.'); return; }
    await axios.put(`${API}/admin/users/${resetId}/reset-password`, { newPassword }, { headers });
    setMessage('Password reset successfully.');
    setResetId(null); setNewPassword('');
  };

  const filtered = filterRole === 'All' ? users : users.filter(u => u.role === filterRole);

  // Get current user email from JWT to prevent self-deletion
  const getCurrentUserEmail = () => {
    const token = getToken();
    if (!token) return '';
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || payload.email || '').toLowerCase();
    } catch { return ''; }
  };
  const currentEmail = getCurrentUserEmail();

  return (
    <div style={{ fontFamily: "'Segoe UI', sans-serif", background: '#f0f4f8', minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Header portalName="Admin Portal" portalIcon="🔐" color={COLOR} />

      <div style={{ padding: '1.5rem' }}>
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h1 style={{ margin: 0, color: COLOR }}>User Management</h1>
            <p style={{ margin: '4px 0 0', color: '#666', fontSize: '0.9rem' }}>Create and manage user accounts for all portals</p>
          </div>
          <button style={{ padding: '10px 20px', background: COLOR, color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600, fontSize: '1rem' }}
            onClick={() => setShowForm(true)}>+ Create New User</button>
        </div>

        {message && <div style={{ background: '#d4edda', color: '#155724', padding: '10px 16px', borderRadius: '6px', marginBottom: '1rem' }}>✓ {message}</div>}

        {/* Stats */}
        <div style={{ display: 'flex', gap: '1rem', marginBottom: '1.5rem', flexWrap: 'wrap' }}>
          {['All', ...ROLES].map(role => {
            const count = role === 'All' ? users.length : users.filter(u => u.role === role).length;
            const color = role === 'All' ? '#555' : ROLE_COLORS[role];
            return (
              <div key={role} onClick={() => setFilterRole(role)}
                style={{ background: '#fff', borderRadius: '8px', padding: '1rem 1.5rem', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', borderTop: `4px solid ${color}`, cursor: 'pointer', minWidth: '120px', opacity: filterRole === role ? 1 : 0.7 }}>
                <div style={{ fontSize: '0.8rem', color: '#666' }}>{role === 'IRD_Officer' ? 'IRD Officer' : role}</div>
                <div style={{ fontSize: '1.8rem', fontWeight: 700, color }}>{count}</div>
              </div>
            );
          })}
        </div>

        {/* Users Table */}
        <div style={{ background: '#fff', borderRadius: '8px', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: COLOR, color: '#fff' }}>
                <th style={th}>Full Name</th>
                <th style={th}>Email</th>
                <th style={th}>Role</th>
                <th style={th}>TIN / Staff ID</th>
                <th style={th}>Status</th>
                <th style={th}>Created</th>
                <th style={th}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>No users found.</td></tr>
              ) : filtered.map(u => (
                <tr key={u.id} style={{ borderBottom: '1px solid #eee', opacity: u.isActive ? 1 : 0.6 }}>
                  <td style={td}>{u.fullName}</td>
                  <td style={td}>{u.email}</td>
                  <td style={td}>
                    <span style={{ background: ROLE_COLORS[u.role] + '20', color: ROLE_COLORS[u.role], padding: '3px 10px', borderRadius: '12px', fontSize: '0.8rem', fontWeight: 600 }}>
                      {u.role.replace('_', ' ')}
                    </span>
                  </td>
                  <td style={td}>
                    {(u.role === 'IRD_Officer' || u.role === 'SystemAdmin')
                      ? <span style={{ color: '#7b1fa2', fontSize: '0.85rem' }}>{u.tin || '—'}</span>
                      : <code>{u.tin || '—'}</code>}
                  </td>
                  <td style={td}>
                    <span style={{ color: u.isActive ? '#27ae60' : '#e74c3c', fontWeight: 600 }}>
                      {u.isActive ? '✓ Active' : '✗ Inactive'}
                    </span>
                  </td>
                  <td style={td}>{new Date(u.createdAt).toLocaleDateString()}</td>
                  <td style={td}>
                    <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                      {u.isProtected ? (
                        <span style={{ color: '#888', fontSize: '0.8rem', fontStyle: 'italic' }}>🔒 Demo account</span>
                      ) : (
                        <>
                          <button style={{ ...btn, background: u.isActive ? '#e67e22' : '#27ae60' }}
                            onClick={() => toggleUser(u.id, u.fullName, u.isActive)}>
                            {u.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                          <button style={{ ...btn, background: '#17a2b8' }}
                            onClick={() => { setResetId(u.id); setNewPassword(''); }}>
                            Reset Pwd
                          </button>
                          <button style={{ ...btn, background: '#e74c3c' }}
                            onClick={() => deleteUser(u.id, u.fullName)}
                            disabled={u.email.toLowerCase() === currentEmail}
                            title={u.email.toLowerCase() === currentEmail ? 'You cannot delete your own account' : 'Delete user'}>
                            Delete
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Create User Modal */}
      {showForm && <CreateUserModal headers={headers} onClose={() => setShowForm(false)}
        onCreated={(msg) => { setMessage(msg); setShowForm(false); loadUsers(); }} />}

      {/* Reset Password Modal */}
      {resetId && (
        <div style={overlay}>
          <div style={modal}>
            <h3 style={{ margin: '0 0 1rem', color: COLOR }}>Reset Password</h3>
            <input style={{ width: '100%', padding: '10px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box', marginBottom: '1rem' }}
              type="password" placeholder="New password (min 6 chars)" value={newPassword}
              onChange={e => setNewPassword(e.target.value)} />
            <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
              <button style={{ ...btn, background: '#6c757d' }} onClick={() => setResetId(null)}>Cancel</button>
              <button style={{ ...btn, background: COLOR }} onClick={resetPassword}>Reset Password</button>
            </div>
          </div>
        </div>
      )}
      <Footer />
    </div>
  );
}

function CreateUserModal({ headers, onClose, onCreated }: { headers: any; onClose: () => void; onCreated: (msg: string) => void }) {
  const [form, setForm] = useState({ email: '', password: '', fullName: '', role: 'Employer', tin: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';

  const set = (f: string, v: string) => setForm(p => ({ ...p, [f]: v }));
  const needsTin = form.role === 'Employer' || form.role === 'Employee';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.fullName) errs.fullName = 'Required';
    if (!form.email) errs.email = 'Required';
    if (!form.password || form.password.length < 6) errs.password = 'Min 6 characters';
    if (needsTin && (!form.tin || !/^\d{9}$/.test(form.tin.trim()))) errs.tin = 'A valid 9-digit TIN is required';
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;
    setLoading(true);
    try {
      const payload = { ...form, tin: needsTin ? form.tin : '' };
      await axios.post(`${API}/admin/users`, payload, { headers });
      onCreated(`User "${form.fullName}" created successfully.`);
    } catch (err: any) {
      setErrors({ general: err.response?.data?.message || 'Failed to create user.' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={overlay}>
      <div style={{ ...modal, width: '440px' }}>
        <h3 style={{ margin: '0 0 1.5rem', color: '#b71c1c' }}>Create New User</h3>
        {errors.general && <div style={{ background: '#fdecea', color: '#c0392b', padding: '8px 12px', borderRadius: '6px', marginBottom: '1rem', fontSize: '0.9rem' }}>⚠ {errors.general}</div>}
        <form onSubmit={handleSubmit}>
          {/* Role selector — FIRST */}
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', fontWeight: 600, marginBottom: '4px', fontSize: '0.9rem' }}>Role *</label>
            <select style={{ width: '100%', padding: '9px 12px', border: '1px solid #ccc', borderRadius: '6px', fontSize: '1rem' }}
              value={form.role} onChange={e => { set('role', e.target.value); if (e.target.value !== 'Employer' && e.target.value !== 'Employee') set('tin', ''); }}>
              {ROLES.map(r => <option key={r} value={r}>{r.replace('_', ' ')}</option>)}
            </select>
          </div>

          {/* Standard fields */}
          {[
            { label: 'Full Name *', field: 'fullName', type: 'text', placeholder: 'Enter full name' },
            { label: 'Email *', field: 'email', type: 'email', placeholder: 'Enter email address' },
            { label: 'Password *', field: 'password', type: 'password', placeholder: 'Min 6 characters' },
          ].map(({ label, field, type, placeholder }) => (
            <div key={field} style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: '4px', fontSize: '0.9rem' }}>{label}</label>
              <input style={{ width: '100%', padding: '9px 12px', border: `1px solid ${errors[field] ? '#e74c3c' : '#ccc'}`, borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box' }}
                type={type} placeholder={placeholder} value={(form as any)[field]}
                onChange={e => set(field, e.target.value)} />
              {errors[field] && <p style={{ color: '#c0392b', fontSize: '0.82rem', margin: '3px 0 0' }}>{errors[field]}</p>}
            </div>
          ))}

          {/* TIN field — only shown for Employer and Employee */}
          {needsTin && (
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: '4px', fontSize: '0.9rem' }}>TIN *</label>
              <input style={{ width: '100%', padding: '9px 12px', border: `1px solid ${errors.tin ? '#e74c3c' : '#ccc'}`, borderRadius: '6px', fontSize: '1rem', boxSizing: 'border-box' }}
                type="text" placeholder="9-digit Taxpayer ID" value={form.tin} maxLength={9}
                onChange={e => set('tin', e.target.value.replace(/\D/g, ''))} />
              {errors.tin && <p style={{ color: '#c0392b', fontSize: '0.82rem', margin: '3px 0 0' }}>{errors.tin}</p>}
            </div>
          )}

          <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
            <button type="button" style={{ ...btn, background: '#6c757d' }} onClick={onClose}>Cancel</button>
            <button type="submit" style={{ ...btn, background: '#b71c1c' }} disabled={loading}>
              {loading ? 'Creating...' : 'Create User'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const th: React.CSSProperties = { padding: '12px 14px', textAlign: 'left', fontWeight: 600, fontSize: '0.9rem' };
const td: React.CSSProperties = { padding: '10px 14px', fontSize: '0.9rem' };
const btn: React.CSSProperties = { padding: '5px 12px', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.82rem', fontWeight: 600 };
const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modal: React.CSSProperties = { background: '#fff', padding: '2rem', borderRadius: '12px', width: '380px', boxShadow: '0 8px 30px rgba(0,0,0,0.2)' };
