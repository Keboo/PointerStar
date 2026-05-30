import React, { useCallback, useEffect, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  ImageList,
  ImageListItem,
  ImageListItemBar,
  Paper,
  TextField,
  Typography,
} from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import SearchIcon from '@mui/icons-material/Search'
import type { GiphyItem } from '../types/contracts'
import { GIPHY_PAGE_SIZE, searchGiphy } from '../services/giphyApi'

interface GiphyVotingPanelProps {
  currentVote?: string | null
  onVoteSubmit: (giphyId: string) => void
}

/**
 * Component for searching and selecting Giphy images as votes.
 * Displays search results as a grid of clickable images.
 * Searches are debounced to avoid API calls on every keystroke.
 */
export const GiphyVotingPanel: React.FC<GiphyVotingPanelProps> = ({ currentVote, onVoteSubmit }) => {
  const [searchQuery, setSearchQuery] = useState('')
  const [results, setResults] = useState<GiphyItem[]>([])
  const [loading, setLoading] = useState(false)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [isSearchExpanded, setIsSearchExpanded] = useState(!currentVote)
  const [activeQuery, setActiveQuery] = useState('')
  const [nextOffset, setNextOffset] = useState(0)
  const [hasMoreResults, setHasMoreResults] = useState(false)
  const requestIdRef = useRef(0)

  const handleSearch = useCallback(
    async (query: string, offset = 0, append = false) => {
      const trimmedQuery = query.trim()
      if (!trimmedQuery) {
        setResults([])
        setError(null)
        setActiveQuery('')
        setNextOffset(0)
        setHasMoreResults(false)
        return
      }

      if (append) {
        setLoadingMore(true)
      } else {
        setLoading(true)
        setResults([])
        setNextOffset(0)
        setHasMoreResults(false)
        setActiveQuery(trimmedQuery)
      }
      setError(null)

      try {
        const requestId = ++requestIdRef.current
        const response = await searchGiphy({
          query: trimmedQuery,
          offset,
        })
        if (requestId !== requestIdRef.current) {
          return
        }

        const nextPageResults = response.data ?? []
        const responseCount = response.pagination?.count ?? nextPageResults.length
        const responseOffset = response.pagination?.offset ?? offset
        const calculatedNextOffset = responseOffset + responseCount
        const knownTotal = response.pagination?.totalCount

        setNextOffset(calculatedNextOffset)
        setHasMoreResults(
          knownTotal !== undefined
            ? calculatedNextOffset < knownTotal
            : nextPageResults.length === GIPHY_PAGE_SIZE,
        )

        if (append) {
          setResults((currentResults) => [...currentResults, ...nextPageResults])
        } else {
          setResults(nextPageResults)
        }

        if (!append && nextPageResults.length === 0) {
          setError('No GIFs found. Try a different search.')
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Search failed'
        setError(errorMessage)
        if (!append) {
          setResults([])
          setNextOffset(0)
          setHasMoreResults(false)
        }
      } finally {
        setLoading(false)
        setLoadingMore(false)
      }
    },
    []
  )

  const clearSearch = useCallback(() => {
    requestIdRef.current += 1
    setSearchQuery('')
    setResults([])
    setError(null)
    setLoading(false)
    setLoadingMore(false)
    setActiveQuery('')
    setNextOffset(0)
    setHasMoreResults(false)
  }, [])

  useEffect(() => {
    if (!isSearchExpanded) {
      return
    }

    const timeout = setTimeout(() => {
      void handleSearch(searchQuery)
    }, 1000)

    return () => {
      clearTimeout(timeout)
    }
  }, [handleSearch, isSearchExpanded, searchQuery])

  useEffect(() => {
    if (currentVote) {
      setSelectedId(currentVote)
      setIsSearchExpanded(false)
      return
    }

    setSelectedId(null)
    setIsSearchExpanded(true)
    clearSearch()
  }, [clearSearch, currentVote])

  const handleSelectGif = (giphyId: string) => {
    setSelectedId(giphyId)
    setIsSearchExpanded(false)
    onVoteSubmit(giphyId)
  }

  const handleShowMore = useCallback(() => {
    if (loading || loadingMore || !hasMoreResults || !activeQuery) {
      return
    }

    void handleSearch(activeQuery, nextOffset, true)
  }, [activeQuery, handleSearch, hasMoreResults, loading, loadingMore, nextOffset])

  return (
    <Box>
      {!isSearchExpanded ? (
        <Alert
          action={
            <Button onClick={() => setIsSearchExpanded(true)} size="small">
              Change GIF
            </Button>
          }
          severity="success"
          sx={{ marginBottom: 2 }}
        >
          Vote submitted
        </Alert>
      ) : (
        <>
          <Box sx={{ marginBottom: 2 }}>
            <TextField
              autoFocus
              fullWidth
              placeholder="Search for a GIF..."
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value)
                setError(null)
              }}
              onKeyDown={(event) => {
                if (event.key === 'Escape') {
                  event.preventDefault()
                  clearSearch()
                }
              }}
              slotProps={{
                input: {
                  startAdornment: <SearchIcon sx={{ marginRight: 1, color: 'action.active' }} />,
                  endAdornment: searchQuery ? (
                    <IconButton aria-label="Clear GIF search" edge="end" onClick={clearSearch} size="small">
                      <CloseIcon fontSize="small" />
                    </IconButton>
                  ) : null,
                },
              }}
            />
          </Box>

          {error && (
            <Alert severity="error" sx={{ marginBottom: 2 }}>
              {error}
            </Alert>
          )}

          {loading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', padding: 4 }}>
              <CircularProgress />
            </Box>
          )}

          {!loading && results.length > 0 && (
            <Paper sx={{ padding: 1 }}>
              <ImageList variant="masonry" cols={3} gap={8}>
                {results.map((gif) => (
                  <ImageListItem
                    key={gif.id}
                    sx={{
                      cursor: 'pointer',
                      opacity: selectedId === gif.id ? 1 : 0.8,
                      border: selectedId === gif.id ? '3px solid' : '1px solid',
                      borderColor: selectedId === gif.id ? 'primary.main' : 'divider',
                      transition: 'all 0.2s ease',
                      '&:hover': {
                        opacity: 1,
                        border: '3px solid',
                        borderColor: 'primary.main',
                      },
                    }}
                    onClick={() => handleSelectGif(gif.id)}
                  >
                    <img
                      src={gif.imageUrl}
                      alt={gif.title}
                      loading="lazy"
                      style={{ cursor: 'pointer', display: 'block', width: '100%' }}
                    />
                    <ImageListItemBar
                      title={gif.title}
                      position="below"
                      sx={{ fontSize: '0.7rem' }}
                    />
                  </ImageListItem>
                ))}
              </ImageList>
              {hasMoreResults ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', pb: 1, pt: 2 }}>
                  <Button
                    onClick={handleShowMore}
                    disabled={loadingMore}
                    variant="outlined"
                  >
                    {loadingMore ? 'Loading…' : 'Show More'}
                  </Button>
                </Box>
              ) : null}
            </Paper>
          )}

          {!loading && results.length === 0 && searchQuery.trim() && !error && (
            <Box sx={{ textAlign: 'center', padding: 2 }}>
              <Typography color="textSecondary">No GIFs found. Try a different search.</Typography>
            </Box>
          )}
        </>
      )}
    </Box>
  )
}
