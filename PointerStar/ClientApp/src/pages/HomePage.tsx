import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Button,
  Card,
  CardContent,
  Container,
  Divider,
  IconButton,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material'
import { Delete as DeleteIcon } from '@mui/icons-material'

import { generateRoomId } from '../services/roomApi'
import { getRecentRooms, removeRecentRoom } from '../services/recentRooms'
import type { RecentRoom } from '../types/contracts'

function getRelativeTime(utcValue: string) {
  const timeSpanMs = Date.now() - new Date(utcValue).getTime()
  const totalMinutes = Math.floor(timeSpanMs / 60_000)
  const totalHours = Math.floor(timeSpanMs / 3_600_000)
  const totalDays = Math.floor(timeSpanMs / 86_400_000)

  if (totalMinutes < 1) {
    return 'just now'
  }

  if (totalMinutes < 60) {
    return `${totalMinutes} minute${totalMinutes === 1 ? '' : 's'} ago`
  }

  if (totalHours < 24) {
    return `${totalHours} hour${totalHours === 1 ? '' : 's'} ago`
  }

  if (totalDays < 30) {
    return `${totalDays} day${totalDays === 1 ? '' : 's'} ago`
  }

  return new Date(utcValue).toLocaleDateString(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

interface HomePageProps {
  notFound?: boolean
}

export function HomePage({ notFound = false }: HomePageProps) {
  const navigate = useNavigate()
  const [recentRooms, setRecentRooms] = useState<RecentRoom[]>([])
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    setRecentRooms(getRecentRooms())
  }, [])

  return (
    <Container maxWidth="md" sx={{ mt: 8 }}>
      <Stack spacing={4}>
        <Stack sx={{ alignItems: 'center', justifyContent: 'center' }}>
          {notFound ? (
            <Typography align="center" variant="h5">
              Sorry, there&apos;s nothing at this address.
            </Typography>
          ) : (
            <Button
              onClick={() => {
                void generateRoomId()
                  .then((roomId) => navigate(`/room/${roomId}`))
                  .catch((error) => {
                    console.error('Unable to create a room.', error)
                    setErrorMessage('Unable to create a room right now.')
                  })
              }}
              size="large"
              variant="contained"
            >
              Create New Room
            </Button>
          )}
        </Stack>

        {!notFound && recentRooms.length > 0 ? (
          <>
            <Divider />
            <Typography align="center" variant="h6">
              Recent Rooms
            </Typography>
            <Stack spacing={2}>
              {recentRooms.map((room) => (
                <Card key={room.roomId}>
                  <CardContent>
                    <Stack
                      direction={{ md: 'row', xs: 'column' }}
                      spacing={2}
                      sx={{
                        alignItems: { md: 'center', xs: 'flex-start' },
                        justifyContent: 'space-between',
                      }}
                    >
                      <Stack spacing={1}>
                        <Typography variant="body1">Room: {room.roomId}</Typography>
                        <Typography color="text.secondary" variant="body2">
                          Last accessed: {getRelativeTime(room.lastAccessed)}
                        </Typography>
                      </Stack>
                      <Stack direction="row" spacing={2}>
                        <Button onClick={() => navigate(`/room/${room.roomId}`)} size="small" variant="contained">
                          Open
                        </Button>
                        <IconButton
                          color="error"
                          onClick={() => {
                            removeRecentRoom(room.roomId)
                            setRecentRooms(getRecentRooms())
                          }}
                          size="small"
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Stack>
                    </Stack>
                  </CardContent>
                </Card>
              ))}
            </Stack>
          </>
        ) : null}
      </Stack>

      <Snackbar autoHideDuration={4_000} onClose={() => setErrorMessage(null)} open={Boolean(errorMessage)}>
        <Alert onClose={() => setErrorMessage(null)} severity="error" variant="filled">
          {errorMessage}
        </Alert>
      </Snackbar>
    </Container>
  )
}
