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
        pb: 'max(1rem, env(safe-area-inset-bottom))',
        pt: 2,
        px: 2,
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
          <Stack direction={{ sm: 'row', xs: 'column-reverse' }} spacing={1.5} sx={{ minWidth: { sm: 196, xs: '100%' } }}>
            <Button
              color="inherit"
              onClick={consent.reject}
              sx={{ minHeight: 44, width: { sm: 'auto', xs: '100%' } }}
              variant="outlined"
            >
              Reject
            </Button>
            <Button onClick={consent.accept} sx={{ minHeight: 44, width: { sm: 'auto', xs: '100%' } }} variant="contained">
              Accept
            </Button>
          </Stack>
        </Stack>
      </Container>
    </Paper>
  )
}
