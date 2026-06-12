import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useParams, useSearchParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Chip,
  Collapse,
  Container,
  FormControlLabel,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Paper,
  Snackbar,
  Stack,
  Switch,
  Tooltip,
  Typography,
  type AlertColor,
} from '@mui/material'
import {
  ContentCopy as CopyIcon,
  Edit as EditIcon,
  ExpandLess as ExpandLessIcon,
  ExpandMore as ExpandMoreIcon,
  MoreVert as MoreVertIcon,
  PersonRemove as RemoveUserIcon,
  Refresh as RefreshIcon,
  Settings as SettingsIcon,
} from '@mui/icons-material'
import { QRCodeSVG } from 'qrcode.react'

import { UserDialog } from '../components/UserDialog'
import { VotingOptionsDialog } from '../components/VotingOptionsDialog'
import { GiphyVotingPanel } from '../components/GiphyVotingPanel'
import { GiphyVoteDisplay } from '../components/GiphyVoteDisplay'
import {
  getStoredName,
  getStoredRecentGifSearches,
  getStoredRoleId,
  getStoredRoomId,
  getStoredVoteOptions,
  setStoredName,
  setStoredRecentGifSearches,
  setStoredRoleId,
  setStoredRoomId,
  setStoredVoteOptions,
} from '../services/cookies'
import { addRecentRoom } from '../services/recentRooms'
import { RoomHubClient } from '../services/roomHubClient'
import { getOrCreateUserId } from '../services/userIdentity'
import {
  defaultVoteOptions,
  roleFromId,
  roles,
  type Role,
  type RoomOptions,
  type RoomState,
  type User,
  type UserOptions,
  VotingMode,
} from '../types/contracts'
import { getElapsedTimeLabel } from './roomTime'

function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === 'AbortError'
}

function isRole(user: User | null | undefined, role: Role) {
  return user?.role.id === role.id
}

export function RoomPage() {
  const { roomId = '' } = useParams()
  const [searchParams] = useSearchParams()
  const preferredName = searchParams.get('Name') ?? ''

  const [roomState, setRoomState] = useState<RoomState | null>(null)
  const [currentUserId, setCurrentUserId] = useState('')
  const [name, setName] = useState(() => preferredName || getStoredName())
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(getStoredRoleId())
  const [votesShown, setVotesShown] = useState(false)
  const [previewVotes, setPreviewVotes] = useState(true)
  const [autoShowVotes, setAutoShowVotes] = useState(false)
  const [facilitatorCanVote, setFacilitatorCanVote] = useState(false)
  const [voteStartTime, setVoteStartTime] = useState<string | null>(null)
  const [resetVotesRequestedAt, setResetVotesRequestedAt] = useState<string | null>(null)
  const [resetVotesRequestedBy, setResetVotesRequestedBy] = useState<string | null>(null)
  const [serverClockOffsetMs, setServerClockOffsetMs] = useState(0)
  const [showQrCode, setShowQrCode] = useState(false)
  const [showAdvancedFacilitatorControls, setShowAdvancedFacilitatorControls] = useState(false)
  const [mobileActionsAnchorEl, setMobileActionsAnchorEl] = useState<HTMLElement | null>(null)
  const [showUserDialog, setShowUserDialog] = useState(false)
  const [showVotingOptionsDialog, setShowVotingOptionsDialog] = useState(false)
  const [timerTick, setTimerTick] = useState(0)
  const [recentGifSearches, setRecentGifSearches] = useState<string[]>(() => getStoredRecentGifSearches())
  const [selectedGifSearchQuery, setSelectedGifSearchQuery] = useState<string | null>(null)
  const [selectedGifSearchRequestId, setSelectedGifSearchRequestId] = useState(0)
  const [snackbar, setSnackbar] = useState<{
    message: string
    open: boolean
    severity: AlertColor
  }>({
    message: '',
    open: false,
    severity: 'success',
  })

  const clientRef = useRef<RoomHubClient | null>(null)
  const lastProcessedResetAtRef = useRef<string | null>(null)

  // Refs for latest values needed inside stable callbacks (avoid stale closures).
  const nameRef = useRef(name)
  const selectedRoleIdRef = useRef(selectedRoleId)

  useEffect(() => {
    nameRef.current = name
  }, [name])

  useEffect(() => {
    selectedRoleIdRef.current = selectedRoleId
  }, [selectedRoleId])

  useEffect(() => {
    setStoredRecentGifSearches(recentGifSearches)
  }, [recentGifSearches])

  const currentUser = useMemo(
    () => roomState?.users.find((user) => user.id === currentUserId) ?? null,
    [currentUserId, roomState],
  )
  const teamMembers = useMemo(
    () => roomState?.users.filter((user) => user.role.id === roles.teamMember.id) ?? [],
    [roomState],
  )
  const facilitators = useMemo(
    () => roomState?.users.filter((user) => user.role.id === roles.facilitator.id) ?? [],
    [roomState],
  )
  const votingMembers = useMemo(
    () => (facilitatorCanVote ? [...teamMembers, ...facilitators] : teamMembers),
    [facilitatorCanVote, teamMembers, facilitators],
  )
  const observers = useMemo(
    () => roomState?.users.filter((user) => user.role.id === roles.observer.id) ?? [],
    [roomState],
  )

  const resetRequestingUser = useMemo(
    () => roomState?.users.find((user) => user.id === resetVotesRequestedBy) ?? null,
    [resetVotesRequestedBy, roomState],
  )

  const resetCountdownSeconds = useMemo(() => {
    if (!resetVotesRequestedAt) {
      return 0
    }

    return Math.max(
      0,
      Math.floor((new Date(resetVotesRequestedAt).getTime() - (Date.now() + serverClockOffsetMs)) / 1_000),
    )
  }, [resetVotesRequestedAt, serverClockOffsetMs, timerTick])

  const groupedVotes = useMemo(() => {
    if (!roomState || !votesShown) {
      return [] as Array<{ count: number; vote: string }>
    }

    const counts = new Map<string, number>()
    for (const member of votingMembers) {
      const vote = member.vote ?? ''
      counts.set(vote, (counts.get(vote) ?? 0) + 1)
    }

    return [...counts.entries()]
      .map(([vote, count]) => ({ count, vote }))
      .sort((left, right) => {
        const leftIndex = roomState.voteOptions.indexOf(left.vote)
        const rightIndex = roomState.voteOptions.indexOf(right.vote)
        return (leftIndex === -1 ? Number.MAX_SAFE_INTEGER : leftIndex) -
          (rightIndex === -1 ? Number.MAX_SAFE_INTEGER : rightIndex)
      })
  }, [roomState, votingMembers, votesShown])

  const maxVoteCount = useMemo(
    () => groupedVotes.reduce((max, entry) => Math.max(max, entry.count), 0),
    [groupedVotes],
  )
  const isMobileActionsOpen = Boolean(mobileActionsAnchorEl)

  const showSnackbar = useCallback((message: string, severity: AlertColor = 'success') => {
    setSnackbar({
      message,
      open: true,
      severity,
    })
  }, [])

  const handleGifSearch = useCallback((query: string) => {
    const trimmedQuery = query.trim()
    if (!trimmedQuery) {
      return
    }

    setSelectedGifSearchQuery(trimmedQuery)
    setRecentGifSearches((currentSearches) => {
      // Only add to the list if it's not already present (no reordering)
      if (currentSearches.includes(trimmedQuery)) {
        return currentSearches
      }
      const nextSearches = [trimmedQuery, ...currentSearches]
      return nextSearches.slice(0, 6)
    })
  }, [])

  const handleRecentGifSearchClick = useCallback((query: string) => {
    setSelectedGifSearchQuery(query)
    setSelectedGifSearchRequestId((requestId) => requestId + 1)
  }, [])

  const callHub = useCallback(
    async <T,>(operation: (client: RoomHubClient) => Promise<T>) => {
      const client = clientRef.current
      if (!client?.isConnected) {
        return null
      }

      try {
        return await operation(client)
      } catch (error) {
        console.error('Room operation failed.', error)
        showSnackbar('Unable to complete that action right now.', 'error')
        return null
      }
    },
    [showSnackbar],
  )

  const connectToRoom = useCallback(
    async (nextName: string, nextRoleId: string | null) => {
      if (!roomId) {
        return
      }

      await callHub(async (client) => {
        if (!roomState) {
          const role = roleFromId(nextRoleId) ?? roles.teamMember
          const resolvedName = nextName.trim() || `User ${Math.floor(Math.random() * 100_000)}`
          const user: User = {
            id: getOrCreateUserId(),
            name: resolvedName,
            role,
          }

          setCurrentUserId(user.id)
          setName(resolvedName)
          setSelectedRoleId(role.id)

          await client.joinRoom(roomId, user)

          if (role.id === roles.facilitator.id) {
            const storedVoteOptions = getStoredVoteOptions()
            if (storedVoteOptions?.length) {
              await client.updateRoom({
                voteOptions: storedVoteOptions,
              })
            }
          }

          nextName = resolvedName
          nextRoleId = role.id
        } else {
          const userUpdate: UserOptions = {
            name: nextName || undefined,
            role: roleFromId(nextRoleId),
          }

          await client.updateUser(userUpdate)
        }

        if (nextName.trim()) {
          setStoredName(nextName)
        }

        setStoredRoomId(roomId)
        setStoredRoleId(nextRoleId)
        addRecentRoom(roomId)
      })
    },
    [callHub, roomId, roomState],
  )

  const requestRoomUpdate = useCallback(
    async (update: RoomOptions) => {
      await callHub(async (client) => {
        await client.updateRoom(update)
      })
    },
    [callHub],
  )

  const handleResetVotes = useCallback(async () => {
    await callHub(async (client) => {
      await client.resetVotes()
    })
  }, [callHub])

  useEffect(() => {
    if (!roomId) {
      return
    }

    const client = new RoomHubClient()
    const controller = new AbortController()
    clientRef.current = client

    const unsubscribe = client.subscribe((nextRoomState) => {
      setAutoShowVotes(nextRoomState.autoShowVotes)
      setFacilitatorCanVote(nextRoomState.facilitatorCanVote)
      setResetVotesRequestedAt(nextRoomState.resetVotesRequestedAt ?? null)
      setResetVotesRequestedBy(nextRoomState.resetVotesRequestedBy ?? null)
      setRoomState(nextRoomState)
      setVoteStartTime(nextRoomState.voteStartTime ?? null)
      setVotesShown(nextRoomState.votesShown)
    })

    let cancelled = false

    // Rejoins the room using the stable user identity. Called by:
    //   - onreconnected (SignalR built-in reconnect succeeded)
    //   - onclose (reconnect exhausted; this also re-opens the connection)
    //   - visibilitychange (tab foregrounded while fully disconnected)
    const rejoinRoom = async () => {
      try {
        await client.open(controller.signal)
        const userId = getOrCreateUserId()
        const resolvedName = nameRef.current
        if (!resolvedName || !roomId) return
        const role = roleFromId(selectedRoleIdRef.current) ?? roles.teamMember
        const user: User = { id: userId, name: resolvedName, role }
        await callHub((roomClient) => roomClient.joinRoom(roomId, user))
      } catch (error) {
        if (!isAbortError(error)) {
          console.error('Failed to rejoin room after reconnect.', error)
        }
      }
    }

    client.setReconnectHandler(rejoinRoom)

    void (async () => {
      try {
        await client.open(controller.signal)

        const clientBefore = Date.now()
        const serverTime = await client.getServerTime()
        const clientAfter = Date.now()
        const estimatedServerNow = new Date(serverTime).getTime() + (clientAfter - clientBefore) / 2
        if (!cancelled) {
          setServerClockOffsetMs(estimatedServerNow - clientAfter)
        }
      } catch (error) {
        if (!isAbortError(error)) {
          console.error('Unable to open the room connection.', error)
          showSnackbar('Unable to connect to the room right now.', 'error')
        }
      }

      if (cancelled) {
        return
      }

      const rememberedName = preferredName || getStoredName()
      if (rememberedName && !nameRef.current) {
        setName(rememberedName)
      }

      const rememberedRoomId = getStoredRoomId()
      const rememberedRoleId = getStoredRoleId()
      const rememberedRole = roleFromId(rememberedRoleId)

      if (rememberedRoomId === roomId && rememberedRole && rememberedName.trim()) {
        const user: User = {
          id: getOrCreateUserId(),
          name: rememberedName,
          role: rememberedRole,
        }

        setCurrentUserId(user.id)
        setSelectedRoleId(rememberedRoleId)

        await callHub(async (roomClient) => {
          await roomClient.joinRoom(roomId, user)
        })

        addRecentRoom(roomId)
      } else {
        setShowUserDialog(true)
      }
    })()

    const handleVisibilityChange = async () => {
      if (document.visibilityState !== 'visible') return
      // Only act when fully disconnected; Reconnecting is already being handled by SignalR.
      if (!client.isDisconnected) return
      await rejoinRoom()
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      cancelled = true
      controller.abort()
      unsubscribe()
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      void client.stop()
      if (clientRef.current === client) {
        clientRef.current = null
      }
    }
  }, [callHub, preferredName, roomId, showSnackbar])

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setTimerTick((current) => current + 1)
    }, 500)

    return () => {
      window.clearInterval(intervalId)
    }
  }, [])

  useEffect(() => {
    if (!resetVotesRequestedAt) {
      lastProcessedResetAtRef.current = null
      return
    }

    if (Date.now() + serverClockOffsetMs < new Date(resetVotesRequestedAt).getTime()) {
      return
    }

    if (lastProcessedResetAtRef.current === resetVotesRequestedAt) {
      return
    }

    lastProcessedResetAtRef.current = resetVotesRequestedAt
    void handleResetVotes()
  }, [handleResetVotes, resetVotesRequestedAt, serverClockOffsetMs, timerTick])

  if (!roomId) {
    return null
  }

  return (
    <Container maxWidth="lg" sx={{ py: 2 }}>
      <Stack spacing={2}>
        <Paper
          elevation={0}
          sx={(theme) => ({
            alignItems: 'center',
            backgroundColor: theme.palette.mode === 'dark' ? theme.palette.background.paper : 'transparent',
            border: theme.palette.mode === 'dark' ? `1px solid ${theme.palette.divider}` : 'none',
            boxShadow: 'none',
            display: 'flex',
            flexWrap: 'wrap',
            gap: 2,
            justifyContent: 'space-between',
            p: theme.palette.mode === 'dark' ? 1.5 : 0,
          })}
        >
          <Stack
            direction="row"
            spacing={1}
            sx={{ alignItems: 'center', flexGrow: { md: 0, xs: 1 }, width: { md: 'auto', xs: '100%' } }}
          >
            <Button
              color="primary"
              onClick={() => {
                void navigator.clipboard
                  .writeText(window.location.href)
                  .then(() => showSnackbar('Link Copied'))
                  .catch((error) => {
                    console.error('Unable to copy the invitation link.', error)
                    showSnackbar('Unable to copy the invitation link.', 'error')
                  })
              }}
              startIcon={<CopyIcon />}
              sx={{ flexGrow: { md: 0, xs: 1 }, minHeight: 48 }}
              variant="contained"
            >
              Copy Invitation URL
            </Button>
            <IconButton
              aria-controls={isMobileActionsOpen ? 'room-mobile-actions-menu' : undefined}
              aria-expanded={isMobileActionsOpen ? 'true' : undefined}
              aria-haspopup="menu"
              aria-label="Open room actions"
              onClick={(event) => setMobileActionsAnchorEl(event.currentTarget)}
              sx={{ display: { md: 'none', xs: 'inline-flex' } }}
            >
              <MoreVertIcon />
            </IconButton>
          </Stack>

          <Box sx={{ display: { md: 'inline-flex', xs: 'none' }, gap: 1 }}>
            <Button
              color="inherit"
              onClick={() => setShowQrCode((current) => !current)}
              startIcon={showQrCode ? <ExpandLessIcon /> : <ExpandMoreIcon />}
              variant="text"
            >
              {showQrCode ? 'Hide' : 'Show'} QR Code
            </Button>
            <Button color="inherit" onClick={() => setShowUserDialog(true)} startIcon={<EditIcon />} variant="outlined">
              {name || currentUser?.name || 'Edit user'}
            </Button>
          </Box>

          <Menu
            anchorEl={mobileActionsAnchorEl}
            id="room-mobile-actions-menu"
            onClose={() => setMobileActionsAnchorEl(null)}
            open={isMobileActionsOpen}
          >
            <MenuItem
              onClick={() => {
                setShowQrCode((current) => !current)
                setMobileActionsAnchorEl(null)
              }}
            >
              <ListItemIcon>{showQrCode ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}</ListItemIcon>
              <ListItemText>{showQrCode ? 'Hide' : 'Show'} QR Code</ListItemText>
            </MenuItem>
            <MenuItem
              onClick={() => {
                setShowUserDialog(true)
                setMobileActionsAnchorEl(null)
              }}
            >
              <ListItemIcon>
                <EditIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText>{name || currentUser?.name || 'Edit user'}</ListItemText>
            </MenuItem>
          </Menu>
        </Paper>

        <Collapse in={showQrCode}>
          <Box
            sx={(theme) => ({
              alignItems: 'center',
              backgroundColor: '#ffffff',
              border: `2px solid ${theme.palette.divider}`,
              borderRadius: 1,
              display: 'inline-flex',
              maxWidth: '100%',
              p: 1,
            })}
          >
            <QRCodeSVG value={window.location.href} />
          </Box>
        </Collapse>

        {voteStartTime ? (
          <Box
            sx={{
              alignItems: { sm: 'center', xs: 'flex-start' },
              display: 'flex',
              flexDirection: { sm: 'row', xs: 'column' },
              gap: 1,
              justifyContent: 'space-between',
            }}
          >
            <Box sx={{ alignItems: 'center', display: 'flex', gap: 1 }}>
              <Typography>Vote Time: {getElapsedTimeLabel(voteStartTime, serverClockOffsetMs)}</Typography>
              {!resetVotesRequestedAt && isRole(currentUser, roles.teamMember) ? (
                <Button
                  color="warning"
                  onClick={() => {
                    void callHub(async (client) => {
                      await client.requestResetVotes()
                    })
                  }}
                  variant="text"
                >
                  <RefreshIcon fontSize="small" />
                </Button>
              ) : null}
            </Box>
            {roomState?.votingMode === VotingMode.Giphy && recentGifSearches.length > 0 ? (
              <Box
                sx={{
                  alignItems: { sm: 'center', xs: 'flex-start' },
                  display: 'flex',
                  flexDirection: { sm: 'row', xs: 'column' },
                  gap: 1,
                  width: { sm: 'auto', xs: '100%' },
                }}
              >
                <Typography color="text.secondary" sx={{ whiteSpace: 'nowrap' }} variant="body2">
                  Recent GIF searches:
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, justifyContent: { sm: 'flex-end', xs: 'flex-start' } }}>
                  {recentGifSearches.map((query) => (
                    <Chip
                      color={selectedGifSearchQuery === query ? 'primary' : 'default'}
                      key={query}
                      label={query}
                      onClick={() => handleRecentGifSearchClick(query)}
                      size="small"
                      variant={selectedGifSearchQuery === query ? 'filled' : 'outlined'}
                    />
                  ))}
                </Box>
              </Box>
            ) : null}
          </Box>
        ) : null}

        {resetVotesRequestedAt ? (
          <Alert severity="warning">
            {isRole(currentUser, roles.facilitator) && resetRequestingUser ? (
              <Stack spacing={1}>
                <Typography>
                  {resetRequestingUser.name} requested a reset. Resetting votes in {resetCountdownSeconds}{' '}
                  second{resetCountdownSeconds === 1 ? '' : 's'}.
                </Typography>
                <Button
                  color="error"
                  onClick={() => {
                    void callHub(async (client) => {
                      await client.cancelResetVotes()
                    })
                  }}
                  sx={{ alignSelf: 'flex-start' }}
                  variant="contained"
                >
                  Cancel Reset
                </Button>
              </Stack>
            ) : (
              <Typography>
                Resetting votes in {resetCountdownSeconds} second{resetCountdownSeconds === 1 ? '' : 's'}.
              </Typography>
            )}
          </Alert>
        ) : null}

        {isRole(currentUser, roles.teamMember) || (facilitatorCanVote && isRole(currentUser, roles.facilitator)) ? (
          <>
            {roomState?.votingMode === VotingMode.Giphy ? (
              <GiphyVotingPanel
                currentVote={currentUser?.vote}
                onSearch={handleGifSearch}
                onVoteSubmit={(giphyId) => {
                  void callHub(async (client) => {
                    await client.submitVote(giphyId)
                  })
                }}
                searchShortcutQuery={selectedGifSearchQuery}
                searchShortcutRequestId={selectedGifSearchRequestId}
              />
            ) : (
              <Box
                sx={{
                  display: 'grid',
                  gap: 1,
                  gridTemplateColumns: { md: 'repeat(8, minmax(0, 1fr))', sm: 'repeat(6, minmax(0, 1fr))', xs: 'repeat(4, minmax(0, 1fr))' },
                }}
              >
                {(roomState?.voteOptions ?? defaultVoteOptions).map((option, index) => (
                  <Button
                    color={option === currentUser?.vote ? 'success' : 'primary'}
                    key={`${option}-${index}`}
                    onClick={() => {
                      void callHub(async (client) => {
                        await client.submitVote(option)
                      })
                    }}
                    sx={{ fontSize: { sm: '1rem', xs: '0.95rem' }, height: { sm: 66, xs: 56 }, minWidth: 0 }}
                    variant="contained"
                  >
                    {option}
                  </Button>
                ))}
              </Box>
            )}
          </>
        ) : null}

        {isRole(currentUser, roles.facilitator) ? (
          <Stack spacing={1.5}>
            <Stack direction={{ md: 'row', xs: 'column' }} spacing={2}>
              <Button
                color={votesShown ? 'success' : 'primary'}
                onClick={() => {
                  const checked = !votesShown
                  setVotesShown(checked)
                  void requestRoomUpdate({ votesShown: checked })
                }}
                sx={{ width: { md: 'auto', xs: '100%' } }}
                variant={votesShown ? 'contained' : 'outlined'}
              >
                {votesShown ? 'Hide Votes' : 'Show Votes'}
              </Button>
              <Button
                color="warning"
                onClick={() => void handleResetVotes()}
                sx={{ width: { md: 'auto', xs: '100%' } }}
                variant="contained"
              >
                Reset Votes
              </Button>
              <Button
                endIcon={showAdvancedFacilitatorControls ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                onClick={() => setShowAdvancedFacilitatorControls((current) => !current)}
                sx={{ width: { md: 'auto', xs: '100%' } }}
                variant="text"
              >
                Advanced
              </Button>
            </Stack>

            <Collapse in={showAdvancedFacilitatorControls} unmountOnExit>
              <Stack direction={{ md: 'row', xs: 'column' }} spacing={2}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={autoShowVotes}
                      color="info"
                      onChange={(event) => {
                        const checked = event.target.checked
                        setAutoShowVotes(checked)
                        void requestRoomUpdate({ autoShowVotes: checked })
                      }}
                    />
                  }
                  label="Automatically reveal votes"
                />
                {!facilitatorCanVote ? (
                  <FormControlLabel
                    control={
                      <Switch
                        checked={previewVotes}
                        color="info"
                        onChange={(event) => setPreviewVotes(event.target.checked)}
                      />
                    }
                    label="Preview votes"
                  />
                ) : null}
                <Button
                  onClick={() => setShowVotingOptionsDialog(true)}
                  startIcon={<SettingsIcon />}
                  sx={{ width: { md: 'auto', xs: '100%' } }}
                  variant="outlined"
                >
                  Configure Options
                </Button>
              </Stack>
            </Collapse>
          </Stack>
        ) : null}

        {isRole(currentUser, roles.observer) ? (
          <FormControlLabel
            control={
              <Switch
                checked={previewVotes}
                color="info"
                onChange={(event) => setPreviewVotes(event.target.checked)}
              />
            }
            label="Preview votes"
          />
        ) : null}

        {votesShown && roomState ? (
          <>
            {roomState.votingMode === VotingMode.Giphy ? (
              <Card>
                <CardHeader title="Votes" />
                <CardContent>
                  <GiphyVoteDisplay users={votingMembers} showVotes={votesShown} />
                </CardContent>
              </Card>
            ) : (
              <Card>
                <CardHeader title="Results" />
                <CardContent>
                  <Stack spacing={1}>
                    {groupedVotes.map((entry) => {
                      const percentage = votingMembers.length === 0 ? 0 : entry.count / votingMembers.length
                      const isHighestCount = entry.count === maxVoteCount
                      const label = `${entry.vote.trim() ? entry.vote : '…'} - ${entry.count} Vote${entry.count === 1 ? '' : 's'} (${percentage.toLocaleString(undefined, { style: 'percent', maximumFractionDigits: 0 })})`

                      return (
                        <Box key={entry.vote || 'blank'}>
                          <Typography variant={isHighestCount ? 'body1' : 'body2'}>{label}</Typography>
                          <Box
                            sx={(theme) => ({
                              backgroundColor: isHighestCount ? theme.palette.primary.dark : theme.palette.primary.light,
                              height: 10,
                              mt: 0.5,
                              width: `${percentage * 100}%`,
                            })}
                          />
                        </Box>
                      )
                    })}
                  </Stack>
                </CardContent>
              </Card>
            )}
          </>
        ) : null}

        <Card>
          <CardHeader title="Team Members" />
          <CardContent>
            {votingMembers.length > 0 ? (
              <Stack spacing={1}>
                {votingMembers.map((user) => (
                  <Typography key={user.id}>
                    {isRole(currentUser, roles.facilitator) ? (
                      <Tooltip title={isRole(user, roles.facilitator) ? `${user.name} is a facilitator` : `Make ${user.name} an Observer`}>
                        <span>
                          <IconButton
                            color="inherit"
                            disabled={isRole(user, roles.facilitator)}
                            onClick={() => {
                              void callHub(async (client) => {
                                await client.removeUser(user.id)
                              })
                            }}
                            size="small"
                            sx={{ mr: 0.5 }}
                          >
                            <RemoveUserIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    ) : null}
                    {user.name}
                    {votesShown && user.vote ? (
                      roomState?.votingMode === VotingMode.Giphy ? (
                        <Box component="span" sx={{ display: 'inline-flex', ml: 0.75, verticalAlign: 'middle' }}>
                          <img
                            alt={`${user.name} vote`}
                            loading="lazy"
                            src={`https://media.giphy.com/media/${user.vote}/giphy.gif`}
                            style={{
                              borderRadius: 4,
                              height: 24,
                              objectFit: 'cover',
                              width: 24,
                            }}
                          />
                        </Box>
                      ) : (
                        <>
                          {' - '}
                          {user.originalVote && user.originalVote !== user.vote ? (
                            <Box component="span" sx={{ fontStyle: 'italic' }}>
                              ({user.originalVote}){' '}
                            </Box>
                          ) : null}
                          <Box component="span" sx={{ fontWeight: 700 }}>
                            {user.vote}
                          </Box>
                        </>
                      )
                    ) : isRole(currentUser, roles.facilitator) || isRole(currentUser, roles.observer) ? (
                      previewVotes ? (
                        roomState?.votingMode === VotingMode.Giphy ? (
                          user.vote ? (
                            <Box component="span" sx={{ display: 'inline-flex', ml: 0.75, verticalAlign: 'middle' }}>
                              <img
                                alt={`${user.name} vote`}
                                loading="lazy"
                                src={`https://media.giphy.com/media/${user.vote}/giphy.gif`}
                                style={{
                                  borderRadius: 4,
                                  height: 24,
                                  objectFit: 'cover',
                                  width: 24,
                                }}
                              />
                            </Box>
                          ) : (
                            <Box component="span" sx={{ fontStyle: 'italic' }}>
                              {' '}
                              - …
                            </Box>
                          )
                        ) : (
                          <Box component="span" sx={{ fontStyle: 'italic' }}>
                            {' '}
                            - {user.vote || '…'}
                          </Box>
                        )
                      ) : (
                        <Box component="span"> - {user.vote ? '✓' : '…'}</Box>
                      )
                    ) : (
                      <Box component="span"> - {user.vote ? '✓' : '…'}</Box>
                    )}
                  </Typography>
                ))}
              </Stack>
            ) : (
              <Typography>Waiting for team members to join...</Typography>
            )}
          </CardContent>
        </Card>

        {facilitators.length > 0 ? (
          <Card>
            <CardHeader title="Facilitators" />
            <CardContent>
              <Stack spacing={1}>
                {facilitators.map((user) => (
                  <Typography key={user.id}>{user.name}</Typography>
                ))}
              </Stack>
            </CardContent>
          </Card>
        ) : null}

        {observers.length > 0 ? (
          <Card>
            <CardHeader title="Observers" />
            <CardContent>
              <Stack spacing={1}>
                {observers.map((user) => (
                  <Typography key={user.id}>{user.name}</Typography>
                ))}
              </Stack>
            </CardContent>
          </Card>
        ) : null}
      </Stack>

      <UserDialog
        defaultName={name}
        defaultRoleId={selectedRoleId}
        onCancel={() => setShowUserDialog(false)}
        onSubmit={({ name: nextName, selectedRoleId: nextRoleId }) => {
          setName(nextName)
          setSelectedRoleId(nextRoleId)
          setShowUserDialog(false)
          void connectToRoom(nextName, nextRoleId)
        }}
        open={showUserDialog}
        roomId={roomId}
      />

      <VotingOptionsDialog
        currentFacilitatorCanVote={facilitatorCanVote}
        currentVoteOptions={roomState?.voteOptions}
        currentVotingMode={roomState?.votingMode}
        onCancel={() => setShowVotingOptionsDialog(false)}
        onSave={(nextVoteOptions, nextVotingMode, nextFacilitatorCanVote) => {
          setShowVotingOptionsDialog(false)
          setStoredVoteOptions(nextVoteOptions)
          const update: RoomOptions = {
            voteOptions: nextVoteOptions,
            facilitatorCanVote: nextFacilitatorCanVote,
          }
          if (nextVotingMode !== undefined) {
            update.votingMode = nextVotingMode
          }
          void requestRoomUpdate(update)
        }}
        open={showVotingOptionsDialog}
      />

      <Snackbar
        autoHideDuration={4_000}
        onClose={() => setSnackbar((current) => ({ ...current, open: false }))}
        open={snackbar.open}
      >
        <Alert
          onClose={() => setSnackbar((current) => ({ ...current, open: false }))}
          severity={snackbar.severity}
          variant="filled"
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  )
}
