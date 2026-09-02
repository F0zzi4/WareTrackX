export type ThemeMode = 'light' | 'dark'

export type DashboardView =
  | 'dashboard'
  | 'customers'
  | 'products'
  | 'orders'
  | 'inventory'
  | 'finance'
  | 'permissions'

export type UserRole = 'Administrator' | 'Manager' | 'Analyst'

export type UserProfile = {
  name: string
  email: string
  role: UserRole
}

export type CustomerStatus = 'Active' | 'OnHold' | 'Inactive'

export type Customer = {
  id: string
  name: string
  taxId: string
  segment: string
  status: CustomerStatus
  creditLimit: number
}

export type Product = {
  id: string
  sku: string
  name: string
  category: string
  unitPrice: number
  reorderPoint: number
}

export type InventoryItem = {
  id: string
  productId: string
  sku: string
  productName: string
  warehouse: string
  onHand: number
  reserved: number
  available: number
  reorderPoint: number
}

export type OrderStatus = 'Draft' | 'Confirmed' | 'Fulfilled' | 'Cancelled'

export type OrderLine = {
  productId: string
  sku: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type Order = {
  id: string
  number: string
  customerId: string
  customerName: string
  status: OrderStatus
  requiredDate: string
  total: number
  lines: OrderLine[]
}

export type InvoiceStatus = 'Open' | 'Paid' | 'Overdue' | 'Cancelled'

export type Invoice = {
  id: string
  number: string
  orderNumber: string
  customerName: string
  status: InvoiceStatus
  dueDate: string
  amount: number
}

export type PermissionRow = {
  capability: string
  description: string
  administrator: boolean
  manager: boolean
  analyst: boolean
}
