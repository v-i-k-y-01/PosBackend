import { request } from './client';
import type { Category, DailyRevenue, PagedResult, Product, Sale, TokenResponse, TopProduct, User } from './types';

/**
 * Builds an URL query string from a key-value record, skipping empty and undefined parameters.
 * @param values - Record of parameters to parse.
 * @returns Query string prefixed with '?' or an empty string.
 */
const buildQueryString = (values: Record<string, string | number | undefined>): string => {
  const searchParams = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      searchParams.set(key, String(value));
    }
  });
  return searchParams.toString() ? `?${searchParams}` : '';
};

/**
 * Point-of-Sale API service object wrapping all backend HTTP interactions.
 */
export const posApi = {
  /**
   * Authenticates user credentials.
   */
  login: (email: string, password: string) =>
    request<TokenResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    }),

  /**
   * Registers a new owner account (initial step).
   */
  register: (email: string, password: string) =>
    request<User>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    }),

  /**
   * Creates an additional Cashier or Owner account.
   */
  createUser: (email: string, password: string, owner = false) =>
    request<User>(`/api/users${owner ? '/owners' : ''}`, {
      method: 'POST',
      body: JSON.stringify({ email, password })
    }),

  /**
   * Retrieves all categories.
   */
  categories: () =>
    request<Category[]>('/api/categories'),

  /**
   * Creates or updates a category depending on presence of an ID.
   */
  saveCategory: (name: string, id?: string) =>
    request<Category>(id ? `/api/categories/${id}` : '/api/categories', {
      method: id ? 'PUT' : 'POST',
      body: JSON.stringify({ name })
    }),

  /**
   * Deletes a category.
   */
  deleteCategory: (id: string) =>
    request<void>(`/api/categories/${id}`, {
      method: 'DELETE'
    }),

  /**
   * Fetches a filtered list of products.
   */
  products: (filters: { categoryId?: string; search?: string } = {}) =>
    request<Product[]>(`/api/products${buildQueryString(filters)}`),

  /**
   * Creates or updates a product depending on presence of an ID.
   */
  saveProduct: (product: Omit<Product, 'id' | 'categoryName' | 'createdAt'>, id?: string) =>
    request<Product>(id ? `/api/products/${id}` : '/api/products', {
      method: id ? 'PUT' : 'POST',
      body: JSON.stringify(product)
    }),

  /**
   * Deletes a product.
   */
  deleteProduct: (id: string) =>
    request<void>(`/api/products/${id}`, {
      method: 'DELETE'
    }),

  /**
   * Retrieves a single product's details by its ID.
   */
  getProduct: (id: string) =>
    request<Product>(`/api/products/${id}`),

  /**
   * Retrieves a product by its exact barcode or SKU.
   */
  getProductByBarcode: (barcode: string) =>
    request<Product>(`/api/products/barcode/${encodeURIComponent(barcode)}`),

  /**
   * Generates a guaranteed unique 12-digit standard barcode from the backend.
   */
  generateBarcode: () =>
    request<{ barcode: string }>('/api/products/generate-barcode'),

  /**
   * Creates a new sales transaction. Maps cash / card / UPI methods to API enumerations.
   */
  createSale: (paymentMethod: 'Cash' | 'Card' | 'Upi', items: { productId: string; quantity: number }[]) => {
    const paymentMethodMap: Record<'Cash' | 'Card' | 'Upi', number> = {
      Cash: 0,
      Card: 1,
      Upi: 2,
    };

    return request<Sale>('/api/sales', {
      method: 'POST',
      body: JSON.stringify({
        paymentMethod: paymentMethodMap[paymentMethod],
        items
      })
    });
  },

  /**
   * Retrieves details of a specific sale.
   */
  sale: (id: string) =>
    request<Sale>(`/api/sales/${id}`),

  /**
   * Fetches paginated sales history.
   */
  sales: (page = 1) =>
    request<PagedResult<Sale>>(`/api/sales${buildQueryString({ page, pageSize: 12 })}`),

  /**
   * Retrieves daily revenue report data.
   */
  dailyRevenue: () =>
    request<DailyRevenue[]>('/api/reports/daily-revenue'),

  /**
   * Retrieves top selling products report.
   */
  topProducts: () =>
    request<TopProduct[]>('/api/reports/top-products'),

  /**
   * Retrieves a single category's details.
   */
  getCategory: (id: string) =>
    request<Category>(`/api/categories/${id}`)
};
