import { clsx } from 'clsx'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import {
  BarChart3,
  Boxes,
  Building2,
  Check,
  ClipboardList,
  DollarSign,
  LayoutDashboard,
  Lock,
  LogOut,
  Moon,
  Package,
  Plus,
  ReceiptText,
  Search,
  ShieldCheck,
  ShoppingCart,
  Sun,
  Users,
  X,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  authenticate,
  createSalesOrder,
  fallbackWorkspace,
  loadWorkspace,
  markInvoicePaid,
  updateCustomerStatus,
} from './api'
import {
  categoryRevenueSeed,
  permissions,
  profiles,
  revenueSeed,
  riskSeed,
} from './data'
import type {
  Customer,
  DashboardView,
  Invoice,
  InventoryItem,
  Order,
  Product,
  ThemeMode,
  UserProfile,
  CustomerStatus,
} from './types'
import './index.css'

const navigation: Array<{ id: DashboardView; label: string; icon: LucideIcon }> = [
  { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { id: 'customers', label: 'Customers', icon: Building2 },
  { id: 'products', label: 'Products', icon: Package },
  { id: 'orders', label: 'Orders', icon: ShoppingCart },
  { id: 'inventory', label: 'Inventory', icon: Boxes },
  { id: 'finance', label: 'Finance', icon: ReceiptText },
  { id: 'permissions', label: 'Permissions', icon: ShieldCheck },
]

const riskColors = ['#2563eb', '#16a34a', '#f59e0b', '#db2777']

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [theme, setTheme] = useState<ThemeMode>('light')
  const [activeView, setActiveView] = useState<DashboardView>('dashboard')
  const [profile, setProfile] = useState<UserProfile>(profiles[0])
  const [searchTerm, setSearchTerm] = useState('')
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [customers, setCustomers] = useState<Customer[]>(fallbackWorkspace.customers)
  const [products, setProducts] = useState<Product[]>(fallbackWorkspace.products)
  const [inventory, setInventory] = useState<InventoryItem[]>(fallbackWorkspace.inventory)
  const [orders, setOrders] = useState<Order[]>(fallbackWorkspace.orders)
  const [invoices, setInvoices] = useState<Invoice[]>(fallbackWorkspace.invoices)
  const [isOrderPanelOpen, setIsOrderPanelOpen] = useState(false)

  const canManage = profile.role !== 'Analyst'
  const canAdmin = profile.role === 'Administrator'

  const filtered = useMemo(() => {
    const term = searchTerm.trim().toLowerCase()
    if (!term) {
      return { customers, products, inventory, orders, invoices }
    }

    return {
      customers: customers.filter((item) =>
        [item.name, item.segment, item.taxId, item.status].some((value) =>
          value.toLowerCase().includes(term),
        ),
      ),
      products: products.filter((item) =>
        [item.sku, item.name, item.category].some((value) =>
          value.toLowerCase().includes(term),
        ),
      ),
      inventory: inventory.filter((item) =>
        [item.sku, item.productName, item.warehouse].some((value) =>
          value.toLowerCase().includes(term),
        ),
      ),
      orders: orders.filter((item) =>
        [item.number, item.customerName, item.status].some((value) =>
          value.toLowerCase().includes(term),
        ),
      ),
      invoices: invoices.filter((item) =>
        [item.number, item.customerName, item.status].some((value) =>
          value.toLowerCase().includes(term),
        ),
      ),
    }
  }, [customers, inventory, invoices, orders, products, searchTerm])

  const dashboard = useMemo(() => {
    const revenue = invoices
      .filter((invoice) => invoice.status === 'Open' || invoice.status === 'Paid')
      .reduce((total, invoice) => total + invoice.amount, 0)
    const receivables = invoices
      .filter((invoice) => invoice.status === 'Open' || invoice.status === 'Overdue')
      .reduce((total, invoice) => total + invoice.amount, 0)
    const lowStock = inventory.filter((item) => item.available <= item.reorderPoint).length
    const openOrders = orders.filter((item) => item.status === 'Confirmed').length

    return {
      kpis: [
        { label: 'Revenue', value: formatCompactCurrency(revenue), delta: '+14.2%', tone: 'green' },
        { label: 'Receivables', value: formatCompactCurrency(receivables), delta: '-3.8%', tone: 'amber' },
        { label: 'Open orders', value: openOrders.toString(), delta: '+5', tone: 'blue' },
        { label: 'Low stock', value: lowStock.toString(), delta: lowStock > 0 ? '+1' : '0', tone: 'rose' },
      ],
    }
  }, [inventory, invoices, orders])

  function applyWorkspace(workspace: typeof fallbackWorkspace) {
    setCustomers(workspace.customers)
    setProducts(workspace.products)
    setInventory(workspace.inventory)
    setOrders(workspace.orders)
    setInvoices(workspace.invoices)
  }

  async function signIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsAuthenticated(true)

    const session = await authenticate(profile.email, 'enterprise-demo')
    if (!session) {
      setAccessToken(null)
      applyWorkspace(fallbackWorkspace)
      return
    }

    setAccessToken(session.accessToken)
    setProfile({
      email: session.user.email,
      name: session.user.name,
      role: session.user.role,
    })

    const workspace = await loadWorkspace(session.accessToken)
    if (workspace) {
      applyWorkspace(workspace)
    }
  }

  async function refreshFromApi(token: string) {
    const workspace = await loadWorkspace(token)
    if (workspace) {
      applyWorkspace(workspace)
    }
  }

  async function createOrder(customerId: string, productId: string, quantity: number) {
    const customer = customers.find((item) => item.id === customerId)
    const product = products.find((item) => item.id === productId)

    if (!customer || !product || quantity < 1) {
      return
    }

    if (accessToken) {
      const created = await createSalesOrder(accessToken, customerId, productId, quantity)
      if (created) {
        await refreshFromApi(accessToken)
        setActiveView('orders')
        return
      }
    }

    const sequence = orders.length + 1
    const number = `SO-2026-${sequence.toString().padStart(4, '0')}`
    const subtotal = product.unitPrice * quantity
    const tax = Math.round(subtotal * 0.08 * 100) / 100
    const total = subtotal + tax
    const dueDate = '2026-08-15'

    const order: Order = {
      id: crypto.randomUUID(),
      number,
      customerId: customer.id,
      customerName: customer.name,
      status: 'Confirmed',
      requiredDate: dueDate,
      total,
      lines: [
        {
          productId: product.id,
          sku: product.sku,
          productName: product.name,
          quantity,
          unitPrice: product.unitPrice,
          lineTotal: subtotal,
        },
      ],
    }

    const invoice: Invoice = {
      id: crypto.randomUUID(),
      number: `INV-2026-${sequence.toString().padStart(4, '0')}`,
      orderNumber: number,
      customerName: customer.name,
      status: 'Open',
      dueDate,
      amount: total,
    }

    setOrders((current) => [order, ...current])
    setInvoices((current) => [invoice, ...current])
    setInventory((current) =>
      current.map((item) =>
        item.productId === productId
          ? { ...item, reserved: item.reserved + quantity, available: item.available - quantity }
          : item,
      ),
    )
    setActiveView('orders')
  }

  async function changeCustomerStatus(id: string, status: CustomerStatus) {
    if (accessToken) {
      const updated = await updateCustomerStatus(accessToken, id, status)
      if (updated) {
        setCustomers((current) =>
          current.map((customer) => (customer.id === id ? updated : customer)),
        )
        return
      }
    }

    setCustomers((current) =>
      current.map((customer) =>
        customer.id === id ? { ...customer, status } : customer,
      ),
    )
  }

  async function settleInvoice(id: string) {
    if (accessToken) {
      const updated = await markInvoicePaid(accessToken, id)
      if (updated) {
        setInvoices((current) =>
          current.map((invoice) => (invoice.id === id ? updated : invoice)),
        )
        return
      }
    }

    setInvoices((current) =>
      current.map((invoice) =>
        invoice.id === id ? { ...invoice, status: 'Paid' } : invoice,
      ),
    )
  }

  if (!isAuthenticated) {
    return (
      <main className="login-shell" data-theme={theme}>
        <section className="login-panel" aria-label="Sign in">
          <div className="brand-block">
            <div className="brand-mark">
              <Boxes size={28} aria-hidden="true" />
            </div>
            <div>
              <p className="eyebrow">Mini ERP Platform</p>
              <h1>Operations Workspace</h1>
            </div>
          </div>
          <form
            className="login-form"
            onSubmit={(event) => void signIn(event)}
          >
            <label>
              Account
              <select
                value={profile.email}
                onChange={(event) => {
                  const selected = profiles.find((item) => item.email === event.target.value)
                  if (selected) {
                    setProfile(selected)
                  }
                }}
              >
                {profiles.map((item) => (
                  <option key={item.email} value={item.email}>
                    {item.name} - {item.role}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Password
              <input type="password" value="enterprise-demo" readOnly />
            </label>
            <button className="primary-button" type="submit">
              <Lock size={18} aria-hidden="true" />
              Sign in
            </button>
          </form>
          <div className="login-footer">
            <ThemeToggle theme={theme} onThemeChange={setTheme} />
            <span>{profile.role}</span>
          </div>
        </section>
      </main>
    )
  }

  return (
    <div className="app-shell" data-theme={theme}>
      <Sidebar
        activeView={activeView}
        profile={profile}
        onNavigate={setActiveView}
      />
      <main className="workspace">
        <Topbar
          activeView={activeView}
          canManage={canManage}
          profile={profile}
          searchTerm={searchTerm}
          theme={theme}
          onCreateOrder={() => setIsOrderPanelOpen(true)}
          onLogout={() => setIsAuthenticated(false)}
          onProfileChange={setProfile}
          onSearch={setSearchTerm}
          onThemeChange={setTheme}
        />
        <section className="workspace-content" aria-label="Mini ERP workspace">
          {activeView === 'dashboard' && (
            <DashboardView
              kpis={dashboard.kpis}
              inventory={inventory}
              orders={orders}
            />
          )}
          {activeView === 'customers' && (
            <CustomersView
              canManage={canManage}
              customers={filtered.customers}
              onStatusChange={(id, status) => void changeCustomerStatus(id, status)}
            />
          )}
          {activeView === 'products' && <ProductsView products={filtered.products} />}
          {activeView === 'orders' && <OrdersView orders={filtered.orders} />}
          {activeView === 'inventory' && <InventoryView inventory={filtered.inventory} />}
          {activeView === 'finance' && (
            <FinanceView
              canAdmin={canAdmin}
              invoices={filtered.invoices}
              onMarkPaid={(id) => void settleInvoice(id)}
            />
          )}
          {activeView === 'permissions' && <PermissionsView activeRole={profile.role} />}
        </section>
      </main>
      {isOrderPanelOpen && (
        <OrderDialog
          customers={customers.filter((customer) => customer.status === 'Active')}
          products={products}
          onClose={() => setIsOrderPanelOpen(false)}
          onSubmit={(customerId, productId, quantity) => {
            void createOrder(customerId, productId, quantity)
            setIsOrderPanelOpen(false)
          }}
        />
      )}
    </div>
  )
}

function Sidebar({
  activeView,
  profile,
  onNavigate,
}: {
  activeView: DashboardView
  profile: UserProfile
  onNavigate: (view: DashboardView) => void
}) {
  return (
    <aside className="sidebar" aria-label="Workspace navigation">
      <div className="brand-row">
        <div className="brand-mark compact">
          <Boxes size={22} aria-hidden="true" />
        </div>
        <div>
          <strong>Mini ERP</strong>
          <span>Enterprise Lab</span>
        </div>
      </div>
      <nav className="nav-list" aria-label="Primary navigation">
        {navigation.map((item) => {
          const Icon = item.icon
          return (
            <button
              className={clsx('nav-item', activeView === item.id && 'active')}
              key={item.id}
              type="button"
              onClick={() => onNavigate(item.id)}
            >
              <Icon size={18} aria-hidden="true" />
              <span>{item.label}</span>
            </button>
          )
        })}
      </nav>
      <div className="profile-chip">
        <div className="avatar" aria-hidden="true">
          {initials(profile.name)}
        </div>
        <div>
          <strong>{profile.name}</strong>
          <span>{profile.role}</span>
        </div>
      </div>
    </aside>
  )
}

function Topbar({
  activeView,
  canManage,
  profile,
  searchTerm,
  theme,
  onCreateOrder,
  onLogout,
  onProfileChange,
  onSearch,
  onThemeChange,
}: {
  activeView: DashboardView
  canManage: boolean
  profile: UserProfile
  searchTerm: string
  theme: ThemeMode
  onCreateOrder: () => void
  onLogout: () => void
  onProfileChange: (profile: UserProfile) => void
  onSearch: (value: string) => void
  onThemeChange: (theme: ThemeMode) => void
}) {
  const label = navigation.find((item) => item.id === activeView)?.label ?? 'Dashboard'

  return (
    <header className="topbar">
      <div>
        <p className="eyebrow">Q3 operating model</p>
        <h2>{label}</h2>
      </div>
      <div className="topbar-actions">
        <label className="search-box">
          <Search size={18} aria-hidden="true" />
          <input
            aria-label="Search workspace"
            placeholder="Search records"
            value={searchTerm}
            onChange={(event) => onSearch(event.target.value)}
          />
        </label>
        <select
          aria-label="Active role"
          className="role-select"
          value={profile.email}
          onChange={(event) => {
            const selected = profiles.find((item) => item.email === event.target.value)
            if (selected) {
              onProfileChange(selected)
            }
          }}
        >
          {profiles.map((item) => (
            <option key={item.email} value={item.email}>
              {item.role}
            </option>
          ))}
        </select>
        <button
          className="primary-button compact"
          disabled={!canManage}
          type="button"
          onClick={onCreateOrder}
        >
          <Plus size={17} aria-hidden="true" />
          New order
        </button>
        <ThemeToggle theme={theme} onThemeChange={onThemeChange} />
        <IconButton label="Log out" onClick={onLogout}>
          <LogOut size={18} aria-hidden="true" />
        </IconButton>
      </div>
    </header>
  )
}

function DashboardView({
  kpis,
  inventory,
  orders,
}: {
  kpis: Array<{ label: string; value: string; delta: string; tone: string }>
  inventory: InventoryItem[]
  orders: Order[]
}) {
  return (
    <>
      <div className="kpi-grid">
        {kpis.map((item) => (
          <article className={clsx('kpi-card', item.tone)} key={item.label}>
            <span>{item.label}</span>
            <strong>{item.value}</strong>
            <em>{item.delta}</em>
          </article>
        ))}
      </div>
      <section className="panel wide" aria-label="Revenue analytics">
        <PanelTitle icon={<BarChart3 size={18} aria-hidden="true" />} title="Revenue analytics" meta="Invoices and receivables" />
        <div className="chart-frame">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={revenueSeed} margin={{ left: 0, right: 18 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="month" tickLine={false} axisLine={false} />
              <YAxis tickLine={false} axisLine={false} />
              <Tooltip contentStyle={{ borderRadius: 8 }} />
              <Area dataKey="revenue" stroke="#2563eb" fill="#2563eb" fillOpacity={0.16} isAnimationActive={false} />
              <Area dataKey="receivables" stroke="#f59e0b" fill="#f59e0b" fillOpacity={0.12} isAnimationActive={false} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </section>
      <section className="panel" aria-label="Category revenue">
        <PanelTitle icon={<DollarSign size={18} aria-hidden="true" />} title="Category mix" meta="Revenue by offer" />
        <div className="chart-frame compact-chart">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={categoryRevenueSeed}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="category" tickLine={false} axisLine={false} />
              <YAxis tickLine={false} axisLine={false} />
              <Tooltip contentStyle={{ borderRadius: 8 }} />
              <Bar dataKey="revenue" fill="#16a34a" radius={[4, 4, 0, 0]} isAnimationActive={false} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </section>
      <section className="panel" aria-label="Operational risk">
        <PanelTitle icon={<ShieldCheck size={18} aria-hidden="true" />} title="Operational risk" meta="Working capital and stock" />
        <div className="risk-layout">
          <div className="pie-frame">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={riskSeed} dataKey="value" innerRadius={48} outerRadius={72} paddingAngle={3} isAnimationActive={false}>
                  {riskSeed.map((entry, index) => (
                    <Cell fill={riskColors[index % riskColors.length]} key={entry.label} />
                  ))}
                </Pie>
                <Tooltip contentStyle={{ borderRadius: 8 }} />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <ul className="risk-list">
            {riskSeed.map((item, index) => (
              <li key={item.label}>
                <span style={{ backgroundColor: riskColors[index % riskColors.length] }} />
                {item.label}
                <strong>{item.value}%</strong>
              </li>
            ))}
          </ul>
        </div>
      </section>
      <section className="panel wide" aria-label="Recent orders">
        <PanelTitle icon={<ClipboardList size={18} aria-hidden="true" />} title="Recent orders" meta="Confirmed demand" />
        <DataTable
          columns={['Order', 'Customer', 'Status', 'Required', 'Total']}
          rows={orders.slice(0, 5).map((order) => [
            order.number,
            order.customerName,
            order.status,
            order.requiredDate,
            formatCurrency(order.total),
          ])}
        />
      </section>
      <section className="panel" aria-label="Stock exceptions">
        <PanelTitle icon={<Boxes size={18} aria-hidden="true" />} title="Stock exceptions" meta="Items near reorder point" />
        <div className="stack-list">
          {inventory
            .filter((item) => item.available <= item.reorderPoint)
            .map((item) => (
              <article className="stack-item" key={item.id}>
                <div>
                  <strong>{item.productName}</strong>
                  <span>{item.warehouse}</span>
                </div>
                <em>{item.available}</em>
              </article>
            ))}
        </div>
      </section>
    </>
  )
}

function CustomersView({
  canManage,
  customers,
  onStatusChange,
}: {
  canManage: boolean
  customers: Customer[]
  onStatusChange: (id: string, status: Customer['status']) => void
}) {
  return (
    <section className="panel full" aria-label="Customers">
      <PanelTitle icon={<Users size={18} aria-hidden="true" />} title="Customers" meta={`${customers.length} accounts`} />
      <DataTable
        columns={['Customer', 'Tax ID', 'Segment', 'Credit limit', 'Status']}
        rows={customers.map((customer) => [
          customer.name,
          customer.taxId,
          customer.segment,
          formatCurrency(customer.creditLimit),
          <select
            aria-label={`Status for ${customer.name}`}
            className="status-select"
            disabled={!canManage}
            key={customer.id}
            value={customer.status}
            onChange={(event) =>
              onStatusChange(customer.id, event.target.value as Customer['status'])
            }
          >
            <option>Active</option>
            <option>OnHold</option>
            <option>Inactive</option>
          </select>,
        ])}
      />
    </section>
  )
}

function ProductsView({ products }: { products: Product[] }) {
  return (
    <section className="panel full" aria-label="Products">
      <PanelTitle icon={<Package size={18} aria-hidden="true" />} title="Products" meta={`${products.length} sellable items`} />
      <DataTable
        columns={['SKU', 'Product', 'Category', 'Unit price', 'Reorder']}
        rows={products.map((product) => [
          product.sku,
          product.name,
          product.category,
          formatCurrency(product.unitPrice),
          product.reorderPoint.toString(),
        ])}
      />
    </section>
  )
}

function OrdersView({ orders }: { orders: Order[] }) {
  return (
    <section className="panel full" aria-label="Orders">
      <PanelTitle icon={<ShoppingCart size={18} aria-hidden="true" />} title="Orders" meta={`${orders.length} sales orders`} />
      <div className="order-grid">
        {orders.map((order) => (
          <article className="order-card" key={order.id}>
            <div className="record-topline">
              <strong>{order.number}</strong>
              <span className="status-pill blue">{order.status}</span>
            </div>
            <h3>{order.customerName}</h3>
            <p>{order.lines.map((line) => `${line.quantity}x ${line.sku}`).join(', ')}</p>
            <div className="record-footer">
              <span>{order.requiredDate}</span>
              <strong>{formatCurrency(order.total)}</strong>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function InventoryView({ inventory }: { inventory: InventoryItem[] }) {
  return (
    <section className="panel full" aria-label="Inventory">
      <PanelTitle icon={<Boxes size={18} aria-hidden="true" />} title="Inventory" meta={`${inventory.length} stock positions`} />
      <DataTable
        columns={['SKU', 'Product', 'Warehouse', 'On hand', 'Reserved', 'Available']}
        rows={inventory.map((item) => [
          item.sku,
          item.productName,
          item.warehouse,
          item.onHand.toString(),
          item.reserved.toString(),
          <span className={clsx('quantity-badge', item.available <= item.reorderPoint && 'low')} key={item.id}>
            {item.available}
          </span>,
        ])}
      />
    </section>
  )
}

function FinanceView({
  canAdmin,
  invoices,
  onMarkPaid,
}: {
  canAdmin: boolean
  invoices: Invoice[]
  onMarkPaid: (id: string) => void
}) {
  return (
    <section className="panel full" aria-label="Finance">
      <PanelTitle icon={<ReceiptText size={18} aria-hidden="true" />} title="Finance" meta={`${invoices.length} invoices`} />
      <DataTable
        columns={['Invoice', 'Order', 'Customer', 'Due date', 'Amount', 'Status']}
        rows={invoices.map((invoice) => [
          invoice.number,
          invoice.orderNumber,
          invoice.customerName,
          invoice.dueDate,
          formatCurrency(invoice.amount),
          invoice.status === 'Open' ? (
            <button
              className="text-button"
              disabled={!canAdmin}
              key={invoice.id}
              type="button"
              onClick={() => onMarkPaid(invoice.id)}
            >
              Mark paid
            </button>
          ) : (
            <span className="status-pill green" key={invoice.id}>
              {invoice.status}
            </span>
          ),
        ])}
      />
    </section>
  )
}

function PermissionsView({ activeRole }: { activeRole: UserProfile['role'] }) {
  return (
    <section className="panel full" aria-label="Permissions">
      <PanelTitle icon={<ShieldCheck size={18} aria-hidden="true" />} title="Permissions" meta={`Active role: ${activeRole}`} />
      <div className="permission-table">
        <div className="permission-row header">
          <span>Capability</span>
          <span>Administrator</span>
          <span>Manager</span>
          <span>Analyst</span>
        </div>
        {permissions.map((permission) => (
          <div className="permission-row" key={permission.capability}>
            <div>
              <strong>{permission.capability}</strong>
              <span>{permission.description}</span>
            </div>
            <PermissionCell active={permission.administrator} highlighted={activeRole === 'Administrator'} />
            <PermissionCell active={permission.manager} highlighted={activeRole === 'Manager'} />
            <PermissionCell active={permission.analyst} highlighted={activeRole === 'Analyst'} />
          </div>
        ))}
      </div>
    </section>
  )
}

function OrderDialog({
  customers,
  products,
  onClose,
  onSubmit,
}: {
  customers: Customer[]
  products: Product[]
  onClose: () => void
  onSubmit: (customerId: string, productId: string, quantity: number) => void
}) {
  const [customerId, setCustomerId] = useState(customers[0]?.id ?? '')
  const [productId, setProductId] = useState(products[0]?.id ?? '')
  const [quantity, setQuantity] = useState(1)

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit(customerId, productId, quantity)
  }

  return (
    <div className="dialog-backdrop">
      <section className="dialog" role="dialog" aria-modal="true" aria-label="Create sales order">
        <div className="dialog-header">
          <div>
            <p className="eyebrow">Sales operations</p>
            <h2>Create sales order</h2>
          </div>
          <IconButton label="Close" onClick={onClose}>
            <X size={18} aria-hidden="true" />
          </IconButton>
        </div>
        <form className="order-form" onSubmit={submit}>
          <label>
            Customer
            <select value={customerId} onChange={(event) => setCustomerId(event.target.value)}>
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Product
            <select value={productId} onChange={(event) => setProductId(event.target.value)}>
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.sku} - {product.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Quantity
            <input
              min={1}
              type="number"
              value={quantity}
              onChange={(event) => setQuantity(Number(event.target.value))}
            />
          </label>
          <button className="primary-button" type="submit">
            <Plus size={18} aria-hidden="true" />
            Create order
          </button>
        </form>
      </section>
    </div>
  )
}

function DataTable({
  columns,
  rows,
}: {
  columns: string[]
  rows: Array<Array<ReactNode>>
}) {
  return (
    <div className="data-table" style={{ '--columns': columns.length } as React.CSSProperties}>
      <div className="data-row header">
        {columns.map((column) => (
          <span key={column}>{column}</span>
        ))}
      </div>
      {rows.map((row, index) => (
        <div className="data-row" key={index}>
          {row.map((cell, cellIndex) => (
            <span data-label={columns[cellIndex]} key={`${index}-${cellIndex}`}>
              {cell}
            </span>
          ))}
        </div>
      ))}
    </div>
  )
}

function PanelTitle({
  icon,
  meta,
  title,
}: {
  icon: ReactNode
  meta: string
  title: string
}) {
  return (
    <div className="panel-title">
      {icon}
      <div>
        <h3>{title}</h3>
        <span>{meta}</span>
      </div>
    </div>
  )
}

function PermissionCell({
  active,
  highlighted,
}: {
  active: boolean
  highlighted: boolean
}) {
  return (
    <span className={clsx('permission-cell', active && 'allowed', highlighted && 'focused')}>
      {active ? <Check size={16} aria-hidden="true" /> : <X size={16} aria-hidden="true" />}
    </span>
  )
}

function ThemeToggle({
  theme,
  onThemeChange,
}: {
  theme: ThemeMode
  onThemeChange: (theme: ThemeMode) => void
}) {
  return (
    <button
      aria-label="Toggle theme"
      className="icon-button"
      title="Toggle theme"
      type="button"
      onClick={() => onThemeChange(theme === 'light' ? 'dark' : 'light')}
    >
      {theme === 'light' ? <Moon size={18} aria-hidden="true" /> : <Sun size={18} aria-hidden="true" />}
    </button>
  )
}

function IconButton({
  children,
  label,
  onClick,
}: {
  children: ReactNode
  label: string
  onClick?: () => void
}) {
  return (
    <button
      aria-label={label}
      className="icon-button"
      title={label}
      type="button"
      onClick={onClick}
    >
      {children}
    </button>
  )
}

function initials(name: string) {
  return name
    .split(' ')
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatCompactCurrency(value: number) {
  if (value >= 1_000_000) {
    return `$${(value / 1_000_000).toFixed(2)}M`
  }

  return `$${(value / 1000).toFixed(0)}K`
}

export default App
