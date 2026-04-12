import type { ClientConfig } from '../types/contracts'

export async function loadClientConfig(): Promise<ClientConfig | null> {
  try {
    const response = await fetch('/api/client-config', {
      headers: {
        Accept: 'application/json',
      },
    })

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
