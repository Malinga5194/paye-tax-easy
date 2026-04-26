const HOME_URL = 'http://localhost:5173';

interface NavbarProps {
  title: string;
  color: string;
  userName?: string;
  onLogout?: () => void;
}

export default function Navbar({ title, color, userName, onLogout }: NavbarProps) {
  return (
    <nav style={{ ...styles.nav, background: color }}>
      <div style={styles.left}>
        <a href={HOME_URL} style={styles.homeBtn} title="Back to Home">
          🏛️ PAYE Tax Easy
        </a>
        <span style={styles.divider}>|</span>
        <span style={styles.portalName}>{title}</span>
      </div>
      <div style={styles.right}>
        {userName && <span style={styles.user}>👤 {userName}</span>}
        {onLogout && (
          <button style={styles.logoutBtn} onClick={onLogout}>
            Sign Out
          </button>
        )}
        <a href={HOME_URL} style={styles.homeLink}>
          ← Home
        </a>
      </div>
    </nav>
  );
}

const styles: Record<string, React.CSSProperties> = {
  nav: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '0.75rem 1.5rem',
    color: '#fff',
    boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
    position: 'sticky',
    top: 0,
    zIndex: 100,
  },
  left: { display: 'flex', alignItems: 'center', gap: '0.75rem' },
  homeBtn: {
    color: '#fff',
    textDecoration: 'none',
    fontWeight: 700,
    fontSize: '1.1rem',
    letterSpacing: '0.3px',
  },
  divider: { opacity: 0.4, fontSize: '1.2rem' },
  portalName: { opacity: 0.85, fontSize: '0.95rem' },
  right: { display: 'flex', alignItems: 'center', gap: '1rem' },
  user: { fontSize: '0.9rem', opacity: 0.9 },
  logoutBtn: {
    padding: '5px 14px',
    background: 'rgba(255,255,255,0.15)',
    color: '#fff',
    border: '1px solid rgba(255,255,255,0.3)',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '0.85rem',
  },
  homeLink: {
    padding: '5px 14px',
    background: 'rgba(255,255,255,0.2)',
    color: '#fff',
    border: '1px solid rgba(255,255,255,0.4)',
    borderRadius: '4px',
    textDecoration: 'none',
    fontSize: '0.85rem',
    fontWeight: 600,
  },
};
