import { API_BASE } from './config.js';

export function getToken() {
  return localStorage.getItem('accessToken') || '';
}

export function setToken(token) {
  localStorage.setItem('accessToken', token);
}

export function clearToken() {
  localStorage.removeItem('accessToken');
}

export function authHeaders(withJson = true) {
  const headers = {};
  if (withJson) headers['Content-Type'] = 'application/json';
  if (getToken()) headers.Authorization = `Bearer ${getToken()}`;
  return headers;
}

function parseMaybeJson(value) {
  if (typeof value !== 'string') return value;
  const trimmed = value.trim();
  if (!trimmed || (!trimmed.startsWith('{') && !trimmed.startsWith('['))) return value;

  try {
    return JSON.parse(trimmed);
  } catch {
    return value;
  }
}

export function getApiErrorMessage(payload, fallback = 'Помилка запиту') {
  const data = parseMaybeJson(payload);

  if (typeof data === 'string') {
    return data || fallback;
  }

  if (!data || typeof data !== 'object') {
    return fallback;
  }

  if (data.errors && typeof data.errors === 'object') {
    const firstError = Object.values(data.errors).flat().find(Boolean);
    if (firstError) {
      const text = String(firstError);
      if (text.includes("'undefined'") || text.toLowerCase().includes('undefined')) {
        return 'Не вдалося визначити товар у кошику. Онови сторінку та спробуй ще раз.';
      }
      return text;
    }
  }

  return data.message || data.title || fallback;
}

export async function api(path, options = {}) {
  const requestOptions = { cache: 'no-store', ...options };
  const response = await fetch(`${API_BASE}${path}`, requestOptions);
  const contentType = response.headers.get('content-type') || '';
  let payload = null;

  if (contentType.includes('application/json')) {
    payload = await response.json().catch(() => null);
  } else {
    const text = await response.text().catch(() => '');
    payload = parseMaybeJson(text);
  }

  if (!response.ok) {
    const error = new Error(getApiErrorMessage(payload));
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  return payload;
}

export function getRefreshToken() {
  return localStorage.getItem('refreshToken') || '';
}

export function setRefreshToken(token) {
  localStorage.setItem('refreshToken', token);
}

export function clearRefreshToken() {
  localStorage.removeItem('refreshToken');
}