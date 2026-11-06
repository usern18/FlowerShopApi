// FlowerShopApi monolith (Module 4). Override in js/config.js for a deployed API.
const isLocal = ['localhost', '127.0.0.1'].includes(window.location.hostname);

export const API_BASE = isLocal
    ? 'http://localhost:5239'
    : 'https://flowershopapi-production.up.railway.app';
export const SITE_BASE = API_BASE;
// Routes here refer to FlowerShopApi/Controllers, not AuthService/CatalogService/Gateway.
export const ROUTES = Object.freeze({
  products: '/api/products',
  categories: '/api/categories',
  discounts: '/api/discounts',
  login: '/api/auth/login',
  register: '/api/auth/register',
  profile: '/api/profile',
  myOrders: '/api/orders/my',
  orders: '/api/orders'
});
