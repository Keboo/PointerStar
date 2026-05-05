const USER_ID_KEY = 'pointerstar-user-id'

/**
 * Returns a stable user UUID for this browser tab session.
 * Creates and persists a new UUID in sessionStorage on first call.
 * Cleared when the tab is closed, so each new tab gets a fresh identity.
 */
export function getOrCreateUserId(): string {
  let userId = sessionStorage.getItem(USER_ID_KEY)
  if (!userId) {
    userId = crypto.randomUUID()
    sessionStorage.setItem(USER_ID_KEY, userId)
  }
  return userId
}
