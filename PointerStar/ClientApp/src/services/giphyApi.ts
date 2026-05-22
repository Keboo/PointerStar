import { GiphySearchResponse } from '../types/contracts'

/**
 * Service for interacting with the Giphy API endpoint.
 * Handles searching for Giphy images and error handling.
 */

export async function searchGiphy(query: string): Promise<GiphySearchResponse> {
  try {
    const response = await fetch(`/api/giphy/search?query=${encodeURIComponent(query)}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    if (!response.ok) {
      if (response.status === 429) {
        throw new Error('Rate limit exceeded. Please wait before searching again.')
      }
      throw new Error(`API error: ${response.status} ${response.statusText}`)
    }

    const data: GiphySearchResponse = await response.json()
    return data
  } catch (error) {
    console.error('Giphy search failed:', error)
    // Return empty results on failure for graceful degradation
    return {
      data: [],
      pagination: null,
    }
  }
}
