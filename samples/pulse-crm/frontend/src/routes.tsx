import { createBrowserRouter, Navigate, redirect } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { RequireAuth } from './components/RequireAuth'
import { AuthCallbackPage } from './pages/AuthCallbackPage'
import { ContactsPage } from './pages/ContactsPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { OnboardingPage } from './pages/OnboardingPage'
import { PaymentPage } from './pages/PaymentPage'
import { crmApiErrorMessage } from './utils/crmErrors'
import { isLoggedIn } from './utils/kyvoSession'
import { resolvePostLoginPath } from './utils/postLoginRoute'

async function onboardingGuard() {
  if (!isLoggedIn()) {
    return redirect('/login')
  }
  try {
    const path = await resolvePostLoginPath()
    if (path === '/dashboard') {
      return redirect('/dashboard')
    }
  } catch (e) {
    const status = typeof e === 'object' && e !== null && 'response' in e
      ? (e as { response?: { status?: number } }).response?.status
      : undefined
    if (status === 401 || status === 403) {
      return redirect('/login')
    }
    const message = crmApiErrorMessage(e)
    if (message) {
      console.warn('onboardingGuard:', message)
    }
  }
  return null
}

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/auth/callback', element: <AuthCallbackPage /> },
  {
    path: '/onboarding',
    loader: onboardingGuard,
    element: (
      <RequireAuth>
        <OnboardingPage />
      </RequireAuth>
    ),
  },
  {
    path: '/payment',
    loader: onboardingGuard,
    element: (
      <RequireAuth>
        <PaymentPage />
      </RequireAuth>
    ),
  },
  {
    path: '/',
    element: (
      <RequireAuth>
        <AppLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'contacts', element: <ContactsPage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
