import { beforeEach, describe, expect, it } from 'vitest'

import {
  acceptCookies,
  getStoredName,
  getStoredRecentGifSearches,
  hasCookieConsent,
  hasUserRespondedToConsent,
  rejectCookies,
  setStoredName,
  setStoredRecentGifSearches,
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

  it('returns an empty array for recent GIF searches when nothing is stored', () => {
    expect(getStoredRecentGifSearches()).toEqual([])
  })

  it('blocks recent GIF search writes until consent is accepted', () => {
    setStoredRecentGifSearches(['cat'])

    expect(getStoredRecentGifSearches()).toEqual([])
  })

  it('persists and restores recent GIF searches after consent is accepted', () => {
    acceptCookies()
    setStoredRecentGifSearches(['cat', 'dog', 'bird'])

    expect(getStoredRecentGifSearches()).toEqual(['cat', 'dog', 'bird'])
  })

  it('clears recent GIF searches when an empty array is stored', () => {
    acceptCookies()
    setStoredRecentGifSearches(['cat'])
    setStoredRecentGifSearches([])

    expect(getStoredRecentGifSearches()).toEqual([])
  })
})
