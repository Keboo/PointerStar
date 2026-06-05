import React, { useState } from 'react'
import {
  Box,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Paper,
  Typography,
} from '@mui/material'
import ErrorIcon from '@mui/icons-material/Error'
import type { User } from '../types/contracts'

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
  const [selectedGif, setSelectedGif] = useState<{ name: string; url: string } | null>(null)
  const voteCardWidth = { xs: 132, sm: 150, md: 184, lg: 216 }
  const voteCardHeight = { xs: 108, sm: 120, md: 136, lg: 152 }

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
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: { xs: 1, sm: 1.5, md: 2 } }}>
        {votedUsers.map((user) => {
          const giphyId = user.vote || ''
          const isErrored = imageErrors.has(giphyId)
          const hasChangedVote = Boolean(user.originalVote && user.originalVote !== user.vote)
          const imageUrl = `https://media.giphy.com/media/${giphyId}/giphy.gif`

          return (
            <Box
              key={user.id}
              sx={{
                display: 'flex',
                flexDirection: 'column',
                width: voteCardWidth,
              }}
            >
              {isErrored ? (
                <Box
                  sx={{
                    alignItems: 'center',
                    backgroundColor: '#f5f5f5',
                    borderRadius: 1,
                    display: 'flex',
                    flexDirection: 'column',
                    height: voteCardHeight,
                    justifyContent: 'center',
                    width: '100%',
                  }}
                >
                  <ErrorIcon sx={{ fontSize: 32, color: 'error.main', marginBottom: 1 }} />
                  <Typography variant="caption" color="error">
                    Image failed to load
                  </Typography>
                </Box>
              ) : (
                <Box
                  component="button"
                  onClick={() => setSelectedGif({ name: user.name, url: imageUrl })}
                  sx={{
                    alignItems: 'center',
                    appearance: 'none',
                    backgroundColor: 'transparent',
                    borderRadius: 1,
                    border: hasChangedVote ? '2px dashed' : 0,
                    borderColor: hasChangedVote ? 'error.main' : 'transparent',
                    color: 'inherit',
                    cursor: 'pointer',
                    display: 'flex',
                    height: voteCardHeight,
                    justifyContent: 'center',
                    overflow: 'hidden',
                    p: 0,
                    '&:hover': {
                      backgroundColor: 'transparent',
                    },
                    width: '100%',
                  }}
                >
                  <img
                    src={imageUrl}
                    alt={`Vote by ${user.name}`}
                    loading="lazy"
                    onError={() => handleImageError(giphyId)}
                    style={{
                      borderRadius: 4,
                      display: 'block',
                      height: 'auto',
                      maxHeight: '100%',
                      maxWidth: '100%',
                      width: 'auto',
                    }}
                  />
                </Box>
              )}
              <Typography
                variant="caption"
                sx={{
                  display: 'block',
                  lineHeight: 1.3,
                  mt: 0.75,
                }}
              >
                {user.name}
              </Typography>
            </Box>
          )
        })}
      </Box>
      <Dialog
        maxWidth={false}
        onClose={() => setSelectedGif(null)}
        open={selectedGif !== null}
      >
        {selectedGif ? <DialogTitle>{selectedGif.name}'s vote</DialogTitle> : null}
        <DialogContent sx={{ alignItems: 'center', display: 'flex', justifyContent: 'center', p: 2 }}>
          {selectedGif ? (
            <img
              alt={`${selectedGif.name} vote`}
              src={selectedGif.url}
              style={{
                height: 'auto',
                maxHeight: '85vh',
                maxWidth: '90vw',
                width: 'auto',
              }}
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </Paper>
  )
}
