import { api, authHeaders, getToken } from './api.js';
import { state } from './state.js';
import { escapeHtml, formatPrice, showToast } from './ui.js';
import { initGoogleLogin, logout, refreshMe } from './auth.js';

export async function initProfilePage() {
  if (document.body.dataset.page !== 'profile') return;

  if (!getToken()) {
    window.location.href = 'index.html';
    return;
  }

  await refreshMe();
  if (!state.me) {
    window.location.href = 'index.html';
    return;
  }

  const profile = state.me.profile || {};
  const orders = state.me.orders || [];
  setValue('profileFirstNamePage', profile.firstName || state.me.firstName || '');
  setValue('profileLastNamePage', profile.lastName || state.me.lastName || '');
  setValue('profileMiddleNamePage', profile.middleName || '');
  setValue('profilePhonePage', profile.phone || state.me.phone || '');
  setValue('profileEmailPage', profile.email || state.me.email || '');
  setValue('profileCountryPage', profile.country || 'Україна');
  setValue('profileCityPage', profile.city || '');
  setValue('profileRegionPage', profile.region || '');
  setValue('profilePostalPage', profile.postalCode || '');
  setValue('profileStreetPage', profile.streetAddress || '');

  const adminBtn = document.getElementById('adminPanelLinkBtn');
  if (adminBtn && String(state.me.role || '').toLowerCase() === 'admin') {
    adminBtn.classList.remove('hidden');
    adminBtn.onclick = () => { window.location.href = 'admin.html'; };
  }

  renderOrders(orders);
  bindProfileEvents();
  initGoogleLogin();
}

function setValue(id, value) {
  const el = document.getElementById(id);
  if (el) el.value = value;
}

function renderOrders(orders) {
  const ordersWrap = document.getElementById('profileOrdersListPage');
  if (!ordersWrap) return;

  ordersWrap.innerHTML = orders.length
    ? orders.map(order => `
        <div class="order-card">
          <strong>Замовлення #${order.orderId}</strong>
          <span>Статус: ${escapeHtml(order.status || '-')}</span>
          <span>Сума: ${formatPrice(order.totalAmount)}</span>
          <span>Дата: ${order.orderDate ? new Date(order.orderDate).toLocaleString('uk-UA') : '-'}</span>
        </div>
      `).join('')
    : '<p class="empty-state">Замовлень поки немає.</p>';
}

function bindProfileEvents() {
  document.getElementById('profileFormPage')?.addEventListener('submit', saveProfileFromPage);
  document.getElementById('changePasswordFormPage')?.addEventListener('submit', changePasswordFromPage);
  document.getElementById('logoutBtnPage')?.addEventListener('click', logout);

  document.querySelectorAll('[data-profile-tab]').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('[data-profile-tab]').forEach(x => x.classList.remove('active'));
      btn.classList.add('active');
      const tab = btn.dataset.profileTab;
      document.getElementById('profileTabInfo')?.classList.toggle('hidden', tab !== 'info');
      document.getElementById('profileTabOrders')?.classList.toggle('hidden', tab !== 'orders');
      document.getElementById('profileTabSecurity')?.classList.toggle('hidden', tab !== 'security');
    });
  });
}

async function saveProfileFromPage(e) {
  e.preventDefault();

  try {
    const personalPayload = {
      firstName: document.getElementById('profileFirstNamePage').value.trim(),
      lastName: document.getElementById('profileLastNamePage').value.trim(),
      middleName: document.getElementById('profileMiddleNamePage').value.trim(),
      phone: document.getElementById('profilePhonePage').value.trim()
    };

    const addressPayload = {
      region: document.getElementById('profileRegionPage').value.trim(),
      city: document.getElementById('profileCityPage').value.trim(),
      streetAddress: document.getElementById('profileStreetPage').value.trim(),
      postalCode: document.getElementById('profilePostalPage').value.trim()
    };

    await api('/api/profile/personal-info', { method: 'PUT', headers: authHeaders(true), body: JSON.stringify(personalPayload) });
    await api('/api/profile/delivery-address', { method: 'PUT', headers: authHeaders(true), body: JSON.stringify(addressPayload) });
    await refreshMe();
    showToast('Профіль оновлено');
  } catch (error) {
    showToast(error.message || 'Не вдалося оновити профіль');
  }
}

async function changePasswordFromPage(e) {
  e.preventDefault();

  try {
    await api('/api/profile/change-password', {
      method: 'PUT',
      headers: authHeaders(true),
      body: JSON.stringify({
        currentPassword: document.getElementById('currentPasswordPage').value,
        newPassword: document.getElementById('newPasswordPage').value
      })
    });
    showToast('Пароль змінено');
    e.target.reset();
  } catch (error) {
    showToast(error.message || 'Не вдалося змінити пароль');
  }
}
