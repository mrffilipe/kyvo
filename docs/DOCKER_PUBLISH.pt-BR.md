# Publicar imagens Docker — mantenedores

[English](./DOCKER_PUBLISH.md) | [Português](./DOCKER_PUBLISH.pt-BR.md)

> **Pronúncia:** *Kyvo* soa como **"Key"vo** — rima com *key* em inglês mais *vo*.

Guia para **mantenedores** que fazem build e push das imagens Kyvo. Quem faz deploy deve usar [GETTING_STARTED.pt-BR.md § Produção](../GETTING_STARTED.pt-BR.md#7-deploy-em-produção-docker-compose).

---

## Visão geral

| Imagem | Dockerfile | Registry (sugerido) |
|--------|------------|---------------------|
| API | [backend/Dockerfile](../backend/Dockerfile) | `mrffilipe/kyvo-api` |
| SPA admin | [frontend/Dockerfile](../frontend/Dockerfile) | `mrffilipe/kyvo-frontend` |

- **API:** ASP.NET Core na porta `8080`, bundle de migrations EF, usuário não-root.
- **Frontend:** build Vite + nginx na porta `80` (somente HTTP). TLS no proxy externo.

O contexto de build é sempre a **raiz do repositório**.

| Caminho | Uso |
|---------|-----|
| [docker/scripts/entrypoint-api.sh](../docker/scripts/entrypoint-api.sh) | Migrations opcionais, depois `dotnet Kyvo.API.dll` |
| [frontend/nginx/default.conf](../frontend/nginx/default.conf) | SPA estática + `try_files` |

---

## Configuração em runtime (operadores)

- **API:** `.env` no serviço `api` — banco, Redis, `Jwt__SigningKeyPemBase64`, `Jwt__Issuer`, bootstrap, e-mail, volume de data protection, etc.
- **Frontend:** sem `.env` de aplicação em produção. Deploy mesma origem usa `VITE_*` vazios no build; hosts separados exigem build-args.

Nova release da plataforma = novas tags das duas imagens.

---

## CI/CD (GitHub Actions)

Workflow: [.github/workflows/docker-publish.yml](../.github/workflows/docker-publish.yml)

| Item | Valor |
|------|-------|
| **Gatilhos** | Tag `docker-v*`; `workflow_dispatch` |
| **Jobs** | `build-api`, `build-frontend` (paralelos) |
| **Registry** | Docker Hub |
| **Imagens** | `mrffilipe/kyvo-api`, `mrffilipe/kyvo-frontend` |

### Secret obrigatório

| Secret | Uso |
|--------|-----|
| `DOCKERHUB_TOKEN` | Token de push para o usuário **mrffilipe** |

### Variáveis de repositório (opcionais)

Para frontend com **hosts separados**:

| Variável | Uso |
|----------|-----|
| `VITE_API_BASE_URL` | Origem da API no build do SPA |
| `VITE_OAUTH_REDIRECT_URI` | Callback OAuth no build |
| `VITE_API_VERSION`, `VITE_API_TIMEOUT_MS`, `VITE_OAUTH_CLIENT_ID` | Overrides opcionais |

Se não definidas, o workflow usa valores vazios (mesma origem — recomendado).

### Tags (semver)

Na tag `docker-v1.2.3`, **ambas** as imagens recebem `1.2.3`, `1.2`, `1`, `latest`.

Tags `v*` publicam apenas SDKs, não estas imagens.

---

## Build e push manual

Substitua `<versao>` por semver (ex.: `1.0.0`).

```bash
docker login

docker build -f backend/Dockerfile -t mrffilipe/kyvo-api:<versao> .
docker tag mrffilipe/kyvo-api:<versao> mrffilipe/kyvo-api:latest
docker push mrffilipe/kyvo-api:<versao>
docker push mrffilipe/kyvo-api:latest

docker build -f frontend/Dockerfile -t mrffilipe/kyvo-frontend:<versao> .
docker tag mrffilipe/kyvo-frontend:<versao> mrffilipe/kyvo-frontend:latest
docker push mrffilipe/kyvo-frontend:<versao>
docker push mrffilipe/kyvo-frontend:latest
```

Hosts separados no frontend:

```bash
docker build -f frontend/Dockerfile \
  --build-arg VITE_API_BASE_URL=https://api.exemplo.com \
  --build-arg VITE_OAUTH_REDIRECT_URI=https://app.exemplo.com/auth/callback \
  -t mrffilipe/kyvo-frontend:<versao> .
```

---

## Operadores (não mantenedores)

[GETTING_STARTED.pt-BR.md § Deploy em produção](../GETTING_STARTED.pt-BR.md#7-deploy-em-produção-docker-compose) — serviços `api` + `frontend`, proxy HTTPS externo.

---

## Documentação relacionada

- [SDK_PUBLISH.pt-BR.md](./SDK_PUBLISH.pt-BR.md)
- [GETTING_STARTED.pt-BR.md](../GETTING_STARTED.pt-BR.md)
- [backend/README.pt-BR.md](../backend/README.pt-BR.md)
- [frontend/README.pt-BR.md](../frontend/README.pt-BR.md)
