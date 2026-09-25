import type { OrderStatus, ProblemDetails } from '~/types/api'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const dateTime = new Intl.DateTimeFormat('en-US', { dateStyle: 'short', timeStyle: 'medium' })

export const formatCurrency = (value: number): string => currency.format(value)

export const formatDateTime = (value: string): string => dateTime.format(new Date(value))

type BadgeColor = 'primary' | 'secondary' | 'success' | 'info' | 'warning' | 'error' | 'neutral'

interface StatusPresentation {
  label: string
  color: BadgeColor
  icon: string
  final: boolean
}

export const orderStatusPresentation: Record<OrderStatus, StatusPresentation> = {
  Submitted: { label: 'Submitted', color: 'neutral', icon: 'i-lucide-inbox', final: false },
  StockReserved: { label: 'Stock reserved', color: 'info', icon: 'i-lucide-package-check', final: false },
  PaymentApproved: { label: 'Payment approved', color: 'primary', icon: 'i-lucide-credit-card', final: false },
  PaymentDeclined: { label: 'Payment declined', color: 'warning', icon: 'i-lucide-credit-card', final: false },
  Confirmed: { label: 'Confirmed', color: 'success', icon: 'i-lucide-circle-check', final: true },
  Rejected: { label: 'Rejected', color: 'error', icon: 'i-lucide-circle-x', final: true },
  Cancelled: { label: 'Cancelled', color: 'error', icon: 'i-lucide-ban', final: true },
}

/** Turns a Problem Details response into one readable sentence. */
export function describeProblem(problem: ProblemDetails | undefined, fallback = 'Unexpected error.'): string {
  if (!problem) {
    return fallback
  }

  const fieldErrors = Object.values(problem.errors ?? {}).flat()
  if (fieldErrors.length > 0) {
    return fieldErrors.join(' ')
  }

  return problem.detail ?? problem.title ?? fallback
}
