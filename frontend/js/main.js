import { api, authHeaders, getToken } from './api.js';
import { initAdminPage } from './admin.js';
import { initAuthPage, initGoogleLogin, refreshMe, submitLogin, submitRegister } from './auth.js';
import { clearCart, refreshCart, setStoredPromocode, validateCartStock } from './cart.js';
import { initCheckoutPage } from './checkout.js';
import { initPageProducts, initProductPage, loadProducts, loadDiscounts, enrichProductImages } from './products.js';
import { initProfilePage } from './profile.js';
import { state } from './state.js';
import { closeAuthModal, closeCartPanel, ensureGlobalUi, openAuthModal, openCartPanel, setAuthResult, showToast, switchAuthView } from './ui.js';

function setupBaseUiEvents() {

  document.getElementById('currencyBtn')?.addEventListener('click', (e) => {
    e.stopPropagation();
    document.getElementById('currencyMenu')?.classList.toggle('hidden');
  });

  document.addEventListener('click', (e) => {
    const btn = document.getElementById('currencyBtn');
    const menu = document.getElementById('currencyMenu');
    if (btn && menu && !menu.contains(e.target) && e.target !== btn) menu.classList.add('hidden');
  });

  document.getElementById('openProfileBtn')?.addEventListener('click', (event) => {
    event.stopPropagation();
    if (!getToken()) {
      openAuthModal();
      return;
    }
    window.location.href = 'profile.html';
  });

  document.getElementById('openCartBtn')?.addEventListener('click', async () => {
    await refreshCart();
    openCartPanel();
  });

  document.getElementById('closeCartPanel')?.addEventListener('click', closeCartPanel);
  document.getElementById('closeAuthModalBtn')?.addEventListener('click', closeAuthModal);
  document.getElementById('globalOverlay')?.addEventListener('click', () => {
    closeCartPanel();
  });

  document.addEventListener('click', (event) => {
    const authShell = document.getElementById('authModalShell');
    const profileBtn = document.getElementById('openProfileBtn');
    if (!authShell || authShell.classList.contains('hidden')) return;
    if (authShell.contains(event.target) || profileBtn?.contains(event.target)) return;
    closeAuthModal();
  });

  window.addEventListener('resize', closeAuthModal);

  document.getElementById('showRegisterView')?.addEventListener('click', () => switchAuthView('register'));
  document.getElementById('showLoginView')?.addEventListener('click', () => switchAuthView('login'));
  document.getElementById('loginForm')?.addEventListener('submit', submitLogin);
  document.getElementById('registerForm')?.addEventListener('submit', submitRegister);

  document.getElementById('goCheckoutBtn')?.addEventListener('click', async () => {
    await refreshCart();
    const items = state.cart?.items || [];
    if (!items.length) {
      showToast('Кошик порожній');
      return;
    }
    if (!validateCartStock(true)) return;
    if (!getToken()) {
      showToast('Щоб оформити замовлення, спочатку увійди в акаунт');
      openAuthModal();
      return;
    }
    window.location.href = 'checkout.html';
  });

  document.getElementById('clearCartBtn')?.addEventListener('click', clearCart);
}

async function init() {
  if (document.body.dataset.page !== 'auth') {
    ensureGlobalUi();
    setupBaseUiEvents();
  }

  await loadProducts();
  await loadDiscounts();
  await enrichProductImages(state.products);
  await refreshMe();
  await refreshCart();

  initGoogleLogin();
  await initPageProducts();
  await initProductPage();
  await initProfilePage();
  await initCheckoutPage();
  await initAdminPage();
  initAuthPage();
}

document.addEventListener('DOMContentLoaded', init);
