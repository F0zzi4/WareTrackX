import {
  customersSeed,
  inventorySeed,
  invoicesSeed,
  ordersSeed,
  productsSeed,
} from './data'
import type {
  Customer,
  CustomerStatus,
  InventoryItem,
  Invoice,
  Order,
  OrderLine,
  Product,
  UserProfile,
} from './types'

const configuredApiUrl = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? ''

export type WorkspaceData = {
  customers: Customer[]
  products: Product[]
  inventory: InventoryItem[]
  orders: Order[]
  invoices: Invoice[]
}

type AuthResponseDto = {
  accessToken: string
  expiresAt: string
  user: {
    name: string
    email: string
    role: UserProfile['role']
  }
}

type CustomerDto = {
  id: string
  name: string
  taxId: string
  segment: string
  status: CustomerStatus
  creditLimit: number
}

type ProductDto = {
  id: string
  sku: string
  name: string
  category: string
  unitPrice: number
  reorderPoint: number
}

type InventoryDto = {
  productId: string
  sku: string
  productName: string
  warehouse: string
  quantityOnHand: number
  quantityReserved: number
  availableQuantity: number
  reorderPoint: number
}

type SalesOrderDto = {
  id: string
  number: string
  customerId: string
  customerName: string
  status: Order['status']
  requiredDate: string
  total: number
  lines: Array<{
    productId: string
    sku: string
    productName: string
    quantity: number
    unitPrice: number
    lineTotal: number
  }>
}

type InvoiceDto = {
  id: string
  number: string
  salesOrderNumber: string
  customerName: string
  status: Invoice['status']
  dueAt: string
  amount: number
}

async function requestJson<T>(
  path: string,
  init: RequestInit = {},
  accessToken?: string,
): Promise<T> {
  const response = await fetch(`${configuredApiUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...init.headers,
    },
  })

  if (!response.ok) {
    throw new Error(`Mini ERP API request failed with ${response.status}`)
  }

  return response.json() as Promise<T>
}

export async function authenticate(
  email: string,
  password: string,
): Promise<AuthResponseDto | null> {
  try {
    return await requestJson<AuthResponseDto>('/api/auth/login', {
      body: JSON.stringify({ email, password }),
      method: 'POST',
    })
  } catch {
    return null
  }
}

export async function loadWorkspace(accessToken: string): Promise<WorkspaceData | null> {
  try {
    const [customers, products, inventory, orders, invoices] = await Promise.all([
      requestJson<CustomerDto[]>('/api/customers', {}, accessToken),
      requestJson<ProductDto[]>('/api/products', {}, accessToken),
      requestJson<InventoryDto[]>('/api/inventory', {}, accessToken),
      requestJson<SalesOrderDto[]>('/api/orders', {}, accessToken),
      requestJson<InvoiceDto[]>('/api/finance/invoices', {}, accessToken),
    ])

    return {
      customers: customers.map(mapCustomer),
      products: products.map(mapProduct),
      inventory: inventory.map(mapInventory),
      orders: orders.map(mapOrder),
      invoices: invoices.map(mapInvoice),
    }
  } catch {
    return null
  }
}

export async function createSalesOrder(
  accessToken: string,
  customerId: string,
  productId: string,
  quantity: number,
): Promise<boolean> {
  try {
    await requestJson<SalesOrderDto>(
      '/api/orders',
      {
        body: JSON.stringify({
          customerId,
          requiredDate: '2026-08-15T00:00:00Z',
          lines: [{ productId, quantity }],
        }),
        method: 'POST',
      },
      accessToken,
    )
    return true
  } catch {
    return false
  }
}

export async function updateCustomerStatus(
  accessToken: string,
  customerId: string,
  status: CustomerStatus,
): Promise<Customer | null> {
  try {
    const customer = await requestJson<CustomerDto>(
      `/api/customers/${customerId}/status`,
      {
        body: JSON.stringify({ status }),
        method: 'PATCH',
      },
      accessToken,
    )
    return mapCustomer(customer)
  } catch {
    return null
  }
}

export async function markInvoicePaid(
  accessToken: string,
  invoiceId: string,
): Promise<Invoice | null> {
  try {
    const invoice = await requestJson<InvoiceDto>(
      `/api/finance/invoices/${invoiceId}/mark-paid`,
      { method: 'POST' },
      accessToken,
    )
    return mapInvoice(invoice)
  } catch {
    return null
  }
}

export const fallbackWorkspace: WorkspaceData = {
  customers: customersSeed,
  products: productsSeed,
  inventory: inventorySeed,
  orders: ordersSeed,
  invoices: invoicesSeed,
}

function mapCustomer(customer: CustomerDto): Customer {
  return {
    id: customer.id,
    name: customer.name,
    taxId: customer.taxId,
    segment: customer.segment,
    status: customer.status,
    creditLimit: customer.creditLimit,
  }
}

function mapProduct(product: ProductDto): Product {
  return {
    id: product.id,
    sku: product.sku,
    name: product.name,
    category: product.category,
    unitPrice: product.unitPrice,
    reorderPoint: product.reorderPoint,
  }
}

function mapInventory(item: InventoryDto): InventoryItem {
  return {
    id: `${item.productId}-${item.warehouse}`,
    productId: item.productId,
    sku: item.sku,
    productName: item.productName,
    warehouse: item.warehouse,
    onHand: item.quantityOnHand,
    reserved: item.quantityReserved,
    available: item.availableQuantity,
    reorderPoint: item.reorderPoint,
  }
}

function mapOrder(order: SalesOrderDto): Order {
  return {
    id: order.id,
    number: order.number,
    customerId: order.customerId,
    customerName: order.customerName,
    status: order.status,
    requiredDate: formatDate(order.requiredDate),
    total: order.total,
    lines: order.lines.map(mapOrderLine),
  }
}

function mapOrderLine(line: SalesOrderDto['lines'][number]): OrderLine {
  return {
    productId: line.productId,
    sku: line.sku,
    productName: line.productName,
    quantity: line.quantity,
    unitPrice: line.unitPrice,
    lineTotal: line.lineTotal,
  }
}

function mapInvoice(invoice: InvoiceDto): Invoice {
  return {
    id: invoice.id,
    number: invoice.number,
    orderNumber: invoice.salesOrderNumber,
    customerName: invoice.customerName,
    status: invoice.status,
    dueDate: formatDate(invoice.dueAt),
    amount: invoice.amount,
  }
}

function formatDate(value: string) {
  return value.slice(0, 10)
}
