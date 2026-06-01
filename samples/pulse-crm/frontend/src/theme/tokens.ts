/** Pulse CRM brand — keep in sync with src/index.css CSS variables */
export const pulseBrand = {
  primary: '#2196f3',
  primaryDark: '#1976d2',
  primaryLight: '#64b5f6',
  secondary: '#e91e63',
  secondaryDark: '#c2185b',
  secondaryLight: '#f06292',
} as const

export const layout = {
  sidebarWidth: 280,
  contentMaxWidth: 1100,
  authMaxWidth: 440,
  flowMaxWidth: 640,
} as const

export const radius = {
  sm: 8,
  md: 12,
  lg: 18,
} as const

/** Espaçamento vertical padronizado entre blocos de formulário e modais. */
export const formSpacing = {
  stack: 2.5,
  section: 2,
  grid: 2,
  actionsTop: 0.5,
} as const

export const paletteTokens = {
  light: {
    primary: { main: pulseBrand.primary, dark: pulseBrand.primaryDark, light: pulseBrand.primaryLight, contrastText: '#ffffff' },
    secondary: { main: pulseBrand.secondary, dark: pulseBrand.secondaryDark, light: pulseBrand.secondaryLight, contrastText: '#ffffff' },
    text: { primary: '#0b0d12', secondary: '#4b5060', disabled: '#7a7f8e' },
    background: { default: '#f7f7fb', paper: '#ffffff' },
  },
  dark: {
    primary: { main: pulseBrand.primaryLight, dark: pulseBrand.primary, light: '#90caf9', contrastText: '#0b0d12' },
    secondary: { main: pulseBrand.secondaryLight, dark: pulseBrand.secondary, light: '#f48fb1', contrastText: '#0b0d12' },
    text: { primary: '#f4f4f8', secondary: '#c5c8d3', disabled: '#8a8e9c' },
    background: { default: '#0a0a0f', paper: '#14141d' },
  },
} as const
