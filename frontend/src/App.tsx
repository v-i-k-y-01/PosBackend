import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react';
import {
  BarChart3,
  Boxes,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  ClipboardList,
  CreditCard,
  LayoutDashboard,
  LogOut,
  PackagePlus,
  Pencil,
  Plus,
  QrCode,
  ReceiptText,
  Search,
  ShoppingBag,
  Trash2,
  Users,
  WalletCards
} from 'lucide-react';
import { ApiError } from './api/client';
import { posApi } from './api/pos';
import type { CartLine, Category, DailyRevenue, PagedResult, Product, Sale, TopProduct } from './api/types';
import { useAuth } from './auth/AuthContext';
import { EmptyState } from './components/EmptyState';
import { Modal } from './components/Modal';
import { Toast } from './components/Toast';

type View = 'dashboard' | 'checkout' | 'inventory' | 'categories' | 'sales' | 'team';
type Notice = { message: string; type?: 'success' | 'error' } | null;

/**
 * Currency formatter configured for Indian Rupees (INR).
 */
const moneyFormatter = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  maximumFractionDigits: 2
});

/**
 * Formats a numeric price or revenue value into INR format.
 */
const formatMoney = (value: number): string => moneyFormatter.format(value);

/**
 * Formats an ISO date-time string into a human-readable representation.
 */
const formatDate = (dateString: string): string => {
  return new Intl.DateTimeFormat('en-IN', {
    day: 'numeric',
    month: 'short',
    hour: 'numeric',
    minute: '2-digit'
  }).format(new Date(dateString));
};

/**
 * Renders the initial login/registration view.
 */
function LoginScreen() {
  const { login } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError('');

    try {
      if (mode === 'register') {
        await posApi.register(email, password);
      }
      await login(email, password);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to continue.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="auth-page">
      <section className="auth-intro">
        <div className="brand-mark">
          <ShoppingBag size={25} />
        </div>
        <p className="eyebrow">SELL SMARTER</p>
        <h1>Everything your counter needs.</h1>
        <p>Run fast, delightful checkouts and keep every product, sale, and team member in one clear workspace.</p>
        <div className="intro-card">
          <CheckCircle2 size={18} /> Designed for the rhythm of real retail
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <div>
            <p className="eyebrow">WELCOME TO COUNTERLY</p>
            <h2>{mode === 'login' ? 'Welcome back' : 'Set up your store'}</h2>
            <p>{mode === 'login' ? 'Sign in to start selling.' : 'Create the first owner account for your store.'}</p>
          </div>

          <form onSubmit={handleSubmit}>
            <label>
              Email address
              <input
                type="email"
                placeholder="you@store.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </label>
            <label>
              Password
              <input
                type="password"
                placeholder="Minimum 8 characters"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
                minLength={8}
              />
            </label>
            {error && <p className="form-error">{error}</p>}
            <button className="primary-button full" disabled={busy}>
              {busy ? 'Please wait…' : mode === 'login' ? 'Sign in to Counterly' : 'Create owner account'}
            </button>
          </form>

          <button
            className="text-button"
            onClick={() => {
              setMode(mode === 'login' ? 'register' : 'login');
              setError('');
            }}
          >
            {mode === 'login' ? 'New store? Create your owner account' : 'Already registered? Sign in'}
          </button>
        </div>
      </section>
    </main>
  );
}

/**
 * Shell component hosting sidebar navigation, user headers, views, and action triggers.
 */
function AppShell() {
  const { session, logout } = useAuth();
  const isOwner = session?.role === 'Owner';
  const [view, setView] = useState<View>(isOwner ? 'dashboard' : 'checkout');

  // Application data states
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [sales, setSales] = useState<PagedResult<Sale> | null>(null);
  const [dailyRevenue, setDailyRevenue] = useState<DailyRevenue[]>([]);
  const [topProducts, setTopProducts] = useState<TopProduct[]>([]);

  // UI state controls
  const [loading, setLoading] = useState(false);
  const [notice, setNotice] = useState<Notice>(null);
  const [modal, setModal] = useState<'product' | 'category' | 'team' | null>(null);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [search, setSearch] = useState('');
  const [confirmation, setConfirmation] = useState<{
    title: string;
    message: string;
    confirmLabel: string;
    cancelLabel: string;
    onConfirm: () => Promise<void>;
    onCancel: () => void;
  } | null>(null);

  const showError = (exception: unknown) => {
    setNotice({
      message: exception instanceof Error ? exception.message : 'Something went wrong.',
      type: 'error'
    });
  };

  const loadCatalog = async () => {
    const [catalog, groups] = await Promise.all([
      posApi.products(),
      isOwner ? posApi.categories() : Promise.resolve([])
    ]);
    setProducts(catalog);
    setCategories(groups);
  };

  const loadViewData = async () => {
    setLoading(true);
    try {
      if (view === 'dashboard') {
        const [catalog, groups, revenue, top] = await Promise.all([
          posApi.products(),
          posApi.categories(),
          posApi.dailyRevenue(),
          posApi.topProducts()
        ]);
        setProducts(catalog);
        setCategories(groups);
        setDailyRevenue(revenue);
        setTopProducts(top);
      } else if (view === 'sales') {
        setSales(await posApi.sales());
      } else {
        await loadCatalog();
      }
    } catch (exception) {
      showError(exception);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadViewData();
  }, [view]);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 4000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  // Sidebar navigation settings
  const navItems: { id: View; label: string; icon: typeof LayoutDashboard; owner?: boolean }[] = [
    { id: 'dashboard', label: 'Overview', icon: LayoutDashboard, owner: true },
    { id: 'checkout', label: 'New sale', icon: WalletCards },
    { id: 'inventory', label: 'Products', icon: Boxes, owner: true },
    { id: 'categories', label: 'Categories', icon: ClipboardList, owner: true },
    { id: 'sales', label: 'Sales history', icon: ReceiptText },
    { id: 'team', label: 'Team', icon: Users, owner: true }
  ];

  const addToCart = (product: Product) => {
    if (product.stockQty < 1) return;
    setCart((current) => {
      const found = current.find((line) => line.id === product.id);
      if (found) {
        return current.map((line) =>
          line.id === product.id
            ? { ...line, quantity: Math.min(line.quantity + 1, product.stockQty) }
            : line
        );
      }
      return [...current, { ...product, quantity: 1 }];
    });
  };

  const changeQuantity = (id: string, quantity: number) => {
    setCart((current) =>
      quantity < 1
        ? current.filter((line) => line.id !== id)
        : current.map((line) =>
            line.id === id ? { ...line, quantity: Math.min(quantity, line.stockQty) } : line
          )
    );
  };

  const handleCheckout = async (paymentMethod: 'Cash' | 'Card' | 'Upi') => {
    try {
      const sale = await posApi.createSale(
        paymentMethod,
        cart.map(({ id, quantity }) => ({ productId: id, quantity }))
      );
      setCart([]);
      await loadCatalog();
      setNotice({ message: `Sale completed · ${formatMoney(sale.totalAmount)}` });
    } catch (exception) {
      showError(exception);
    }
  };

  const handleSaveProduct = async (productData: Omit<Product, 'id' | 'categoryName' | 'createdAt'>) => {
    try {
      await posApi.saveProduct(productData, editingProduct?.id);
      setModal(null);
      setEditingProduct(null);
      await loadCatalog();
      setNotice({ message: `Product ${editingProduct ? 'updated' : 'added'} successfully.` });
    } catch (exception) {
      showError(exception);
    }
  };

  const handleSaveCategory = async (name: string) => {
    try {
      await posApi.saveCategory(name, editingCategory?.id);
      setModal(null);
      setEditingCategory(null);
      await loadCatalog();
      setNotice({
        message: editingCategory ? 'Category updated successfully.' : 'Category created successfully.'
      });
    } catch (exception) {
      showError(exception);
    }
  };

  const requestDeleteCategory = (id: string) => {
    const category = categories.find((c) => c.id === id);
    if (!category) return;

    setConfirmation({
      title: 'Delete category?',
      message: `Are you sure you want to delete "${category.name}"?`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await posApi.deleteCategory(category.id);
          setConfirmation(null);
          await loadCatalog();
          setNotice({ message: 'Category deleted.' });
        } catch (exception) {
          showError(exception);
        }
      },
      onCancel: () => setConfirmation(null)
    });
  };

  const requestDeleteProduct = (product: Product) => {
    setConfirmation({
      title: 'Delete product?',
      message: `Are you sure you want to delete "${product.name}"?`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await posApi.deleteProduct(product.id);
          setConfirmation(null);
          await loadCatalog();
          setNotice({ message: 'Product deleted.' });
        } catch (exception) {
          showError(exception);
        }
      },
      onCancel: () => setConfirmation(null)
    });
  };

  // Filter catalog based on search input matches
  const filteredCatalog = products.filter((p) =>
    `${p.name} ${p.sku}`.toLowerCase().includes(search.toLowerCase())
  );

  const cartTotal = cart.reduce((sum, line) => sum + line.price * line.quantity, 0);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="brand-mark">
            <ShoppingBag size={20} />
          </div>
          <span>counterly</span>
        </div>

        <nav>
          {navItems
            .filter((item) => !item.owner || isOwner)
            .map((item) => {
              const Icon = item.icon;
              return (
                <button
                  key={item.id}
                  className={view === item.id ? 'nav-item active' : 'nav-item'}
                  onClick={() => setView(item.id)}
                >
                  <Icon size={19} />
                  {item.label}
                </button>
              );
            })}
        </nav>

        <div className="sidebar-user">
          <div className="avatar">{session?.email[0].toUpperCase()}</div>
          <div>
            <strong>{session?.email.split('@')[0]}</strong>
            <span>{session?.role}</span>
          </div>
          <button className="icon-button" title="Sign out" onClick={logout}>
            <LogOut size={18} />
          </button>
        </div>
      </aside>

      <main className="content">
        <header className="topbar">
          <div>
            <p className="eyebrow">{view === 'checkout' ? 'POINT OF SALE' : 'STORE OPERATIONS'}</p>
            <h1>
              {
                ({
                  dashboard: 'Good morning',
                  checkout: 'New sale',
                  inventory: 'Product catalog',
                  categories: 'Categories',
                  sales: 'Sales history',
                  team: 'Your team'
                } as Record<View, string>)[view]
              }
            </h1>
          </div>
          {view === 'dashboard' && isOwner && (
            <button
              className="primary-button"
              onClick={() => {
                setEditingProduct(null);
                setModal('product');
              }}
            >
              <PackagePlus size={18} /> Add product
            </button>
          )}
          {view === 'inventory' && (
            <button
              className="primary-button"
              onClick={() => {
                setEditingProduct(null);
                setModal('product');
              }}
            >
              <PackagePlus size={18} /> Add product
            </button>
          )}
          {view === 'categories' && (
            <button
              className="primary-button"
              onClick={() => {
                setEditingCategory(null);
                setModal('category');
              }}
            >
              <Plus size={18} /> New category
            </button>
          )}
          {view === 'team' && (
            <button className="primary-button" onClick={() => setModal('team')}>
              <Plus size={18} /> Invite teammate
            </button>
          )}
        </header>

        {loading ? (
          <div className="loading">Loading your workspace…</div>
        ) : (
          <>
            {view === 'dashboard' && (
              <Dashboard
                products={products}
                categories={categories}
                revenue={dailyRevenue}
                top={topProducts}
                onStartSale={() => setView('checkout')}
              />
            )}
            {view === 'checkout' && (
              <Checkout
                products={filteredCatalog}
                cart={cart}
                search={search}
                total={cartTotal}
                onSearch={setSearch}
                onAdd={addToCart}
                onQuantity={changeQuantity}
                onCheckout={handleCheckout}
              />
            )}
            {view === 'inventory' && (
              <Inventory
                products={filteredCatalog}
                search={search}
                onSearch={setSearch}
                onEdit={(product) => {
                  setEditingProduct(product);
                  setModal('product');
                }}
                onDelete={(id) => {
                  const product = products.find((item) => item.id === id);
                  if (product) {
                    requestDeleteProduct(product);
                  }
                }}
              />
            )}
            {view === 'categories' && (
              <Categories
                categories={categories}
                onEdit={(category) => {
                  setEditingCategory(category);
                  setModal('category');
                }}
                onDelete={requestDeleteCategory}
              />
            )}
            {view === 'sales' && (
              <SalesHistory
                sales={sales}
                onPage={async (page) => {
                  try {
                    setLoading(true);
                    setSales(await posApi.sales(page));
                  } catch (exception) {
                    showError(exception);
                  } finally {
                    setLoading(false);
                  }
                }}
              />
            )}
            {view === 'team' && <Team />}
          </>
        )}
      </main>

      {modal === 'product' && (
        <ProductModal
          categories={categories}
          product={editingProduct}
          onSave={handleSaveProduct}
          onClose={() => setModal(null)}
        />
      )}

      {modal === 'category' && (
        <SimpleModal
          title={editingCategory ? 'Edit category' : 'New category'}
          label="Category name"
          action={editingCategory ? 'Save category' : 'Create category'}
          initialValue={editingCategory?.name}
          onSubmit={handleSaveCategory}
          onClose={() => {
            setModal(null);
            setEditingCategory(null);
          }}
        />
      )}

      {modal === 'team' && (
        <TeamModal
          onClose={() => setModal(null)}
          onSuccess={(message) => {
            setModal(null);
            setNotice({ message });
          }}
          onError={showError}
        />
      )}

      {confirmation && (
        <div className="toast confirm">
          <strong>{confirmation.title}</strong>
          <p>{confirmation.message}</p>
          <div className="toast-actions">
            <button className="text-button" onClick={confirmation.onCancel}>
              {confirmation.cancelLabel}
            </button>
            <button
              className="danger"
              onClick={async () => {
                await confirmation.onConfirm();
              }}
            >
              {confirmation.confirmLabel}
            </button>
          </div>
        </div>
      )}

      {notice && <Toast {...notice} />}
    </div>
  );
}

interface DashboardProps {
  products: Product[];
  categories: Category[];
  revenue: DailyRevenue[];
  top: TopProduct[];
  onStartSale: () => void;
}

/**
 * Analytics Dashboard component rendering store KPIs, charts, and best sellers list.
 */
function Dashboard({ products, categories, revenue, top, onStartSale }: DashboardProps) {
  const todayDateString = new Date().toISOString().slice(0, 10);
  const todayRevenueRecord = revenue.find((item) => item.date.slice(0, 10) === todayDateString);
  const totalRevenue = revenue.reduce((sum, item) => sum + item.totalRevenue, 0);
  const lowStockCount = products.filter((item) => item.stockQty < 6).length;

  return (
    <>
      <section className="stats">
        <Stat
          icon={<WalletCards />}
          label="Today’s sales"
          value={formatMoney(todayRevenueRecord?.totalRevenue ?? 0)}
          detail={`${todayRevenueRecord?.saleCount ?? 0} transactions`}
          tone="purple"
        />
        <Stat
          icon={<BarChart3 />}
          label="Total revenue"
          value={formatMoney(totalRevenue)}
          detail="All recorded sales"
          tone="orange"
        />
        <Stat
          icon={<Boxes />}
          label="Products in stock"
          value={String(products.length)}
          detail={`${lowStockCount} running low`}
          tone="blue"
        />
        <Stat
          icon={<ClipboardList />}
          label="Categories"
          value={String(categories.length)}
          detail="Organize your catalog"
          tone="green"
        />
      </section>

      <section className="dashboard-grid">
        <article className="panel revenue-panel">
          <div className="panel-heading">
            <div>
              <h2>Revenue pulse</h2>
              <p>Daily sales activity</p>
            </div>
            <span className="pill">Live data</span>
          </div>

          {revenue.length ? (
            <div className="chart">
              {revenue.slice(-10).map((item) => {
                const maxRevenue = Math.max(...revenue.map((row) => row.totalRevenue));
                const barHeight = Math.max(12, (item.totalRevenue / maxRevenue) * 155);

                return (
                  <div className="bar-group" key={item.date}>
                    <div
                      className="bar"
                      style={{ height: `${barHeight}px` }}
                      title={formatMoney(item.totalRevenue)}
                    />
                    <small>
                      {new Date(item.date).toLocaleDateString('en-IN', {
                        day: 'numeric',
                        month: 'short'
                      })}
                    </small>
                  </div>
                );
              })}
            </div>
          ) : (
            <EmptyState
              icon={<BarChart3 />}
              title="No revenue yet"
              detail="Completed sales will appear here."
            />
          )}
        </article>

        <article className="panel">
          <div className="panel-heading">
            <div>
              <h2>Best sellers</h2>
              <p>Your top-performing products</p>
            </div>
          </div>

          {top.length ? (
            <div className="rank-list">
              {top.slice(0, 5).map((item, index) => (
                <div className="rank-row" key={item.productId}>
                  <span className="rank">0{index + 1}</span>
                  <div>
                    <strong>{item.productName}</strong>
                    <small>{item.quantitySold} units sold</small>
                  </div>
                  <b>{formatMoney(item.revenue)}</b>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              icon={<ShoppingBag />}
              title="Your counter is ready"
              detail="Make your first sale to see best sellers."
            />
          )}
        </article>
      </section>

      <section className="callout">
        <div>
          <span className="callout-icon">
            <CreditCard size={22} />
          </span>
          <div>
            <h2>Ready to make a sale?</h2>
            <p>Open a fresh checkout and keep your line moving.</p>
          </div>
        </div>
        <button className="light-button" onClick={onStartSale}>
          Open checkout <ChevronRight size={17} />
        </button>
      </section>
    </>
  );
}

interface StatProps {
  icon: ReactNode;
  label: string;
  value: string;
  detail: string;
  tone: string;
}

/**
 * Metric card displaying KPI summary details.
 */
function Stat({ icon, label, value, detail, tone }: StatProps) {
  return (
    <article className="stat">
      <span className={`stat-icon ${tone}`}>{icon}</span>
      <p>{label}</p>
      <h2>{value}</h2>
      <small>{detail}</small>
    </article>
  );
}

interface CheckoutProps {
  products: Product[];
  cart: CartLine[];
  search: string;
  total: number;
  onSearch: (value: string) => void;
  onAdd: (product: Product) => void;
  onQuantity: (id: string, quantity: number) => void;
  onCheckout: (method: 'Cash' | 'Card' | 'Upi') => Promise<void>;
}

/**
 * Checkout terminal interface for searching catalog, building order carts, and paying.
 */
function Checkout({
  products,
  cart,
  search,
  total,
  onSearch,
  onAdd,
  onQuantity,
  onCheckout
}: CheckoutProps) {
  const [paying, setPaying] = useState(false);

  const handlePay = async (method: 'Cash' | 'Card' | 'Upi') => {
    if (!cart.length) return;
    setPaying(true);
    await onCheckout(method);
    setPaying(false);
  };

  return (
    <div className="checkout-layout">
      <section>
        <div className="search-field">
          <Search size={19} />
          <input
            value={search}
            onChange={(event) => onSearch(event.target.value)}
            placeholder="Search products or SKU…"
          />
        </div>

        <div className="product-grid">
          {products.map((product) => (
            <button
              className="product-card"
              key={product.id}
              disabled={product.stockQty < 1}
              onClick={() => onAdd(product)}
            >
              <div className="product-image">
                <ShoppingBag size={25} />
              </div>
              <div>
                <span>{product.categoryName ?? 'Uncategorized'}</span>
                <h3>{product.name}</h3>
                <p>{formatMoney(product.price)}</p>
              </div>
              <small className={product.stockQty < 6 ? 'low-stock' : ''}>
                {product.stockQty ? `${product.stockQty} left` : 'Out of stock'}
              </small>
            </button>
          ))}
        </div>

        {!products.length && (
          <EmptyState
            icon={<Search />}
            title="No matching products"
            detail="Try a different search or add inventory first."
          />
        )}
      </section>

      <aside className="cart">
        <div className="cart-heading">
          <div>
            <h2>Current order</h2>
            <p>
              {cart.length
                ? `${cart.length} item${cart.length === 1 ? '' : 's'} in cart`
                : 'Add products to get started'}
            </p>
          </div>
          <ReceiptText size={21} />
        </div>

        <div className="cart-lines">
          {cart.length ? (
            cart.map((line) => (
              <div className="cart-line" key={line.id}>
                <div className="line-product">
                  <strong>{line.name}</strong>
                  <small>{formatMoney(line.price)} each</small>
                </div>
                <div className="quantity">
                  <button onClick={() => onQuantity(line.id, line.quantity - 1)}>−</button>
                  <span>{line.quantity}</span>
                  <button onClick={() => onQuantity(line.id, line.quantity + 1)}>+</button>
                </div>
                <b>{formatMoney(line.price * line.quantity)}</b>
              </div>
            ))
          ) : (
            <EmptyState
              icon={<ShoppingBag />}
              title="Your cart is empty"
              detail="Choose products from the catalog."
            />
          )}
        </div>

        <div className="cart-total">
          <span>Total</span>
          <strong>{formatMoney(total)}</strong>
        </div>

        <div className="payment-buttons">
          <button disabled={!cart.length || paying} onClick={() => void handlePay('Cash')}>
            <WalletCards size={18} /> Cash
          </button>
          <button disabled={!cart.length || paying} onClick={() => void handlePay('Card')}>
            <CreditCard size={18} /> Card
          </button>
          <button disabled={!cart.length || paying} onClick={() => void handlePay('Upi')}>
            <QrCode size={18} /> UPI
          </button>
        </div>
      </aside>
    </div>
  );
}

interface InventoryProps {
  products: Product[];
  search: string;
  onSearch: (value: string) => void;
  onEdit: (product: Product) => void;
  onDelete: (id: string) => void;
}

/**
 * Inventory catalog management table.
 */
function Inventory({ products, search, onSearch, onEdit, onDelete }: InventoryProps) {
  return (
    <section className="panel table-panel">
      <div className="table-toolbar">
        <div className="search-field compact">
          <Search size={18} />
          <input
            value={search}
            onChange={(event) => onSearch(event.target.value)}
            placeholder="Search products…"
          />
        </div>
        <span>{products.length} products</span>
      </div>

      {products.length ? (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Product</th>
                <th>SKU</th>
                <th>Category</th>
                <th>Price</th>
                <th>Stock</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id}>
                  <td>
                    <strong>{product.name}</strong>
                  </td>
                  <td>{product.sku}</td>
                  <td>{product.categoryName ?? '—'}</td>
                  <td>{formatMoney(product.price)}</td>
                  <td>
                    <span className={`stock-badge ${product.stockQty < 6 ? 'warning' : ''}`}>
                      {product.stockQty} units
                    </span>
                  </td>
                  <td className="actions">
                    <button onClick={() => onEdit(product)}>Edit</button>
                    <button className="danger" onClick={() => void onDelete(product.id)}>
                      <Trash2 size={16} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <EmptyState
          icon={<Boxes />}
          title="No products yet"
          detail="Add your first product to start selling."
        />
      )}
    </section>
  );
}

interface CategoriesProps {
  categories: Category[];
  onEdit: (category: Category) => void;
  onDelete: (id: string) => void;
}

/**
 * Catalog product categorization cards layout.
 */
function Categories({ categories, onEdit, onDelete }: CategoriesProps) {
  return (
    <section className="category-grid">
      {categories.map((category) => (
        <article className="category-card" key={category.id}>
          <div className="category-icon">
            <ClipboardList size={20} />
          </div>
          <strong>{category.name}</strong>
          <div className="category-actions">
            <button className="icon-button" onClick={() => onEdit(category)} title="Edit category">
              <Pencil size={17} />
            </button>
            <button
              className="icon-button danger"
              onClick={() => void onDelete(category.id)}
              title="Delete category"
            >
              <Trash2 size={17} />
            </button>
          </div>
        </article>
      ))}

      {!categories.length && (
        <EmptyState
          icon={<ClipboardList />}
          title="No categories yet"
          detail="Create categories to keep your catalog tidy."
        />
      )}
    </section>
  );
}

interface SalesHistoryProps {
  sales: PagedResult<Sale> | null;
  onPage: (page: number) => Promise<void>;
}

/**
 * Historical transaction receipt logs layout.
 */
function SalesHistory({ sales, onPage }: SalesHistoryProps) {
  if (!sales?.items.length) {
    return (
      <EmptyState
        icon={<ReceiptText />}
        title="No sales recorded"
        detail="Completed checkouts will show up here."
      />
    );
  }

  return (
    <section className="panel table-panel">
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Receipt</th>
              <th>Cashier</th>
              <th>Payment</th>
              <th>Items</th>
              <th>Created</th>
              <th>Total</th>
            </tr>
          </thead>
          <tbody>
            {sales.items.map((sale) => {
              const itemsCount = sale.items.reduce((sum, item) => sum + item.quantity, 0);

              return (
                <tr key={sale.id}>
                  <td>
                    <strong>#{sale.id.slice(0, 8).toUpperCase()}</strong>
                  </td>
                  <td>{sale.cashierEmail}</td>
                  <td>
                    <span className="pill">{sale.paymentMethod}</span>
                  </td>
                  <td>{itemsCount}</td>
                  <td>{formatDate(sale.createdAt)}</td>
                  <td>
                    <strong>{formatMoney(sale.totalAmount)}</strong>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="pagination">
        <span>
          Page {sales.page} of {Math.max(1, Math.ceil(sales.totalCount / sales.pageSize))}
        </span>
        <div>
          <button
            className="icon-button"
            disabled={sales.page <= 1}
            onClick={() => void onPage(sales.page - 1)}
          >
            <ChevronLeft size={18} />
          </button>
          <button
            className="icon-button"
            disabled={sales.page * sales.pageSize >= sales.totalCount}
            onClick={() => void onPage(sales.page + 1)}
          >
            <ChevronRight size={18} />
          </button>
        </div>
      </div>
    </section>
  );
}

/**
 * Component explaining team credentials and system roles.
 */
function Team() {
  return (
    <section className="team-card">
      <div className="team-illustration">
        <Users size={34} />
      </div>
      <div>
        <p className="eyebrow">STAFF ACCESS</p>
        <h2>Build a confident counter team</h2>
        <p>
          Invite cashiers from the button above. They can make sales and view their own sales history, while
          your catalog and reports remain protected.
        </p>
      </div>
    </section>
  );
}

interface ProductModalProps {
  categories: Category[];
  product: Product | null;
  onSave: (product: Omit<Product, 'id' | 'categoryName' | 'createdAt'>) => Promise<void>;
  onClose: () => void;
}

/**
 * Modal dialog for product creation and modification details.
 */
function ProductModal({ categories, product, onSave, onClose }: ProductModalProps) {
  const [name, setName] = useState(product?.name ?? '');
  const [sku, setSku] = useState(product?.sku ?? '');
  const [price, setPrice] = useState(String(product?.price ?? ''));
  const [stockQty, setStockQty] = useState(String(product?.stockQty ?? 0));
  const [categoryId, setCategoryId] = useState(product?.categoryId ?? '');
  const [busy, setBusy] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    await onSave({
      name,
      sku,
      price: Number(price),
      stockQty: Number(stockQty),
      categoryId: categoryId || null
    });
    setBusy(false);
  };

  return (
    <Modal title={product ? 'Edit product' : 'Add product'} onClose={onClose}>
      <form className="modal-form" onSubmit={handleSubmit}>
        <label>
          Product name
          <input value={name} onChange={(event) => setName(event.target.value)} required />
        </label>
        <div className="form-row">
          <label>
            SKU
            <input value={sku} onChange={(event) => setSku(event.target.value)} required />
          </label>
          <label>
            Category
            <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
              <option value="">Uncategorized</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div className="form-row">
          <label>
            Price
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={price}
              onChange={(event) => setPrice(event.target.value)}
              required
            />
          </label>
          <label>
            Stock quantity
            <input
              type="number"
              min="0"
              value={stockQty}
              onChange={(event) => setStockQty(event.target.value)}
              required
            />
          </label>
        </div>
        <button className="primary-button full" disabled={busy}>
          {busy ? 'Saving…' : 'Save product'}
        </button>
      </form>
    </Modal>
  );
}

interface SimpleModalProps {
  title: string;
  label: string;
  action: string;
  initialValue?: string;
  onSubmit: (value: string) => Promise<void>;
  onClose: () => void;
}

/**
 * A reusable modal with a single text field (e.g. creating categories).
 */
function SimpleModal({ title, label, action, initialValue, onSubmit, onClose }: SimpleModalProps) {
  const [value, setValue] = useState(initialValue ?? '');

  useEffect(() => {
    setValue(initialValue ?? '');
  }, [initialValue]);

  return (
    <Modal title={title} onClose={onClose}>
      <form
        className="modal-form"
        onSubmit={async (event) => {
          event.preventDefault();
          await onSubmit(value);
        }}
      >
        <label>
          {label}
          <input value={value} onChange={(event) => setValue(event.target.value)} required autoFocus />
        </label>
        <button className="primary-button full">{action}</button>
      </form>
    </Modal>
  );
}

interface TeamModalProps {
  onClose: () => void;
  onSuccess: (message: string) => void;
  onError: (exception: unknown) => void;
}

/**
 * Modal form used to register/invite cashier or owner accounts.
 */
function TeamModal({ onClose, onSuccess, onError }: TeamModalProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<'Cashier' | 'Owner'>('Cashier');
  const [busy, setBusy] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    try {
      await posApi.createUser(email, password, role === 'Owner');
      onSuccess(`${role} account created successfully.`);
    } catch (exception) {
      onError(exception);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal title="Invite teammate" onClose={onClose}>
      <form className="modal-form" onSubmit={handleSubmit}>
        <label>
          Email address
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </label>
        <label>
          Access level
          <select
            value={role}
            onChange={(event) => setRole(event.target.value as 'Cashier' | 'Owner')}
          >
            <option value="Cashier">Cashier — checkout and own sales</option>
            <option value="Owner">Owner — full store access</option>
          </select>
        </label>
        <label>
          Temporary password
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            minLength={8}
          />
        </label>
        <button className="primary-button full" disabled={busy}>
          {busy ? 'Creating…' : `Create ${role.toLowerCase()}`}
        </button>
      </form>
    </Modal>
  );
}

/**
 * Main application router selecting between login screen and app shell context.
 */
export default function App() {
  const { session } = useAuth();
  return session ? <AppShell /> : <LoginScreen />;
}
