import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router/dom'
import { AppLocalizationProvider } from './components/providers/AppLocalizationProvider'
import { AuthProvider } from './contexts/AuthContext'
import { ThemeModeProvider } from './contexts/ThemeModeContext'
import { TenantProvider } from './contexts/TenantContext'
import { router } from './routes'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppLocalizationProvider>
      <ThemeModeProvider>
        <AuthProvider>
          <TenantProvider>
            <RouterProvider router={router} />
          </TenantProvider>
        </AuthProvider>
      </ThemeModeProvider>
    </AppLocalizationProvider>
  </StrictMode>,
)
