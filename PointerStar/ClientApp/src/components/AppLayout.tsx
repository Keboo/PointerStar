import type { ReactNode } from 'react'
import {
  AppBar,
  Box,
  Button,
  IconButton,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import {
  Brightness4 as SystemThemeIcon,
  DarkMode as DarkModeIcon,
  GitHub as GitHubIcon,
  LightMode as LightModeIcon,
} from '@mui/icons-material'
import { Link as RouterLink } from 'react-router-dom'

import { CookieConsentBanner } from './CookieConsentBanner'
import type { ThemePreference } from '../hooks/useThemePreference'
import { useCookieConsent } from '../hooks/useCookieConsent'

interface AppLayoutProps {
  appVersion?: string | null
  children: ReactNode
  onCycleTheme: () => void
  themePreference: ThemePreference
}

function getThemeIcon(preference: ThemePreference) {
  switch (preference) {
    case 'light':
      return <LightModeIcon />
    case 'dark':
      return <DarkModeIcon />
    default:
      return <SystemThemeIcon />
  }
}

function getThemeTooltip(preference: ThemePreference) {
  switch (preference) {
    case 'light':
      return 'Theme: Light (click to switch to Dark)'
    case 'dark':
      return 'Theme: Dark (click to switch to Light)'
    default:
      return 'Toggle theme'
  }
}

export function AppLayout({
  appVersion,
  children,
  onCycleTheme,
  themePreference,
}: AppLayoutProps) {
  const repositoryUrl = 'https://github.com/Keboo/PointerStar'
  const consent = useCookieConsent()
  const hasConsentBanner = !consent.hasUserResponded

  return (
    <Box
      sx={(theme) => ({
        backgroundColor: theme.palette.background.default,
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100%',
      })}
    >
      <AppBar
        color="transparent"
        elevation={0}
        position="sticky"
        sx={(theme) => ({
          borderBottom: `1px solid ${theme.palette.divider}`,
        })}
      >
        <Toolbar sx={{ gap: 1 }}>
          <RouterLink
            style={{
              alignItems: 'center',
              color: 'inherit',
              display: 'inline-flex',
              textDecoration: 'none',
            }}
            to="/"
          >
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <img
                alt="Pointer* logo"
                src={`${import.meta.env.BASE_URL}favicon.svg`}
                style={{
                  borderRadius: 4,
                  display: 'block',
                  height: 28,
                  width: 28,
                }}
              />
              <Typography sx={{ fontWeight: 700 }} variant="h6">
                Pointer*
              </Typography>
            </Stack>
          </RouterLink>
          <Box sx={{ flexGrow: 1 }} />
          <Button
            color="inherit"
            href={repositoryUrl}
            rel="noreferrer"
            size="small"
            startIcon={<GitHubIcon />}
            sx={{ minWidth: 0 }}
            target="_blank"
            variant="text"
          >
          </Button>
          <Tooltip title={getThemeTooltip(themePreference)}>
            <IconButton aria-label={getThemeTooltip(themePreference)} color="inherit" onClick={onCycleTheme}>
              {getThemeIcon(themePreference)}
            </IconButton>
          </Tooltip>
          {appVersion ? (
            <Typography
              sx={(theme) => ({
                color: theme.palette.mode === 'dark' ? 'text.secondary' : 'inherit',
                ml: 1,
                opacity: theme.palette.mode === 'dark' ? 1 : 0.92,
              })}
              variant="subtitle1"
            >
              {appVersion}
            </Typography>
          ) : null}
        </Toolbar>
      </AppBar>
      <Box
        component="main"
        sx={{
          backgroundColor: 'background.default',
          flexGrow: 1,
          pb: hasConsentBanner ? { md: 12, xs: 'calc(9rem + env(safe-area-inset-bottom))' } : { md: 6, xs: 4 },
        }}
      >
        {children}
      </Box>
      <CookieConsentBanner />
    </Box>
  )
}
