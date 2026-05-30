import { alpha, createTheme, type PaletteMode } from '@mui/material/styles'

export function createAppTheme(mode: PaletteMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      primary: {
        dark: isDark ? '#2f7f9d' : '#1d5c74',
        light: isDark ? '#7fc3d8' : '#66afc8',
        main: isDark ? '#4fa0be' : '#2f7f9d',
      },
      secondary: {
        main: isDark ? '#8dc6d8' : '#174a58',
      },
      info: {
        main: isDark ? '#5db8d2' : '#2f7f9d',
      },
      success: {
        main: isDark ? '#62bb72' : '#2e7d32',
      },
      warning: {
        contrastText: '#ffffff',
        dark: '#e39a27',
        light: '#ffc966',
        main: '#f7b347',
      },
      error: {
        main: isDark ? '#ff6b6b' : '#d32f2f',
      },
      background: isDark
        ? {
            default: '#0b1e24',
            paper: '#102a32',
          }
        : {
            default: '#f3f8fa',
            paper: '#ffffff',
          },
      divider: isDark ? alpha('#eaf4f7', 0.16) : alpha('#15262d', 0.14),
      text: isDark
        ? {
            primary: '#eaf4f7',
            secondary: '#a8bdc6',
          }
        : {
            primary: '#15262d',
            secondary: '#4c626b',
          },
      action: isDark
        ? {
            active: '#eaf4f7',
            hover: alpha('#eaf4f7', 0.08),
            selected: alpha('#eaf4f7', 0.16),
          }
        : {
            active: '#15262d',
            hover: alpha('#15262d', 0.04),
            selected: alpha('#15262d', 0.08),
          },
    },
    shape: {
      borderRadius: 6,
    },
    typography: {
      fontFamily: 'Roboto, Helvetica, Arial, sans-serif',
      h5: {
        fontWeight: 500,
      },
    },
    components: {
      MuiAppBar: {
        styleOverrides: {
          root: ({ theme }) => ({
            backgroundColor: theme.palette.mode === 'dark' ? '#0d252d' : theme.palette.primary.main,
            backgroundImage: 'none',
            boxShadow: 'none',
            color: theme.palette.mode === 'dark' ? theme.palette.text.primary : '#ffffff',
          }),
        },
      },
      MuiButton: {
        styleOverrides: {
          contained: {
            boxShadow: 'none',
          },
          outlined: ({ ownerState, theme }) => ({
            ...(ownerState.color === 'inherit'
              ? {
                  borderColor: alpha(theme.palette.text.primary, theme.palette.mode === 'dark' ? 0.28 : 0.18),
                }
              : {}),
          }),
          root: {
            fontWeight: 600,
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: ({ theme }) => ({
            backgroundColor: theme.palette.background.paper,
            backgroundImage: 'none',
            boxShadow:
              theme.palette.mode === 'dark' ? 'none' : '0 1px 4px rgba(0, 0, 0, 0.16)',
          }),
        },
      },
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundImage: 'none',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
        },
      },
    },
  })
}
