import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

// Attach JWT token from memory on every request
let _token: string | null = null;
export const setToken = (token: string | null) => { _token = token; };
export const getToken = () => _token;

apiClient.interceptors.request.use(config => {
  if (_token) config.headers.Authorization = `Bearer ${_token}`;
  return config;
});

apiClient.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      setToken(null);
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);
