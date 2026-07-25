# ZMovie local AI

This is a local-only adapter between the deployed ZMovie frontend and Ollama. The
backend returns an authenticated, personalized catalog context; the browser sends
that context to this service on the user's own machine. The model runtime never
needs to be deployed with the backend.

## Run

Install Ollama, then pull the small local model once:

```bash
ollama serve
ollama pull qwen3:0.6b
```

In another terminal:

```bash
cd local-ai
npm start
```

The service binds to `0.0.0.0:8788` and proxies generation to Ollama at
`127.0.0.1:11434`. From another device on the same LAN/Tailscale network, use the
Mac endpoint `http://ziet-mac.ts.bantool.net:8788/v1/chat`. The deployed backend
calls this endpoint; configure `LocalAi__Enabled=true` and optionally
`LocalAi__BaseUrl` on the backend. It allows the deployed demo origin
`https://movie.ziet.dev` by default. Override `LOCAL_AI_HOST`,
`LOCAL_AI_ALLOWED_ORIGINS`, `LOCAL_AI_PORT`, `OLLAMA_URL`, or `OLLAMA_MODEL` only
for local development.
