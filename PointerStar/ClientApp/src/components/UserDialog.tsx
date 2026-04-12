import { useEffect, useState } from 'react'
import {
  Button,
  ButtonGroup,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  Stack,
  TextField,
  Typography,
} from '@mui/material'

import { getNewUserRole } from '../services/roomApi'
import { roles, userNameMaxLength } from '../types/contracts'

interface UserDialogProps {
  defaultName?: string | null
  defaultRoleId?: string | null
  onCancel: () => void
  onSubmit: (values: { name: string; selectedRoleId: string | null }) => void
  open: boolean
  roomId?: string
}

export function UserDialog({
  defaultName,
  defaultRoleId,
  onCancel,
  onSubmit,
  open,
  roomId,
}: UserDialogProps) {
  const [name, setName] = useState(defaultName ?? '')
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(defaultRoleId ?? null)
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!open) {
      return
    }

    setName(defaultName ?? '')
    setSelectedRoleId(defaultRoleId ?? null)
  }, [defaultName, defaultRoleId, open])

  useEffect(() => {
    if (!open || selectedRoleId || !roomId) {
      return
    }

    let cancelled = false
    setIsLoading(true)

    void getNewUserRole(roomId)
      .then((role) => {
        if (!cancelled) {
          setSelectedRoleId(role.id)
        }
      })
      .catch((error) => {
        console.error('Unable to load the default role for the room.', error)
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [open, roomId, selectedRoleId])

  return (
    <Dialog fullWidth maxWidth="sm" onClose={onCancel} open={open}>
      <DialogContent>
        <Stack spacing={2}>
          <TextField
            fullWidth
            label="Name"
            onChange={(event) => setName(event.target.value)}
            slotProps={{ htmlInput: { maxLength: userNameMaxLength } }}
            value={name}
            variant="standard"
          />
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <Typography>I want to...</Typography>
            <ButtonGroup>
              <Button
                disabled={isLoading}
                onClick={() => setSelectedRoleId(roles.teamMember.id)}
                variant={selectedRoleId === roles.teamMember.id ? 'contained' : 'outlined'}
              >
                Vote
              </Button>
              <Button
                disabled={isLoading}
                onClick={() => setSelectedRoleId(roles.facilitator.id)}
                variant={selectedRoleId === roles.facilitator.id ? 'contained' : 'outlined'}
              >
                Facilitate
              </Button>
              <Button
                disabled={isLoading}
                onClick={() => setSelectedRoleId(roles.observer.id)}
                variant={selectedRoleId === roles.observer.id ? 'contained' : 'outlined'}
              >
                Observe
              </Button>
            </ButtonGroup>
            {isLoading ? <CircularProgress size={18} /> : null}
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button disabled={isLoading} onClick={onCancel}>
          Cancel
        </Button>
        <Button
          disabled={isLoading}
          onClick={() => onSubmit({ name: name.trim(), selectedRoleId })}
          variant="contained"
        >
          Ok
        </Button>
      </DialogActions>
    </Dialog>
  )
}
