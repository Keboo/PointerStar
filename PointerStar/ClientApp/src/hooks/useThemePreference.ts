import { useCallback, useEffect, useMemo, useState } from 'react'
import type { PaletteMode } from '@mui/material'

import { getStoredThemePreferenceValue, setStoredThemePreferenceValue } from '../services/cookies'

export type ThemePreference = 'system' | 'light' | 'dark'

function parseThemePreference(value: string): ThemePreference {
  if (value === 'light' || value === 'dark') {
    return value
  }

  return 'system'
}

export function useThemePreference() {
  const mediaQuery = useMemo(() => window.matchMedia('(prefers-color-scheme: dark)'), [])

  const [preference, setPreferenceState] = useState<ThemePreference>(() =>
    parseThemePreference(getStoredThemePreferenceValue()),
  )
  const [systemMode, setSystemMode] = useState<PaletteMode>(() => (mediaQuery.matches ? 'dark' : 'light'))

  useEffect(() => {
    const listener = (event: MediaQueryListEvent) => {
      setSystemMode(event.matches ? 'dark' : 'light')
    }

    mediaQuery.addEventListener('change', listener)
    return () => {
      mediaQuery.removeEventListener('change', listener)
    }
  }, [mediaQuery])

  const setPreference = useCallback((value: ThemePreference) => {
    setPreferenceState(value)
    setStoredThemePreferenceValue(value)
  }, [])

  const mode = preference === 'system' ? systemMode : preference

  const cycleTheme = useCallback(() => {
    setPreference(mode === 'dark' ? 'light' : 'dark')
  }, [mode, setPreference])

  return {
    cycleTheme,
    mode,
    preference,
    setPreference,
  }
}
