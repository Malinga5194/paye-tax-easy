import { useState } from 'react';
import { apiClient } from '../api/client';
import TaxReportModal from './TaxReportModal';

interface Props {
  employeeTin: string;
  financialYear: string;
  period: string;
  onFetched: () => void;
}

export default function IrdFetchButton({ employeeTin, financialYear, period, onFetched }: Props) {
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState<any>(null);
  const [showModal, setShowModal] = useState(false);

  const handleFetch = async () => {
    console.log('Fetch IRD clicked for TIN:', employeeTin, 'period:', period);
    setLoading(true);
    try {
      console.log('Calling IRD cumulative...');
      await apiClient.get(`/ird/cumulative/${employeeTin}/${financialYear}`);
      console.log('Calling tax report...');
      const res = await apiClient.get(`/tax-report/${employeeTin}/${financialYear}/${period}`);
      console.log('Report received:', res.data);
      setReport(res.data);
      setShowModal(true);
      onFetched();
    } catch (err: any) {
      console.error('Error:', err);
      const msg = err.response?.data?.message || err.message || 'Unknown error';
      alert(`Failed to fetch IRD data: ${msg}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button type="button" style={styles.btn} onClick={handleFetch} disabled={loading}>
        {loading ? '⏳ Fetching...' : '📋 Fetch IRD & Report'}
      </button>

      {showModal && report && (
        <TaxReportModal
          report={report}
          period={period}
          onClose={() => setShowModal(false)}
        />
      )}
    </>
  );
}

const styles: Record<string, React.CSSProperties> = {
  btn: { padding: '5px 12px', background: '#17a2b8', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.82rem', fontWeight: 600, whiteSpace: 'nowrap' },
};
