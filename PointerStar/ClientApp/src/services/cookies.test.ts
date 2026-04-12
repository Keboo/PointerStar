import { beforeEach, describe, expect, it } from 'vitest'

import {
  acceptCookies,
  getStoredName,
  hasCookieConsent,
  hasUserRespondedToConsent,
  rejectCookies,
  setStoredName,
} from './cookies'

function clearCookies() {
  document.cookie.split(';').forEach((entry) => {
    const [rawKey] = entry.split('=')
    const key = rawKey?.trim()
    if (key) {
      document.cookie = `${key}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`
    }
  })
}

describe('cookies', () => {
  beforeEach(() => {
    clearCookies()
  })

  it('blocks non-essential cookie writes until consent is accepted', () => {
    setStoredName('Taylor')

    expect(getStoredName()).toBe('')
  })

  it('allows non-essential cookies after consent is accepted', () => {
    acceptCookies()
    setStoredName('Taylor')

    expect(hasCookieConsent()).toBe(true)
    expect(hasUserRespondedToConsent()).toBe(true)
    expect(getStoredName()).toBe('Taylor')
  })

  it('tracks a rejected consent response without enabling cookies', () => {
    rejectCookies()

    expect(hasCookieConsent()).toBe(false)
    expect(hasUserRespondedToConsent()).toBe(true)
  })
})
