import { api, authHeaders, getToken } from './api.js';
import { state } from './state.js';
import { escapeHtml, formatPrice, getImageUrl, showToast, openCartPanel } from './ui.js';

const PROMO_STORAGE_KEY = 'rosemary_applied_promocode';

function ensureCartCompactStyles() {
  if (document.getElementById('cartCompactStyles')) return;
  const style = document.createElement('style');
  style.id = 'cartCompactStyles';
  style.textContent = `
    .cart-total-compact {
      display: grid;
      gap: 3px;
      line-height: 1.2;
    }
    .cart-total-compact strong {
      font-size: 22px;
      color: #221321;
    }
    .cart-total-compact span {
      display: block;
      font-size: 12px;
      font-weight: 500;
      color: rgba(76, 45, 63, .72);
    }
    .cart-discount-note {
      margin-top: 6px;
      font-size: 12px;
      line-height: 1.3;
      font-weight: 600;
      color: rgba(184, 67, 115, .88);
    }
  `;
  document.head.appendChild(style);
}


export function getGuestCart() {
  try { return JSON.parse(localStorage.getItem('guestCart') || '[]'); }
  catch { return []; }
}

export function setGuestCart(items) {
  localStorage.setItem('guestCart', JSON.stringify(items));
}

export function clearGuestCart() {
  localStorage.removeItem('guestCart');
}

export function setStoredPromocode(promocode) {
  if (!promocode) {
    localStorage.removeItem(PROMO_STORAGE_KEY);
    state.appliedPromocode = null;
    return;
  }

  const normalized = {
    promocodeId: promocode.promocodeId ?? promocode.PromocodeId ?? null,
    code: promocode.code ?? promocode.Code ?? '',
    discountPercent: Number(promocode.discountPercent ?? promocode.DiscountPercent ?? 0)
  };

  if (!normalized.code || normalized.discountPercent <= 0) {
    localStorage.removeItem(PROMO_STORAGE_KEY);
    state.appliedPromocode = null;
    return;
  }

  state.appliedPromocode = normalized;
  localStorage.setItem(PROMO_STORAGE_KEY, JSON.stringify(normalized));
}

export function getStoredPromocode() {
  if (state.appliedPromocode) return state.appliedPromocode;
  try {
    const parsed = JSON.parse(localStorage.getItem(PROMO_STORAGE_KEY) || 'null');
    if (!parsed?.code || Number(parsed.discountPercent) <= 0) return null;
    state.appliedPromocode = parsed;
    return parsed;
  } catch {
    return null;
  }
}

function findProduct(productId) {
  return state.products.find(p => Number(p.productId ?? p.ProductId ?? p.id ?? p.Id) === Number(productId));
}

function getNumber(value, fallback = 0) {
  const n = Number(value);
  return Number.isFinite(n) ? n : fallback;
}

function getActiveDiscountPercent(productId) {
  const now = new Date();
  const discounts = state.discounts || [];
  const activeDiscount = discounts
    .filter(d => Number(d.productId ?? d.ProductId ?? d.product?.productId ?? d.Product?.ProductId ?? d.product?.id ?? d.Product?.Id) === Number(productId))
    .filter(d => {
      const isActive = d.isActive ?? d.IsActive ?? true;
      const start = d.startDate ?? d.StartDate;
      const end = d.endDate ?? d.EndDate;
      return isActive && (!start || new Date(start) <= now) && (!end || new Date(end) >= now);
    })
    .sort((a, b) => Number(b.discountPercent ?? b.DiscountPercent ?? 0) - Number(a.discountPercent ?? a.DiscountPercent ?? 0))[0];

  return Number(activeDiscount?.discountPercent ?? activeDiscount?.DiscountPercent ?? 0);
}

function getOriginalPrice(product, rawItem = {}) {
  return getNumber(
    product?.price ?? product?.Price ?? rawItem.originalPrice ?? rawItem.OriginalPrice ?? rawItem.price ?? rawItem.Price,
    0
  );
}

function getDiscountedPrice(originalPrice, discountPercent) {
  return discountPercent > 0
    ? Math.max(0, Math.round((originalPrice - originalPrice * discountPercent / 100) * 100) / 100)
    : originalPrice;
}

function getCartProductPricing(productId, rawItem = {}) {
  const product = findProduct(productId) || rawItem.product || rawItem.Product || {};
  const originalPrice = getOriginalPrice(product, rawItem);

  const frontendDiscount = getActiveDiscountPercent(productId);
  const backendDiscount = getNumber(rawItem.discountPercent ?? rawItem.DiscountPercent, 0);
  const discountPercent = frontendDiscount > 0 ? frontendDiscount : backendDiscount;

  const price = getDiscountedPrice(originalPrice, discountPercent);
  return { product, originalPrice, discountPercent, price };
}

function normalizeCartItem(rawItem = {}) {
  const productFromResponse = rawItem.product || rawItem.Product || {};
  const productId = rawItem.productId ?? rawItem.ProductId ?? productFromResponse.productId ?? productFromResponse.ProductId ?? productFromResponse.id ?? productFromResponse.Id;
  const cartItemId = rawItem.cartItemId
    ?? rawItem.CartItemId
    ?? rawItem.id
    ?? rawItem.Id
    ?? rawItem.cart_item_id
    ?? rawItem.cartItemID;

  const quantity = getNumber(rawItem.quantity ?? rawItem.Quantity, 0);
  const pricing = getCartProductPricing(productId, rawItem);
  const product = pricing.product || {};

  return {
    ...rawItem,
    cartItemId,
    productId,
    productName: rawItem.productName ?? rawItem.ProductName ?? product.name ?? product.Name ?? 'Товар',
    price: pricing.price,
    originalPrice: pricing.originalPrice,
    discountPercent: pricing.discountPercent,
    quantity,
    stockQuantity: getNumber(product.stockQuantity ?? product.StockQuantity ?? rawItem.stockQuantity ?? rawItem.StockQuantity, 0),
    subtotal: Math.round(pricing.price * quantity * 100) / 100
  };
}

function normalizePromocode(cart = {}) {
  const backendPromo = cart.promocode || cart.Promocode || null;
  const promo = backendPromo || getStoredPromocode();
  if (!promo) return null;

  const code = promo.code ?? promo.Code ?? '';
  const discountPercent = getNumber(promo.discountPercent ?? promo.DiscountPercent, 0);
  if (!code || discountPercent <= 0) return null;

  return {
    promocodeId: promo.promocodeId ?? promo.PromocodeId ?? null,
    code,
    discountPercent
  };
}

function normalizeCart(cart = {}) {
  const items = (cart.items || cart.Items || []).map(normalizeCartItem);
  const itemsTotal = Math.round(items.reduce((sum, item) => sum + Number(item.subtotal || 0), 0) * 100) / 100;
  const promocode = normalizePromocode(cart);
  const promoDiscountAmount = promocode
    ? Math.round(itemsTotal * promocode.discountPercent) / 100
    : 0;
  const totalAmount = Math.max(0, Math.round((itemsTotal - promoDiscountAmount) * 100) / 100);

  return {
    ...cart,
    cartId: cart.cartId ?? cart.CartId,
    userId: cart.userId ?? cart.UserId,
    items,
    itemsTotal,
    promocode: promocode ? { ...promocode, discountAmount: promoDiscountAmount } : null,
    totalAmount
  };
}

function getItemKey(item) {
  return item?.cartItemId ?? item?.CartItemId ?? item?.id ?? item?.Id;
}

function isValidServerCartItemId(value) {
  return value !== undefined && value !== null && value !== '' && value !== 'undefined' && value !== 'null' && !Number.isNaN(Number(value));
}

function getStock(productId) {
  const product = findProduct(productId);
  return Number(product?.stockQuantity ?? product?.StockQuantity ?? 0);
}

function getCurrentCartQuantity(productId) {
  const items = getGuestCart();
  return Number(items.find(i => Number(i.productId ?? i.ProductId) === Number(productId))?.quantity || 0);
}

function showStockError(stock) {
  showToast(stock > 0
    ? `Недостатньо товару на складі. Доступно: ${stock}`
    : 'Цього товару зараз немає на складі');
}

function canSetQuantity(productId, nextQuantity) {
  const stock = getStock(productId);
  if (nextQuantity > stock) {
    showStockError(stock);
    openCartPanel();
    return false;
  }
  return true;
}

function buildGuestCartDetailed() {
  const items = getGuestCart().map(guestItem => normalizeCartItem({
    cartItemId: `guest-${guestItem.productId}`,
    productId: guestItem.productId,
    quantity: guestItem.quantity
  }));

  return normalizeCart({ items });
}

export function hasCartStockProblems() {
  return (state.cart?.items || []).map(normalizeCartItem).some(item => item.quantity > getStock(item.productId));
}

export function validateCartStock(showMessage = true) {
  const problem = (state.cart?.items || []).map(normalizeCartItem).find(item => item.quantity > getStock(item.productId));
  if (!problem) return true;

  if (showMessage) {
    const stock = getStock(problem.productId);
    showToast(`У кошику забагато товару "${problem.productName || 'Товар'}". Доступно: ${stock}`);
    openCartPanel();
  }
  return false;
}

export async function refreshCart() {
  state.cart = buildGuestCartDetailed();
  renderCartUi();
}

export function renderCartUi() {
  ensureCartCompactStyles();
  const cartItemsWrap = document.getElementById('cartItemsWrap');
  const cartPanelTotal = document.getElementById('cartPanelTotal');
  const cartHeaderTotal = document.getElementById('cartHeaderTotal');

  const normalizedCart = normalizeCart(state.cart || {});
  state.cart = normalizedCart;

  const items = normalizedCart.items || [];
  const itemsTotal = Number(normalizedCart.itemsTotal || 0);
  const totalAmount = Number(normalizedCart.totalAmount || 0);
  const promocode = normalizedCart.promocode || null;

  if (cartPanelTotal) {
    cartPanelTotal.innerHTML = promocode
      ? `<div class="cart-total-compact">
          <strong>${formatPrice(totalAmount)}</strong>
          <span>Промокод ${escapeHtml(promocode.code)}: -${Number(promocode.discountPercent).toFixed(0)}% (${formatPrice(promocode.discountAmount)})</span>
          <span>Було: ${formatPrice(itemsTotal)}</span>
        </div>`
      : formatPrice(totalAmount);
  }
  if (cartHeaderTotal) cartHeaderTotal.textContent = formatPrice(totalAmount);
  if (!cartItemsWrap) return;

  if (!items.length) {
    cartItemsWrap.innerHTML = '<p class="empty-state">Кошик порожній.</p>';
    return;
  }

  cartItemsWrap.innerHTML = items.map(item => {
    const productId = item.productId ?? item.ProductId;
    const cartItemId = getItemKey(item);
    const product = findProduct(productId) || {};
    const imageUrl = getImageUrl(product);
    const stock = getStock(productId);
    const isTooMany = Number(item.quantity) > stock;
    const hasDiscount = Number(item.discountPercent || 0) > 0;

    return `
      <div class="drawer-item ${isTooMany ? 'cart-stock-warning' : ''}">
        <div class="drawer-item-image">
          ${imageUrl ? `<img src="${imageUrl}" alt="${escapeHtml(item.productName || '')}">` : `<span style="font-size:42px;color:rgba(139,76,103,.45)">✿</span>`}
        </div>
        <div>
          <div class="drawer-item-name">${escapeHtml(item.productName || 'Товар')}</div>
          <div class="drawer-item-meta">Кількість: ${item.quantity} / на складі: ${stock}</div>
          ${isTooMany ? `<div class="cart-warning-text">У кошику забагато цього товару. Зменш кількість до ${stock}.</div>` : ''}
          <div class="drawer-item-actions">
            <div class="qty-row">
              <button type="button" class="qty-minus" data-id="${cartItemId || ''}" data-product="${productId}">−</button>
              <span>${item.quantity}</span>
              <button type="button" class="qty-plus" data-id="${cartItemId || ''}" data-product="${productId}">+</button>
            </div>
            <strong>${formatPrice(item.subtotal)}</strong>
          </div>
          ${hasDiscount ? `<div class="cart-discount-note">-${Number(item.discountPercent).toFixed(0)}% · було ${formatPrice(item.originalPrice)} · зараз ${formatPrice(item.price)}</div>` : ``}
          <button class="drawer-item-remove" type="button" data-remove-id="${cartItemId || ''}" data-product="${productId}">Видалити</button>
        </div>
      </div>`;
  }).join('');

  cartItemsWrap.querySelectorAll('.qty-minus').forEach(btn => {
    btn.onclick = () => updateCartQty(btn.dataset.id, Number(btn.dataset.product), -1);
  });

  cartItemsWrap.querySelectorAll('.qty-plus').forEach(btn => {
    btn.onclick = () => updateCartQty(btn.dataset.id, Number(btn.dataset.product), 1);
  });

  cartItemsWrap.querySelectorAll('[data-remove-id]').forEach(btn => {
    btn.onclick = () => removeCartItem(btn.dataset.id, Number(btn.dataset.product));
  });
}

export async function addToCart(productId, quantity = 1) {
  const currentQty = getCurrentCartQuantity(productId);
  if (!canSetQuantity(productId, currentQty + quantity)) return;
  const items = getGuestCart();
  const existing = items.find(i => Number(i.productId) === Number(productId));
  if (existing) existing.quantity += quantity;
  else items.push({ productId: Number(productId), quantity });
  setGuestCart(items);
  await refreshCart();
  openCartPanel();
  showToast('Товар додано у кошик');
}

async function updateCartQty(cartItemId, productId, delta) {
  const items = getGuestCart();
  const item = items.find(i => Number(i.productId) === Number(productId));
  if (!item) return;
  const nextQty = item.quantity + delta;
  if (nextQty > 0 && !canSetQuantity(productId, nextQty)) return;
  if (nextQty <= 0) setGuestCart(items.filter(i => Number(i.productId) !== Number(productId)));
  else { item.quantity = nextQty; setGuestCart(items); }
  await refreshCart();
}

async function removeCartItem(cartItemId, productId) {
  setGuestCart(getGuestCart().filter(i => Number(i.productId) !== Number(productId)));
  await refreshCart();
}

export async function clearCart() {
  setStoredPromocode(null);
  clearGuestCart();
  await refreshCart();
  showToast('Кошик очищено');
}

// Monolith has no cart endpoint. The same local cart remains after login.
export async function mergeGuestCartToServer() { await refreshCart(); }
