import { ROUTES } from './config.js';
import { api, authHeaders, getToken } from './api.js';
import { state } from './state.js';
import { clearGuestCart, getStoredPromocode, refreshCart, renderCartUi, setStoredPromocode, validateCartStock } from './cart.js';
import { escapeHtml, formatPrice, showToast } from './ui.js';
import { refreshMe } from './auth.js';

const ONLINE_SANDBOX_PAYMENTS = ['visa', 'mastercard', 'applepay', 'googlepay', 'crypto'];
const SANDBOX_SUCCESS_CARD = '4242424242424242';
const SANDBOX_DECLINE_CARD = '4000000000000002';

function ensureCheckoutCompactStyles() {
  if (document.getElementById('checkoutCompactStyles')) return;
  const style = document.createElement('style');
  style.id = 'checkoutCompactStyles';
  style.textContent = `
    .checkout-total-compact {
      display: grid;
      gap: 3px;
      text-align: right;
      line-height: 1.2;
    }
    .checkout-total-compact strong {
      font-size: 18px;
      color: #221321;
    }
    .checkout-total-compact span {
      display: block;
      font-size: 12px;
      font-weight: 500;
      color: rgba(76, 45, 63, .72);
    }
  `;
  document.head.appendChild(style);
}


export async function initCheckoutPage() {
  if (document.body.dataset.page !== 'checkout') return;

  await refreshMe();
  await refreshCart();
  const items = state.cart?.items || [];
  if (!items.length) {
    showToast('Кошик порожній');
    setTimeout(() => { window.location.href = 'products.html'; }, 700);
    return;
  }

  if (!validateCartStock(true)) {
    setTimeout(() => { window.location.href = 'products.html'; }, 700);
    return;
  }

  document.querySelectorAll('.payment-card:not([data-payment="cash"])').forEach(el => el.remove());
  document.getElementById('sandboxPaymentBox')?.remove();
  prefillCheckoutFields();
  renderCheckoutSummary();
  setupCheckoutEvents();
  toggleSandboxPayment(false);
}

function setValue(id, value) {
  const el = document.getElementById(id);
  if (el) el.value = value;
}

function prefillCheckoutFields() {
  const profile = state.me?.profile || {};
  setValue('checkoutFirstName', profile.firstName || state.me?.firstName || '');
  setValue('checkoutLastName', profile.lastName || state.me?.lastName || '');
  setValue('checkoutMiddleName', profile.middleName || '');
  setValue('checkoutPhone', profile.phone || state.me?.phone || '');
  setValue('checkoutEmail', profile.email || state.me?.email || '');
  setValue('checkoutCountry', 'Україна');
  setValue('checkoutCity', profile.city || '');
  setValue('checkoutRegion', profile.region || '');
  setValue('checkoutPostal', profile.postalCode || '');
  setValue('checkoutStreet', profile.streetAddress || '');
}

function renderCheckoutSummary() {
  ensureCheckoutCompactStyles();
  const wrap = document.getElementById('checkoutItemsList');
  const total = document.getElementById('checkoutTotalPrice');
  if (!wrap || !total) return;

  const items = state.cart?.items || [];
  wrap.innerHTML = items.map(item => `
    <div class="checkout-order-item">
      <strong>${escapeHtml(item.productName || 'Товар')}</strong>
      <span>Кількість: ${item.quantity}</span>
      <span>${formatPrice(item.subtotal)}</span>
    </div>`).join('');

  const itemsTotal = Number(state.cart?.itemsTotal ?? state.cart?.ItemsTotal ?? state.cart?.totalAmount ?? 0);
  const totalAmount = Number(state.cart?.totalAmount ?? state.cart?.TotalAmount ?? itemsTotal);
  const promo = state.cart?.promocode || state.cart?.Promocode || getStoredPromocode() || null;
  const promoCode = promo?.code || promo?.Code || '';
  const promoPercent = Number(promo?.discountPercent ?? promo?.DiscountPercent ?? 0);
  const promoDiscountAmount = Number(promo?.discountAmount ?? promo?.DiscountAmount ?? Math.max(0, itemsTotal - totalAmount));

  total.innerHTML = promo
    ? `<div class="checkout-total-compact">
        <strong>${formatPrice(totalAmount)}</strong>
        <span>Промокод ${escapeHtml(promoCode)}: -${promoPercent.toFixed(0)}% (${formatPrice(promoDiscountAmount)})</span>
        <span>Було: ${formatPrice(itemsTotal)}</span>
      </div>`
    : formatPrice(totalAmount);
}

function setupCheckoutEvents() {
  document.getElementById('checkoutStep1Form')?.addEventListener('submit', (e) => {
    e.preventDefault();
    document.getElementById('checkoutStep1')?.classList.add('hidden');
    document.getElementById('checkoutStep2')?.classList.remove('hidden');
    document.getElementById('checkoutStepLabel').textContent = '2';
  });

  document.getElementById('checkoutBackBtn')?.addEventListener('click', () => {
    document.getElementById('checkoutStep2')?.classList.add('hidden');
    document.getElementById('checkoutStep1')?.classList.remove('hidden');
    document.getElementById('checkoutStepLabel').textContent = '1';
  });

  document.querySelectorAll('.payment-card').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.payment-card').forEach(x => x.classList.remove('active'));
      btn.classList.add('active');
      state.selectedPayment = btn.dataset.payment;
      toggleSandboxPayment(ONLINE_SANDBOX_PAYMENTS.includes(state.selectedPayment));
    });
  });

  document.querySelectorAll('[data-fill-card]').forEach(btn => {
    btn.addEventListener('click', () => fillSandboxCard(btn.dataset.fillCard));
  });

  document.getElementById('sandboxCardNumber')?.addEventListener('input', formatSandboxCardNumber);
  document.getElementById('sandboxCardExpiry')?.addEventListener('input', formatSandboxExpiry);
  document.getElementById('sandboxCardCvv')?.addEventListener('input', formatSandboxCvv);

  document.getElementById('checkoutSubmitBtn')?.addEventListener('click', submitCheckoutOrder);
}

function toggleSandboxPayment(show) {
  const box = document.getElementById('sandboxPaymentBox');
  const submitBtn = document.getElementById('checkoutSubmitBtn');
  box?.classList.toggle('hidden', !show);

  if (submitBtn) {
    submitBtn.textContent = show ? 'ОПЛАТИТИ SANDBOX' : 'ОФОРМИТИ ЗАМОВЛЕННЯ';
  }

  setSandboxStatus(show
    ? 'Для успішного тесту використай 4242 4242 4242 4242.'
    : '', 'info');
}

function onlyDigits(value) {
  return String(value || '').replace(/\D/g, '');
}

function formatSandboxCardNumber(event) {
  const digits = onlyDigits(event.target.value).slice(0, 16);
  event.target.value = digits.replace(/(.{4})/g, '$1 ').trim();
}

function formatSandboxExpiry(event) {
  const digits = onlyDigits(event.target.value).slice(0, 4);
  event.target.value = digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
}

function formatSandboxCvv(event) {
  event.target.value = onlyDigits(event.target.value).slice(0, 3);
}

function fillSandboxCard(type) {
  setValue('sandboxCardNumber', type === 'decline' ? '4000 0000 0000 0002' : '4242 4242 4242 4242');
  setValue('sandboxCardExpiry', '12/30');
  setValue('sandboxCardCvv', '123');
  setValue('sandboxCardName', 'TEST USER');
  setSandboxStatus(type === 'decline'
    ? 'Ця тестова карта спеціально імітує відхилену оплату.'
    : 'Ця тестова карта імітує успішну оплату.', 'info');
}

function setSandboxStatus(message, type = 'info') {
  const status = document.getElementById('sandboxPaymentStatus');
  if (!status) return;
  status.textContent = message;
  status.classList.remove('success', 'error', 'processing');
  if (type) status.classList.add(type);
}

function validateSandboxForm() {
  const cardNumber = onlyDigits(document.getElementById('sandboxCardNumber')?.value);
  const expiry = document.getElementById('sandboxCardExpiry')?.value.trim() || '';
  const cvv = onlyDigits(document.getElementById('sandboxCardCvv')?.value);
  const name = document.getElementById('sandboxCardName')?.value.trim() || '';

  if (!cardNumber || !expiry || !cvv || !name) {
    return { ok: false, message: 'Заповни всі поля тестової картки.' };
  }

  if (cardNumber !== SANDBOX_SUCCESS_CARD && cardNumber !== SANDBOX_DECLINE_CARD) {
    return { ok: false, message: 'Для sandbox-оплати використовуй тільки тестову карту 4242 4242 4242 4242.' };
  }

  if (!/^\d{2}\/\d{2}$/.test(expiry)) {
    return { ok: false, message: 'Термін дії введи у форматі MM/YY, наприклад 12/30.' };
  }

  if (cvv.length !== 3) {
    return { ok: false, message: 'CVV має містити 3 цифри.' };
  }

  if (cardNumber === SANDBOX_DECLINE_CARD) {
    return { ok: false, message: 'Sandbox оплату відхилено. Це тестова карта для помилки.' };
  }

  return { ok: true };
}

function wait(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function processSandboxPayment(orderPayload) {
  const validation = validateSandboxForm();
  if (!validation.ok) {
    setSandboxStatus(validation.message, 'error');
    showToast(validation.message);
    return null;
  }

  const submitBtn = document.getElementById('checkoutSubmitBtn');
  if (submitBtn) submitBtn.disabled = true;
  setSandboxStatus('Відправляємо sandbox-оплату на бекенд...', 'processing');

  const paymentPayload = {
    ...orderPayload,
    paymentMethod: state.selectedPayment,
    cardNumber: document.getElementById('sandboxCardNumber')?.value || '',
    cardExpiry: document.getElementById('sandboxCardExpiry')?.value || '',
    cardCvv: document.getElementById('sandboxCardCvv')?.value || '',
    cardHolderName: document.getElementById('sandboxCardName')?.value || ''
  };

  const payment = await api('/api/Payments/sandbox', {
    method: 'POST',
    headers: authHeaders(true),
    body: JSON.stringify(paymentPayload)
  });

  const history = JSON.parse(localStorage.getItem('sandboxPayments') || '[]');
  history.unshift({
    transactionId: payment.transactionId,
    orderId: payment.orderId,
    paymentId: payment.paymentId,
    amount: payment.amount,
    method: payment.paymentMethod,
    status: payment.status,
    sandbox: true,
    createdAt: new Date().toISOString()
  });
  localStorage.setItem('sandboxPayments', JSON.stringify(history.slice(0, 20)));

  setSandboxStatus(`Оплату підтверджено бекендом. ID: ${payment.transactionId}`, 'success');
  await wait(450);
  return payment;
}

function buildOrderPayload(paymentResult = null) {
  const items = (state.cart?.items || []).map(item => ({ productId: item.productId, quantity: item.quantity }));
  const firstName = document.getElementById('checkoutFirstName').value.trim();
  const lastName = document.getElementById('checkoutLastName').value.trim();
  const middleName = document.getElementById('checkoutMiddleName').value.trim();
  const phone = document.getElementById('checkoutPhone').value.trim();
  const city = document.getElementById('checkoutCity').value.trim();
  const region = document.getElementById('checkoutRegion').value.trim();
  const postal = document.getElementById('checkoutPostal').value.trim();
  const street = document.getElementById('checkoutStreet').value.trim();
  const comment = document.getElementById('checkoutComment').value.trim();

  const recipientName = [lastName, firstName, middleName].filter(Boolean).join(' ').trim();
  const deliveryAddress = `Україна, ${region}, ${city}, ${street}, ${postal}`;
  const paymentText = paymentResult
    ? `Спосіб оплати: ${state.selectedPayment}; Sandbox payment: paid; Transaction ID: ${paymentResult.transactionId}; Реальні гроші не списувались`
    : `Спосіб оплати: ${state.selectedPayment}`;
  const finalComment = [comment, paymentText].filter(Boolean).join(' | ');

  return { recipientName, recipientPhone: phone, deliveryAddress, comment: finalComment, items };
}

async function submitCheckoutOrder() {
  if (!validateCartStock(true)) return;

  const submitBtn = document.getElementById('checkoutSubmitBtn');
  const orderPayload = buildOrderPayload(null);
  let result = null;

  try {
    if (submitBtn) submitBtn.disabled = true;
    result = await api(ROUTES.orders, {
      method: 'POST',
      headers: authHeaders(true),
      body: JSON.stringify(orderPayload)
    });
    clearGuestCart();

    setStoredPromocode(null);
    state.cart = { items: [], itemsTotal: 0, totalAmount: 0, promocode: null };
    renderCartUi();
    showToast(`Замовлення створено #${result?.orderId ?? result?.id ?? ''}`.trim());
    setTimeout(() => { window.location.href = 'profile.html'; }, 1100);
  } catch (error) {

    showToast(error.message || 'Не вдалося оформити замовлення');
    if (submitBtn) submitBtn.disabled = false;
  }
}
