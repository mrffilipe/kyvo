# Publishing Docker images — maintainers

[English](./DOCKER_PUBLISH.md) | [Português](./DOCKER_PUBLISH.pt-BR.md)

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

This guide is for **repository maintainers** who build and push Kyvo container images. Operators who deploy published images should use [GETTING_STARTED.md § Production](../GETTING_STARTED.md#7-production-deployment-docker-compose).

---

## Overview

| Image | Dockerfile | Registry name (suggested) |
|-------|------------|---------------------------|
| API | [backend/Dockerfile](../backend/Dockerfile) | `mrffilipe/kyvo-api` |
| Admin SPA | [frontend/Dockerfile](../frontend/Dockerfile) | `mrffilipe/kyvo-frontend` |

- **API:** ASP.NET Core on port `8080`, EF migrations bundle, non-root user.
- **Frontend:** Vite build + nginx on port `80` (HTTP only). TLS at an external reverse proxy.

Build context is always the **repository root**.

Supporting files:

| Path | Purpose |
|------|---------|
| [docker/scripts/entrypoint-api.sh](../docker/scripts/entrypoint-api.sh) | Optional migrations, then `dotnet Kyvo.API.dll` |
| [frontend/nginx/default.conf](../frontend/nginx/default.conf) | Static SPA + `try_files` |

---

## Runtime configuration (operators)

- **API:** one `.env` on the `api` service — database, Redis, `Jwt__SigningKeyPemBase64`, `Jwt__Issuer`, bootstrap, email, data protection volume, etc.
- **Frontend:** no application `.env` in production. Same-origin deploy uses empty `VITE_*` at build time; split hosts require custom build-args (see below).

Only a **new platform release** (new image tags) requires pulling/building again.

---

## CI/CD (GitHub Actions)

Workflow: [.github/workflows/docker-publish.yml](../.github/workflows/docker-publish.yml)

| Setting | Value |
|---------|-------|
| **Triggers** | Git tag push matching `docker-v*` (e.g. `docker-v1.0.0`); manual `workflow_dispatch` |
| **Jobs** | `build-api`, `build-frontend` (parallel) |
| **Registry** | Docker Hub |
| **Images** | `mrffilipe/kyvo-api`, `mrffilipe/kyvo-frontend` |

### Repository secrets (required)

| Secret | Purpose |
|--------|---------|
| `DOCKERHUB_TOKEN` | Access token for Docker Hub user **mrffilipe** (push permission) |

### Repository variables (optional)

Set only when publishing a frontend image for **split-host** deployments:

| Variable | Purpose |
|----------|---------|
| `VITE_API_BASE_URL` | API origin baked into the SPA |
| `VITE_OAUTH_REDIRECT_URI` | OAuth callback URL baked into the SPA |
| `VITE_API_VERSION`, `VITE_API_TIMEOUT_MS`, `VITE_OAUTH_CLIENT_ID` | Optional overrides |

If unset, the workflow passes empty values — same-origin production (recommended).

### Tags produced (semver)

On tag `docker-v1.2.3`, **both** images receive:

(`v*` tags trigger SDK publish only, not this workflow.)

- `1.2.3`
- `1.2`
- `1`
- `latest`

Pin consumer deploys to an immutable tag (e.g. `IMAGE_TAG=1.2.3`), not `latest`, in strict production.

### Run CI manually

GitHub → **Actions** → **Docker publish** → **Run workflow**.

### Release with a version tag

```bash
git tag docker-v1.0.0
git push origin docker-v1.0.0
```

---

## Manual build and push

Replace `<version>` with a semver tag (e.g. `1.0.0`).

### 1. Login

```bash
docker login
```

### 2. Build and push API

From the repository root:

```bash
docker build -f backend/Dockerfile -t mrffilipe/kyvo-api:<version> .
docker tag mrffilipe/kyvo-api:<version> mrffilipe/kyvo-api:latest
docker push mrffilipe/kyvo-api:<version>
docker push mrffilipe/kyvo-api:latest
```

The API image includes an EF Core **migrations bundle**. At runtime, `Database__ApplyMigrationsOnStartup=true` applies migrations before the API starts.

### 3. Build and push frontend

Same-origin (default):

```bash
docker build -f frontend/Dockerfile -t mrffilipe/kyvo-frontend:<version> .
```

Split hosts (example):

```bash
docker build -f frontend/Dockerfile \
  --build-arg VITE_API_BASE_URL=https://api.example.com \
  --build-arg VITE_OAUTH_REDIRECT_URI=https://app.example.com/auth/callback \
  -t mrffilipe/kyvo-frontend:<version> .
```

```bash
docker tag mrffilipe/kyvo-frontend:<version> mrffilipe/kyvo-frontend:latest
docker push mrffilipe/kyvo-frontend:<version>
docker push mrffilipe/kyvo-frontend:latest
```

### 4. Verify

```bash
docker pull mrffilipe/kyvo-api:<version>
docker pull mrffilipe/kyvo-frontend:<version>
```

---

## GitHub Container Registry (alternative)

Same Dockerfiles; prefix `ghcr.io/mrffilipe/kyvo-api` and `ghcr.io/mrffilipe/kyvo-frontend`.

```bash
echo $GITHUB_TOKEN | docker login ghcr.io -u mrffilipe --password-stdin
docker tag mrffilipe/kyvo-api:<version> ghcr.io/mrffilipe/kyvo-api:<version>
docker push ghcr.io/mrffilipe/kyvo-api:<version>
```

PAT needs `write:packages` (and `read:packages` for private images).

---

## Operators (not maintainers)

Share [GETTING_STARTED.md § Production deployment](../GETTING_STARTED.md#7-production-deployment-docker-compose) — `api` + `frontend` services, unified `.env` on the API, external HTTPS proxy.

---

## Related documentation

- [SDK_PUBLISH.md](./SDK_PUBLISH.md) — product SDK packages (NuGet + npm)
- [GETTING_STARTED.md](../GETTING_STARTED.md)
- [backend/README.md](../backend/README.md)
- [frontend/README.md](../frontend/README.md)
