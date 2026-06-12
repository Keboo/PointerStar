import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { GiphyVotingPanel } from './GiphyVotingPanel'
import { GIPHY_PAGE_SIZE, searchGiphy } from '../services/giphyApi'

vi.mock('../services/giphyApi', () => ({
  GIPHY_PAGE_SIZE: 20,
  searchGiphy: vi.fn(),
}))

const searchGiphyMock = vi.mocked(searchGiphy)

const createGif = (index: number) => ({
  id: `gif-${index}`,
  title: `GIF ${index}`,
  imageUrl: `https://example.test/gif-${index}.gif`,
})

describe('GiphyVotingPanel', () => {
  beforeEach(() => {
    searchGiphyMock.mockReset()
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
})
