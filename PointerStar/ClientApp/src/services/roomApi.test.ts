import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { roles } from '../types/contracts'
import { generateRoomId, getNewUserRole } from './roomApi'

describe('roomApi', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('retries transient room generation failures', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response('', { status: 503, statusText: 'Service Unavailable' }))
      .mockResolvedValueOnce(new Response('room-123', { status: 200 }))

    vi.stubGlobal('fetch', fetchMock)

    const roomIdPromise = generateRoomId()

    await vi.runAllTimersAsync()

    await expect(roomIdPromise).resolves.toBe('room-123')
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('does not retry permanent room generation failures', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValue(new Response('', { status: 404, statusText: 'Not Found' }))

    vi.stubGlobal('fetch', fetchMock)

    await expect(generateRoomId()).rejects.toThrow('Unable to generate a room: 404 Not Found')
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('retries transient default role lookups', async () => {
    const fetchMock = vi.fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({
          id: roles.facilitator.id,
          name: roles.facilitator.name,
        }), {
          headers: { 'Content-Type': 'application/json' },
          status: 200,
        }),
      )

    vi.stubGlobal('fetch', fetchMock)

    const rolePromise = getNewUserRole('room-123')

    await vi.runAllTimersAsync()

    await expect(rolePromise).resolves.toEqual(roles.facilitator)
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
