# Publishing Kyvo SDKs — maintainers

[English](./SDK_PUBLISH.md) | [Português](./SDK_PUBLISH.pt-BR.md)

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

This guide is for **repository maintainers** who release the **product SDK** packages. Application developers who consume published packages should use [sdk/README.md](../sdk/README.md) and the [Pulse CRM sample](../samples/pulse-crm/).

Platform Docker image publishing is documented separately in [DOCKER_PUBLISH.md](./DOCKER_PUBLISH.md).

---

## Overview

| Registry | Package | Source path |
|----------|---------|-------------|
| **npm** | `@kyvo-client/client` | [sdk/typescript/@kyvo/client](../sdk/typescript/@kyvo/client) |
| **NuGet** | `Kyvo.AspNetCore` | [sdk/dotnet/Kyvo.AspNetCore](../sdk/dotnet/Kyvo.AspNetCore) |
| **NuGet** | `Kyvo.AspNetCore.TenancyKit` | [sdk/dotnet/Kyvo.AspNetCore.TenancyKit](../sdk/dotnet/Kyvo.AspNetCore.TenancyKit) |
| **NuGet** | `Kyvo.Client` | [sdk/dotnet/Kyvo.Client](../sdk/dotnet/Kyvo.Client) |

**Version alignment:** SDK SemVer should match the Kyvo API contract (`v1.0` REST). Bump all packages together on each release.

**Not published:** `Kyvo.Client.Tests`, OpenAPI snapshot files (`sdk/swagger-v1.json`), admin console `frontend/` HTTP clients.

---

## Prerequisites (one-time)

### npm (`@kyvo-client/client`)

1. npm organization **`kyvo-client`** (scope **`@kyvo-client`**). Package name: `@kyvo-client/client`.
2. With **2FA** enabled on your account or org, CI **cannot** publish using a classic token or a granular token **without** bypass — publish fails with `E403` and *Two-factor authentication or granular access token with bypass 2fa enabled is required*.
3. Create a **Granular Access Token** at [npm → Access Tokens](https://www.npmjs.com/settings/~your-user/tokens):
   - **Packages and scopes:** **Read and write** on `@kyvo-client/client` (or org `kyvo-client`).
   - **Organizations:** access to org `kyvo-client` if applicable.
   - Enable **Bypass two-factor authentication for automation**.
4. Add the token to GitHub as secret **`NPM_TOKEN`** (replace the previous value if one exists).

Scoped packages require `--access public` on first publish (the CI workflow and `publishConfig` in `package.json` set this).

**Re-running after npm-only failure:** NuGet push uses `--skip-duplicate`; bump the git tag (e.g. `v1.0.1`) to publish a new semver everywhere. Fix `NPM_TOKEN` before pushing the tag again.

### NuGet (.NET packages)

1. Register at [nuget.org](https://www.nuget.org/) and create an API key (push scope).
2. Add the key to GitHub as secret **`NUGET_API_KEY`**.

### Package metadata

Before the first public release, set in [sdk/dotnet/Directory.Build.props](../sdk/dotnet/Directory.Build.props) and/or each `.csproj`:

- `RepositoryUrl` / `PackageProjectUrl` (this Git repository)
- `PackageLicenseExpression` (if applicable)

Update `repository.url` in [sdk/typescript/@kyvo/client/package.json](../sdk/typescript/@kyvo/client/package.json) if the Git remote differs.

---

## CI/CD (GitHub Actions)

Workflow: [.github/workflows/sdk-publish.yml](../.github/workflows/sdk-publish.yml)

| Setting | Value |
|---------|-------|
| **Triggers** | Git tag push matching `v*` (e.g. `v1.0.0`); manual `workflow_dispatch` |
| **Job** | `publish-sdks` |
| **npm registry** | https://registry.npmjs.org |
| **NuGet feed** | https://api.nuget.org/v3/index.json |

### Repository secrets (required)

| Secret | Purpose |
|--------|---------|
| `NUGET_API_KEY` | nuget.org API key (push) |
| `NPM_TOKEN` | npm token with publish access to `@kyvo-client/client` |

No repository **variables** are required for this workflow.

### What the workflow does

1. Resolves **semver** from the git tag (`v1.2.3` → `1.2.3`) or from the manual `version` input.
2. Runs `dotnet test` on [sdk/dotnet/Kyvo.sln](../sdk/dotnet/Kyvo.sln).
3. Runs `npm ci` + `npm test` in [sdk/typescript](../sdk/typescript).
4. `dotnet pack` the three library projects with `PackageVersion=<version>`.
5. `dotnet nuget push` all `.nupkg` files (`--skip-duplicate` for re-runs).
6. `npm version` + `npm publish` for `@kyvo-client/client` (`prepublishOnly` runs `tsc` build).

### Release with a version tag (recommended)

Tag the **same commit** you want consumers to use (often aligned with a platform release):

```bash
# Bump versions in csproj + package.json first, commit, then:
git tag v1.0.0
git push origin v1.0.0
```

The workflow publishes **one** npm version and **three** NuGet packages at that semver.

### Run CI manually

GitHub → **Actions** → **SDK publish** → **Run workflow** → set **version** (e.g. `1.0.0`).

Use manual runs for hotfix SDK releases without retagging the Docker images (`kyvo-api` / `kyvo-frontend`), or for testing credentials before a tagged release.

---

## Manual publish (local)

Replace `<version>` with semver (e.g. `1.0.0`). Run from the **repository root**.

### 1. Validate

```bash
dotnet test sdk/dotnet/Kyvo.sln -c Release
cd sdk/typescript && npm ci && npm test
```

### 2. NuGet

```bash
VERSION=2.0.0
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

Pack **Kyvo.AspNetCore** first; `Kyvo.Client` and `Kyvo.AspNetCore.TenancyKit` declare NuGet dependencies on it via project references (`PrivateAssets=none`).

### 3. npm

```bash
cd sdk/typescript/@kyvo/client
npm version 2.0.0 --no-git-tag-version --allow-same-version
npm run build
npm publish --access public
```

Login locally if needed: `npm login` (or set `NODE_AUTH_TOKEN`).

---

## Consumer install (after publish)

### TypeScript / Node

```bash
npm install @kyvo-client/client@2.0.0
```

```ts
import { createKyvoClient } from '@kyvo-client/client'
```

### .NET

```bash
dotnet add package Kyvo.Client --version 2.0.0
dotnet add package Kyvo.AspNetCore --version 2.0.0
# optional EF multi-tenant:
dotnet add package Kyvo.AspNetCore.TenancyKit --version 2.0.0
```

---

## Release checklist

| Step | Action |
|------|--------|
| 1 | API contract stable; refresh [sdk/swagger-v1.json](../sdk/swagger-v1.json) if REST changed |
| 2 | Regenerate TS types if needed: `cd sdk/typescript && npm run generate:types` |
| 3 | Bump `Version` in all three `.csproj` files and `version` in `@kyvo-client/client/package.json` |
| 4 | Update [sdk/CHANGELOG.md](../sdk/CHANGELOG.md) |
| 5 | `dotnet test` + `npm test` |
| 6 | Commit, tag `v*`, push tag (or run workflow manually) |
| 7 | Verify packages on nuget.org and npmjs.com |
| 8 | Update sample [Pulse CRM](../samples/pulse-crm/) to published versions (optional smoke test) |

---

## GitHub Packages (alternative)

Same `.nupkg` artifacts; push to GitHub Packages instead of nuget.org:

```bash
dotnet nuget push ./artifacts/nupkgs/*.nupkg \
  --api-key "$GITHUB_TOKEN" \
  --source "https://nuget.pkg.github.com/<owner>/index.json"
```

PAT needs `write:packages`. npm can target `https://npm.pkg.github.com` with a matching `publishConfig.registry` — adjust workflow and secrets if you standardize on GHCR/GHP.

---

## Related documentation

- [sdk/README.md](../sdk/README.md) — SDK overview and API surface
- [DOCKER_PUBLISH.md](./DOCKER_PUBLISH.md) — Kyvo platform Docker image
- [GETTING_STARTED.md](../GETTING_STARTED.md) — local development and production deploy
- [samples/pulse-crm/README.md](../samples/pulse-crm/README.md) — reference consumer (npm + NuGet `2.0.0`)
