import { request } from './client';
import type { Category, DailyRevenue, PagedResult, Product, Sale, TokenResponse, TopProduct, User } from './types';

const query = (values: Record<string, string | number | undefined>) => {
  const search = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => value !== undefined && value !== '' && search.set(key, String(value)));
  return search.toString() ? `?${search}` : '';
};

export const posApi = {
  login: (email: string, password: string) => request<TokenResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string) => request<User>('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) }),
  createUser: (email: string, password: string, owner = false) => request<User>(`/api/users${owner ? '/owners' : ''}`, { method: 'POST', body: JSON.stringify({ email, password }) }),
  categories: () => request<Category[]>('/api/categories'),
  saveCategory: (name: string, id?: string) => request<Category>(id ? `/api/categories/${id}` : '/api/categories', { method: id ? 'PUT' : 'POST', body: JSON.stringify({ name }) }),
  deleteCategory: (id: string) => request<void>(`/api/categories/${id}`, { method: 'DELETE' }),
  products: (filters: { categoryId?: string; search?: string } = {}) => request<Product[]>(`/api/products${query(filters)}`),
  saveProduct: (product: Omit<Product, 'id' | 'categoryName' | 'createdAt'>, id?: string) => request<Product>(id ? `/api/products/${id}` : '/api/products', { method: id ? 'PUT' : 'POST', body: JSON.stringify(product) }),
  deleteProduct: (id: string) => request<void>(`/api/products/${id}`, { method: 'DELETE' }),
  getProduct: (id: string) => request<Product>(`/api/products/${id}`),
  createSale: (paymentMethod: 'Cash' | 'Card', items: { productId: string; quantity: number }[]) => request<Sale>('/api/sales', { method: 'POST', body: JSON.stringify({ paymentMethod: paymentMethod === 'Cash' ? 0 : 1, items }) }),
  sale: (id: string) => request<Sale>(`/api/sales/${id}`),
  sales: (page = 1) => request<PagedResult<Sale>>(`/api/sales${query({ page, pageSize: 12 })}`),
  dailyRevenue: () => request<DailyRevenue[]>('/api/reports/daily-revenue'),
  topProducts: () => request<TopProduct[]>('/api/reports/top-products'),
  getCategory: (id: string) => request<Category>(`/api/categories/${id}`)
};
