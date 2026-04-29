import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5050';

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

// Attach JWT token — persisted in sessionStorage so it survives page refreshes
let _token: string | null = sessionStorage.getItem('paye_token');
export const setToken = (token: string | null) => {
  _token = token;
  if (token) sessionStorage.setItem('paye_token', token);
  else sessionStorage.removeItem('paye_token');
};
export const getToken = () => _token;

apiClient.interceptors.request.use(config => {
  if (_token) config.headers.Authorization = `Bearer ${_token}`;
  return config;
});

apiClient.interceptors.response.use(
  res => res,
  err => {
    // Only redirect on 401 if NOT on the tax-report or ird endpoints (avoid redirect loop)
    if (err.response?.status === 401 && !err.config?.url?.includes('tax-report')) {
      setToken(null);
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);
