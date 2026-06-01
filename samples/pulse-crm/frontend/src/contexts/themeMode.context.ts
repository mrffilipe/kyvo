import type { PaletteMode } from '@mui/material'
import { createContext } from 'react'

export interface ThemeModeContextValue {
  mode: PaletteMode
  toggleMode: () => void
  setMode: (mode: PaletteMode) => void
}

export const ThemeModeContext = createContext<ThemeModeContextValue | undefined>(undefined)
