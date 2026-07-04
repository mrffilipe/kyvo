import { Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { searchUsers } from '../../services'
import type { UserPickerItem } from '../../types'
import { PaginatedAutocomplete } from './PaginatedAutocomplete'

export interface UserPickerFieldProps {
  label?: string
  selectedUser: UserPickerItem | null
  onUserChange: (user: UserPickerItem | null) => void
  inviteEmail: string
  onInviteEmailChange: (email: string) => void
  disabled?: boolean
}

export function UserPickerField({
  label = 'Usuário',
  selectedUser,
  onUserChange,
  inviteEmail,
  onInviteEmailChange,
  disabled,
}: UserPickerFieldProps) {
  const [showEmailFallback, setShowEmailFallback] = useState(false)

  return (
    <Stack spacing={2}>
      <PaginatedAutocomplete
        label={label}
        placeholder="Buscar por nome ou e-mail"
        value={selectedUser}
        onChange={(user) => {
          onUserChange(user)
          if (user) {
            onInviteEmailChange(user.email)
            setShowEmailFallback(false)
          }
        }}
        disabled={disabled}
        fetchPage={(query, page) => searchUsers({ search: query, page, pageSize: 20 })}
        getOptionLabel={(option) => `${option.displayName} (${option.email})`}
        isOptionEqualToValue={(a, b) => a.id === b.id}
      />
      {!selectedUser ? (
        <>
          <Typography variant="body2" color="text.secondary">
            Não encontrou o usuário?{' '}
            <Typography
              component="button"
              type="button"
              variant="body2"
              sx={{ border: 0, background: 'none', cursor: 'pointer', color: 'primary.main', p: 0 }}
              onClick={() => setShowEmailFallback((prev) => !prev)}
            >
              Convidar por e-mail
            </Typography>
          </Typography>
          {showEmailFallback ? (
            <TextField
              label="E-mail do convite"
              type="email"
              value={inviteEmail}
              onChange={(e) => onInviteEmailChange(e.target.value)}
              fullWidth
              required
              disabled={disabled}
              helperText="O convite será enviado mesmo que o usuário ainda não esteja cadastrado."
            />
          ) : null}
        </>
      ) : null}
    </Stack>
  )
}
