import type { OrderStatus, ProblemDetails } from '~/types/api'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'medium' })

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
  Submitted: { label: 'Recebido', color: 'neutral', icon: 'i-lucide-inbox', final: false },
  StockReserved: { label: 'Estoque reservado', color: 'info', icon: 'i-lucide-package-check', final: false },
  PaymentApproved: { label: 'Pagamento aprovado', color: 'primary', icon: 'i-lucide-credit-card', final: false },
  PaymentDeclined: { label: 'Pagamento recusado', color: 'warning', icon: 'i-lucide-credit-card', final: false },
  Confirmed: { label: 'Confirmado', color: 'success', icon: 'i-lucide-circle-check', final: true },
  Rejected: { label: 'Rejeitado', color: 'error', icon: 'i-lucide-circle-x', final: true },
  Cancelled: { label: 'Cancelado', color: 'error', icon: 'i-lucide-ban', final: true },
}

/** Turns a Problem Details response into one readable sentence. */
export function describeProblem(problem: ProblemDetails | undefined, fallback = 'Erro inesperado.'): string {
  if (!problem) {
    return fallback
  }

  const fieldErrors = Object.values(problem.errors ?? {}).flat()
  if (fieldErrors.length > 0) {
    return fieldErrors.join(' ')
  }

  return problem.detail ?? problem.title ?? fallback
}
