import { Box, Button, Container, Paper, Stack, Typography } from '@mui/material'

import { useCookieConsent } from '../hooks/useCookieConsent'

export function CookieConsentBanner() {
  const consent = useCookieConsent()

  if (consent.hasUserResponded) {
    return null
  }

  return (
    <Paper
      elevation={3}
      sx={(theme) => ({
        backgroundColor: theme.palette.background.paper,
        borderTop: `1px solid ${theme.palette.divider}`,
        bottom: 0,
        left: 0,
        padding: 2,
        position: 'fixed',
        right: 0,
        width: '100%',
        zIndex: theme.zIndex.snackbar,
      })}
    >
      <Container maxWidth="lg">
        <Stack
          direction={{ md: 'row', xs: 'column' }}
          spacing={2}
          sx={{
            alignItems: { md: 'center', xs: 'flex-start' },
            justifyContent: 'space-between',
          }}
        >
          <Box>
            <Typography variant="body1">
              <strong>This site uses cookies</strong>
            </Typography>
            <Typography color="text.secondary" variant="body2">
              We use cookies to remember your preferences and improve your experience. By clicking
              &quot;Accept&quot;, you consent to the use of cookies.
            </Typography>
          </Box>
          <Stack direction="row" spacing={2}>
            <Button color="inherit" onClick={consent.reject} variant="outlined">
              Reject
            </Button>
            <Button onClick={consent.accept} variant="contained">
              Accept
            </Button>
          </Stack>
        </Stack>
      </Container>
    </Paper>
  )
}
