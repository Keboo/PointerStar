import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Button,
  Card,
  CardContent,
  CardHeader,
  Container,
  Divider,
  IconButton,
  Snackbar,
  Stack,
  TextField,
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
  const [roomCode, setRoomCode] = useState('')

  useEffect(() => {
    setRecentRooms(getRecentRooms())
  }, [])

  const openRoom = (roomId: string) => {
    const normalizedRoomId = roomId.trim()
    if (!normalizedRoomId) {
      return
    }

    navigate(`/room/${encodeURIComponent(normalizedRoomId)}`)
  }

  return (
    <Container maxWidth="md" sx={{ mt: { md: 8, xs: 4 } }}>
      <Stack spacing={4}>
        <Stack spacing={1} sx={{ alignItems: 'center', justifyContent: 'center' }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <img
              alt="Pointer* logo"
              src={`${import.meta.env.BASE_URL}favicon.svg`}
              style={{
                borderRadius: 6,
                display: 'block',
                height: 40,
                width: 40,
              }}
            />
            <Typography component="h1" variant="h4">
              Pointer*
            </Typography>
          </Stack>
          <Typography align="center" color="text.secondary" sx={{ maxWidth: 560 }} variant="body1">
            Run fast, focused estimation sessions with your team from any device.
          </Typography>
        </Stack>

        <Stack sx={{ alignItems: 'center', justifyContent: 'center' }}>
          {notFound ? (
            <Typography align="center" variant="h5">
              Sorry, there&apos;s nothing at this address.
            </Typography>
          ) : (
            <Card sx={{ width: '100%' }}>
              <CardHeader title="Start or join a room" />
              <CardContent>
                <Stack spacing={2.5}>
                  <Button
                    onClick={() => {
                      void generateRoomId()
                        .then((roomId) => openRoom(roomId))
                        .catch((error) => {
                          console.error('Unable to create a room.', error)
                          setErrorMessage('Unable to create a room right now.')
                        })
                    }}
                    size="large"
                    sx={{ minHeight: 56, width: { md: 'fit-content', xs: '100%' } }}
                    variant="contained"
                  >
                    Create New Room
                  </Button>

                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <Divider sx={{ flexGrow: 1 }} />
                    <Typography color="text.secondary" sx={{ px: 1 }} variant="body2">
                      or
                    </Typography>
                    <Divider sx={{ flexGrow: 1 }} />
                  </Stack>

                  <Stack direction={{ sm: 'row', xs: 'column' }} spacing={1.5}>
                    <TextField
                      fullWidth
                      label="Room code"
                      onChange={(event) => setRoomCode(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter') {
                          event.preventDefault()
                          openRoom(roomCode)
                        }
                      }}
                      placeholder="Enter room code"
                      value={roomCode}
                    />
                    <Button
                      disabled={!roomCode.trim()}
                      onClick={() => openRoom(roomCode)}
                      size="large"
                      sx={{ minHeight: 56, minWidth: { sm: 130 }, width: { sm: 'auto', xs: '100%' } }}
                      variant="outlined"
                    >
                      Join Room
                    </Button>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
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
                        <Button onClick={() => openRoom(room.roomId)} size="small" variant="contained">
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
