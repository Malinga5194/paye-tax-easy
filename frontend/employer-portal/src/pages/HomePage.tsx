export default function HomePage() {
  return (
    <div style={styles.page}>
      <div style={styles.hero}>
        <div style={styles.logo}>🏛️</div>
        <h1 style={styles.title}>PAYE Tax Easy</h1>
        <p style={styles.subtitle}>
          Sri Lanka's centralized PAYE tax management platform.<br />
          Accurate deductions across employment changes — powered by IRD integration.
        </p>
      </div>

      <div style={styles.cards}>
        <PortalCard
          icon="🏢"
          title="Employer Portal"
          description="Manage employee salaries, calculate PAYE deductions, retrieve IRD records, and file tax returns."
          color="#003366"
          href="http://localhost:5173/login"
          buttonLabel="Employer Login"
        />
        <PortalCard
          icon="👤"
          title="Employee Portal"
          description="View your PAYE deduction history across all employers and download your tax records as PDF."
          color="#1a6b3c"
          href="http://localhost:5174"
          buttonLabel="Employee Login"
        />
        <PortalCard
          icon="🏛️"
          title="IRD Dashboard"
          description="Monitor employer compliance, generate reports, and access full audit logs of all submissions."
          color="#7b1fa2"
          href="http://localhost:5175"
          buttonLabel="IRD Officer Login"
        />
        <PortalCard
          icon="🔐"
          title="Admin Portal"
          description="Manage user accounts, create logins for all portal types, activate or deactivate users."
          color="#b71c1c"
          href="http://localhost:5176"
          buttonLabel="Admin Login"
        />
      </div>

      <div style={styles.infoBar}>
        <div style={styles.infoItem}>
          <span style={styles.infoIcon}>📅</span>
          <span>Financial Year: 1 April 2025 – 31 March 2026</span>
        </div>
        <div style={styles.infoItem}>
          <span style={styles.infoIcon}>⚖️</span>
          <span>Inland Revenue (Amendment) Act No. 02 of 2025</span>
        </div>
        <div style={styles.infoItem}>
          <span style={styles.infoIcon}>📋</span>
          <span>Filing Deadline: 30 November 2026</span>
        </div>
      </div>

      <footer style={styles.footer}>
        <p>PAYE Tax Easy — SLIIT MSc IT | Software Engineering Practices (IT5030) | Group 02</p>
      </footer>
    </div>
  );
}

interface PortalCardProps {
  icon: string;
  title: string;
  description: string;
  color: string;
  href: string;
  buttonLabel: string;
}

function PortalCard({ icon, title, description, color, href, buttonLabel }: PortalCardProps) {
  return (
    <div style={{ ...styles.card, borderTop: `5px solid ${color}` }}>
      <div style={styles.cardIcon}>{icon}</div>
      <h2 style={{ ...styles.cardTitle, color }}>{title}</h2>
      <p style={styles.cardDesc}>{description}</p>
      <a href={href} target="_blank" rel="noopener noreferrer" style={{ ...styles.cardBtn, background: color }}>
        {buttonLabel} →
      </a>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: {
    fontFamily: "'Segoe UI', sans-serif",
    minHeight: '100vh',
    background: 'linear-gradient(135deg, #f0f4f8 0%, #e8edf2 100%)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
  },
  hero: {
    textAlign: 'center',
    padding: '3rem 2rem 2rem',
  },
  logo: {
    fontSize: '3.5rem',
    marginBottom: '0.5rem',
  },
  title: {
    fontSize: '2.8rem',
    color: '#003366',
    margin: '0 0 1rem',
    fontWeight: 700,
  },
  subtitle: {
    fontSize: '1.1rem',
    color: '#555',
    lineHeight: 1.7,
    maxWidth: '600px',
    margin: '0 auto',
  },
  cards: {
    display: 'flex',
    gap: '1.5rem',
    padding: '2rem',
    flexWrap: 'wrap',
    justifyContent: 'center',
    maxWidth: '1100px',
    width: '100%',
  },
  card: {
    background: '#fff',
    borderRadius: '12px',
    padding: '2rem',
    width: '300px',
    boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    textAlign: 'center',
    transition: 'transform 0.2s',
  },
  cardIcon: {
    fontSize: '2.5rem',
    marginBottom: '1rem',
  },
  cardTitle: {
    fontSize: '1.3rem',
    fontWeight: 700,
    margin: '0 0 0.75rem',
  },
  cardDesc: {
    color: '#666',
    fontSize: '0.95rem',
    lineHeight: 1.6,
    flex: 1,
    marginBottom: '1.5rem',
  },
  cardBtn: {
    display: 'inline-block',
    padding: '10px 24px',
    color: '#fff',
    borderRadius: '6px',
    textDecoration: 'none',
    fontWeight: 600,
    fontSize: '0.95rem',
    width: '100%',
    boxSizing: 'border-box',
  },
  infoBar: {
    display: 'flex',
    gap: '2rem',
    background: '#003366',
    color: '#fff',
    padding: '1rem 2rem',
    borderRadius: '8px',
    flexWrap: 'wrap',
    justifyContent: 'center',
    margin: '0 2rem 2rem',
    fontSize: '0.9rem',
  },
  infoItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
  },
  infoIcon: {
    fontSize: '1.1rem',
  },
  footer: {
    color: '#999',
    fontSize: '0.85rem',
    padding: '1rem',
    textAlign: 'center',
  },
};
