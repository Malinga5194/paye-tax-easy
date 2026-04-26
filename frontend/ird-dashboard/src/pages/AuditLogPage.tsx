import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { getToken } from '../App';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5050';
const HOME_URL = 'http://localhost:5173';

interface AuditEntry {
  id: number;
  actorId: string;
  actorRole: string;
  action: string;
  entityType: string;
  entityId: string;
  financialYear: string;
  timestamp: string;
}

export default function AuditLogPage() {
  const [logs, setLogs] = useState<AuditEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [actionType, setActionType] = useState('');
  const navigate = useNavigate();
  const headers = { Authorization: `Bearer ${getToken()}` };

  const loadLogs = async () => {
    const params = new URLSearchParams({ page: String(page), pageSize: '20' });
    if (actionType) params.append('actionType', actionType);
    const res = await axios.get(`${API}/ird-dashboard/audit-logs?${params}`, { headers });
    setLogs(res.data.items);
    setTotal(res.data.totalCount);
  };

  useEffect(() => { loadLogs(); }, [page, actionType]);

  return (
    <div style={{ fontFamily: 'sans-serif', background: '#f8f9fa', minHeight: '100vh' }}>
      <nav style={{ background: '#7b1fa2', display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.75rem 1.5rem', color: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.15)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <a href={HOME_URL} style={{ color: '#fff', textDecoration: 'none', fontWeight: 700, fontSize: '1.1rem' }}>🏛️ PAYE Tax Easy</a>
          <span style={{ opacity: 0.4 }}>|</span>
          <span style={{ opacity: 0.85, fontSize: '0.95rem' }}>IRD Dashboard</span>
        </div>
        <a href={HOME_URL} style={{ padding: '5px 14px', background: 'rgba(255,255,255,0.2)', color: '#fff', border: '1px solid rgba(255,255,255,0.4)', borderRadius: '4px', textDecoration: 'none', fontSize: '0.85rem', fontWeight: 600 }}>← Home</a>
      </nav>
      <div style={styles.page}>
      <header style={styles.header}>
        <h1 style={styles.title}>Audit Logs</h1>
        <button style={styles.btnBack} onClick={() => navigate('/compliance')}>← Back</button>
      </header>

      <div style={styles.toolbar}>
        <input style={styles.input} placeholder="Filter by action type..."
          value={actionType} onChange={e => { setActionType(e.target.value); setPage(1); }} />
        <span style={{ color: '#666' }}>Total: {total} entries</span>
      </div>

      <table style={styles.table}>
        <thead>
          <tr style={styles.thead}>
            <th>ID</th><th>Actor</th><th>Role</th><th>Action</th>
            <th>Entity</th><th>FY</th><th>Timestamp</th>
          </tr>
        </thead>
        <tbody>
          {logs.map(log => (
            <tr key={log.id} style={styles.tr}>
              <td style={styles.td}>{log.id}</td>
              <td style={styles.td}>{log.actorId.slice(0, 8)}...</td>
              <td style={styles.td}>{log.actorRole}</td>
              <td style={styles.td}><span style={styles.badge}>{log.action}</span></td>
              <td style={styles.td}>{log.entityType}: {log.entityId.slice(0, 8)}</td>
              <td style={styles.td}>{log.financialYear || '—'}</td>
              <td style={styles.td}>{new Date(log.timestamp).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div style={styles.pagination}>
        <button disabled={page === 1} onClick={() => setPage(p => p - 1)} style={styles.pageBtn}>← Prev</button>
        <span>Page {page}</span>
        <button disabled={logs.length < 20} onClick={() => setPage(p => p + 1)} style={styles.pageBtn}>Next →</button>
      </div>
    </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { fontFamily: 'sans-serif', padding: '1.5rem', background: '#f8f9fa', minHeight: '100vh' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' },
  title: { color: '#7b1fa2', margin: 0 },
  toolbar: { display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '1rem' },
  input: { padding: '8px 12px', border: '1px solid #ccc', borderRadius: '4px', width: '300px' },
  table: { width: '100%', borderCollapse: 'collapse', background: '#fff', borderRadius: '8px', overflow: 'hidden', boxShadow: '0 1px 4px rgba(0,0,0,0.1)' },
  thead: { background: '#7b1fa2', color: '#fff' },
  tr: { borderBottom: '1px solid #eee' },
  td: { padding: '10px 12px', fontSize: '0.85rem' },
  badge: { background: '#e8eaf6', color: '#3949ab', padding: '2px 8px', borderRadius: '12px', fontSize: '0.8rem' },
  pagination: { display: 'flex', gap: '1rem', alignItems: 'center', justifyContent: 'center', marginTop: '1rem' },
  pageBtn: { padding: '6px 16px', background: '#7b1fa2', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
  btnBack: { padding: '8px 16px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' },
};
