import { useEffect, useMemo, useState } from 'react'
import {
  Add as AddIcon,
  ArrowDownward as ArrowDownwardIcon,
  ArrowUpward as ArrowUpwardIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material'
import {
  Button,
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

import { defaultVoteOptions, votingPresets, VotingMode } from '../types/contracts'

interface VotingOptionsDialogProps {
  currentVoteOptions?: string[]
  currentVotingMode?: VotingMode
  onCancel: () => void
  onSave: (voteOptions: string[], votingMode?: VotingMode) => void
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
  currentVotingMode,
  onCancel,
  onSave,
  open,
}: VotingOptionsDialogProps) {
  const [voteOptions, setVoteOptions] = useState<string[]>(currentVoteOptions ?? [...defaultVoteOptions])
  const [votingMode, setVotingMode] = useState<VotingMode>(currentVotingMode ?? VotingMode.Standard)

  useEffect(() => {
    if (!open) {
      return
    }

    setVoteOptions(currentVoteOptions ? [...currentVoteOptions] : [...defaultVoteOptions])
    setVotingMode(currentVotingMode ?? VotingMode.Standard)
  }, [currentVoteOptions, currentVotingMode, open])

  const isValid = useMemo(
    () => {
      if (votingMode === VotingMode.Giphy) {
        return true
      }
      return voteOptions.length > 0 && voteOptions.every((option) => option.trim().length > 0)
    },
    [voteOptions, votingMode],
  )

  return (
    <Dialog fullWidth maxWidth="md" onClose={onCancel} open={open}>
      <DialogTitle>Configure Voting Options</DialogTitle>
      <DialogContent sx={{ pt: 2 }}>
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="body2">Select a preset:</Typography>
            <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
              {votingPresets.map((preset) => (
                <Button
                  key={preset.name}
                  onClick={() => {
                    setVotingMode(VotingMode.Standard)
                    setVoteOptions([...preset.options])
                  }}
                  size="small"
                  variant={
                    votingMode === VotingMode.Standard && isPresetSelected(voteOptions, preset.options)
                      ? 'contained'
                      : 'outlined'
                  }
                >
                  {preset.name}
                </Button>
              ))}
              <Button
                onClick={() => setVotingMode(VotingMode.Giphy)}
                size="small"
                variant={votingMode === VotingMode.Giphy ? 'contained' : 'outlined'}
              >
                Giphy Mode
              </Button>
            </Stack>
          </Stack>

          {votingMode === VotingMode.Standard ? (
            <>
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
            </>
          ) : (
            <Typography variant="body2" color="textSecondary">
              In Giphy mode, team members will select images from Giphy instead of using preset voting options.
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel}>Cancel</Button>
        <Button
          disabled={!isValid}
          onClick={() => onSave(votingMode === VotingMode.Giphy ? [] : voteOptions.map((option) => option.trim()), votingMode)}
          variant="contained"
        >
          Save
        </Button>
      </DialogActions>
    </Dialog>
  )
}
