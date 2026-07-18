import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Collapse,
  CircularProgress,
  IconButton,
  ImageList,
  ImageListItem,
  ImageListItemBar,
  Paper,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import ExpandLessIcon from '@mui/icons-material/ExpandLess'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import SearchIcon from '@mui/icons-material/Search'
import type { GiphyItem } from '../types/contracts'
import { getStoredFavoriteGifIds, setStoredFavoriteGifIds } from '../services/cookies'
import { GIPHY_PAGE_SIZE, searchGiphy } from '../services/giphyApi'

interface GiphyVotingPanelProps {
  currentVote?: string | null
  onSearch?: (query: string) => void
  onVoteSubmit: (giphyId: string) => void
  searchShortcutQuery?: string | null
  searchShortcutRequestId?: number
}

/**
 * Component for searching and selecting Giphy images as votes.
 * Displays search results as a grid of clickable images.
 * Searches are debounced to avoid API calls on every keystroke.
 */
export const GiphyVotingPanel: React.FC<GiphyVotingPanelProps> = ({
  currentVote,
  onSearch,
  onVoteSubmit,
  searchShortcutQuery,
  searchShortcutRequestId,
}) => {
  const theme = useTheme()
  const isSmUp = useMediaQuery(theme.breakpoints.up('sm'))
  const isMdUp = useMediaQuery(theme.breakpoints.up('md'))

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
  const [favoriteGifIds, setFavoriteGifIds] = useState<string[]>(() => getStoredFavoriteGifIds())
  const [areFavoritesExpanded, setAreFavoritesExpanded] = useState(true)
  const requestIdRef = useRef(0)
  const favoriteGifSet = useMemo(() => new Set(favoriteGifIds), [favoriteGifIds])
  const favoriteGifs = useMemo(
    () => favoriteGifIds.map((gifId) => ({
      id: gifId,
      title: `Favorite GIF ${gifId}`,
      imageUrl: `https://media.giphy.com/media/${gifId}/giphy.gif`,
    })),
    [favoriteGifIds],
  )

  useEffect(() => {
    setStoredFavoriteGifIds(favoriteGifIds)
  }, [favoriteGifIds])

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
        onSearch?.(trimmedQuery)
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
    [onSearch]
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

  useEffect(() => {
    const trimmedShortcutQuery = searchShortcutQuery?.trim()
    if (!trimmedShortcutQuery || !searchShortcutRequestId) {
      return
    }

    setIsSearchExpanded(true)
    setSearchQuery(trimmedShortcutQuery)
    setError(null)
  }, [searchShortcutQuery, searchShortcutRequestId])

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

  const handleToggleFavoriteGif = useCallback((gifId: string) => {
    setFavoriteGifIds((currentIds) => (
      currentIds.includes(gifId)
        ? currentIds.filter((currentId) => currentId !== gifId)
        : [gifId, ...currentIds]
    ))
  }, [])

  const giphyColumns = isMdUp ? 4 : isSmUp ? 3 : 2

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
          <Paper sx={{ marginBottom: 2, padding: 1.5 }} variant="outlined">
            <Box sx={{ alignItems: 'center', display: 'flex', justifyContent: 'space-between' }}>
              <Typography variant="subtitle2">Favorite GIFs ({favoriteGifIds.length})</Typography>
              <Button
                endIcon={areFavoritesExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                onClick={() => setAreFavoritesExpanded((expanded) => !expanded)}
                size="small"
                variant="text"
              >
                {areFavoritesExpanded ? 'Hide' : 'Show'}
              </Button>
            </Box>
            <Collapse in={areFavoritesExpanded}>
              {favoriteGifs.length > 0 ? (
                <ImageList cols={giphyColumns} gap={8} sx={{ marginTop: 1 }} variant="masonry">
                  {favoriteGifs.map((gif) => (
                    <ImageListItem
                      aria-label={`Select GIF: ${gif.title}`}
                      aria-pressed={selectedId === gif.id}
                      key={`favorite-${gif.id}`}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault()
                          handleSelectGif(gif.id)
                        }
                      }}
                      sx={{
                        '&:focus-visible': {
                          outline: '2px solid',
                          outlineColor: 'primary.main',
                          outlineOffset: 2,
                        },
                        border: selectedId === gif.id ? '3px solid' : '1px solid',
                        borderColor: selectedId === gif.id ? 'primary.main' : 'divider',
                        cursor: 'pointer',
                        opacity: selectedId === gif.id ? 1 : 0.9,
                        position: 'relative',
                        transition: 'all 0.2s ease',
                        '&:hover': {
                          border: '3px solid',
                          borderColor: 'primary.main',
                          opacity: 1,
                        },
                      }}
                      onClick={() => handleSelectGif(gif.id)}
                      role="button"
                      tabIndex={0}
                    >
                      <IconButton
                        aria-label={`Remove ${gif.title} from favorites`}
                        onClick={(event) => {
                          event.preventDefault()
                          event.stopPropagation()
                          handleToggleFavoriteGif(gif.id)
                        }}
                        size="small"
                        sx={{
                          backgroundColor: 'rgba(0, 0, 0, 0.45)',
                          color: 'common.white',
                          position: 'absolute',
                          right: 4,
                          top: 4,
                          zIndex: 1,
                          '&:hover': {
                            backgroundColor: 'rgba(0, 0, 0, 0.6)',
                          },
                        }}
                      >
                        <FavoriteIcon fontSize="small" />
                      </IconButton>
                      <img
                        src={gif.imageUrl}
                        alt={gif.title}
                        loading="lazy"
                        style={{ cursor: 'pointer', display: 'block', width: '100%' }}
                      />
                    </ImageListItem>
                  ))}
                </ImageList>
              ) : (
                <Typography color="text.secondary" sx={{ marginTop: 1 }} variant="body2">
                  No favorite GIFs yet.
                </Typography>
              )}
            </Collapse>
          </Paper>

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
              <ImageList cols={giphyColumns} gap={8} variant="masonry">
                {results.map((gif) => (
                  <ImageListItem
                    aria-label={`Select GIF: ${gif.title}`}
                    aria-pressed={selectedId === gif.id}
                    key={gif.id}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault()
                        handleSelectGif(gif.id)
                      }
                    }}
                    sx={{
                      '&:focus-visible': {
                        outline: '2px solid',
                        outlineColor: 'primary.main',
                        outlineOffset: 2,
                      },
                      cursor: 'pointer',
                      opacity: selectedId === gif.id ? 1 : 0.8,
                      border: selectedId === gif.id ? '3px solid' : '1px solid',
                      borderColor: selectedId === gif.id ? 'primary.main' : 'divider',
                      position: 'relative',
                      transition: 'all 0.2s ease',
                      '&:hover': {
                        opacity: 1,
                        border: '3px solid',
                        borderColor: 'primary.main',
                      },
                    }}
                    onClick={() => handleSelectGif(gif.id)}
                    role="button"
                    tabIndex={0}
                  >
                    <IconButton
                      aria-label={`${favoriteGifSet.has(gif.id) ? 'Remove' : 'Add'} ${gif.title} ${favoriteGifSet.has(gif.id) ? 'from' : 'to'} favorites`}
                      onClick={(event) => {
                        event.preventDefault()
                        event.stopPropagation()
                        handleToggleFavoriteGif(gif.id)
                      }}
                      size="small"
                      sx={{
                        backgroundColor: 'rgba(0, 0, 0, 0.45)',
                        color: favoriteGifSet.has(gif.id) ? 'error.light' : 'common.white',
                        position: 'absolute',
                        right: 4,
                        top: 4,
                        zIndex: 1,
                        '&:hover': {
                          backgroundColor: 'rgba(0, 0, 0, 0.6)',
                        },
                      }}
                    >
                      {favoriteGifSet.has(gif.id) ? <FavoriteIcon fontSize="small" /> : <FavoriteBorderIcon fontSize="small" />}
                    </IconButton>
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
