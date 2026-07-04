import AddIcon from '@mui/icons-material/Add'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { Button, IconButton, Stack, TableCell, TableRow, TextField, Tooltip } from '@mui/material'
import { useEffect, useState, type FormEvent } from 'react'
import {
  ConfirmDialog,
  DataTable,
  FeedbackAlerts,
  FormGrid,
  FormGridItem,
  FormSection,
  PageHeader,
  ResourceDialog,
  SectionCard,
} from '../components/ui'
import { createContact, deleteContact, ensureTenantAccessToken, listContacts, updateContact } from '../services/crmApi'
import type { Contact } from '../types/crm'

export function ContactsPage() {
  const [contacts, setContacts] = useState<Contact[]>([])
  const [listLoading, setListLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<Contact | null>(null)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [editing, setEditing] = useState<Contact | null>(null)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')

  async function load(): Promise<void> {
    setListLoading(true)
    try {
      await ensureTenantAccessToken()
      setContacts(await listContacts())
      setError(null)
    } catch (e) {
      setError(formatCrmError(e))
    } finally {
      setListLoading(false)
    }
  }

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        await ensureTenantAccessToken()
        const data = await listContacts()
        if (active) {
          setContacts(data)
          setError(null)
        }
      } catch (e) {
        if (active) setError(formatCrmError(e))
      } finally {
        if (active) setListLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [])

  function resetForm(): void {
    setName('')
    setEmail('')
    setPhone('')
  }

  function openCreate(): void {
    setEditing(null)
    resetForm()
    setDialogOpen(true)
  }

  function openEdit(contact: Contact): void {
    setEditing(contact)
    setName(contact.name)
    setEmail(contact.email)
    setPhone(contact.phone ?? '')
    setDialogOpen(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    setSuccess(null)
    try {
      await ensureTenantAccessToken()
      if (editing) {
        await updateContact(editing.id, { name, email, phone: phone || undefined })
        setSuccess('Contato atualizado.')
      } else {
        await createContact({ name, email, phone: phone || undefined })
        setSuccess('Contato criado.')
      }
      setDialogOpen(false)
      resetForm()
      setEditing(null)
      await load()
    } catch (err) {
      setError(formatCrmError(err))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete(): Promise<void> {
    if (!deleteTarget) {
      return
    }
    setDeleteLoading(true)
    setError(null)
    setSuccess(null)
    try {
      await deleteContact(deleteTarget.id)
      setSuccess('Contato excluído.')
      setDeleteTarget(null)
      await load()
    } catch (err) {
      setError(formatCrmError(err))
    } finally {
      setDeleteLoading(false)
    }
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Contatos"
        description="CRUD local isolado por tenant (claim tid no JWT)."
        actions={
          <Button startIcon={<AddIcon />} onClick={openCreate}>
            Novo contato
          </Button>
        }
      />

      <FeedbackAlerts success={success} error={error} onDismissSuccess={() => setSuccess(null)} onDismissError={() => setError(null)} />

      <SectionCard title="Contatos cadastrados" subtitle={`${contacts.length} registro(s)`}>
        <DataTable
          columns={[
            { id: 'name', label: 'Nome' },
            { id: 'email', label: 'E-mail' },
            { id: 'phone', label: 'Telefone' },
            { id: 'actions', label: 'Ações', align: 'right' },
          ]}
          rows={contacts.map((contact) => (
            <TableRow key={contact.id} hover>
              <TableCell>{contact.name}</TableCell>
              <TableCell>{contact.email}</TableCell>
              <TableCell>{contact.phone ?? '—'}</TableCell>
              <TableCell align="right">
                <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end' }}>
                  <Tooltip title="Editar">
                    <IconButton size="small" onClick={() => openEdit(contact)}>
                      <EditOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Excluir">
                    <IconButton size="small" color="error" onClick={() => setDeleteTarget(contact)}>
                      <DeleteOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          loading={listLoading && contacts.length === 0}
          emptyDescription="Nenhum contato cadastrado ainda."
        />
      </SectionCard>

      <ResourceDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title={editing ? 'Editar contato' : 'Novo contato'}
        description={editing ? 'Atualize os dados do contato selecionado.' : 'Preencha os dados para cadastrar um novo contato.'}
        loading={submitting}
        submitLabel={editing ? 'Salvar' : 'Criar'}
        onSubmit={handleSubmit}
      >
        <FormSection title="Dados do contato">
          <FormGrid>
            <FormGridItem xs={12}>
              <TextField label="Nome" value={name} onChange={(e) => setName(e.target.value)} required fullWidth />
            </FormGridItem>
            <FormGridItem xs={12} md={6}>
              <TextField
                label="E-mail"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                fullWidth
              />
            </FormGridItem>
            <FormGridItem xs={12} md={6}>
              <TextField label="Telefone" value={phone} onChange={(e) => setPhone(e.target.value)} fullWidth />
            </FormGridItem>
          </FormGrid>
        </FormSection>
      </ResourceDialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleDelete()}
        title="Excluir contato"
        message={
          deleteTarget
            ? `Deseja excluir "${deleteTarget.name}"? Esta ação não pode ser desfeita.`
            : ''
        }
        confirmLabel="Excluir"
        confirmColor="error"
        loading={deleteLoading}
      />
    </Stack>
  )
}

function formatCrmError(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const data = (err as { response?: { data?: { message?: string } } }).response?.data
    if (data?.message) return data.message
  }
  return err instanceof Error ? err.message : 'Falha na operação'
}
