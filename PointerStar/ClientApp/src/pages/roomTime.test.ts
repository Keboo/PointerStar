import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { getElapsedTimeLabel } from './roomTime'

describe('roomTime', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-04-07T12:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('returns an empty label when the timer has not started', () => {
    expect(getElapsedTimeLabel()).toBe('')
  })

  it('shows minutes and seconds for timers under an hour', () => {
    expect(getElapsedTimeLabel('2026-04-07T11:58:55Z')).toBe('01:05')
  })

  it('shows hours, minutes, and seconds for timers longer than an hour', () => {
    expect(getElapsedTimeLabel('2026-04-07T10:58:55Z')).toBe('1:01:05')
  })

  it('applies the server clock offset to the elapsed time', () => {
    expect(getElapsedTimeLabel('2026-04-07T11:59:30Z', 5_000)).toBe('00:35')
  })
})
