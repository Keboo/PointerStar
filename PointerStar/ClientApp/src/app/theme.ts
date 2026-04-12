import { alpha, createTheme, type PaletteMode } from '@mui/material/styles'

export function createAppTheme(mode: PaletteMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      primary: {
        dark: isDark ? '#5d4fcb' : '#4b3ec2',
        light: isDark ? '#9185ff' : '#7e72e8',
        main: isDark ? '#7568f2' : '#5b48d6',
      },
      secondary: {
        main: isDark ? '#8f82ff' : '#6d5be0',
      },
      info: {
        main: isDark ? '#5da9ff' : '#0dcaf0',
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
            default: '#353746',
            paper: '#3d3f4e',
          }
        : {
            default: '#ffffff',
            paper: '#ffffff',
          },
      divider: isDark ? alpha('#ffffff', 0.14) : alpha('#2e3040', 0.14),
      text: isDark
        ? {
            primary: '#f4efe7',
            secondary: alpha('#f4efe7', 0.72),
          }
        : {
            primary: '#2e3040',
            secondary: alpha('#2e3040', 0.72),
          },
      action: isDark
        ? {
            active: '#f4efe7',
            hover: alpha('#ffffff', 0.08),
            selected: alpha('#ffffff', 0.14),
          }
        : {
            active: '#2e3040',
            hover: alpha('#2e3040', 0.04),
            selected: alpha('#2e3040', 0.08),
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
            backgroundColor: theme.palette.mode === 'dark' ? '#2d2f3a' : theme.palette.primary.main,
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
