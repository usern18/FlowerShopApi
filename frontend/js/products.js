import { api } from './api.js';
import { state } from './state.js';
import { ROUTES } from './config.js';
import { escapeHtml, formatPrice, getCategoryName, normalizeImageUrl } from './ui.js';
import { addToCart } from './cart.js';
const LOCAL_DISCOUNTS_KEY = 'rosemary_local_discounts';

function toArray(payload, keys = ['items', 'data', 'discounts', '$values']) {
  if (Array.isArray(payload)) return payload;
  if (!payload || typeof payload !== 'object') return [];
  for (const key of keys) {
    if (Array.isArray(payload[key])) return payload[key];
  }
  return [];
}

function productIdValue(product) {
  return product?.productId ?? product?.ProductId ?? product?.id ?? product?.Id;
}

function discountIdValue(discount) {
  return discount?.discountId ?? discount?.DiscountId ?? discount?.id ?? discount?.Id;
}

function discountProductIdValue(discount) {
  return discount?.productId
    ?? discount?.ProductId
    ?? discount?.product?.productId
    ?? discount?.Product?.ProductId
    ?? discount?.product?.id
    ?? discount?.Product?.Id;
}

function discountPercentValue(discount) {
  return Number(discount?.discountPercent ?? discount?.DiscountPercent ?? discount?.percent ?? discount?.Percent ?? 0);
}

function discountStartValue(discount) {
  return discount?.startDate ?? discount?.StartDate ?? discount?.validFrom ?? discount?.ValidFrom ?? '';
}

function discountEndValue(discount) {
  return discount?.endDate ?? discount?.EndDate ?? discount?.validTo ?? discount?.ValidTo ?? '';
}

function readLocalDiscounts() {
  return [];
}

function mergeDiscounts(apiDiscounts) {
  const map = new Map();
  apiDiscounts.forEach(discount => {
    const productId = discountProductIdValue(discount);
    if (!productId) return;
    const id = discountIdValue(discount);
    const key = id ? `discount-${id}` : `product-${productId}-${discountPercentValue(discount)}-${discountStartValue(discount)}-${discountEndValue(discount)}`;
    map.set(key, discount);
  });
  return [...map.values()];
}

export function saveLocalDiscount() {
  // Знижки тепер зберігаються тільки на бекенді, щоб після видалення не залишався старий localStorage.
}

export function removeLocalDiscount() {
  // Знижки тепер зберігаються тільки на бекенді.
}

export async function loadProducts() {
  try {
    const products = toArray(await api(ROUTES.products), ['items', 'data', 'products', '$values']);
    state.products = products;
    state.catalogError = null;
  } catch (error) {
    state.products = [];
    state.catalogError = error.message;
    console.error('Не вдалося завантажити каталог FlowerShopApi:', error);
  }
}

export async function loadCategories() {
  try {
    const categories = toArray(await api(ROUTES.categories), ['items', 'data', 'categories', '$values']);
    state.categories = categories;
  } catch {
    state.categories = [];
  }
}

export async function loadDiscounts() {
  try {
    const discounts = await api(`/api/Discounts?_=${Date.now()}`);
    state.discounts = mergeDiscounts(toArray(discounts));
  } catch {
    state.discounts = [];
  }
}

export async function enrichProductImages(products = state.products) {
  await Promise.all(products.map(async (product) => {
    try {
      const id = productIdValue(product);

      const images =
        await api(`/api/products/${id}/images`);
      product.productImages = Array.isArray(images) ? images : [];
    } catch {
      product.productImages = product.productImages || [];
    }
  }));
}

export function getActiveDiscount(productId) {
  const now = new Date();
  return (state.discounts || [])
    .filter(d => Number(discountProductIdValue(d)) === Number(productId))
    .filter(d => {
      const start = discountStartValue(d);
      const end = discountEndValue(d);
      const active = d.isActive ?? d.IsActive ?? true;
      const afterStart = !start || new Date(start) <= now;
      const beforeEnd = !end || new Date(end) >= now;
      return active && afterStart && beforeEnd;
    })
    .sort((a, b) => discountPercentValue(b) - discountPercentValue(a))[0] || null;
}

export function getDiscountPercent(productId) {
  const discount = getActiveDiscount(productId);
  return discountPercentValue(discount);
}

export function getDiscountedPrice(product) {
  const price = Number(product?.price ?? product?.Price ?? 0);
  const percent = getDiscountPercent(productIdValue(product));
  if (!percent) return price;
  return Math.max(0, price - (price * percent / 100));
}

function buildPriceHtml(product, className = 'product-price') {
  const percent = getDiscountPercent(productIdValue(product));
  const original = Number(product?.price ?? product?.Price ?? 0);
  const discounted = getDiscountedPrice(product);

  if (!percent) return `<div class="${className}">${formatPrice(original)}</div>`;

  return `
    <div class="${className} price-with-discount">
      <span class="discount-badge">-${percent.toFixed(0)}%</span>
      <span class="old-price">${formatPrice(original)}</span>
      <span class="new-price">${formatPrice(discounted)}</span>
    </div>`;
}

function buildProductCard(product) {
  const images = product.productImages || [];
  const hasSlider = images.length > 1;
  const productId = productIdValue(product);

  return `
    <article class="product-card">
      ${buildPriceHtml(product)}
      <a class="product-image ${hasSlider ? 'product-image-slider' : ''}" data-product-slider="${productId}" href="product.html?id=${productId}" aria-label="Відкрити товар ${escapeHtml(product.name)}">
        ${images.length
      ? images.map((img, index) => {
        const url = normalizeImageUrl(img.imageUrl || img.url || '');
        return `<img src="${url}" alt="${escapeHtml(product.name)}" class="slider-image ${index === 0 ? 'active' : ''}" data-slide="${index}">`;
      }).join('')
      : `<span style="font-size:64px;color:rgba(139,76,103,.45)">✿</span>`}
      </a>
      ${hasSlider ? `
        <button class="slider-btn prev" type="button" data-slider-prev="${productId}">‹</button>
        <button class="slider-btn next" type="button" data-slider-next="${productId}">›</button>` : ''}
      <div class="product-body">
        <div class="product-name">${escapeHtml(product.name)}</div>
        <div class="product-meta">${escapeHtml(product.description || '')}</div>
        <div class="product-meta">Категорія: ${escapeHtml(getCategoryName(product))}</div>
        <div class="product-actions">
          <button class="product-btn primary add-to-cart-btn" type="button" data-product-id="${productId}">КУПИТИ</button>
          <a class="product-btn details-btn" href="product.html?id=${productId}">ДЕТАЛІ</a>
        </div>
      </div>
    </article>`;
}

function filterProductsForCatalog(products) {
  if (document.body.dataset.page !== 'products') return products;
  const params = new URLSearchParams(window.location.search);
  const selectedCategory = params.get('category');
  const title = document.getElementById('catalogTitle');

  if (!selectedCategory) {
    if (title) title.textContent = 'Усі товари';
    return products;
  }

  if (title) title.textContent = selectedCategory;
  if (selectedCategory.toLowerCase() === 'квіти') {
    const flowerCategories = ['троянди', 'тюльпани', 'рози', 'кімнатні рослини', 'квіти'];
    return products.filter(p => flowerCategories.includes(getCategoryName(p).toLowerCase()));
  }

  return products.filter(p => getCategoryName(p).toLowerCase() === selectedCategory.toLowerCase());
}

function moveSlider(productId, direction) {
  const container = document.querySelector(`[data-product-slider="${productId}"]`);
  if (!container) return;

  const slides = [...container.querySelectorAll('.slider-image')];
  if (slides.length <= 1) return;

  const currentIndex = slides.findIndex(slide => slide.classList.contains('active'));
  slides[currentIndex]?.classList.remove('active');
  let nextIndex = currentIndex + direction;
  if (nextIndex < 0) nextIndex = slides.length - 1;
  if (nextIndex >= slides.length) nextIndex = 0;
  slides[nextIndex].classList.add('active');
}

function bindProductButtons() {
  document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
    btn.onclick = () => addToCart(Number(btn.dataset.productId), 1);
  });

  document.querySelectorAll('[data-slider-prev]').forEach(btn => {
    btn.onclick = (event) => {
      event.preventDefault();
      event.stopPropagation();
      moveSlider(Number(btn.dataset.sliderPrev), -1);
    };
  });

  document.querySelectorAll('[data-slider-next]').forEach(btn => {
    btn.onclick = (event) => {
      event.preventDefault();
      event.stopPropagation();
      moveSlider(Number(btn.dataset.sliderNext), 1);
    };
  });
}

export function renderProducts() {
  const featured = document.getElementById('featuredProducts');
  const catalog = document.getElementById('catalogProducts');

  if (featured) featured.innerHTML = state.catalogError
    ? `<p class="empty-state">Не вдалося завантажити товари: ${escapeHtml(state.catalogError)}</p>`
    : state.products.slice(0, 3).map(buildProductCard).join('');

  if (catalog) {
    const filtered = filterProductsForCatalog(state.products);
    catalog.innerHTML = state.catalogError
      ? `<p class="empty-state">Помилка завантаження каталогу: ${escapeHtml(state.catalogError)}</p>`
      : filtered.length ? filtered.map(buildProductCard).join('') : '<p class="empty-state">Товарів поки немає.</p>'; 
  }

  bindProductButtons();
}

export async function initPageProducts() {
  if (!['home', 'products'].includes(document.body.dataset.page)) return;
  await loadProducts();
  await loadDiscounts();
  await enrichProductImages(state.products);
  renderProducts();
}

function renderSingleProductSlider(images, productName) {
  if (!images.length) {
    return '<div class="single-product-placeholder">✿</div>';
  }

  const hasSlider = images.length > 1;
  return `
    <div class="single-product-gallery-slider" id="singleProductSlider">
      ${images.map((img, index) => {
    const url = normalizeImageUrl(img.imageUrl || img.url || '');
    return `<img src="${url}" alt="${escapeHtml(productName)}" class="single-product-slide ${index === 0 ? 'active' : ''}" data-single-slide="${index}">`;
  }).join('')}
      ${hasSlider ? `<button class="single-product-nav prev" type="button" data-single-slider-prev>‹</button>
      <button class="single-product-nav next" type="button" data-single-slider-next>›</button>
      <div class="single-product-dots">
        ${images.map((_, index) => `<button class="single-product-dot ${index === 0 ? 'active' : ''}" type="button" data-single-slide-to="${index}" aria-label="Фото ${index + 1}"></button>`).join('')}
      </div>` : ''}
    </div>`;
}

function moveSingleProductSlider(direction = 1, targetIndex = null) {
  const slider = document.getElementById('singleProductSlider');
  if (!slider) return;

  const slides = [...slider.querySelectorAll('.single-product-slide')];
  const dots = [...slider.querySelectorAll('.single-product-dot')];
  if (slides.length <= 1) return;

  const currentIndex = slides.findIndex(slide => slide.classList.contains('active'));
  slides[currentIndex]?.classList.remove('active');
  dots[currentIndex]?.classList.remove('active');

  let nextIndex = typeof targetIndex === 'number' ? targetIndex : currentIndex + direction;
  if (nextIndex < 0) nextIndex = slides.length - 1;
  if (nextIndex >= slides.length) nextIndex = 0;

  slides[nextIndex]?.classList.add('active');
  dots[nextIndex]?.classList.add('active');
}

function bindSingleProductSlider() {
  document.querySelector('[data-single-slider-prev]')?.addEventListener('click', () => moveSingleProductSlider(-1));
  document.querySelector('[data-single-slider-next]')?.addEventListener('click', () => moveSingleProductSlider(1));
  document.querySelectorAll('[data-single-slide-to]').forEach(btn => {
    btn.addEventListener('click', () => moveSingleProductSlider(0, Number(btn.dataset.singleSlideTo)));
  });
}

export async function initProductPage() {
  if (document.body.dataset.page !== 'product') return;
  const container = document.getElementById('productPageContent');
  if (!container) return;

  const productId = Number(new URLSearchParams(window.location.search).get('id'));
  if (!productId) {
    container.innerHTML = '<p class="empty-state">Товар не знайдено.</p>';
    return;
  }

  try {
    const product =
      await api(`${ROUTES.products}/${productId}`);
    await loadDiscounts();
    try {
      const images =
        await api(`/api/products/${productId}/images`);
      product.productImages = Array.isArray(images) ? images : [];
    } catch {
      product.productImages = product.productImages || [];
    }

    const isAdmin = String(state.me?.role || '').toLowerCase() === 'admin';
    const images = product.productImages || [];
    const percent = getDiscountPercent(productIdValue(product));
    const priceHtml = percent
      ? `<p class="single-product-price price-line"><span class="discount-badge">-${percent.toFixed(0)}%</span> <span class="old-price">${formatPrice(product.price)}</span> <span class="new-price">${formatPrice(getDiscountedPrice(product))}</span></p>`
      : `<p class="single-product-price">${formatPrice(product.price)}</p>`;

    container.innerHTML = `
      <section class="single-product-card">
        <div class="single-product-gallery">
          ${renderSingleProductSlider(images, product.name)}
        </div>
        <div class="single-product-info">
          <h1>${escapeHtml(product.name)}</h1>
          ${priceHtml}
          <p>${escapeHtml(product.description || '')}</p>
          <p>Категорія: ${escapeHtml(getCategoryName(product))}</p>
          <p>На складі: ${Number(product.stockQuantity || 0)}</p>
          <div class="single-product-actions">
            <button class="primary-btn" id="buySingleProductBtn" type="button">ДОДАТИ У КОШИК</button>
            ${isAdmin ? `<a class="product-btn" href="admin-products.html?edit=${productIdValue(product)}">РЕДАГУВАТИ</a>` : ''}
          </div>
        </div>
      </section>`;

    document.getElementById('buySingleProductBtn')?.addEventListener('click', () => addToCart(productIdValue(product), 1));
    bindSingleProductSlider();
  } catch (error) {
    container.innerHTML = `<p class="empty-state">${escapeHtml(error.message || 'Не вдалося завантажити товар.')}</p>`;
  }
}
