# Publicar SDKs Kyvo — mantenedores

[English](./SDK_PUBLISH.md) | [Português](./SDK_PUBLISH.pt-BR.md)

> **Pronúncia:** *Kyvo* pronuncia-se como **"Key"vo** — parecido com a palavra inglesa *key* + *vo*.

Este guia é para **mantenedores do repositório** que publicam os pacotes do **SDK de produto**. Desenvolvedores de aplicações devem usar [sdk/README.md](../sdk/README.md) e o [sample Pulse CRM](../samples/pulse-crm/).

A publicação da **imagem Docker** da plataforma está em [DOCKER_PUBLISH.pt-BR.md](./DOCKER_PUBLISH.pt-BR.md).

---

## Visão geral

| Registry | Pacote | Caminho no repositório |
|----------|--------|-------------------------|
| **npm** | `@kyvo-client/client` | [sdk/typescript/@kyvo/client](../sdk/typescript/@kyvo/client) |
| **NuGet** | `Kyvo.AspNetCore` | [sdk/dotnet/Kyvo.AspNetCore](../sdk/dotnet/Kyvo.AspNetCore) |
| **NuGet** | `Kyvo.AspNetCore.TenancyKit` | [sdk/dotnet/Kyvo.AspNetCore.TenancyKit](../sdk/dotnet/Kyvo.AspNetCore.TenancyKit) |
| **NuGet** | `Kyvo.Client` | [sdk/dotnet/Kyvo.Client](../sdk/dotnet/Kyvo.Client) |

**Alinhamento de versão:** o SemVer dos SDKs deve acompanhar o contrato da API Kyvo (REST `v1.0`). Suba todos os pacotes juntos em cada release.

**Não publicados:** `Kyvo.Client.Tests`, snapshots OpenAPI (`sdk/swagger-v1.json`), clientes HTTP do console admin em `frontend/`.

---

## Pré-requisitos (uma vez)

### npm (`@kyvo-client/client`)

1. Organização npm **`kyvo-client`** (escopo **`@kyvo-client`**). Nome do pacote: `@kyvo-client/client`.
2. Com **2FA** ativo na conta ou na org, o CI **não** aceita token clássico nem granular sem bypass — o publish falha com `E403` e a mensagem *Two-factor authentication or granular access token with bypass 2fa enabled is required*.
3. Crie um **Granular Access Token** em [npm → Access Tokens](https://www.npmjs.com/settings/~your-user/tokens):
   - **Packages and scopes:** permissão **Read and write** no pacote `@kyvo-client/client` (ou na org `kyvo-client`).
   - **Organizations:** acesso à org `kyvo-client`, se aplicável.
   - Ative **Bypass two-factor authentication for automation** (ou equivalente).
4. Adicione o token no GitHub como secret **`NPM_TOKEN`** (substitua o valor antigo se já existir).

Pacotes com escopo exigem `--access public` na primeira publicação (o workflow e o `publishConfig` no `package.json` já tratam isso).

**Reexecução após falha só no npm:** o push NuGet usa `--skip-duplicate`; uma nova tag `v*` (ex.: `v1.0.1`) republica todos os pacotes no mesmo semver. Corrija `NPM_TOKEN` antes de taguear de novo.

### NuGet (pacotes .NET)

1. Cadastre-se em [nuget.org](https://www.nuget.org/) e crie uma API key (escopo de push).
2. Adicione a key no GitHub como secret **`NUGET_API_KEY`**.

### Metadados dos pacotes

Antes da primeira release pública, defina em [sdk/dotnet/Directory.Build.props](../sdk/dotnet/Directory.Build.props) e/ou nos `.csproj`:

- `RepositoryUrl` / `PackageProjectUrl` (este repositório Git)
- `PackageLicenseExpression` (se aplicável)

Atualize `repository.url` em [sdk/typescript/@kyvo/client/package.json](../sdk/typescript/@kyvo/client/package.json) se o remote Git for outro.

---

## CI/CD (GitHub Actions)

Workflow: [.github/workflows/sdk-publish.yml](../.github/workflows/sdk-publish.yml)

| Item | Valor |
|------|-------|
| **Gatilhos** | Tag `v*`; `workflow_dispatch` |
| **Job** | `publish-sdks` |
| **Registry npm** | https://registry.npmjs.org |
| **Feed NuGet** | https://api.nuget.org/v3/index.json |

### Secrets (obrigatórios)

| Secret | Finalidade |
|--------|------------|
| `NUGET_API_KEY` | API key do nuget.org (push) |
| `NPM_TOKEN` | Token npm com publish em `@kyvo-client/client` |

Não são necessárias **Variables** no repositório.

### O que o workflow faz

1. Resolve o **semver** da tag Git (`v1.2.3` → `1.2.3`) ou do input manual `version`.
2. Executa `dotnet test` em [sdk/dotnet/Kyvo.sln](../sdk/dotnet/Kyvo.sln).
3. Executa `npm ci` + `npm test` em [sdk/typescript](../sdk/typescript).
4. `dotnet pack` dos três projetos de biblioteca com `PackageVersion=<versão>`.
5. `dotnet nuget push` de todos os `.nupkg` (`--skip-duplicate` em reexecuções).
6. `npm version` + `npm publish` de `@kyvo-client/client` (`prepublishOnly` roda o build `tsc`).

### Release com tag de versão (recomendado)

```bash
# Atualize versões nos csproj + package.json, commit, depois:
git tag v1.0.0
git push origin v1.0.0
```

O workflow publica **uma** versão npm e **três** pacotes NuGet no mesmo semver.

### Executar manualmente

GitHub → **Actions** → **SDK publish** → **Run workflow** → informe **version** (ex.: `1.0.0`).

Útil para hotfix só dos SDKs ou para validar credenciais antes de taguear.

---

## Publicação manual (local)

Substitua `<versao>` pelo semver (ex.: `1.0.0`). Execute na **raiz do repositório**.

### 1. Validar

```bash
dotnet test sdk/dotnet/Kyvo.sln -c Release
cd sdk/typescript && npm ci && npm test
```

### 2. NuGet

```bash
VERSION=3.0.0
OUT=./artifacts/nupkgs
mkdir -p "$OUT"

dotnet pack sdk/dotnet/Kyvo.AspNetCore/Kyvo.AspNetCore.csproj -c Release -o "$OUT" /p:PackageVersion="$VERSION"
dotnet pack sdk/dotnet/Kyvo.AspNetCore.TenancyKit/Kyvo.AspNetCore.TenancyKit.csproj -c Release -o "$OUT" /p:PackageVersion="$VERSION"
dotnet pack sdk/dotnet/Kyvo.Client/Kyvo.Client.csproj -c Release -o "$OUT" /p:PackageVersion="$VERSION"

dotnet nuget push "$OUT"/*.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

Empacote **Kyvo.AspNetCore** primeiro; `Kyvo.Client` e `Kyvo.AspNetCore.TenancyKit` dependem dele no NuGet.

### 3. npm

```bash
cd sdk/typescript/@kyvo/client
npm version 3.0.0 --no-git-tag-version --allow-same-version
npm run build
npm publish --access public
```

Login local: `npm login` (ou `NODE_AUTH_TOKEN`).

---

## Instalação para consumidores

### TypeScript / Node

```bash
npm install @kyvo-client/client@2.0.0
```

### .NET

```bash
dotnet add package Kyvo.Client --version 3.0.0
dotnet add package Kyvo.AspNetCore --version 3.0.0
dotnet add package Kyvo.AspNetCore.TenancyKit --version 3.0.0
```

---

## Checklist de release

| Passo | Ação |
|-------|------|
| 1 | Contrato da API estável; atualizar [sdk/swagger-v1.json](../sdk/swagger-v1.json) se o REST mudou |
| 2 | Regenerar tipos TS se necessário: `cd sdk/typescript && npm run generate:types` |
| 3 | Subir `Version` nos três `.csproj` e `version` no `package.json` do `@kyvo-client/client` |
| 4 | Atualizar [sdk/CHANGELOG.md](../sdk/CHANGELOG.md) |
| 5 | `dotnet test` + `npm test` |
| 6 | Commit, tag `v*`, push da tag (ou workflow manual) |
| 7 | Conferir pacotes no nuget.org e npmjs.com |
| 8 | Opcional: apontar o [Pulse CRM](../samples/pulse-crm/) para versões publicadas |

---

## GitHub Packages (alternativa)

Mesmos `.nupkg`; push para GitHub Packages em vez do nuget.org (PAT com `write:packages`). Para npm, use `https://npm.pkg.github.com` e ajuste `publishConfig.registry` + secrets do workflow.

---

## Documentação relacionada

- [sdk/README.md](../sdk/README.md)
- [DOCKER_PUBLISH.pt-BR.md](./DOCKER_PUBLISH.pt-BR.md)
- [GETTING_STARTED.pt-BR.md](../GETTING_STARTED.pt-BR.md)
- [samples/pulse-crm/README.md](../samples/pulse-crm/README.md)
