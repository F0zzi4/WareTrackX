import type {
  Customer,
  InventoryItem,
  Invoice,
  Order,
  PermissionRow,
  Product,
  UserProfile,
} from './types'

export const profiles: UserProfile[] = [
  {
    name: 'Victor Siqueira',
    email: 'victor.siqueira@enterprise.dev',
    role: 'Administrator',
  },
  {
    name: 'Marina Costa',
    email: 'marina.costa@enterprise.dev',
    role: 'Manager',
  },
  {
    name: 'Rafael Lima',
    email: 'rafael.lima@enterprise.dev',
    role: 'Analyst',
  },
]

export const customersSeed: Customer[] = [
  { id: 'cus-northwind', name: 'Northwind Manufacturing', taxId: 'NW-102938', segment: 'Manufacturing', status: 'Active', creditLimit: 450000 },
  { id: 'cus-contoso', name: 'Contoso Logistics', taxId: 'CL-485920', segment: 'Supply Chain', status: 'Active', creditLimit: 620000 },
  { id: 'cus-fabrikam', name: 'Fabrikam Retail', taxId: 'FR-330219', segment: 'Retail', status: 'Active', creditLimit: 310000 },
  { id: 'cus-adatum', name: 'A. Datum Energy', taxId: 'AD-761042', segment: 'Energy', status: 'Active', creditLimit: 780000 },
  { id: 'cus-tailspin', name: 'Tailspin Health', taxId: 'TH-904120', segment: 'Healthcare', status: 'OnHold', creditLimit: 260000 },
]

export const productsSeed: Product[] = [
  { id: 'prd-erp-svc', sku: 'ERP-SVC-001', name: 'ERP implementation sprint', category: 'Services', unitPrice: 18000, reorderPoint: 10 },
  { id: 'prd-dyn-con', sku: 'DYN-CON-010', name: 'Dynamics connector pack', category: 'Integrations', unitPrice: 12500, reorderPoint: 8 },
  { id: 'prd-bi-dat', sku: 'BI-DAT-204', name: 'Analytics data mart', category: 'Analytics', unitPrice: 22600, reorderPoint: 6 },
  { id: 'prd-sup-aut', sku: 'SUP-AUT-115', name: 'Warehouse automation kit', category: 'Operations', unitPrice: 34800, reorderPoint: 4 },
  { id: 'prd-fin-clo', sku: 'FIN-CLO-330', name: 'Finance close accelerator', category: 'Finance', unitPrice: 16400, reorderPoint: 7 },
  { id: 'prd-ai-erp', sku: 'AI-ERP-700', name: 'ERP AI assistant bundle', category: 'AI', unitPrice: 41000, reorderPoint: 3 },
]

export const inventorySeed: InventoryItem[] = [
  { id: 'inv-erp-svc', productId: 'prd-erp-svc', sku: 'ERP-SVC-001', productName: 'ERP implementation sprint', warehouse: 'Main Warehouse', onHand: 36, reserved: 5, available: 31, reorderPoint: 10 },
  { id: 'inv-dyn-con', productId: 'prd-dyn-con', sku: 'DYN-CON-010', productName: 'Dynamics connector pack', warehouse: 'Main Warehouse', onHand: 24, reserved: 4, available: 20, reorderPoint: 8 },
  { id: 'inv-bi-dat', productId: 'prd-bi-dat', sku: 'BI-DAT-204', productName: 'Analytics data mart', warehouse: 'Data Center', onHand: 18, reserved: 2, available: 16, reorderPoint: 6 },
  { id: 'inv-sup-aut', productId: 'prd-sup-aut', sku: 'SUP-AUT-115', productName: 'Warehouse automation kit', warehouse: 'Main Warehouse', onHand: 7, reserved: 3, available: 4, reorderPoint: 4 },
  { id: 'inv-fin-clo', productId: 'prd-fin-clo', sku: 'FIN-CLO-330', productName: 'Finance close accelerator', warehouse: 'Finance Hub', onHand: 14, reserved: 1, available: 13, reorderPoint: 7 },
  { id: 'inv-ai-erp', productId: 'prd-ai-erp', sku: 'AI-ERP-700', productName: 'ERP AI assistant bundle', warehouse: 'Innovation Lab', onHand: 6, reserved: 2, available: 4, reorderPoint: 3 },
]

export const ordersSeed: Order[] = [
  {
    id: 'ord-0003',
    number: 'SO-2026-0003',
    customerId: 'cus-adatum',
    customerName: 'A. Datum Energy',
    status: 'Confirmed',
    requiredDate: '2026-07-08',
    total: 83160,
    lines: [
      { productId: 'prd-ai-erp', sku: 'AI-ERP-700', productName: 'ERP AI assistant bundle', quantity: 1, unitPrice: 41000, lineTotal: 41000 },
      { productId: 'prd-erp-svc', sku: 'ERP-SVC-001', productName: 'ERP implementation sprint', quantity: 2, unitPrice: 18000, lineTotal: 36000 },
    ],
  },
  {
    id: 'ord-0002',
    number: 'SO-2026-0002',
    customerId: 'cus-contoso',
    customerName: 'Contoso Logistics',
    status: 'Confirmed',
    requiredDate: '2026-07-05',
    total: 99576,
    lines: [
      { productId: 'prd-sup-aut', sku: 'SUP-AUT-115', productName: 'Warehouse automation kit', quantity: 2, unitPrice: 34800, lineTotal: 69600 },
      { productId: 'prd-bi-dat', sku: 'BI-DAT-204', productName: 'Analytics data mart', quantity: 1, unitPrice: 22600, lineTotal: 22600 },
    ],
  },
  {
    id: 'ord-0001',
    number: 'SO-2026-0001',
    customerId: 'cus-northwind',
    customerName: 'Northwind Manufacturing',
    status: 'Fulfilled',
    requiredDate: '2026-06-18',
    total: 75924,
    lines: [
      { productId: 'prd-dyn-con', sku: 'DYN-CON-010', productName: 'Dynamics connector pack', quantity: 3, unitPrice: 12500, lineTotal: 37500 },
      { productId: 'prd-fin-clo', sku: 'FIN-CLO-330', productName: 'Finance close accelerator', quantity: 2, unitPrice: 16400, lineTotal: 32800 },
    ],
  },
]

export const invoicesSeed: Invoice[] = [
  { id: 'inv-0003', number: 'INV-2026-0003', orderNumber: 'SO-2026-0003', customerName: 'A. Datum Energy', status: 'Open', dueDate: '2026-08-01', amount: 83160 },
  { id: 'inv-0002', number: 'INV-2026-0002', orderNumber: 'SO-2026-0002', customerName: 'Contoso Logistics', status: 'Open', dueDate: '2026-07-21', amount: 99576 },
  { id: 'inv-0001', number: 'INV-2026-0001', orderNumber: 'SO-2026-0001', customerName: 'Northwind Manufacturing', status: 'Paid', dueDate: '2026-07-01', amount: 75924 },
]

export const revenueSeed = [
  { month: 'Jan', revenue: 160, receivables: 64 },
  { month: 'Feb', revenue: 182, receivables: 72 },
  { month: 'Mar', revenue: 210, receivables: 78 },
  { month: 'Apr', revenue: 248, receivables: 91 },
  { month: 'May', revenue: 272, receivables: 86 },
  { month: 'Jun', revenue: 301, receivables: 94 },
  { month: 'Jul', revenue: 326, receivables: 102 },
]

export const categoryRevenueSeed = [
  { category: 'ERP', revenue: 142 },
  { category: 'BI', revenue: 96 },
  { category: 'Ops', revenue: 118 },
  { category: 'AI', revenue: 84 },
  { category: 'Finance', revenue: 72 },
]

export const riskSeed = [
  { label: 'Receivables', value: 34 },
  { label: 'Inventory', value: 22 },
  { label: 'Customer credit', value: 18 },
  { label: 'Delivery', value: 26 },
]

export const permissions: PermissionRow[] = [
  {
    capability: 'View executive dashboard',
    description: 'Access sales, stock, finance and operational KPIs.',
    administrator: true,
    manager: true,
    analyst: true,
  },
  {
    capability: 'Create sales orders',
    description: 'Reserve stock and generate open invoices.',
    administrator: true,
    manager: true,
    analyst: false,
  },
  {
    capability: 'Manage customers',
    description: 'Update status, credit posture and operating segments.',
    administrator: true,
    manager: true,
    analyst: false,
  },
  {
    capability: 'Adjust inventory',
    description: 'Post stock movements and rebalance warehouse inventory.',
    administrator: true,
    manager: true,
    analyst: false,
  },
  {
    capability: 'Settle invoices',
    description: 'Mark invoices as paid and close receivables.',
    administrator: true,
    manager: false,
    analyst: false,
  },
]
