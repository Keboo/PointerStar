import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { GiphyVotingPanel } from './GiphyVotingPanel'
import { GIPHY_PAGE_SIZE, searchGiphy } from '../services/giphyApi'
import {
  getStoredFavoriteGifIds,
  getStoredFavoriteGifsExpanded,
  setStoredFavoriteGifIds,
  setStoredFavoriteGifsExpanded,
} from '../services/cookies'

vi.mock('../services/giphyApi', () => ({
  GIPHY_PAGE_SIZE: 20,
  searchGiphy: vi.fn(),
}))

vi.mock('../services/cookies', () => ({
  getStoredFavoriteGifIds: vi.fn(() => []),
  getStoredFavoriteGifsExpanded: vi.fn(() => true),
  setStoredFavoriteGifIds: vi.fn(),
  setStoredFavoriteGifsExpanded: vi.fn(),
}))

const searchGiphyMock = vi.mocked(searchGiphy)
const getStoredFavoriteGifIdsMock = vi.mocked(getStoredFavoriteGifIds)
const getStoredFavoriteGifsExpandedMock = vi.mocked(getStoredFavoriteGifsExpanded)
const setStoredFavoriteGifIdsMock = vi.mocked(setStoredFavoriteGifIds)
const setStoredFavoriteGifsExpandedMock = vi.mocked(setStoredFavoriteGifsExpanded)

const createGif = (index: number) => ({
  id: `gif-${index}`,
  title: `GIF ${index}`,
  imageUrl: `https://example.test/gif-${index}.gif`,
})

describe('GiphyVotingPanel', () => {
  beforeEach(() => {
    searchGiphyMock.mockReset()
    getStoredFavoriteGifIdsMock.mockReset()
    getStoredFavoriteGifIdsMock.mockReturnValue([])
    getStoredFavoriteGifsExpandedMock.mockReset()
    getStoredFavoriteGifsExpandedMock.mockReturnValue(true)
    setStoredFavoriteGifIdsMock.mockReset()
    setStoredFavoriteGifsExpandedMock.mockReset()
  })

  it('tracks the searched GIF query for recent chips', async () => {
    const onSearch = vi.fn()
    searchGiphyMock.mockResolvedValue({
      data: [createGif(1)],
      pagination: {
        count: 1,
        offset: 0,
        totalCount: 1,
      },
    })

    render(<GiphyVotingPanel onSearch={onSearch} onVoteSubmit={vi.fn()} />)

    fireEvent.change(screen.getByPlaceholderText('Search for a GIF...'), {
      target: { value: '  cat  ' },
    })

    await waitFor(() => {
      expect(searchGiphyMock).toHaveBeenCalledWith({
        query: 'cat',
        offset: 0,
      })
    }, { timeout: 3000 })

    expect(onSearch).toHaveBeenCalledWith('cat')
  })

  it('runs a search when a recent-search shortcut is selected', async () => {
    searchGiphyMock.mockResolvedValue({
      data: [createGif(1)],
      pagination: {
        count: 1,
        offset: 0,
        totalCount: 1,
      },
    })

    render(<GiphyVotingPanel onVoteSubmit={vi.fn()} searchShortcutQuery="dog" searchShortcutRequestId={1} />)

    await waitFor(() => {
      expect(searchGiphyMock).toHaveBeenCalledWith({
        query: 'dog',
        offset: 0,
      })
    }, { timeout: 3000 })
  })

  it('loads the next result page when show more is clicked', async () => {
    searchGiphyMock
      .mockResolvedValueOnce({
        data: Array.from({ length: GIPHY_PAGE_SIZE }, (_, index) => createGif(index + 1)),
        pagination: {
          count: GIPHY_PAGE_SIZE,
          offset: 0,
          totalCount: GIPHY_PAGE_SIZE * 2,
        },
      })
      .mockResolvedValueOnce({
        data: Array.from({ length: GIPHY_PAGE_SIZE }, (_, index) => createGif(index + GIPHY_PAGE_SIZE + 1)),
        pagination: {
          count: GIPHY_PAGE_SIZE,
          offset: GIPHY_PAGE_SIZE,
          totalCount: GIPHY_PAGE_SIZE * 2,
        },
      })

    render(<GiphyVotingPanel onVoteSubmit={vi.fn()} />)

    fireEvent.change(screen.getByPlaceholderText('Search for a GIF...'), {
      target: { value: 'cat' },
    })

    await waitFor(() => {
      expect(searchGiphyMock).toHaveBeenCalledWith({
        query: 'cat',
        offset: 0,
      })
    }, { timeout: 3000 })

    fireEvent.click(await screen.findByRole('button', { name: 'Show More' }))

    await waitFor(() => {
      expect(searchGiphyMock).toHaveBeenLastCalledWith({
        query: 'cat',
        offset: GIPHY_PAGE_SIZE,
      })
    })

    expect(await screen.findByAltText('GIF 21')).toBeInTheDocument()
  })

  it('toggles GIF favorites from the search results', async () => {
    searchGiphyMock.mockResolvedValue({
      data: [createGif(1)],
      pagination: {
        count: 1,
        offset: 0,
        totalCount: 1,
      },
    })

    render(<GiphyVotingPanel onVoteSubmit={vi.fn()} />)

    fireEvent.change(screen.getByPlaceholderText('Search for a GIF...'), {
      target: { value: 'cat' },
    })

    await waitFor(() => {
      expect(searchGiphyMock).toHaveBeenCalledWith({
        query: 'cat',
        offset: 0,
      })
    }, { timeout: 3000 })

    await screen.findByAltText('GIF 1')
    const favoriteButton = await screen.findByRole('button', {
      name: 'Add GIF 1 to favorites',
    })
    fireEvent.click(favoriteButton)

    await waitFor(() => {
      expect(setStoredFavoriteGifIdsMock).toHaveBeenLastCalledWith(['gif-1'])
    })
  })

  it('shows and collapses the favorite GIF section', () => {
    getStoredFavoriteGifIdsMock.mockReturnValue(['favorite-1'])

    render(<GiphyVotingPanel onVoteSubmit={vi.fn()} />)
    const favoritesToggle = screen.getByRole('button', { name: 'Toggle favorite GIFs' })

    expect(screen.getByAltText('Favorite GIF favorite-1')).toBeInTheDocument()
    expect(favoritesToggle).toHaveAttribute('aria-expanded', 'true')
    fireEvent.click(favoritesToggle)
    expect(favoritesToggle).toHaveAttribute('aria-expanded', 'false')
    fireEvent.click(favoritesToggle)
    expect(favoritesToggle).toHaveAttribute('aria-expanded', 'true')
  })

  it('starts collapsed when favorite GIF section expansion is stored as false', () => {
    getStoredFavoriteGifIdsMock.mockReturnValue(['favorite-1'])
    getStoredFavoriteGifsExpandedMock.mockReturnValue(false)

    render(<GiphyVotingPanel onVoteSubmit={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Toggle favorite GIFs' })).toHaveAttribute('aria-expanded', 'false')
  })

  it('persists favorite GIF section expansion changes', () => {
    getStoredFavoriteGifIdsMock.mockReturnValue(['favorite-1'])

    render(<GiphyVotingPanel onVoteSubmit={vi.fn()} />)
    const favoritesToggle = screen.getByRole('button', { name: 'Toggle favorite GIFs' })

    fireEvent.click(favoritesToggle)
    expect(setStoredFavoriteGifsExpandedMock).toHaveBeenLastCalledWith(false)

    fireEvent.click(favoritesToggle)
    expect(setStoredFavoriteGifsExpandedMock).toHaveBeenLastCalledWith(true)
  })
})
