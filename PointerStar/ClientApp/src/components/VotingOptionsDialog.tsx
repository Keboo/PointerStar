import { useEffect, useMemo, useState } from 'react'
import {
  Add as AddIcon,
  ArrowDownward as ArrowDownwardIcon,
  ArrowUpward as ArrowUpwardIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material'
import {
  Button,
  ButtonGroup,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material'

import { defaultVoteOptions, votingPresets } from '../types/contracts'

interface VotingOptionsDialogProps {
  currentVoteOptions?: string[]
  onCancel: () => void
  onSave: (voteOptions: string[]) => void
  open: boolean
}

function isPresetSelected(currentVoteOptions: string[], preset: readonly string[]) {
  return (
    currentVoteOptions.length === preset.length &&
    currentVoteOptions.every((option, index) => option === preset[index])
  )
}

export function VotingOptionsDialog({
  currentVoteOptions,
  onCancel,
  onSave,
  open,
}: VotingOptionsDialogProps) {
  const [voteOptions, setVoteOptions] = useState<string[]>(currentVoteOptions ?? [...defaultVoteOptions])

  useEffect(() => {
    if (!open) {
      return
    }

    setVoteOptions(currentVoteOptions ? [...currentVoteOptions] : [...defaultVoteOptions])
  }, [currentVoteOptions, open])

  const isValid = useMemo(
    () => voteOptions.length > 0 && voteOptions.every((option) => option.trim().length > 0),
    [voteOptions],
  )

  return (
    <Dialog fullWidth maxWidth="md" onClose={onCancel} open={open}>
      <DialogTitle>Configure Voting Options</DialogTitle>
      <DialogContent>
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="body2">Select a preset:</Typography>
            <ButtonGroup sx={{ flexWrap: 'wrap', gap: 1 }}>
              {votingPresets.map((preset) => (
                <Button
                  key={preset.name}
                  onClick={() => setVoteOptions([...preset.options])}
                  size="small"
                  variant={isPresetSelected(voteOptions, preset.options) ? 'contained' : 'outlined'}
                >
                  {preset.name}
                </Button>
              ))}
            </ButtonGroup>
          </Stack>

          <Divider />

          <Stack spacing={1}>
            <Typography variant="body2">Or customize your options:</Typography>
            <Stack spacing={1}>
              {voteOptions.map((option, index) => (
                <Stack direction="row" key={`${option}-${index}`} spacing={1} sx={{ alignItems: 'center' }}>
                  <Stack spacing={0}>
                    <IconButton
                      disabled={index === 0}
                      onClick={() =>
                        setVoteOptions((current) => {
                          const next = [...current]
                          ;[next[index - 1], next[index]] = [next[index], next[index - 1]]
                          return next
                        })
                      }
                      size="small"
                    >
                      <ArrowUpwardIcon fontSize="small" />
                    </IconButton>
                    <IconButton
                      disabled={index === voteOptions.length - 1}
                      onClick={() =>
                        setVoteOptions((current) => {
                          const next = [...current]
                          ;[next[index + 1], next[index]] = [next[index], next[index + 1]]
                          return next
                        })
                      }
                      size="small"
                    >
                      <ArrowDownwardIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                  <TextField
                    fullWidth
                    label={`Option ${index + 1}`}
                    margin="dense"
                    onChange={(event) =>
                      setVoteOptions((current) =>
                        current.map((currentOption, currentIndex) =>
                          currentIndex === index ? event.target.value : currentOption,
                        ),
                      )
                    }
                    value={option}
                    variant="outlined"
                  />
                  <IconButton
                    color="error"
                    disabled={voteOptions.length <= 1}
                    onClick={() =>
                      setVoteOptions((current) => current.filter((_, currentIndex) => currentIndex !== index))
                    }
                    size="small"
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </Stack>
              ))}
            </Stack>
            <Button
              onClick={() => setVoteOptions((current) => [...current, ''])}
              size="small"
              startIcon={<AddIcon />}
              sx={{ alignSelf: 'flex-start' }}
              variant="outlined"
            >
              Add Option
            </Button>
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel}>Cancel</Button>
        <Button
          disabled={!isValid}
          onClick={() => onSave(voteOptions.map((option) => option.trim()))}
          variant="contained"
        >
          Save
        </Button>
      </DialogActions>
    </Dialog>
  )
}
