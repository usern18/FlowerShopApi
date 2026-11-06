import { api } from './api.js';
import { addToCart } from './cart.js';
import { state } from './state.js';
import { SITE_BASE, ROUTES } from './config.js';

function getIdFromUrl() {
  const params = new URLSearchParams(window.location.search);
  return Number(params.get('id'));
}

export async function initProductPage() {
  const container = document.getElementById('productPageContent');
  if (!container) return;

  const productId = getIdFromUrl();
  if (!productId) {
    container.innerHTML = '<p>Товар не знайдено.</p>';
    return;
  }

  try {
    const product = await api(`${ROUTES.products}/${productId}`);
    let me = null;

    try {
      me = state.me;
    } catch {}

    const isAdmin = String(me?.role || '').toLowerCase() === 'admin';

    const imageUrl = product.productImages?.[0]?.imageUrl
      ? `${SITE_BASE}${product.productImages[0].imageUrl}`
      : '';

    container.innerHTML = `
      <section class="single-product-card">
        <div class="single-product-gallery">
          ${imageUrl ? `<img src="${imageUrl}" alt="${product.name}" class="single-product-image">` : '<div class="single-product-placeholder">✿</div>'}
        </div>

        <div class="single-product-info">
          <h1>${product.name}</h1>
          <p class="single-product-price">${Number(product.price).toFixed(0)} UAH</p>
          <p>${product.description || ''}</p>
          <p>Категорія: ${product.category?.categoryName || ''}</p>

          <div class="single-product-actions">
            <button class="primary-btn" id="buySingleProductBtn">ДОДАТИ У КОШИК</button>
            ${isAdmin ? `<a class="product-btn" href="admin.html?edit=${(product.id ?? product.productId)}">РЕДАГУВАТИ</a>` : ''}
          </div>
        </div>
      </section>
    `;

    document.getElementById('buySingleProductBtn')?.addEventListener('click', () => {
      addToCart((product.id ?? product.productId), 1);
    });
  } catch (error) {
    container.innerHTML = `<p>${error.message || 'Не вдалося завантажити товар.'}</p>`;
  }
}