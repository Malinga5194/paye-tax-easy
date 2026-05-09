const HOME_URL = 'http://localhost:5173';

interface HeaderProps {
  portalName: string;
  portalIcon: string;
  color: string;
  onLogout?: () => void;
}

export function Header({ portalName, portalIcon, color, onLogout }: HeaderProps) {
  return (
    <header style={{ ...styles.header, background: color }}>
      <div style={styles.headerLeft}>
        <span style={styles.logo}>🏛️</span>
        <div>
          <div style={styles.title}>PAYE Tax Easy</div>
          <div style={styles.subtitle}>Inland Revenue Department — Sri Lanka</div>
        </div>
      </div>
      <div style={styles.headerRight}>
        <span style={styles.portalBadge}>{portalIcon} {portalName}</span>
        <a href={HOME_URL} style={styles.homeBtn}>← Home</a>
        {onLogout && <button onClick={onLogout} style={styles.logoutBtn}>⏻ Logout</button>}
      </div>
    </header>
  );
}

export function Footer() {
  return (
    <footer style={styles.footer}>
      <div style={styles.footerTop}>
        <div style={styles.footerCol}>
          <strong style={styles.footerHeading}>🏛️ PAYE Tax Easy</strong>
          <p style={styles.footerText}>
            A centralized PAYE tax management platform for Sri Lanka,
            integrating employer payroll systems with the Inland Revenue Department.
          </p>
        </div>
        <div style={styles.footerCol}>
          <strong style={styles.footerHeading}>Portals</strong>
          <a href="http://localhost:5173" style={styles.footerLink}>🏠 Landing Page</a>
          <a href="http://localhost:5173" target="_blank" rel="noreferrer" style={styles.footerLink}>🏢 Employer Portal</a>
          <a href="http://localhost:5174" target="_blank" rel="noreferrer" style={styles.footerLink}>👤 Employee Portal</a>
          <a href="http://localhost:5175" target="_blank" rel="noreferrer" style={styles.footerLink}>🏛️ IRD Dashboard</a>
          <a href="http://localhost:5176" target="_blank" rel="noreferrer" style={styles.footerLink}>🔐 Admin Portal</a>
        </div>
        <div style={styles.footerCol}>
          <strong style={styles.footerHeading}>Legal &amp; Compliance</strong>
          <p style={styles.footerText}>Inland Revenue (Amendment) Act, No. 02 of 2025</p>
          <p style={styles.footerText}>Financial Year: 1 April 2025 – 31 March 2026</p>
          <p style={styles.footerText}>Filing Deadline: 30 November 2026</p>
        </div>
        <div style={styles.footerCol}>
          <strong style={styles.footerHeading}>Project Info</strong>
          <p style={styles.footerText}>SLIIT MSc IT</p>
          <p style={styles.footerText}>Software Engineering Practices (IT5030)</p>
          <p style={styles.footerText}>Group 02 — 2026</p>
        </div>
      </div>
      <div style={styles.footerBottom}>
        © 2025 PAYE Tax Easy &nbsp;|&nbsp; All rights reserved &nbsp;|&nbsp;
        SLIIT MSc IT — Software Engineering Practices (IT5030) &nbsp;|&nbsp; Group 02
      </div>
    </footer>
  );
}

const styles: Record<string, React.CSSProperties> = {
  // Header
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.9rem 2rem', color: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.15)', position: 'sticky', top: 0, zIndex: 100 },
  headerLeft: { display: 'flex', alignItems: 'center', gap: '1rem' },
  logo: { fontSize: '2rem' },
  title: { fontWeight: 700, fontSize: '1.2rem', letterSpacing: '0.3px' },
  subtitle: { fontSize: '0.75rem', opacity: 0.8 },
  headerRight: { display: 'flex', alignItems: 'center', gap: '1rem' },
  portalBadge: { background: 'rgba(255,255,255,0.15)', padding: '4px 12px', borderRadius: '20px', fontSize: '0.85rem', fontWeight: 600 },
  homeBtn: { padding: '6px 16px', background: 'rgba(255,255,255,0.2)', color: '#fff', border: '1.5px solid rgba(255,255,255,0.5)', borderRadius: '6px', textDecoration: 'none', fontWeight: 600, fontSize: '0.85rem' },
  logoutBtn: { padding: '6px 16px', background: '#e74c3c', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 600, fontSize: '0.85rem' },
  // Footer
  footer: { background: '#1a1a2e', color: '#ccc', padding: '2.5rem 2rem 1rem', marginTop: 'auto' },
  footerTop: { display: 'flex', gap: '2rem', flexWrap: 'wrap', marginBottom: '1.5rem', justifyContent: 'space-between' },
  footerCol: { display: 'flex', flexDirection: 'column', gap: '0.4rem', flex: 1, minWidth: '160px' },
  footerHeading: { color: '#fff', fontSize: '0.95rem', marginBottom: '0.3rem' },
  footerText: { fontSize: '0.82rem', color: '#999', margin: 0 },
  footerLink: { color: '#aaa', textDecoration: 'none', fontSize: '0.82rem' },
  footerBottom: { borderTop: '1px solid #333', paddingTop: '1rem', textAlign: 'center', fontSize: '0.78rem', color: '#666' },
};
