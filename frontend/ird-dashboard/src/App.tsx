import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import CompliancePage from './pages/CompliancePage';
import AuditLogPage from './pages/AuditLogPage';
import EmployeeSearchPage from './pages/EmployeeSearchPage';
import EmployerSearchPage from './pages/EmployerSearchPage';

let _token: string | null = sessionStorage.getItem('token');
export const setToken = (t: string | null) => { _token = t; if (t) sessionStorage.setItem('token', t); else sessionStorage.removeItem('token'); };
export const getToken = () => _token;

function PrivateRoute({ children }: { children: React.ReactNode }) {
  return getToken() ? <>{children}</> : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/compliance" element={<PrivateRoute><CompliancePage /></PrivateRoute>} />
        <Route path="/audit-logs" element={<PrivateRoute><AuditLogPage /></PrivateRoute>} />
        <Route path="/employee-search" element={<PrivateRoute><EmployeeSearchPage /></PrivateRoute>} />
        <Route path="/employer-search" element={<PrivateRoute><EmployerSearchPage /></PrivateRoute>} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
