import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { loadClientConfig } from './clientConfig'

describe('clientConfig', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('retries transient client configuration failures before succeeding', async () => {
    const fetchMock = vi.fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({
          appVersion: '1.2.3',
          applicationInsightsConnectionString: 'InstrumentationKey=abc',
        }), {
          headers: { 'Content-Type': 'application/json' },
          status: 200,
        }),
      )

    vi.stubGlobal('fetch', fetchMock)

    const configPromise = loadClientConfig()

    await vi.runAllTimersAsync()

    await expect(configPromise).resolves.toEqual({
      appVersion: '1.2.3',
      applicationInsightsConnectionString: 'InstrumentationKey=abc',
    })
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('returns null after exhausting retries', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})

    vi.stubGlobal('fetch', fetchMock)

    const configPromise = loadClientConfig()

    await vi.runAllTimersAsync()

    await expect(configPromise).resolves.toBeNull()
    expect(fetchMock).toHaveBeenCalledTimes(4)
    expect(warnSpy).toHaveBeenCalled()
  })
})
