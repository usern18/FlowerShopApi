// FlowerShopApi monolith (Module 4). Override in js/config.js for a deployed API.
export const API_BASE = 'https://flowershopapi-production.up.railway.app';
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
