import React, { useState, useCallback } from 'react'
import {
  Box,
  TextField,
  CircularProgress,
  Alert,
  Paper,
  Typography,
  ImageList,
  ImageListItem,
  ImageListItemBar,
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import type { GiphyItem } from '../types/contracts'
import { searchGiphy } from '../services/giphyApi'

interface GiphyVotingPanelProps {
  onVoteSubmit: (giphyId: string) => void
  disabled?: boolean
}

/**
 * Component for searching and selecting Giphy images as votes.
 * Displays search results as a grid of clickable images.
 */
export const GiphyVotingPanel: React.FC<GiphyVotingPanelProps> = ({ onVoteSubmit, disabled }) => {
  const [searchQuery, setSearchQuery] = useState('')
  const [results, setResults] = useState<GiphyItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const handleSearch = useCallback(
    async (query: string) => {
      if (!query.trim()) {
        setResults([])
        setError(null)
        return
      }

      setLoading(true)
      setError(null)

      try {
        const response = await searchGiphy(query)
        setResults(response.data || [])

        if (response.data?.length === 0) {
          setError('No GIFs found. Try a different search.')
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Search failed'
        setError(errorMessage)
        setResults([])
      } finally {
        setLoading(false)
      }
    },
    []
  )

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
    setSearchQuery(value)
    handleSearch(value)
  }

  const handleSelectGif = (giphyId: string) => {
    setSelectedId(giphyId)
    onVoteSubmit(giphyId)
  }

  return (
    <Box sx={{ padding: 2 }}>
      <Box sx={{ marginBottom: 2 }}>
        <TextField
          fullWidth
          placeholder="Search for a GIF..."
          value={searchQuery}
          onChange={handleSearchChange}
          disabled={disabled || loading}
          slotProps={{
            input: {
              startAdornment: <SearchIcon sx={{ marginRight: 1, color: 'action.active' }} />,
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
          <ImageList cols={Math.min(3, Math.max(1, results.length))} gap={8}>
            {results.map((gif) => (
              <ImageListItem
                key={gif.id}
                sx={{
                  cursor: 'pointer',
                  opacity: selectedId === gif.id ? 1 : 0.8,
                  border: selectedId === gif.id ? '3px solid primary.main' : '1px solid #ddd',
                  transition: 'all 0.2s ease',
                  '&:hover': {
                    opacity: 1,
                    border: '3px solid primary.main',
                  },
                }}
                onClick={() => handleSelectGif(gif.id)}
              >
                <img
                  src={gif.imageUrl}
                  alt={gif.title}
                  loading="lazy"
                  style={{ cursor: 'pointer' }}
                />
                <ImageListItemBar
                  title={gif.title}
                  position="bottom"
                  sx={{
                    background: 'linear-gradient(to top, rgba(0,0,0,0.7) 0%, rgba(0,0,0,0) 100%)',
                  }}
                />
              </ImageListItem>
            ))}
          </ImageList>
        </Paper>
      )}

      {!loading && results.length === 0 && searchQuery.trim() && !error && (
        <Box sx={{ textAlign: 'center', padding: 2 }}>
          <Typography color="textSecondary">Enter a search term to find GIFs</Typography>
        </Box>
      )}
    </Box>
  )
}
