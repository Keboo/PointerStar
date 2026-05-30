import type { GiphySearchResponse } from '../types/contracts'

export const GIPHY_PAGE_SIZE = 20

/**
 * Service for interacting with the Giphy API endpoint.
 * Handles searching for Giphy images and error handling.
 */

interface SearchGiphyOptions {
  query: string
  offset?: number
}

export async function searchGiphy({ query, offset = 0 }: SearchGiphyOptions): Promise<GiphySearchResponse> {
  const trimmedQuery = query.trim()
  if (!trimmedQuery) {
    return {
      data: [],
      pagination: null,
    }
  }

  try {
    const params = new URLSearchParams({
      query: trimmedQuery,
      offset: String(offset),
    })

    const response = await fetch(`/api/giphy/search?${params.toString()}`, {
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
    return {
      data: [],
      pagination: null,
    }
  }
}
