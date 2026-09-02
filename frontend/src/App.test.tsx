import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from './App'

async function signIn() {
  const user = userEvent.setup()
  render(<App />)
  await user.click(screen.getByRole('button', { name: /sign in/i }))
  return user
}

describe('App', () => {
  it('authenticates into the Mini ERP workspace', async () => {
    await signIn()

    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument()
    expect(screen.getByText('Revenue')).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: /primary navigation/i })).toBeInTheDocument()
  })

  it('filters customer records from the global search', async () => {
    const user = await signIn()

    await user.click(screen.getByRole('button', { name: /customers/i }))
    await user.type(screen.getByRole('textbox', { name: /search workspace/i }), 'contoso')

    expect(screen.getByText('Contoso Logistics')).toBeInTheDocument()
    expect(screen.queryByText('Northwind Manufacturing')).not.toBeInTheDocument()
  })

  it('creates a sales order and matching finance record', async () => {
    const user = await signIn()

    await user.click(screen.getByRole('button', { name: /new order/i }))
    const dialog = screen.getByRole('dialog', { name: /create sales order/i })
    await user.selectOptions(within(dialog).getByLabelText('Customer'), 'cus-fabrikam')
    await user.selectOptions(within(dialog).getByLabelText('Product'), 'prd-ai-erp')
    await user.clear(within(dialog).getByLabelText('Quantity'))
    await user.type(within(dialog).getByLabelText('Quantity'), '2')
    await user.click(within(dialog).getByRole('button', { name: /create order/i }))

    expect(screen.getByText('SO-2026-0004')).toBeInTheDocument()
    expect(screen.getByText('Fabrikam Retail')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /finance/i }))
    expect(screen.getByText('INV-2026-0004')).toBeInTheDocument()
  })

  it('limits order creation for analyst role', async () => {
    const user = await signIn()

    await user.selectOptions(screen.getByRole('combobox', { name: /active role/i }), [
      'rafael.lima@enterprise.dev',
    ])

    expect(screen.getByRole('button', { name: /new order/i })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: /permissions/i }))
    expect(screen.getByText('Active role: Analyst')).toBeInTheDocument()
  })

  it('toggles dark mode on the login screen', async () => {
    const user = userEvent.setup()
    render(<App />)

    await user.click(screen.getByRole('button', { name: /toggle theme/i }))

    expect(screen.getByRole('main')).toHaveAttribute('data-theme', 'dark')
  })
})
