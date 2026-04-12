import type { RecentRoom } from '../types/contracts'

const maxDaysToKeep = 30
const storageKey = 'RecentRooms'

function saveRooms(rooms: RecentRoom[]) {
  localStorage.setItem(storageKey, JSON.stringify(rooms))
}

export function getRecentRooms(): RecentRoom[] {
  try {
    const rawValue = localStorage.getItem(storageKey)
    if (!rawValue) {
      return []
    }

    const rooms = JSON.parse(rawValue) as RecentRoom[]
    const cutoff = Date.now() - maxDaysToKeep * 24 * 60 * 60 * 1000
    const filteredRooms = rooms
      .filter((room) => new Date(room.lastAccessed).getTime() >= cutoff)
      .sort((left, right) => new Date(right.lastAccessed).getTime() - new Date(left.lastAccessed).getTime())

    if (filteredRooms.length !== rooms.length) {
      saveRooms(filteredRooms)
    }

    return filteredRooms
  } catch (error) {
    console.warn('Unable to read recent rooms from local storage.', error)
    return []
  }
}

export function addRecentRoom(roomId: string) {
  if (!roomId.trim()) {
    return
  }

  const rooms = getRecentRooms().filter((room) => room.roomId !== roomId)
  rooms.unshift({
    lastAccessed: new Date().toISOString(),
    roomId,
  })

  saveRooms(rooms)
}

export function removeRecentRoom(roomId: string) {
  if (!roomId.trim()) {
    return
  }

  saveRooms(getRecentRooms().filter((room) => room.roomId !== roomId))
}
