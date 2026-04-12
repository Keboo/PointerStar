import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { addRecentRoom, getRecentRooms, removeRecentRoom } from './recentRooms'

describe('recentRooms', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-04-07T12:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('adds rooms in most-recent-first order without duplicates', () => {
    addRecentRoom('ROOM-1')
    vi.advanceTimersByTime(1_000)
    addRecentRoom('ROOM-2')
    vi.advanceTimersByTime(1_000)
    addRecentRoom('ROOM-1')

    expect(getRecentRooms().map((room) => room.roomId)).toEqual(['ROOM-1', 'ROOM-2'])
  })

  it('filters out rooms older than 30 days', () => {
    localStorage.setItem(
      'RecentRooms',
      JSON.stringify([
        {
          lastAccessed: '2026-04-06T12:00:00Z',
          roomId: 'RECENT',
        },
        {
          lastAccessed: '2026-02-01T12:00:00Z',
          roomId: 'OLD',
        },
      ]),
    )

    expect(getRecentRooms().map((room) => room.roomId)).toEqual(['RECENT'])
  })

  it('removes a room from storage', () => {
    addRecentRoom('ROOM-1')
    addRecentRoom('ROOM-2')

    removeRecentRoom('ROOM-1')

    expect(getRecentRooms().map((room) => room.roomId)).toEqual(['ROOM-2'])
  })
})
