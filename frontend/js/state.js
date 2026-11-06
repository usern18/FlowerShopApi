export const state = {
  products: [],
  categories: [],
  discounts: [],
  cart: null,
  me: null,
  selectedPayment: 'cash'
};

export const demoProducts = [
  { productId: 1, name: 'Букет троянд', description: 'Ніжний букет у світлих відтінках', price: 1200, stockQuantity: 5, isAvailable: true, category: { categoryId: 1, categoryName: 'Букети' }, productImages: [] },
  { productId: 2, name: 'Півонії', description: 'Сезонні квіти для подарунка', price: 950, stockQuantity: 8, isAvailable: true, category: { categoryId: 2, categoryName: 'Квіти' }, productImages: [] },
  { productId: 3, name: 'Подарунковий набір', description: 'Квіти + солодощі + листівка', price: 1500, stockQuantity: 3, isAvailable: true, category: { categoryId: 3, categoryName: 'Подарункові набори' }, productImages: [] }
];
