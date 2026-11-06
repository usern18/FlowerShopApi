import { API_BASE, ROUTES } from './config.js';
import { api, authHeaders, getToken } from './api.js';
import { state } from './state.js';
import { loadCategories, loadProducts, enrichProductImages, loadDiscounts, getDiscountPercent, getDiscountedPrice, saveLocalDiscount, removeLocalDiscount } from './products.js';
import { escapeHtml, formatPrice, getCategoryName, normalizeImageUrl, setAdminResult, showToast } from './ui.js';

export async function initAdminPage() {
  if (document.body.dataset.page !== 'admin') return;

  if (!getToken()) {
    showToast('Потрібно увійти як адміністратор');
    window.location.href = 'index.html';
    return;
  }

  if (String(state.me?.role || '').toLowerCase() !== 'admin') {
    showToast('Доступ тільки для адміністратора');
    window.location.href = 'index.html';
    return;
  }

  await loadCategories();
  await loadProducts();
  await loadDiscounts();
  state.promocodes = []; // No PromocodesController in current monolith
  await enrichProductImages(state.products);

  renderAdminCategoriesSelect();
  renderAdminFilterOptions();
  bindAdminListControls();
  renderCategoryList();
  renderDiscountProductSelect();
  renderDiscountsList();
  renderPromocodesList();
  renderAdminProducts();
  bindAdminEvents();

  const editId = Number(new URLSearchParams(window.location.search).get('edit'));
  if (editId) fillAdminForm(editId);
}

function categoryIdOf(category) {
  return category?.categoryId ?? category?.CategoryId ?? category?.id ?? category?.Id;
}

function categoryNameOf(category) {
  return category?.categoryName ?? category?.CategoryName ?? category?.name ?? category?.Name ?? '';
}

function categoryDescriptionOf(category) {
  return category?.description ?? category?.Description ?? '';
}

function discountIdOf(discount) {
  return discount?.discountId ?? discount?.DiscountId;
}

function discountProductIdOf(discount) {
  return discount?.productId
    ?? discount?.ProductId
    ?? discount?.product?.productId
    ?? discount?.Product?.ProductId
    ?? discount?.product?.id
    ?? discount?.Product?.Id;
}

function discountPercentOf(discount) {
  return Number(discount?.discountPercent ?? discount?.DiscountPercent ?? 0);
}

function discountStartOf(discount) {
  return discount?.startDate ?? discount?.StartDate ?? '';
}

function discountEndOf(discount) {
  return discount?.endDate ?? discount?.EndDate ?? '';
}
function promocodeIdOf(promocode) {
  return promocode?.promocodeId ?? promocode?.PromocodeId ?? promocode?.id ?? promocode?.Id;
}

function promocodeCodeOf(promocode) {
  return promocode?.code ?? promocode?.Code ?? '';
}

function promocodePercentOf(promocode) {
  return Number(promocode?.discountPercent ?? promocode?.DiscountPercent ?? 0);
}

function promocodeMaxUsesOf(promocode) {
  return promocode?.maxUses ?? promocode?.MaxUses ?? null;
}

function promocodeUsedCountOf(promocode) {
  return Number(promocode?.usedCount ?? promocode?.UsedCount ?? 0);
}

function promocodeStartOf(promocode) {
  return promocode?.startDate ?? promocode?.StartDate ?? '';
}

function promocodeEndOf(promocode) {
  return promocode?.endDate ?? promocode?.EndDate ?? '';
}

function isPromocodeActive(promocode) {
  return promocode?.isActive ?? promocode?.IsActive ?? true;
}

async function loadPromocodes() {
  try {
    const response = await api(`/api/promocodes?_=${Date.now()}`, { headers: authHeaders(false) });
    state.promocodes = Array.isArray(response) ? response : (response?.items || response?.data || response?.promocodes || response?.$values || []);
  } catch {
    state.promocodes = [];
  }
}


function formatDateForInput(date = new Date()) {
  const d = new Date(date);
  const offset = d.getTimezoneOffset();
  const localDate = new Date(d.getTime() - offset * 60000);
  return localDate.toISOString().slice(0, 16);
}

const adminListState = {
  products: { page: 1 },
  categories: { page: 1 },
  discounts: { page: 1 },
  promocodes: { page: 1 }
};

function textValue(value) {
  return String(value ?? '').toLowerCase().trim();
}

function numberValue(value) {
  return Number(value || 0);
}

function controlValue(id, fallback = 'all') {
  return document.getElementById(id)?.value || fallback;
}

function getPageSize(kind) {
  return Number(controlValue(`admin${capitalize(kind)}PageSize`, '5')) || 5;
}

function capitalize(value) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function categoryIdOfProduct(product) {
  return product?.categoryId ?? product?.category?.categoryId ?? product?.category?.id ?? product?.CategoryId ?? product?.Category?.CategoryId ?? product?.Category?.Id ?? '';
}

function productIdOf(product) {
  return product?.productId ?? product?.ProductId ?? product?.id ?? product?.Id;
}

function productImagesOf(product) {
  return product?.productImages ?? product?.ProductImages ?? product?.images ?? product?.Images ?? [];
}

function imageIdOf(image) {
  return image?.imageId ?? image?.ImageId ?? image?.id ?? image?.Id;
}

function imageUrlOf(image) {
  return image?.imageUrl ?? image?.ImageUrl ?? image?.url ?? image?.Url ?? '';
}

function productById(productId) {
  return state.products.find(p => Number(productIdOf(p)) === Number(productId));
}

function isDiscountActive(discount) {
  const now = new Date();
  const start = discountStartOf(discount) ? new Date(discountStartOf(discount)) : null;
  const end = discountEndOf(discount) ? new Date(discountEndOf(discount)) : null;
  return (!start || start <= now) && (!end || end >= now);
}

function discountStatus(discount) {
  const now = new Date();
  const start = discountStartOf(discount) ? new Date(discountStartOf(discount)) : null;
  const end = discountEndOf(discount) ? new Date(discountEndOf(discount)) : null;
  if (start && start > now) return 'future';
  if (end && end < now) return 'expired';
  return 'active';
}

function paginateItems(kind, items) {
  const pageSize = getPageSize(kind);
  const pages = Math.max(1, Math.ceil(items.length / pageSize));
  adminListState[kind].page = Math.min(Math.max(1, adminListState[kind].page || 1), pages);
  const page = adminListState[kind].page;
  return {
    page,
    pages,
    total: items.length,
    items: items.slice((page - 1) * pageSize, page * pageSize)
  };
}

function renderPagination(kind, total) {
  const box = document.getElementById(`admin${capitalize(kind)}Pagination`);
  if (!box) return;

  const pageSize = getPageSize(kind);
  const pages = Math.max(1, Math.ceil(total / pageSize));
  const page = Math.min(adminListState[kind].page || 1, pages);

  if (total <= pageSize) {
    box.innerHTML = '';
    return;
  }

  const pageButtons = Array.from({ length: pages }, (_, index) => index + 1)
    .filter(pageNumber => pageNumber === 1 || pageNumber === pages || Math.abs(pageNumber - page) <= 1)
    .map((pageNumber, index, arr) => {
      const gap = index > 0 && pageNumber - arr[index - 1] > 1 ? '<span class="admin-page-gap">...</span>' : '';
      return `${gap}<button class="admin-page-btn ${pageNumber === page ? 'active' : ''}" type="button" data-admin-page="${kind}" data-page="${pageNumber}">${pageNumber}</button>`;
    }).join('');

  box.innerHTML = `
    <button class="product-btn" type="button" data-admin-page="${kind}" data-page="${page - 1}" ${page <= 1 ? 'disabled' : ''}>Назад</button>
    <div class="admin-page-numbers">${pageButtons}</div>
    <button class="product-btn" type="button" data-admin-page="${kind}" data-page="${page + 1}" ${page >= pages ? 'disabled' : ''}>Далі</button>`;

  box.querySelectorAll('[data-admin-page]').forEach(btn => {
    btn.onclick = () => {
      adminListState[kind].page = Number(btn.dataset.page);
      if (kind === 'products') renderAdminProducts();
      if (kind === 'categories') renderCategoryList();
      if (kind === 'discounts') renderDiscountsList();
    };
  });
}

function renderSummary(kind, shown, total, page, pages) {
  const box = document.getElementById(`admin${capitalize(kind)}Summary`);
  if (!box) return;
  box.textContent = `Знайдено: ${total}. Сторінка ${page} з ${pages}. Показано: ${shown}.`;
}

function renderAdminFilterOptions() {
  const productCategory = document.getElementById('adminProductsCategoryFilter');
  const discountCategory = document.getElementById('adminDiscountsCategoryFilter');
  const options = '<option value="all">Усі категорії</option>' + state.categories
    .map(c => `<option value="${categoryIdOf(c)}">${escapeHtml(categoryNameOf(c))}</option>`)
    .join('');

  if (productCategory) {
    const current = productCategory.value;
    productCategory.innerHTML = options;
    productCategory.value = current || 'all';
  }

  if (discountCategory) {
    const current = discountCategory.value;
    discountCategory.innerHTML = options;
    discountCategory.value = current || 'all';
  }
}

function bindAdminListControls() {
  document.querySelectorAll('[data-admin-filter]').forEach(control => {
    if (control.dataset.boundAdminFilter) return;
    control.dataset.boundAdminFilter = 'true';
    const kind = control.dataset.adminFilter;
    const eventName = control.matches('input[type="search"]') ? 'input' : 'change';
    control.addEventListener(eventName, () => {
      adminListState[kind].page = 1;
      if (kind === 'products') renderAdminProducts();
      if (kind === 'categories') renderCategoryList();
      if (kind === 'discounts') renderDiscountsList();
    });
  });
}

function renderAdminCategoriesSelect() {
  const select = document.getElementById('categoryId');
  if (!select) return;

  if (!state.categories.length) {
    select.innerHTML = '<option value="">Категорії не знайдено</option>';
    return;
  }

  select.innerHTML = state.categories
    .map(c => `<option value="${categoryIdOf(c)}">${escapeHtml(categoryNameOf(c))}</option>`)
    .join('');
}

function renderCategoryList() {
  const list = document.getElementById('adminCategoriesList');
  if (!list) return;

  const search = textValue(controlValue('adminCategoriesSearch', ''));
  const usageFilter = controlValue('adminCategoriesUsageFilter', 'all');
  const sort = controlValue('adminCategoriesSort', 'idDesc');

  let categories = [...state.categories];
  categories = categories.filter(category => {
    const categoryId = categoryIdOf(category);
    const productsCount = state.products.filter(p => Number(categoryIdOfProduct(p)) === Number(categoryId)).length;
    const matchesSearch = !search || [categoryId, categoryNameOf(category), categoryDescriptionOf(category)]
      .some(value => textValue(value).includes(search));
    const matchesUsage = usageFilter === 'all'
      || (usageFilter === 'used' && productsCount > 0)
      || (usageFilter === 'empty' && productsCount === 0);
    return matchesSearch && matchesUsage;
  });

  categories.sort((a, b) => {
    const idA = Number(categoryIdOf(a) || 0);
    const idB = Number(categoryIdOf(b) || 0);
    const nameA = categoryNameOf(a).localeCompare(categoryNameOf(b), 'uk');
    if (sort === 'idAsc') return idA - idB;
    if (sort === 'nameAsc') return nameA;
    if (sort === 'nameDesc') return -nameA;
    return idB - idA;
  });

  if (!categories.length) {
    list.innerHTML = '<p class="empty-state">Категорій за такими параметрами не знайдено.</p>';
    renderSummary('categories', 0, 0, 1, 1);
    renderPagination('categories', 0);
    return;
  }

  const pageData = paginateItems('categories', categories);
  list.innerHTML = pageData.items.map(category => {
    const productsCount = state.products.filter(p => Number(categoryIdOfProduct(p)) === Number(categoryIdOf(category))).length;
    return `
    <div class="admin-product-item">
      <div class="admin-product-head">
        <strong>${escapeHtml(categoryNameOf(category))}</strong>
        <span>ID: ${categoryIdOf(category)}</span>
      </div>
      <div class="muted">${escapeHtml(categoryDescriptionOf(category) || 'Без опису')}</div>
      <div class="muted">Товарів у категорії: ${productsCount}</div>
      <div class="admin-product-actions">
        <button class="product-btn" type="button" data-edit-category="${categoryIdOf(category)}">РЕДАГУВАТИ</button>
        <button class="product-btn primary" type="button" data-delete-category="${categoryIdOf(category)}">ВИДАЛИТИ</button>
      </div>
    </div>`;
  }).join('');

  renderSummary('categories', pageData.items.length, pageData.total, pageData.page, pageData.pages);
  renderPagination('categories', pageData.total);

  list.querySelectorAll('[data-edit-category]').forEach(btn => {
    btn.onclick = () => fillCategoryForm(Number(btn.dataset.editCategory));
  });

  list.querySelectorAll('[data-delete-category]').forEach(btn => {
    btn.onclick = () => deleteCategory(Number(btn.dataset.deleteCategory));
  });
}


function renderDiscountProductSelect() {
  const input = document.getElementById('discountProductSearch');
  const hidden = document.getElementById('discountProductId');
  const datalist = document.getElementById('discountProductOptions');
  if (!input || !hidden || !datalist) return;

  if (!state.products.length) {
    datalist.innerHTML = '';
    input.placeholder = 'Товарів не знайдено';
    hidden.value = '';
    return;
  }

  datalist.innerHTML = state.products
    .map(product => {
      const productId = productIdOf(product);
      const label = `${product.name || 'Товар'} — ID ${productId}`;
      return `<option value="${escapeHtml(label)}"></option>`;
    })
    .join('');

  if (!input.dataset.boundProductSearch) {
    input.dataset.boundProductSearch = 'true';
    input.addEventListener('input', syncDiscountProductInput);
    input.addEventListener('change', syncDiscountProductInput);
    input.addEventListener('blur', syncDiscountProductInput);
  }
}

function syncDiscountProductInput() {
  const input = document.getElementById('discountProductSearch');
  const hidden = document.getElementById('discountProductId');
  if (!input || !hidden) return;

  const raw = input.value.trim();
  const idFromText = raw.match(/(?:ID\s*)?(\d+)\s*$/i)?.[1];
  const product = state.products.find(p => {
    const productId = Number(productIdOf(p));
    return (idFromText && productId === Number(idFromText))
      || textValue(p.name) === textValue(raw)
      || textValue(`${p.name} — ID ${productId}`) === textValue(raw);
  });

  hidden.value = product ? productIdOf(product) : (idFromText || '');
}

function renderDiscountsList() {
  const list = document.getElementById('adminDiscountsList');
  if (!list) return;

  const search = textValue(controlValue('adminDiscountsSearch', ''));
  const categoryFilter = controlValue('adminDiscountsCategoryFilter', 'all');
  const statusFilter = controlValue('adminDiscountsStatusFilter', 'all');
  const percentFilter = controlValue('adminDiscountsPercentFilter', 'all');
  const sort = controlValue('adminDiscountsSort', 'idDesc');

  let discounts = [...state.discounts];
  discounts = discounts.filter(discount => {
    const productId = discountProductIdOf(discount);
    const product = productById(productId);
    const percent = discountPercentOf(discount);
    const status = discountStatus(discount);
    const matchesSearch = !search || [discountIdOf(discount), productId, product?.name, percent]
      .some(value => textValue(value).includes(search));
    const matchesCategory = categoryFilter === 'all' || Number(categoryIdOfProduct(product)) === Number(categoryFilter);
    const matchesStatus = statusFilter === 'all' || status === statusFilter;
    const matchesPercent = percentFilter === 'all'
      || (percentFilter === 'small' && percent < 10)
      || (percentFilter === 'medium' && percent >= 10 && percent <= 30)
      || (percentFilter === 'big' && percent > 30);
    return matchesSearch && matchesCategory && matchesStatus && matchesPercent;
  });

  discounts.sort((a, b) => {
    const idA = Number(discountIdOf(a) || 0);
    const idB = Number(discountIdOf(b) || 0);
    const productA = productById(discountProductIdOf(a))?.name || '';
    const productB = productById(discountProductIdOf(b))?.name || '';
    const startA = new Date(discountStartOf(a) || 0).getTime();
    const startB = new Date(discountStartOf(b) || 0).getTime();
    if (sort === 'idAsc') return idA - idB;
    if (sort === 'percentAsc') return discountPercentOf(a) - discountPercentOf(b);
    if (sort === 'percentDesc') return discountPercentOf(b) - discountPercentOf(a);
    if (sort === 'startAsc') return startA - startB;
    if (sort === 'startDesc') return startB - startA;
    if (sort === 'productAsc') return productA.localeCompare(productB, 'uk');
    return idB - idA;
  });

  if (!discounts.length) {
    list.innerHTML = '<p class="empty-state">Знижок за такими параметрами не знайдено.</p>';
    renderSummary('discounts', 0, 0, 1, 1);
    renderPagination('discounts', 0);
    return;
  }

  const pageData = paginateItems('discounts', discounts);
  list.innerHTML = pageData.items.map(discount => {
    const productId = discountProductIdOf(discount);
    const product = productById(productId);
    const status = discountStatus(discount);
    const statusText = status === 'active' ? 'Активна' : status === 'future' ? 'Запланована' : 'Завершена';
    return `
      <div class="admin-product-item">
        <div class="admin-product-head">
          <strong>${escapeHtml(product?.name || `Товар ID ${productId}`)}</strong>
          <span class="discount-badge">-${discountPercentOf(discount).toFixed(0)}%</span>
        </div>
        <div class="muted">ID знижки: ${discountIdOf(discount) || '—'}</div>
        <div class="muted">Категорія: ${escapeHtml(product ? getCategoryName(product) : 'невідомо')}</div>
        <div class="muted">Статус: ${statusText}</div>
        <div class="muted">Початок: ${escapeHtml(formatDateReadable(discountStartOf(discount)))}</div>
        <div class="muted">Кінець: ${escapeHtml(formatDateReadable(discountEndOf(discount)))}</div>
        <div class="admin-product-actions">
          <button class="product-btn primary" type="button" data-delete-discount="${discountIdOf(discount)}">ВИДАЛИТИ</button>
        </div>
      </div>`;
  }).join('');

  renderSummary('discounts', pageData.items.length, pageData.total, pageData.page, pageData.pages);
  renderPagination('discounts', pageData.total);

  list.querySelectorAll('[data-delete-discount]').forEach(btn => {
    btn.onclick = () => deleteDiscount(Number(btn.dataset.deleteDiscount));
  });
}


function renderPromocodesList() {
  const list = document.getElementById('adminPromocodesList');
  if (!list) return;

  const promocodes = [...(state.promocodes || [])].sort((a, b) => Number(promocodeIdOf(b) || 0) - Number(promocodeIdOf(a) || 0));

  if (!promocodes.length) {
    list.innerHTML = '<p class="empty-state">Промокодів поки немає.</p>';
    return;
  }

  list.innerHTML = promocodes.map(promocode => {
    const id = promocodeIdOf(promocode);
    const maxUses = promocodeMaxUsesOf(promocode);
    const used = promocodeUsedCountOf(promocode);
    const statusText = isPromocodeActive(promocode) ? 'Активний' : 'Вимкнений';
    return `
      <div class="admin-product-item">
        <div class="admin-product-head">
          <strong>${escapeHtml(promocodeCodeOf(promocode))}</strong>
          <span class="discount-badge">-${promocodePercentOf(promocode).toFixed(0)}%</span>
        </div>
        <div class="muted">ID промокоду: ${id || '—'}</div>
        <div class="muted">Статус: ${statusText}</div>
        <div class="muted">Використано: ${used}${maxUses ? ` / ${maxUses}` : ' / без ліміту'}</div>
        <div class="muted">Початок: ${escapeHtml(formatDateReadable(promocodeStartOf(promocode)))}</div>
        <div class="muted">Кінець: ${escapeHtml(formatDateReadable(promocodeEndOf(promocode)))}</div>
        <div class="admin-product-actions">
          <button class="product-btn primary" type="button" data-delete-promocode="${id}">ВИДАЛИТИ</button>
        </div>
      </div>`;
  }).join('');

  list.querySelectorAll('[data-delete-promocode]').forEach(btn => {
    btn.onclick = () => deletePromocode(Number(btn.dataset.deletePromocode));
  });
}

async function deletePromocode(promocodeId) {
  if (!confirm('Видалити промокод?')) return;

  try {
    const result = await api(`/api/promocodes/${promocodeId}`, { method: 'DELETE', headers: authHeaders(false) });
    setAdminResult(result || 'Промокод видалено');
    state.promocodes = []; // No PromocodesController in current monolith
    renderPromocodesList();
    showToast('Промокод видалено');
  } catch (error) {
    setAdminResult(error.payload || error.message);
    showToast(error.message || 'Не вдалося видалити промокод');
  }
}


function formatDateReadable(value) {
  if (!value) return 'не вказано';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return date.toLocaleString('uk-UA');
}

function renderAdminProducts() {
  const list = document.getElementById('adminProductsList');
  if (!list) return;

  const search = textValue(controlValue('adminProductsSearch', ''));
  const categoryFilter = controlValue('adminProductsCategoryFilter', 'all');
  const statusFilter = controlValue('adminProductsStatusFilter', 'all');
  const discountFilter = controlValue('adminProductsDiscountFilter', 'all');
  const stockFilter = controlValue('adminProductsStockFilter', 'all');
  const sort = controlValue('adminProductsSort', 'idDesc');

  let products = [...state.products];
  products = products.filter(product => {
    const stock = numberValue(product.stockQuantity);
    const percent = getDiscountPercent(productIdOf(product));
    const matchesSearch = !search || [productIdOf(product), product.name, product.description, getCategoryName(product)]
      .some(value => textValue(value).includes(search));
    const matchesCategory = categoryFilter === 'all' || Number(categoryIdOfProduct(product)) === Number(categoryFilter);
    const matchesStatus = statusFilter === 'all'
      || (statusFilter === 'available' && product.isAvailable)
      || (statusFilter === 'unavailable' && !product.isAvailable);
    const matchesDiscount = discountFilter === 'all'
      || (discountFilter === 'with' && percent > 0)
      || (discountFilter === 'without' && !percent);
    const matchesStock = stockFilter === 'all'
      || (stockFilter === 'inStock' && stock > 0)
      || (stockFilter === 'lowStock' && stock > 0 && stock <= 5)
      || (stockFilter === 'outOfStock' && stock <= 0);
    return matchesSearch && matchesCategory && matchesStatus && matchesDiscount && matchesStock;
  });

  products.sort((a, b) => {
    const idA = Number(a.productId || 0);
    const idB = Number(b.productId || 0);
    const nameCompare = String(a.name || '').localeCompare(String(b.name || ''), 'uk');
    if (sort === 'idAsc') return idA - idB;
    if (sort === 'nameAsc') return nameCompare;
    if (sort === 'nameDesc') return -nameCompare;
    if (sort === 'priceAsc') return numberValue(a.price) - numberValue(b.price);
    if (sort === 'priceDesc') return numberValue(b.price) - numberValue(a.price);
    if (sort === 'stockAsc') return numberValue(a.stockQuantity) - numberValue(b.stockQuantity);
    if (sort === 'stockDesc') return numberValue(b.stockQuantity) - numberValue(a.stockQuantity);
    return idB - idA;
  });

  if (!products.length) {
    list.innerHTML = '<p class="empty-state">Товарів за такими параметрами не знайдено.</p>';
    renderSummary('products', 0, 0, 1, 1);
    renderPagination('products', 0);
    return;
  }

  const pageData = paginateItems('products', products);
  list.innerHTML = pageData.items.map(product => {
    const productImages = productImagesOf(product);
    const percent = getDiscountPercent(productIdOf(product));
    const discounted = getDiscountedPrice(product);
    return `
      <div class="admin-product-item">
        <div class="admin-product-head">
          <strong>${escapeHtml(product.name)}</strong>
          <span>${percent ? `<span class="old-price">${formatPrice(product.price)}</span> <span class="new-price">${formatPrice(discounted)}</span>` : formatPrice(product.price)}</span>
        </div>
        ${percent ? `<div><span class="discount-badge">-${percent.toFixed(0)}%</span></div>` : ''}
        ${productImages.length ? `
          <div class="admin-product-images-grid">
            ${productImages.map(image => {
              const imageId = imageIdOf(image);
              const imageUrl = normalizeImageUrl(imageUrlOf(image));
              return `
                <div class="admin-product-image-card">
                  <img src="${imageUrl}" alt="${escapeHtml(product.name)}" class="admin-product-preview">
                  <button class="admin-image-delete-btn" type="button" data-delete-product-image="${imageId}" title="Видалити фото">×</button>
                </div>`;
            }).join('')}
          </div>` : '<div class="muted">Фото ще не додано</div>'}
        <div class="muted">ID: ${productIdOf(product)}</div>
        <div class="muted">Категорія: ${escapeHtml(getCategoryName(product))}</div>
        <div class="muted">Склад: ${Number(product.stockQuantity || 0)}</div>
        <div class="muted">Статус: ${product.isAvailable ? 'Доступний' : 'Недоступний'}</div>
        <div class="admin-product-actions">
          <button class="product-btn" type="button" data-edit-product="${productIdOf(product)}">РЕДАГУВАТИ</button>
          <button class="product-btn primary" type="button" data-delete-product="${productIdOf(product)}">ВИДАЛИТИ</button>
        </div>
      </div>`;
  }).join('');

  renderSummary('products', pageData.items.length, pageData.total, pageData.page, pageData.pages);
  renderPagination('products', pageData.total);

  list.querySelectorAll('[data-edit-product]').forEach(btn => {
    btn.onclick = () => {
      window.location.href = `admin-products.html?edit=${btn.dataset.editProduct}`;
    };
  });

  list.querySelectorAll('[data-delete-product]').forEach(btn => {
    btn.onclick = () => deleteProduct(Number(btn.dataset.deleteProduct));
  });

  list.querySelectorAll('[data-delete-product-image]').forEach(btn => {
    btn.onclick = () => deleteProductImage(Number(btn.dataset.deleteProductImage));
  });
}


function bindAdminEvents() {
  document.getElementById('refreshAdminProducts')?.addEventListener('click', async () => {
    await loadProducts();
    await loadDiscounts();
    await enrichProductImages(state.products);
    renderAdminFilterOptions();
    renderAdminProducts();
    renderDiscountProductSelect();
  });

  document.getElementById('resetProductForm')?.addEventListener('click', () => resetAdminForm());
  document.getElementById('resetCategoryForm')?.addEventListener('click', () => resetCategoryForm());
  document.getElementById('productForm')?.addEventListener('submit', submitProductForm);
  document.getElementById('imageUploadForm')?.addEventListener('submit', submitImageUpload);
  document.getElementById('categoryForm')?.addEventListener('submit', submitCategoryForm);
  document.getElementById('discountForm')?.addEventListener('submit', submitDiscountForm);
  // Відсутній PromocodesController: не реєструємо форму промокодів.

  const start = document.getElementById('discountStartDate');
  const end = document.getElementById('discountEndDate');
  if (start && !start.value) start.value = formatDateForInput(new Date());
  if (end && !end.value) {
    const later = new Date();
    later.setMonth(later.getMonth() + 1);
    end.value = formatDateForInput(later);
  }
}

function fillAdminForm(productId) {
  const product = state.products.find(p => Number(p.productId) === Number(productId));
  if (!product) return;

  document.getElementById('adminFormTitle').textContent = 'Редагувати товар';
  document.getElementById('productId').value = productIdOf(product);
  document.getElementById('categoryId').value = product.categoryId || product.category?.categoryId || '';
  document.getElementById('productName').value = product.name || '';
  document.getElementById('productDescription').value = product.description || '';
  document.getElementById('productPrice').value = product.price || '';
  document.getElementById('productStock').value = product.stockQuantity || 0;
  document.getElementById('productAvailable').checked = Boolean(product.isAvailable);
  if (document.getElementById('imageProductId')) document.getElementById('imageProductId').value = productIdOf(product);
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function resetAdminForm(clearImageId = true) {
  document.getElementById('adminFormTitle').textContent = 'Додати товар';
  document.getElementById('productForm')?.reset();
  document.getElementById('productId').value = '';
  document.getElementById('productAvailable').checked = true;
  if (clearImageId && document.getElementById('imageProductId')) document.getElementById('imageProductId').value = '';
  renderAdminCategoriesSelect();
}

function fillCategoryForm(categoryId) {
  const category = state.categories.find(c => Number(categoryIdOf(c)) === Number(categoryId));
  if (!category) return;

  document.getElementById('categoryFormTitle').textContent = 'Редагувати категорію';
  document.getElementById('editCategoryId').value = categoryIdOf(category);
  document.getElementById('categoryName').value = categoryNameOf(category);
  document.getElementById('categoryDescription').value = categoryDescriptionOf(category);
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function resetCategoryForm() {
  document.getElementById('categoryFormTitle').textContent = 'Додати категорію';
  document.getElementById('categoryForm')?.reset();
  document.getElementById('editCategoryId').value = '';
}

async function submitProductForm(e) {
  e.preventDefault();
  const productId = document.getElementById('productId').value;
  const payload = {
    categoryId: Number(document.getElementById('categoryId').value),
    name: document.getElementById('productName').value.trim(),
    description: document.getElementById('productDescription').value.trim(),
    price: Number(document.getElementById('productPrice').value),
    stockQuantity: Number(document.getElementById('productStock').value),
    isAvailable: document.getElementById('productAvailable').checked
  };

  try {
    const result = productId
      ? await api(`${ROUTES.products}/${productId}`, { method: 'PUT', headers: authHeaders(true), body: JSON.stringify(payload) })
      : await api(ROUTES.products, { method: 'POST', headers: authHeaders(true), body: JSON.stringify(payload) });

    setAdminResult(result || 'Успішно збережено');
    showToast(productId ? 'Товар оновлено' : 'Товар створено');
    if (!productId && (result?.id ?? result?.productId) && document.getElementById('imageProductId')) document.getElementById('imageProductId').value = result.id ?? result.productId;
    resetAdminForm(false);
    await loadProducts();
    await loadDiscounts();
    await enrichProductImages(state.products);
    renderAdminFilterOptions();
    renderAdminProducts();
    renderDiscountProductSelect();
  } catch (error) {
    setAdminResult(error.payload || error.message || 'Помилка збереження товару');
    showToast(error.message || 'Не вдалося зберегти товар');
  }
}

async function deleteProduct(productId) {
  if (!confirm('Видалити товар?')) return;

  try {
    const result = await api(`${ROUTES.products}/${productId}`, { method: 'DELETE', headers: authHeaders(false) });
    setAdminResult(result || 'Товар видалено');
    await loadProducts();
    await loadDiscounts();
    await enrichProductImages(state.products);
    renderAdminFilterOptions();
    renderAdminProducts();
    renderDiscountProductSelect();
    showToast('Товар видалено');
  } catch (error) {
    setAdminResult(error.payload || error.message);
  }
}

async function submitImageUpload(e) {
  e.preventDefault();
  const productId = Number(document.getElementById('imageProductId').value);
  const file = document.getElementById('imageFile').files[0];
  if (!productId || !file) {
    showToast('Вкажи ID товару та файл');
    return;
  }

  const formData = new FormData();
  formData.append('file', file);

  try {
    const response = await fetch(`${API_BASE}/api/products/${productId}/images`, {
      method: 'POST',
      headers: getToken() ? { Authorization: `Bearer ${getToken()}` } : {},
      body: formData
    });
    const contentType = response.headers.get('content-type') || '';
    const payload = contentType.includes('application/json') ? await response.json().catch(() => null) : await response.text().catch(() => '');
    if (!response.ok) throw new Error(typeof payload === 'string' ? payload : payload?.message || 'Не вдалося завантажити фото');

    setAdminResult(payload || 'Фото завантажено');
    document.getElementById('imageUploadForm').reset();
    await loadProducts();
    await enrichProductImages(state.products);
    renderAdminProducts();
    showToast('Фото завантажено');
  } catch (error) {
    setAdminResult(error.message);
    showToast(error.message || 'Не вдалося завантажити фото');
  }
}

async function deleteProductImage(imageId) {
  if (!imageId) {
    showToast('Не вдалося визначити ID фото');
    return;
  }
  if (!confirm('Видалити це фото товару?')) return;

  try {
    const result = await api(`/api/product-images/${imageId}`, { method: 'DELETE', headers: authHeaders(false) });
    setAdminResult(result || 'Фото видалено');
    await loadProducts();
    await enrichProductImages(state.products);
    renderAdminProducts();
    showToast('Фото видалено');
  } catch (error) {
    setAdminResult(error.payload || error.message || 'Помилка видалення фото');
    showToast(error.message || 'Не вдалося видалити фото');
  }
}

async function submitCategoryForm(e) {
  e.preventDefault();
  const categoryId = document.getElementById('editCategoryId').value;
  const payload = {
    categoryName: document.getElementById('categoryName').value.trim(),
    description: document.getElementById('categoryDescription').value.trim()
  };

  try {
    const result = categoryId
      ? await api(`/api/Categories/${categoryId}`, { method: 'PUT', headers: authHeaders(true), body: JSON.stringify(payload) })
      : await api('/api/Categories', { method: 'POST', headers: authHeaders(true), body: JSON.stringify(payload) });

    setAdminResult(result || 'Категорію збережено');
    showToast(categoryId ? 'Категорію оновлено' : 'Категорію створено');
    resetCategoryForm();
    await loadCategories();
    renderAdminFilterOptions();
    renderCategoryList();
    renderAdminCategoriesSelect();
  } catch (error) {
    setAdminResult(error.payload || error.message || 'Помилка категорії');
    showToast(error.message || 'Не вдалося зберегти категорію');
  }
}

async function deleteCategory(categoryId) {
  if (!confirm('Видалити категорію? Якщо до неї привʼязані товари, бекенд може не дозволити видалення.')) return;

  try {
    const result = await api(`/api/Categories/${categoryId}`, { method: 'DELETE', headers: authHeaders(false) });
    setAdminResult(result || 'Категорію видалено');
    await loadCategories();
    renderAdminFilterOptions();
    renderCategoryList();
    renderAdminCategoriesSelect();
    showToast('Категорію видалено');
  } catch (error) {
    setAdminResult(error.payload || error.message);
    showToast(error.message || 'Не вдалося видалити категорію');
  }
}

async function submitDiscountForm(e) {
  e.preventDefault();
  syncDiscountProductInput();
  const payload = {
    productId: Number(document.getElementById('discountProductId').value),
    discountPercent: Number(document.getElementById('discountPercent').value),
    startDate: new Date(document.getElementById('discountStartDate').value).toISOString(),
    endDate: new Date(document.getElementById('discountEndDate').value).toISOString()
  };

  if (!payload.productId) {
    showToast('Оберіть товар');
    return;
  }
  if (payload.discountPercent < 0 || payload.discountPercent > 100) {
    showToast('Знижка має бути від 0 до 100');
    return;
  }
  if (new Date(payload.startDate) >= new Date(payload.endDate)) {
    showToast('Дата завершення має бути пізніше дати початку');
    return;
  }

  try {
    const result = await api('/api/Discounts', { method: 'POST', headers: authHeaders(true), body: JSON.stringify(payload) });
    saveLocalDiscount(result && typeof result === 'object' ? { ...payload, ...result } : payload);
    setAdminResult(result || payload);
    showToast('Знижку збережено');
    document.getElementById('discountForm').reset();
    const productSearch = document.getElementById('discountProductSearch');
    const productHidden = document.getElementById('discountProductId');
    if (productSearch) productSearch.value = '';
    if (productHidden) productHidden.value = '';
    await loadDiscounts();
    renderDiscountsList();
    renderAdminProducts();
    const start = document.getElementById('discountStartDate');
    const end = document.getElementById('discountEndDate');
    if (start) start.value = formatDateForInput(new Date());
    if (end) {
      const later = new Date();
      later.setMonth(later.getMonth() + 1);
      end.value = formatDateForInput(later);
    }
  } catch (error) {
    setAdminResult(error.payload || error.message);
    showToast(error.message || 'Не вдалося зберегти знижку');
  }
}

async function deleteDiscount(discountId) {
  if (!confirm('Видалити знижку?')) return;

  try {
    const result = await api(`/api/Discounts/${discountId}`, { method: 'DELETE', headers: authHeaders(false) });
    setAdminResult(result || 'Знижку видалено');
    removeLocalDiscount(discountId);
    await loadDiscounts();
    renderDiscountsList();
    renderAdminProducts();
    showToast('Знижку видалено');
  } catch (error) {
    setAdminResult(error.payload || error.message);
    showToast(error.message || 'Не вдалося видалити знижку');
  }
}

async function submitPromoForm(e) {
  e.preventDefault();
  const payload = {
    code: document.getElementById('promoCode').value.trim(),
    discountPercent: Number(document.getElementById('promoPercent').value),
    maxUses: Number(document.getElementById('promoMaxUses').value)
  };

  if (payload.discountPercent < 0 || payload.discountPercent > 100) {
    showToast('Промокод має бути від 0 до 100');
    return;
  }
  if (payload.maxUses < 0) {
    showToast('Кількість використань не може бути менше 0');
    return;
  }

  try {
    const result = await api('/api/promocodes', { method: 'POST', headers: authHeaders(true), body: JSON.stringify(payload) });
    setAdminResult(result);
    showToast('Промокод створено');
    document.getElementById('promoForm').reset();
    state.promocodes = []; // No PromocodesController in current monolith
    renderPromocodesList();
  } catch (error) {
    setAdminResult(error.payload || error.message || 'Бекенд поки не має endpoint для промокодів');
    showToast(error.message || 'Не вдалося створити промокод');
  }
}
