import { ROUTES } from './config.js';
import { state } from './state.js';
import { refreshCart, mergeGuestCartToServer } from './cart.js';
import { closeAuthModal, setAuthResult, showToast, switchAuthView } from './ui.js';
import {
  api,
  authHeaders,
  clearToken,
  clearRefreshToken,
  getToken,
  getRefreshToken,
  setToken,
  setRefreshToken
} from './api.js';

// JWT claims are used only for frontend presentation; backend still enforces authorization.
function jwtClaims() {
  try {
    const encoded = getToken().split('.')[1];
    const base64 = encoded.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  } catch { return {}; }
}

function currentRole(claims, me = {}) {
  const names = [
    me.role, ...(Array.isArray(me.roles) ? me.roles : []),
    claims.role,
    claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
  ].flat().filter(Boolean);
  return names.some(x => String(x).toLowerCase() === 'admin') ? 'Admin' : 'User';
}

export async function refreshMe() {
  if (!getToken()) { state.me = null; return; }
  const claims = jwtClaims();
  try {
    const profile = await api(ROUTES.profile, { headers: authHeaders(false) });
    let orders = [];
    try {
      const payload = await api(ROUTES.myOrders, { headers: authHeaders(false) });
      orders = Array.isArray(payload) ? payload : (payload?.items || payload?.orders || []);
    } catch (error) { console.warn('Історія замовлень недоступна:', error.message); }
    state.me = { ...profile, role: currentRole(claims, profile), profile, orders };
  } catch (error) {
    if (error.status === 401) { clearToken(); clearRefreshToken(); state.me = null; return; }
    // Some versions of the monolith expose only the token, without a /api/profile GET.
    state.me = { email: claims.email || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '', role: currentRole(claims), profile: null, orders: [] };
    console.warn('Профіль не завантажено:', error.message);
  }
}

export async function submitLogin(e) {
  e.preventDefault();
  const payload = {
    email: document.getElementById('loginEmail')?.value.trim(),
    password: document.getElementById('loginPassword')?.value
  };

  try {
    const data = await api(ROUTES.login, {
      method: 'POST',
      headers: authHeaders(true),
      body: JSON.stringify(payload)
    });
    setToken(data.accessToken || data.token || data.jwtToken || '');
    if (data.refreshToken) setRefreshToken(data.refreshToken);
    await mergeGuestCartToServer();
    await refreshMe();
    await refreshCart();
    setAuthResult(data);
    closeAuthModal();
    showToast('Вхід виконано успішно');
    if (document.body.dataset.page === 'profile') window.location.reload();
  } catch (error) {
    setAuthResult(error.payload || error.message);
    showToast(error.message || 'Не вдалося увійти');
  }
}

export async function submitRegister(e) {
  e.preventDefault();
  const firstName =
    document.getElementById('registerFirstName')?.value.trim();

  const lastName =
    document.getElementById('registerLastName')?.value.trim();

  const payload = {
    email: document.getElementById('registerEmail')?.value.trim(),
    password: document.getElementById('registerPassword')?.value,
    firstName,
    lastName,
    phone: document.getElementById('registerPhone')?.value.trim() || ''
  };
  try {
    const data = await api(ROUTES.register, {
      method: 'POST',
      headers: authHeaders(true),
      body: JSON.stringify(payload)
    });
    setAuthResult(data);
    switchAuthView('login');
    showToast('Реєстрація успішна. Тепер увійди.');
  } catch (error) {
    setAuthResult(error.payload || error.message);
    showToast(error.message || 'Не вдалося зареєструватися');
  }
}

export function initGoogleLogin() {
    return;
}


export async function logout() {
  // Stateless JWT logout: discard locally stored access/refresh tokens.
  clearToken();
  clearRefreshToken();
  state.me = null;
  showToast('Ви вийшли з акаунта');
  window.location.href = 'index.html';
}

export function initAuthPage() {
  if (document.body.dataset.page !== 'auth') return;
  document.getElementById('loginBtn')?.addEventListener('click', (e) => submitLogin(e));
  document.getElementById('registerBtn')?.addEventListener('click', (e) => submitRegister(e));
  document.getElementById('meBtn')?.addEventListener('click', async () => {
    await refreshMe();
    setAuthResult(state.me || 'Користувач не авторизований');
  });
  document.getElementById('logoutBtn')?.addEventListener('click', logout);
  initGoogleLogin();
}
