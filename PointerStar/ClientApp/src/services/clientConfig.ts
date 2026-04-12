import type { ClientConfig } from '../types/contracts'
import { fetchWithRetry } from './retry'

const clientConfigRetryOptions = {
  baseDelayMs: 300,
  maxAttempts: 4,
  maxDelayMs: 2_000,
}

export async function loadClientConfig(): Promise<ClientConfig | null> {
  try {
    const response = await fetchWithRetry('/api/client-config', {
      headers: {
        Accept: 'application/json',
      },
    }, clientConfigRetryOptions)

    if (!response.ok) {
      console.warn(`Unable to load client configuration: ${response.status} ${response.statusText}`)
      return null
    }

    return (await response.json()) as ClientConfig
  } catch (error) {
    console.warn('Unable to load client configuration.', error)
    return null
  }
}
