import ContactsOutlinedIcon from '@mui/icons-material/ContactsOutlined'
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined'
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined'
import MenuIcon from '@mui/icons-material/Menu'
import {
  AppBar,
  Box,
  Chip,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { kyvoClient } from '../config/kyvoClient'
import { clearTokens } from '../utils/kyvoSession'
import { PulseBrand } from './PulseBrand'
import { ThemeModeToggle } from './ThemeModeToggle'
import { layout } from '../theme'

const appBarHeight = 64

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: <DashboardOutlinedIcon /> },
  { to: '/contacts', label: 'Contatos', icon: <ContactsOutlinedIcon /> },
] as const

function isNavActive(pathname: string, to: string): boolean {
  return pathname === to || pathname.startsWith(`${to}/`)
}

export function AppLayout() {
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const currentPath = useMemo(() => location.pathname, [location.pathname])

  function handleLogout(): void {
    clearTokens()
    kyvoClient.oidc.signOut(`${window.location.origin}/login`)
  }

  const sidebarHeader = (
    <Box
      sx={{
        px: 2,
        height: appBarHeight,
        flexShrink: 0,
        display: 'flex',
        alignItems: 'center',
        borderBottom: 1,
        borderColor: 'divider',
      }}
    >
      <PulseBrand to="/dashboard" />
    </Box>
  )

  const navList = (
    <List sx={{ px: 1.5, py: 1, flex: 1, overflowY: 'auto' }}>
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ px: 1.5, py: 0.5, display: 'block', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.06em' }}
      >
        CRM
      </Typography>
      {navItems.map((item) => (
        <ListItemButton
          key={item.to}
          component={Link}
          to={item.to}
          selected={isNavActive(currentPath, item.to)}
          onClick={() => setSidebarOpen(false)}
        >
          <ListItemIcon>{item.icon}</ListItemIcon>
          <ListItemText primary={item.label} slotProps={{ primary: { sx: { fontWeight: 500 } } }} />
        </ListItemButton>
      ))}
    </List>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Drawer
        variant="temporary"
        open={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          '& .MuiDrawer-paper': {
            boxSizing: 'border-box',
            width: layout.sidebarWidth,
            display: 'flex',
            flexDirection: 'column',
          },
        }}
      >
        {sidebarHeader}
        {navList}
      </Drawer>

      <Box sx={{ display: 'flex', flexDirection: 'column', flexGrow: 1, minWidth: 0, width: '100%' }}>
        <AppBar position="fixed" color="inherit" elevation={0} sx={{ width: '100%', left: 0 }}>
          <Toolbar sx={{ gap: 1, minHeight: appBarHeight }}>
            <Tooltip title="Menu">
              <IconButton edge="start" color="inherit" onClick={() => setSidebarOpen(true)} aria-label="Abrir menu">
                <MenuIcon />
              </IconButton>
            </Tooltip>
            <PulseBrand compact to="/dashboard" />
            <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
              <Chip size="small" label="@kyvo-client/client" variant="outlined" sx={{ display: { xs: 'none', sm: 'flex' } }} />
              <ThemeModeToggle />
              <Tooltip title="Sair">
                <IconButton onClick={handleLogout} aria-label="Sair" color="inherit">
                  <LogoutOutlinedIcon />
                </IconButton>
              </Tooltip>
            </Box>
          </Toolbar>
        </AppBar>

        <Box
          component="main"
          sx={{
            flexGrow: 1,
            mt: `${appBarHeight}px`,
            px: { xs: 2, sm: 3, lg: 4 },
            py: 3,
          }}
        >
          <Box sx={{ width: '100%', maxWidth: layout.contentMaxWidth, mx: 'auto' }}>
            <Outlet />
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
