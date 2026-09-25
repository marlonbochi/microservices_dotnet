import { describe, expect, it } from 'vitest'
import { describeProblem, formatCurrency, orderStatusPresentation } from '~/utils/format'

describe('formatCurrency', () => {
  it('formats values as USD', () => {
    expect(formatCurrency(1234.5)).toBe('$1,234.50')
  })
})

describe('describeProblem', () => {
  it('prefers field validation errors', () => {
    expect(describeProblem({ title: 'Validation', errors: { Price: ['Price must be > 0.'] } })).toBe('Price must be > 0.')
  })

  it('falls back to detail, then title, then default', () => {
    expect(describeProblem({ title: 'T', detail: 'D' })).toBe('D')
    expect(describeProblem({ title: 'T' })).toBe('T')
    expect(describeProblem(undefined, 'fallback')).toBe('fallback')
  })
})

describe('orderStatusPresentation', () => {
  it('marks only terminal statuses as final', () => {
    const finals = Object.entries(orderStatusPresentation).filter(([, value]) => value.final).map(([key]) => key)
    expect(finals.sort()).toEqual(['Cancelled', 'Confirmed', 'Rejected'])
  })
})
