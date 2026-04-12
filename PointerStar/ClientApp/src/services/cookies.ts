const acceptedValue = 'accepted'
const consentCookieKey = 'CookieConsent'
const defaultExpirationDays = 30
const nameKey = 'Name'
const rejectedValue = 'rejected'
const roleKey = 'RoleId'
const roomKey = 'RoomId'
const themePreferenceKey = 'ThemePreference'
const voteOptionsKey = 'VoteOptions'

function getCookieEntry(key: string): string | null {
  const value = document.cookie
    .split(';')
    .map((entry) => entry.trim())
    .find((entry) => entry.startsWith(`${key}=`))

  if (!value) {
    return null
  }

  return value.slice(key.length + 1)
}

function getExpiration(days?: number | null): string {
  if (days === 0) {
    return ''
  }

  const effectiveDays = days ?? defaultExpirationDays
  const expiresAt = new Date(Date.now() + effectiveDays * 24 * 60 * 60 * 1000)
  return `; expires=${expiresAt.toUTCString()}`
}

function setCookieValue(key: string, value: string, days?: number | null, force = false) {
  if (!force && key !== consentCookieKey && !hasCookieConsent()) {
    return
  }

  document.cookie = `${key}=${encodeURIComponent(value)}${getExpiration(days)}; path=/`
}

export function getCookieValue(key: string, defaultValue = '') {
  const value = getCookieEntry(key)
  return value ? decodeURIComponent(value) : defaultValue
}

export function hasCookieConsent() {
  return getCookieValue(consentCookieKey) === acceptedValue
}

export function hasUserRespondedToConsent() {
  return getCookieValue(consentCookieKey) !== ''
}

export function acceptCookies() {
  setCookieValue(consentCookieKey, acceptedValue, 365, true)
}

export function rejectCookies() {
  setCookieValue(consentCookieKey, rejectedValue, 0, true)
}

export function getStoredName() {
  return getCookieValue(nameKey)
}

export function setStoredName(value: string) {
  setCookieValue(nameKey, value)
}

export function getStoredRoleId() {
  const value = getCookieValue(roleKey)
  return value || null
}

export function setStoredRoleId(value?: string | null) {
  setCookieValue(roleKey, value ?? '')
}

export function getStoredRoomId() {
  return getCookieValue(roomKey)
}

export function setStoredRoomId(value: string) {
  setCookieValue(roomKey, value)
}

export function getStoredThemePreferenceValue() {
  return getCookieValue(themePreferenceKey)
}

export function setStoredThemePreferenceValue(value: string) {
  setCookieValue(themePreferenceKey, value)
}

export function getStoredVoteOptions() {
  const value = getCookieValue(voteOptionsKey)
  if (!value) {
    return null
  }

  try {
    const parsed = JSON.parse(value)
    if (Array.isArray(parsed) && parsed.every((entry) => typeof entry === 'string')) {
      return parsed
    }
  } catch (error) {
    console.warn('Unable to parse stored vote options.', error)
  }

  return null
}

export function setStoredVoteOptions(value?: string[] | null) {
  setCookieValue(voteOptionsKey, value ? JSON.stringify(value) : '')
}
