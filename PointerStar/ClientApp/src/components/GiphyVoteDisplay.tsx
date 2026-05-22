import React, { useState } from 'react'
import {
  ImageList,
  ImageListItem,
  ImageListItemBar,
  Paper,
  Box,
  Typography,
  CircularProgress,
} from '@mui/material'
import ErrorIcon from '@mui/icons-material/Error'
import { User } from '../types/contracts'

interface GiphyVoteDisplayProps {
  users: User[]
  showVotes: boolean
}

/**
 * Component for displaying user Giphy votes as a grid of images.
 * Shows a placeholder if votes are not shown yet.
 */
export const GiphyVoteDisplay: React.FC<GiphyVoteDisplayProps> = ({ users, showVotes }) => {
  const [imageErrors, setImageErrors] = useState<Set<string>>(new Set())

  // Filter users who have voted with Giphy IDs
  const votedUsers = users.filter((user) => user.vote && showVotes)

  const handleImageError = (giphyId: string) => {
    setImageErrors((prev) => new Set(prev).add(giphyId))
  }

  if (!showVotes) {
    return (
      <Box sx={{ textAlign: 'center', padding: 4 }}>
        <CircularProgress />
        <Typography sx={{ marginTop: 2 }} color="textSecondary">
          Waiting for all team members to vote...
        </Typography>
      </Box>
    )
  }

  if (votedUsers.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', padding: 4 }}>
        <Typography color="textSecondary">No votes submitted yet</Typography>
      </Box>
    )
  }

  return (
    <Paper sx={{ padding: 2 }}>
      <ImageList cols={Math.min(3, Math.max(1, votedUsers.length))} gap={16}>
        {votedUsers.map((user) => {
          const giphyId = user.vote || ''
          const isErrored = imageErrors.has(giphyId)
          const imageUrl = `https://media.giphy.com/media/${giphyId}/giphy.gif`

          return (
            <ImageListItem key={user.id}>
              {isErrored ? (
                <Box
                  sx={{
                    width: '100%',
                    aspectRatio: '1/1',
                    backgroundColor: '#f5f5f5',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexDirection: 'column',
                  }}
                >
                  <ErrorIcon sx={{ fontSize: 48, color: 'error.main', marginBottom: 1 }} />
                  <Typography variant="caption" color="error">
                    Image failed to load
                  </Typography>
                </Box>
              ) : (
                <img
                  src={imageUrl}
                  alt={`Vote by ${user.name}`}
                  loading="lazy"
                  onError={() => handleImageError(giphyId)}
                  style={{ width: '100%', height: 'auto', minHeight: 200 }}
                />
              )}
              <ImageListItemBar
                title={user.name}
                position="bottom"
                sx={{
                  background: 'linear-gradient(to top, rgba(0,0,0,0.7) 0%, rgba(0,0,0,0) 100%)',
                }}
              />
            </ImageListItem>
          )
        })}
      </ImageList>
    </Paper>
  )
}
