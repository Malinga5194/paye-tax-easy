import { useState } from 'react';
import { apiClient } from '../api/client';
import { CumulativeData } from '../types';

interface Props {
  employeeTin: string;
  financialYear: string;
  onFetched: () => void;
}

export default function IrdFetchButton({ employeeTin, financialYear, onFetched }: Props) {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<CumulativeData | null>(null);

  const handleFetch = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(`/ird/cumulative/${employeeTin}/${financialYear}`);
      setData(res.data);
      onFetched();
    } catch {
      alert('Failed to fetch IRD data. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (data) {
    return (
      <div style={{ fontSize: '0.8rem', color: '#27ae60' }}>
        ✓ IRD: Rs. {data.cumulativeDeduction.toLocaleString()} deducted
      </div>
    );
  }

  return (
    <button style={styles.btn} onClick={handleFetch} disabled={loading}>
      {loading ? '...' : 'Fetch IRD'}
    </button>
  );
}

const styles: Record<string, React.CSSProperties> = {
  btn: { padding: '4px 10px', background: '#17a2b8', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85rem' },
};
