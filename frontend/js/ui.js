import { SITE_BASE } from './config.js';

export function escapeHtml(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

export function formatPrice(value) {
  return `${Number(value || 0).toFixed(0)} UAH`;
}

export function getCategoryName(product) {
  return product?.category?.categoryName || product?.category?.name || product?.categoryName || product?.CategoryName || 'Категорія';
}

export function normalizeImageUrl(url) {
  if (!url) return '';
  if (url.startsWith('http')) return url;
  return `${SITE_BASE}${url}`;
}

export function getImageUrl(product) {
  const images = product?.productImages || product?.images || [];
  const firstImage = images[0];
  return normalizeImageUrl(firstImage?.imageUrl || firstImage?.url || '');
}

function normalizeToastMessage(message) {
  if (message && typeof message === 'object') {
    if (message.errors && typeof message.errors === 'object') {
      const firstError = Object.values(message.errors).flat().find(Boolean);
      if (firstError) return String(firstError);
    }
    return message.message || message.title || 'Помилка запиту';
  }

  const text = String(message || '');
  const trimmed = text.trim();

  if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
    try {
      const parsed = JSON.parse(trimmed);
      return normalizeToastMessage(parsed);
    } catch { }
  }

  return text || 'Помилка запиту';
}

export function showToast(message) {
  let toast = document.getElementById('appToast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'appToast';
    toast.style.cssText = `
      position: fixed;
      bottom: 20px;
      left: 50%;
      transform: translateX(-50%);
      background: #4b2e39;
      color: #fff;
      padding: 12px 18px;
      border-radius: 999px;
      z-index: 1200;
      box-shadow: 0 8px 24px rgba(0,0,0,.18);
      opacity: 0;
      transition: .25s ease;
      max-width: min(560px, calc(100vw - 32px));
      text-align: center;
      line-height: 1.35;
    `;
    document.body.appendChild(toast);
  }
  toast.textContent = normalizeToastMessage(message);
  toast.style.opacity = '1';
  clearTimeout(toast._timer);
  toast._timer = setTimeout(() => { toast.style.opacity = '0'; }, 2200);
}

export function ensureGlobalUi() {
  if (document.getElementById('globalOverlay')) return;

  document.body.insertAdjacentHTML('beforeend', `
    <div class="overlay hidden" id="globalOverlay"></div>

    <div class="side-panel cart-panel-animated cart-panel-closed" id="cartPanel">
      <div class="side-panel-header">
        <div class="side-panel-title">Мої покупки</div>
        <button class="side-panel-close" id="closeCartPanel" type="button">×</button>
      </div>
      <div class="side-panel-body">
        <div class="side-panel-section" id="cartItemsWrap"></div>
        <div class="side-panel-section summary-box">
          <div class="summary-line"><span>В сумі:</span><strong class="summary-total" id="cartPanelTotal">0 UAH</strong></div>

          <div class="cart-checkout-area">
            <button class="primary-btn" id="goCheckoutBtn" type="button">ОФОРМИТИ ЗАМОВЛЕННЯ</button>
          </div>
        </div>
      </div>
    </div>

    <div class="auth-modal-shell hidden" id="authModalShell">
      <div class="auth-modal-card">
        <button class="auth-close-btn" id="closeAuthModalBtn" type="button">×</button>
        <div class="auth-box" id="loginBox">
          <h2>Авторизація</h2>
          <form id="loginForm" class="auth-form">
            <input type="email" id="loginEmail" placeholder="Ел. адреса" required />
            <input type="password" id="loginPassword" placeholder="Пароль" required />
            <button class="primary-btn" type="submit">УВІЙТИ</button>
          </form>
          <div class="auth-google-wrap" id="googleLoginWrap"></div>
          <div class="auth-small-note">Немає акаунту? <span class="auth-switch-link" id="showRegisterView">Реєстрація</span></div>
        </div>
        <div class="auth-box hidden" id="registerBox">
          <h2>Реєстрація</h2>
          <form id="registerForm" class="auth-form">
            <input type="text" id="registerFirstName" placeholder="Ім'я" required />
            <input type="text" id="registerLastName" placeholder="Прізвище" required />
            <input type="email" id="registerEmail" placeholder="Ел. адреса" required />
            <input type="text" id="registerPhone" placeholder="Телефон" required />
            <input type="password" id="registerPassword" placeholder="Пароль" required />
            <button class="primary-btn" type="submit">ЗАРЕЄСТРУВАТИСЯ</button>
          </form>
          <div class="auth-small-note">Уже є акаунт? <span class="auth-switch-link" id="showLoginView">Увійти</span></div>
        </div>
      </div>
    </div>
  `);
}

export function openOverlay() {
  const overlay = document.getElementById('globalOverlay');
  overlay?.classList.remove('overlay-pass-through');
  overlay?.classList.remove('hidden');
}

export function closeOverlayIfNeeded() {
  const cartPanel = document.getElementById('cartPanel');
  const cartClosed = cartPanel?.classList.contains('cart-panel-closed') ?? true;
  if (cartClosed) {
    const overlay = document.getElementById('globalOverlay');
    overlay?.classList.add('hidden');
    overlay?.classList.remove('overlay-pass-through');
  }
}

export function openCartPanel() {
  const panel = document.getElementById('cartPanel');
  if (!panel) return;

  panel.classList.remove('hidden');
  openOverlay();
  panel.classList.remove('cart-panel-closing');
  requestAnimationFrame(() => panel.classList.remove('cart-panel-closed'));
}

export function closeCartPanel() {
  const panel = document.getElementById('cartPanel');
  if (!panel) return;

  const overlay = document.getElementById('globalOverlay');
  overlay?.classList.add('overlay-pass-through');

  panel.classList.add('cart-panel-closing');
  panel.classList.add('cart-panel-closed');
  window.setTimeout(() => {
    closeOverlayIfNeeded();
    panel.classList.remove('cart-panel-closing');
  }, 500);
}

function positionAuthPopup() {
  const shell = document.getElementById('authModalShell');
  const profileBtn = document.getElementById('openProfileBtn');
  if (!shell || !profileBtn) return;

  const rect = profileBtn.getBoundingClientRect();
  const popupWidth = Math.min(380, window.innerWidth - 32);
  const right = Math.max(16, window.innerWidth - rect.right - 12);
  const top = rect.bottom + 26;
  const arrowLeft = rect.left + rect.width / 2 - (window.innerWidth - right - popupWidth);

  shell.style.setProperty('--auth-popup-top', `${top}px`);
  shell.style.setProperty('--auth-popup-right', `${right}px`);
  shell.style.setProperty('--auth-arrow-left', `${Math.max(28, Math.min(popupWidth - 28, arrowLeft))}px`);
}

export function openAuthModal() {
  const shell = document.getElementById('authModalShell');
  if (!shell) return;

  shell.classList.remove('auth-register-mode');
  switchAuthView('login');
  positionAuthPopup();
  requestAnimationFrame(() => shell.classList.remove('hidden'));
}

export function closeAuthModal() {
  const shell = document.getElementById('authModalShell');
  if (!shell) return;

  shell.classList.add('hidden');
  window.setTimeout(() => {
    shell.classList.remove('auth-register-mode');
  }, 360);
}

export function switchAuthView(view) {
  const showLogin = view === 'login';
  const shell = document.getElementById('authModalShell');

  shell?.classList.toggle('auth-register-mode', !showLogin);
  if (showLogin) positionAuthPopup();

  document.getElementById('loginBox')?.classList.toggle('hidden', !showLogin);
  document.getElementById('registerBox')?.classList.toggle('hidden', showLogin);
}

export function setAuthResult(value) {
  if (typeof value === 'string' && value.trim()) showToast(value);
}

export function setAdminResult(value) {
  if (typeof value === 'string' && value.trim()) showToast(value);
}
