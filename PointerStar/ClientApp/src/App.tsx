import { useEffect, useMemo, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { CssBaseline, ThemeProvider } from '@mui/material'

import { createAppTheme } from './app/theme'
import { AppLayout } from './components/AppLayout'
import { useThemePreference } from './hooks/useThemePreference'
import { HomePage } from './pages/HomePage'
import { RoomPage } from './pages/RoomPage'
import { loadClientConfig } from './services/clientConfig'
import { initializeTelemetry } from './services/telemetry'
import type { ClientConfig } from './types/contracts'

function NotFoundPage() {
  return <HomePage notFound />
}

export default function App() {
  const [clientConfig, setClientConfig] = useState<ClientConfig | null>(null)
  const themeController = useThemePreference()

  useEffect(() => {
    document.title = 'Pointer*'

    void loadClientConfig().then((config) => {
      setClientConfig(config)
      initializeTelemetry(config?.applicationInsightsConnectionString)
    })
  }, [])

  const theme = useMemo(() => createAppTheme(themeController.mode), [themeController.mode])

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <AppLayout
          appVersion={clientConfig?.appVersion}
          onCycleTheme={themeController.cycleTheme}
          themePreference={themeController.preference}
        >
          <Routes>
            <Route element={<HomePage />} path="/" />
            <Route element={<RoomPage />} path="/room/:roomId" />
            <Route element={<NotFoundPage />} path="*" />
          </Routes>
        </AppLayout>
      </BrowserRouter>
    </ThemeProvider>
  )
}
