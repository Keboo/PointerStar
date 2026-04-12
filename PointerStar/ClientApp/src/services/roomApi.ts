import { roleFromId } from '../types/contracts'

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`)
  }

  return (await response.json()) as T
}

export async function generateRoomId() {
  const response = await fetch('/api/room/generate')
  if (!response.ok) {
    throw new Error(`Unable to generate a room: ${response.status} ${response.statusText}`)
  }

  return response.text()
}

export async function getNewUserRole(roomId: string) {
  const response = await fetch(`/api/room/GetNewUserRole/${encodeURIComponent(roomId)}`, {
    headers: {
      Accept: 'application/json',
    },
  })

  const role = await readJson<{ id: string; name: string }>(response)
  return roleFromId(role.id) ?? role
}
