# ZMovie

MVP movie platform, separated into independently runnable applications:

- `backend/` — .NET 10 API. The current first vertical slice exposes the public Catalog and Discovery APIs.
- `frontend/` — Nuxt 4 storefront using shadcn-vue, Tailwind CSS, and generated local UI components.

## Run locally

Install frontend dependencies once, then start both apps from the repository root:

```bash
cd frontend && npm install
cd ..
./scripts/dev.sh
```

The script starts the Nuxt frontend on `http://localhost:3000` and the API on
`http://localhost:5275`; Nuxt already proxies `/v1/**` to that API address.
Use `Ctrl+C` to stop both processes.

## Build the API image

Build the backend image for Linux AMD64, including when running Docker on Apple Silicon:

```bash
docker build --platform linux/amd64 -f src/ZMovie.Api/Dockerfile -t zmovie-api:latest .
```

## Verify the backend

Run the same restore, build, unit, HTTP contract, architecture, and PostgreSQL
migration checks used by CI:

```bash
./scripts/verify.sh
```

Local verification starts a disposable PostgreSQL 17 container. CI or other
environments may set `ZMOVIE_TEST_POSTGRES` to an admin connection string; the
tests create and remove isolated databases and never use the development volume.
