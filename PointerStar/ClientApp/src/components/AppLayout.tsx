import type { ReactNode } from 'react'
import {
  AppBar,
  Box,
  IconButton,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import {
  Brightness4 as SystemThemeIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
} from '@mui/icons-material'

import { CookieConsentBanner } from './CookieConsentBanner'
import type { ThemePreference } from '../hooks/useThemePreference'

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
        <Toolbar>
          <Typography sx={{ ml: 1 }} variant="h5">
            Pointer*
          </Typography>
          <Box sx={{ flexGrow: 1 }} />
          <Tooltip title={getThemeTooltip(themePreference)}>
            <IconButton color="inherit" onClick={onCycleTheme}>
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
      <Box component="main" sx={{ backgroundColor: 'background.default', flexGrow: 1, pb: 12 }}>
        {children}
      </Box>
      <CookieConsentBanner />
    </Box>
  )
}
